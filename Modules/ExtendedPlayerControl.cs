//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SocialPlatforms;
using static BanMod.HostAfkManager;
using static BanMod.Utils;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;
using static UnityEngine.GraphicsBuffer;

namespace BanMod;

static class ExtendedPlayerControl
{
    public static byte GetPlayerIdFromClientId(int clientId)
    {
        if (clientId < 0)
            return byte.MaxValue;

        foreach (var c in AmongUsClient.Instance.allClients)
        {
            if (c.Id == clientId)
            {
                if (c.Character != null)
                    return c.Character.PlayerId;
            }
        }

        return byte.MaxValue;
    }

    public static ClientData GetClient(this PlayerControl player)
    {
        if (player == null)
            return null;

        foreach (var client in AmongUsClient.Instance.allClients)
        {
            if (client?.Character != null && client.Character.PlayerId == player.PlayerId)
                return client;
        }

        return null;
    }

    public static int GetClientId(this PlayerControl player)
    {
        if (player == null)
            return -1;

        var client = player.GetClient();
        if (client != null)
            return client.Id;

        try
        {
            var clientBuffer = new Il2CppSystem.Collections.Generic.List<ClientData>();
            AmongUsClient.Instance.GetAllClients(clientBuffer);

            foreach (var c in clientBuffer)
            {
                if (c != null && c.Character != null && c.Character.PlayerId == player.PlayerId)
                    return c.Id;
            }
        }
        catch (System.Exception ex)
        {
            BMLogger.LogError($"Errore nel fallback GetClientId (buffer): {ex.Message}");
        }

        return -1;
    }

    public static string GetFriendCode(this PlayerControl player)
    {
        if (player == null)
            return null;

        var client = player.GetClient();
        return client?.FriendCode;
    }

    public static Vector2 Pos(this PlayerControl pc)
    {
        return new(pc.transform.position.x, pc.transform.position.y);
    }

    public static bool IsAlive(this PlayerControl target)
    {
        if (GameStates.isLobby && !GameStates.InGame)
            return true;

        if (target == null)
            return false;

        return !BanMod.PlayerStates.TryGetValue(target.PlayerId, out var playerState) || !playerState.IsDead;
    }

    public static bool IsDead(this PlayerControl target)
    {
        return BanMod.PlayerStates.TryGetValue(target.PlayerId, out var playerState) && playerState.IsDead;
    }
    public static string GetRealName(this PlayerControl player, bool useClientData = false)
    {
        if (player == null)
            return "Unknown";

        var client = player.GetClient();

        if (useClientData && client != null)
        {
            return client.PlayerName ?? "Unknown";
        }

        return player.name ?? player.Data?.PlayerName ?? "Unknown";
    }
    public static void RpcTeleport(this PlayerControl player, Vector2 position, bool isRandomSpawn = false, bool sendInfoInLogs = true)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var netTransform = player.NetTransform;

        if (AmongUsClient.Instance.AmHost)
        {
            netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + 328));
            netTransform.SetDirtyBit(uint.MaxValue);
        }

        var sendOption = SendOption.Reliable;

        ushort newSid = (ushort)(netTransform.lastSequenceId + 8);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, sendOption);
        NetHelpers.WriteVector2(position, messageWriter);
        messageWriter.Write(newSid);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class FixedUpdateUnifiedPatch
{
    public static readonly Dictionary<byte, Vector2> lastPosition = new();
    public static readonly Dictionary<byte, Vector2> meetingPosition = new();
    public static readonly Dictionary<byte, bool> lastInVentState = new();

    public static Dictionary<string, string> CustomNames = new();
    public static readonly Dictionary<byte, string> RpcCustomNames = new();
    private static float taskNameRefreshTimer = 0f;
    private const float TaskNameRefreshInterval = 1f;

    private static void Prefix(PlayerControl __instance)
    {
        if (BanMod.IsBanModDisabled) return;

        if (__instance == null || __instance.Data == null || __instance.Data.Disconnected)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            Utils.MessageRetryHandler.TrySendPending();
            PreviousMatchPopupTracker.ValidateRecentProtection(__instance);

            Vector2 currentPos = __instance.GetTruePosition();
            lastPosition[__instance.PlayerId] = currentPos;

            AFKDetector.OnFixedUpdate(__instance, false);
            ProximityMonitor.OnFixedUpdate(__instance);

            bool isCurrentlyProtected = BanMod.ShieldedPlayers.Contains(__instance.PlayerId);
            bool isProtected = __instance.protectedByGuardianId != -1;

            if (isCurrentlyProtected && GameStates.IsInTask)
            {
                if (!isProtected && !__instance.Data.IsDead)
                {
                    PlayerControl protector = PlayerControl.LocalPlayer;

                    if (protector != null && protector.Data != null)
                    {
                        int colorId = protector.Data.DefaultOutfit.ColorId;

                        protector.RpcProtectPlayer(
                            __instance,
                            colorId
                        );
                    }
                }
            }

            if (BanMod.EveryRandomActive)
            {
                BanMod.everyRandomTimer += Time.deltaTime;

                if (BanMod.everyRandomTimer >= 1.5f)
                {
                    BanMod.everyRandomTimer = 0f;

                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player.Data == null || player.Data.Disconnected)
                            continue;

                        byte randomColor = (byte)UnityEngine.Random.Range(0, Palette.PlayerColors.Length);
                        player.RpcSetColor(randomColor);
                    }

                    RefreshAllNameDisplays();
                }
            }

            if (BanMod.RainbowTarget != null)
            {
                if (BanMod.RainbowTarget.Data == null || BanMod.RainbowTarget.Data.Disconnected)
                {
                    BanMod.RainbowTarget = null;
                }
                else
                {
                    BanMod.rainbowPlayerTimer += Time.deltaTime;

                    if (BanMod.rainbowPlayerTimer >= 1.5f)
                    {
                        BanMod.rainbowPlayerTimer = 0f;

                        byte currentColor = (byte)BanMod.RainbowTarget.Data.DefaultOutfit.ColorId;
                        byte nextColor;

                        do
                        {
                            nextColor = (byte)UnityEngine.Random.Range(0, Palette.PlayerColors.Length);
                        }
                        while (nextColor == currentColor);

                        BanMod.RainbowTarget.RpcSetColor(nextColor);

                        RefreshNameDisplay(BanMod.RainbowTarget);
                    }
                }
            }

            if (Options.EnableCamTaskDetector.GetBool() && Options.EnableCamDetector.GetBool())
            {
                CamTaskDetector.OnFixedUpdate(__instance, false);

                if (PlayerHasCompletedRequiredTasks(__instance))
                    CamDetector.OnFixedUpdate(__instance, false);
            }
            else if (Options.EnableCamTaskDetector.GetBool())
            {
                CamTaskDetector.OnFixedUpdate(__instance, false);
            }
            else if (Options.EnableCamDetector.GetBool())
            {
                CamDetector.OnFixedUpdate(__instance, false);
            }
        }
    }

    private static bool PlayerHasCompletedRequiredTasks(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Tasks == null || player.Data.Role == null)
            return false;

        int completedTasks = CamTaskDetector.CountCompletedTasks(player.Data.Tasks);

        int requiredTasksCrew = Options.MinTasksToUseCamCrew.GetInt();
        int requiredTasksImp = Options.MinTasksToUseCamImp.GetInt();

        bool isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
        bool isCrewmate = player.Data.Role.TeamType == RoleTeamTypes.Crewmate;

        if (isCrewmate)
        {
            return completedTasks >= requiredTasksCrew;
        }
        else if (isImpostor)
        {
            var aliveCrewmates = BanMod.AllAlivePlayerControls
                .Where(p => p.Data != null &&
                            p.Data.Role != null &&
                            p.Data.Role.TeamType == RoleTeamTypes.Crewmate &&
                            !p.Data.IsDead &&
                            !p.inVent)
                .ToList();

            bool allCrewmatesCompleted = aliveCrewmates.All(cm =>
                cm.Data.Tasks != null &&
                CamTaskDetector.CountCompletedTasks(cm.Data.Tasks) >= requiredTasksImp);

            return allCrewmatesCompleted;
        }

        return false;
    }

    private static void Postfix(PlayerControl __instance)
    {
        if (__instance == null || __instance.Data == null || __instance.Data.Disconnected)
            return;

        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
            return;

        if (PlayerControl.LocalPlayer.Data.Role == null)
            return;

        byte pid = __instance.PlayerId;

        if (BanMod.Taskremain &&
            AmongUsClient.Instance != null &&
            AmongUsClient.Instance.IsGameStarted)
        {
            taskNameRefreshTimer += Time.fixedDeltaTime;

            if (taskNameRefreshTimer >= TaskNameRefreshInterval)
            {
                taskNameRefreshTimer = 0f;
                RefreshTaskNameDisplaysOnly();
            }
        }

        lastInVentState.TryGetValue(pid, out bool wasInVent);

        if (Options.EnableAntiCheat.GetBool() &&
            Options.UseVentCheat.GetBool() &&
            __instance.inVent && !wasInVent)
        {
            if (!IsAuthorizedVentUser(__instance) && !__instance.Data.IsDead)
            {
                if (!AmongUsClient.Instance.AmHost)
                    return;

                string msg = $"{__instance.Data.PlayerName} {Translator.GetAuto("VentCheat1")}";

                try
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, msg);
                }
                catch
                {
                }
            }
        }

        lastInVentState[pid] = __instance.inVent;

        if (GameStates.isLobby && Options.ApplyDenyNameList.GetBool())
        {
            string denyFilePath = "./BAN_DATA/DENIED/DenyName.txt";

            if (!File.Exists(denyFilePath))
                File.WriteAllText(denyFilePath, "");

            string[] denyNames = File.ReadAllLines(denyFilePath)
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            if (__instance.Data != null)
            {
                string playerName = __instance.Data.PlayerName.Trim().ToLower();

                if (denyNames.Any(n => n == playerName))
                    AmongUsClient.Instance.KickPlayer(__instance.OwnerId, false);
            }
        }
    }

    private static bool IsAuthorizedVentUser(PlayerControl player)
    {
        return Utils.Impostor(player) ||
               Utils.Engineer(player) ||
               Utils.Phantom(player) ||
               Utils.Shapeshifter(player) ||
               Utils.ImpostorTeam(player);
    }

    public static void LoadCustomNames()
    {
        CustomNames.Clear();

        string path = "BAN_DATA/CUSTOM/NAME/CustomNames.txt";

        if (!File.Exists(path))
            return;

        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains(":"))
                continue;

            int separatorIndex = line.IndexOf(':');
            string fCode = line.Substring(0, separatorIndex).Trim();
            string cName = line.Substring(separatorIndex + 1).Trim();

            CustomNames[fCode] = cName;
        }

        RefreshAllNameDisplays();
    }

    public static void RefreshNameDisplay(PlayerControl target)
    {
        try
        {
            if (target == null || target.Data == null)
                return;

            UpdatePlayerNameDisplay(target);
        }
        catch
        {
        }
    }

    public static void RefreshAllNameDisplays()
    {
        try
        {
            if (PlayerControl.AllPlayerControls == null)
                return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected)
                    continue;

                UpdatePlayerNameDisplay(player);
            }
        }
        catch
        {
        }
    }

    private static void RefreshTaskNameDisplaysOnly()
    {
        try
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                return;

            if (PlayerControl.AllPlayerControls == null)
                return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected)
                    continue;

                UpdatePlayerNameDisplay(player);
            }
        }
        catch
        {
        }
    }

    private static void UpdatePlayerNameDisplay(PlayerControl target)
    {
        if (target == null || target.Data == null || target.cosmetics?.nameText == null)
            return;

        bool isShapeshifted = target.CurrentOutfitType == PlayerOutfitType.Shapeshifted;

        if (isShapeshifted)
            return;

        var local = PlayerControl.LocalPlayer;

        if (local == null || local.Data == null)
            return;

        bool isLocalDead = local.Data.IsDead;
        bool isLocalImpostor = Utils.ImpostorTeam(local);
        bool isTargetImpostor = Utils.ImpostorTeam(target);

        if (BanMod.ShowInfo)
        {
            ShowExtendedPlayerInfo(target);
            return;
        }

        if (BanMod.ShowNoName)
        {
            if (isLocalImpostor && !isLocalDead && isTargetImpostor)
            {
            }
            else
            {
                target.cosmetics.nameText.text = "";
                return;
            }
        }

        string finalName = "";
        string playerName = target.Data.PlayerName;
        bool hasCustomColor = false;

        string targetFriendCode = target.Data.FriendCode;

        bool isBanModDev =
            AllowedManager.IsModCreator(targetFriendCode);

        bool showBanModDevName =
            isBanModDev &&
            target.PlayerId != local.PlayerId;

        if (showBanModDevName)
        {
            playerName = "<color=#00D9FF>BanMod_Dev</color>";
            hasCustomColor = true;
        }
        else if (BanMod.UseCustomNames)
        {
            if (RpcCustomNames.TryGetValue(
                    target.PlayerId,
                    out string rpcCustomName) &&
                !string.IsNullOrWhiteSpace(rpcCustomName))
            {
                playerName = rpcCustomName;

                if (rpcCustomName.Contains("<color="))
                    hasCustomColor = true;
            }
            else if (CustomNames.TryGetValue(
                         target.Data.FriendCode,
                         out string customName))
            {
                playerName = customName;

                if (customName.Contains("<color="))
                    hasCustomColor = true;
            }
        }

        if (BanMod.ShowVipModTag && !showBanModDevName)
        {
            string friendCode = target.Data.FriendCode;

            if (Utils.IsModerator(friendCode))
                finalName += "<color=#ff5555>[M]</color> ";
            else if (Utils.IsVip(friendCode))
                finalName += "<color=#ffd700>[V]</color> ";
        }

        if (AmongUsClient.Instance.AmHost &&
            target == PlayerControl.LocalPlayer &&
            HostAfkManager.IsHostAfk)
        {
            finalName += HostAfkLogicPatch.GetAfkTimerString(target);
        }

        if (BanMod.ShowColorName && !(isLocalImpostor && !isLocalDead) && !hasCustomColor)
        {
            int colorId = target.Data.DefaultOutfit.ColorId;
            string hexColor = ColorUtility.ToHtmlStringRGB(ColorIdToColor(colorId));
            finalName += $"<color=#{hexColor}>{playerName}</color>";
        }
        else
        {
            finalName += playerName;
        }

        if (BanMod.level)
        {
            var playerInfo = GameData.Instance?.GetPlayerById(target.PlayerId);

            if (playerInfo != null)
                finalName += $" <color=#00ffff>({playerInfo.PlayerLevel + 1})</color>";
        }

        if (BanMod.Taskremain && AmongUsClient.Instance.IsGameStarted)
        {
            bool showTasks = isLocalDead
                ? !isTargetImpostor
                : (local.PlayerId == target.PlayerId && !isLocalImpostor);

            if (showTasks)
            {
                int total = 0;
                int done = 0;

                try
                {
                    if (target.Data.Tasks != null)
                    {
                        total = target.Data.Tasks.Count;

                        foreach (var t in target.Data.Tasks)
                        {
                            if (t != null && t.Complete)
                                done++;
                        }
                    }
                }
                catch
                {
                    total = 0;
                    done = 0;
                }

                finalName += $" <color=#00FFFF>({done}/{total})</color>";
            }
        }

        if (target.cosmetics.nameText.text != finalName)
            target.cosmetics.nameText.text = finalName;
    }
    
   
    private static void ShowExtendedPlayerInfo(PlayerControl player)
    {
        if (player == null || player.Data == null || player.cosmetics?.nameText == null)
            return;

        var text = player.cosmetics.nameText;

        int colorId = player.Data.DefaultOutfit.ColorId;
        Color pColor = ColorIdToColor(colorId);
        string hexColor = ColorUtility.ToHtmlStringRGB(pColor);

        string name = player.Data.PlayerName;

        var pInfo = GameData.Instance?.GetPlayerById(player.PlayerId);
        int level = pInfo != null ? (int)(pInfo.PlayerLevel + 1) : 0;

        string platform = Utils.GetPlatformName(player);
        string friendCode = player.Data.FriendCode;

        string final =
            $"<color=#{hexColor}>{name}</color> " +
            $"<color=#FFD700>({level})</color>\n" +
            $"<size=70%><color=#4FC3F7>{platform}</color></size>\n" +
            $"<size=70%><color=#AAAAAA>{friendCode}</color></size>";

        if (text.text != final)
            text.text = final;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
public static class PlayerControl_Start_RefreshNamePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            FixedUpdateUnifiedPatch.RefreshNameDisplay(__instance);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
public static class PlayerControl_SetName_RefreshNamePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            FixedUpdateUnifiedPatch.RefreshNameDisplay(__instance);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
public static class PlayerControl_SetColor_RefreshNamePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            FixedUpdateUnifiedPatch.RefreshNameDisplay(__instance);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class AmongUsClient_OnGameJoined_RefreshNamesPatch
{
    public static void Postfix()
    {
        try
        {
            FixedUpdateUnifiedPatch.RpcCustomNames.Clear();
            FixedUpdateUnifiedPatch.LoadCustomNames();
            FixedUpdateUnifiedPatch.RefreshAllNameDisplays();
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(VisualOptions), nameof(VisualOptions.SaveSettings))]
public static class VisualOptions_SaveSettings_RefreshNamesPatch
{
    public static void Postfix()
    {
        try
        {
            FixedUpdateUnifiedPatch.LoadCustomNames();
            FixedUpdateUnifiedPatch.RefreshAllNameDisplays();
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(CosmeticsLayer), nameof(CosmeticsLayer.GetColorBlindText))]
public static class GetColorBlindTextPatch
{
    public static bool Prefix(CosmeticsLayer __instance, ref string __result)
    {
        if (!BanMod.ShowColorName)
            return true;

        int colorId = __instance.ColorId;
        string colorName = Palette.GetColorName(colorId);
        colorName = char.ToUpper(colorName[0]) + colorName.Substring(1).ToLower();

        Color c = Palette.PlayerColors[colorId];
        string hex = ColorUtility.ToHtmlStringRGB(c);

        __result = $"<color=#{hex}>{colorName}</color>";

        return false;
    }
}
