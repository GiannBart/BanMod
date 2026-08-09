//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime;
using Il2CppSystem.Diagnostics;
using Il2CppSystem.IO.Ports;
using Il2CppSystem.Security.Cryptography;
using InnerNet;
using LibCpp2IL.Elf;
using MonoMod.Cil;
using MS.Internal.Xml.XPath;
using Rewired.Utils.Classes.Data;
using Rewired.Utils.Platforms.Windows;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ProBuilder;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.UIR;
using static BanMod.ChatCommands;
using static BanMod.ExtendedPlayerControl;
using static BanMod.Scientist;
using static BanMod.SpamManager;
using static BanMod.Translator;
using static BanMod.Utils;
using static FilterPopUp.FilterInfoUI;
using static Il2CppMono.Security.X509.X520;
using static Il2CppSystem.Net.Mail.SmtpClient;
using static InnerNet.InnerNetClient;
using static NetworkedPlayerInfo;
using static PlayerOutfit;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.UIElements.UIR.Allocator2D;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Math = System.Math;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using StackFrame = System.Diagnostics.StackFrame;
using StackTrace = System.Diagnostics.StackTrace;
using Timer = System.Timers.Timer;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace BanMod;

public static class Utils
{
    public static void RpcSpam(PlayerControl reporter, PlayerControl target)
    {

        for (int i = 0; i < 250; i++)
        {
            reporter.CmdReportDeadBody(target.Data);
        }
    }
    public static void DestroyMap()
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            Debug.LogWarning("[MapCheats] Solo l'host può rimuovere la mappa o la lobby.");
            return;
        }

        var lobby = LobbyBehaviour.Instance;
        if (lobby != null)
        {
            (lobby as InnerNetObject)?.Despawn();
            LobbyBehaviour.Instance = null; 
            BMLogger.Info("[MapCheats] LobbyBehaviour rimosso e singleton azzerato.");
        }

    }
    public static void MeetingNametags(MeetingHud meetingHud)
    {
        try
        {
            foreach (var playerState in meetingHud.playerStates)
            {
                var data = GameData.Instance.GetPlayerById(playerState.TargetPlayerId);

                if (data.IsNull() || data.Disconnected || data.Outfits[PlayerOutfitType.Default].IsNull()) continue;

                playerState.NameText.text = Utils.GetNameTag(data, data.DefaultOutfit.PlayerName);

                {
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
            }
        }
        catch { }
    }
    public static void ChatNametags(ChatBubble chatBubble)
    {
        try
        {
            chatBubble.NameText.text = Utils.GetNameTag(chatBubble.playerInfo, chatBubble.NameText.text, true);

            chatBubble.NameText.ForceMeshUpdate(true, true);
            chatBubble.Background.size = new Vector2(5.52f, 0.2f + chatBubble.NameText.GetNotDumbRenderedHeight() + chatBubble.TextArea.GetNotDumbRenderedHeight());
            chatBubble.MaskArea.size = chatBubble.Background.size - new Vector2(0f, 0.03f);

        }
        catch { }
    }
    public static string GetNameTag(NetworkedPlayerInfo playerInfo, string playerName, bool isChat = false)
    {
        var nameTag = playerName;

        if (playerInfo.Role.IsNull() || playerInfo.IsNull() || playerInfo.Disconnected ||
            playerInfo.Object.CurrentOutfit.IsNull()) return nameTag;

        var roleColor = ColorUtility.ToHtmlStringRGB(playerInfo.Role.TeamColor);

        if (isChat)
        {
            nameTag = $"<color=#{roleColor}>{nameTag} <size=70%>{GetRoleName(playerInfo)}</size></color>";
            return nameTag;
        }

        nameTag = $"<color=#{roleColor}><size=70%>{GetRoleName(playerInfo)}</size>\r\n{nameTag}</color>";

        return nameTag;
    }
    public static string GetRoleName(NetworkedPlayerInfo playerData)
    {
        var translatedRole = DestroyableSingleton<TranslationController>.Instance.GetString(playerData.Role.StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
        if (translatedRole != "STRMISS") return translatedRole;

        translatedRole = DestroyableSingleton<TranslationController>.Instance.GetString(GetBehaviourByTeamType(playerData.Role.TeamType).StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
        return translatedRole;
    }
    public static RoleBehaviour GetBehaviourByRoleType(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.ToArray().First(r => r.Role == roleType);
    }
    public static RoleBehaviour GetBehaviourByTeamType(RoleTeamTypes roleTeamType)
    {
        RoleTypes roleType = (RoleTypes)Enum.Parse(typeof(RoleTypes), roleTeamType.ToString(), true);
        RoleBehaviour role = GetBehaviourByRoleType(roleType);

        return role;
    }
    public static class MeetingVoteCloser
    {
        private static readonly MethodInfo ForceSkipAllMethod =
            AccessTools.Method(typeof(MeetingHud), "ForceSkipAll");

        public static void CloseVoteNow()
        {
            MeetingHud meeting = MeetingHud.Instance;
            if (meeting == null) return;

            if (!AmongUsClient.Instance.AmHost)
                return;

            if (meeting.CurrentState != MeetingHud.VoteStates.NotVoted &&
                meeting.CurrentState != MeetingHud.VoteStates.Voted)
                return;

            ForceSkipAllMethod.Invoke(meeting, null);
        }
    }
    public static bool seeGhosts = false;
    public static PlayerControl GetTarget(string input)
    {
        if (byte.TryParse(input, out byte id)) return GetPlayerById(id);

        byte colorId = MsgToColor(input);
        if (colorId != byte.MaxValue)
            return PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.Data?.DefaultOutfit.ColorId == colorId);

        string norm = NameNormalizer.NormalizeInputName(input);
        return PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p =>
            NameNormalizer.NormalizeInputName(p.Data.PlayerName).Equals(norm, StringComparison.OrdinalIgnoreCase));
    }
    public static void SetPlayerRole(PlayerControl target, RoleTypes role)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            Debug.LogWarning("[RoleCheats] Solo l'host può impostare i ruoli.");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[RoleCheats] Player nullo.");
            return;
        }

        target.RpcSetRole(role);

        BMLogger.Info($"[RoleCheats] Ruolo {role} assegnato a {target.Data.PlayerName}");
    }

    internal static string GetPlatformName(PlayerControl player, bool useTag = false)
    {
        if (player?.GetClient()?.PlatformData == null) return string.Empty;
        return GetPlatformName(player.GetClient().PlatformData.Platform, useTag);
    }
    public static void FlashColor(Color color, float duration = 1f)
    {
        HudManager hud = FastDestroyableSingleton<HudManager>.Instance;
        if (hud.FullScreen == null) return;

        GameObject obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;

        if (obj == null)
        {
            obj = Object.Instantiate(hud.FullScreen.gameObject, hud.transform);
            obj.name = "FlashColor_FullScreen";
        }

        hud.StartCoroutine(Effects.Lerp(duration, new Action<float>(t =>
        {
            obj.SetActive(Math.Abs(t - 1f) > 0.1f);
            obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01(((-2f * Mathf.Abs(t - 0.5f)) + 1) * color.a / 2));
        })));
    }
    internal static string GetPlatformName(Platforms platform, bool useTag = false)
    {
        var (platformName, tag) = platform switch
        {
            Platforms.StandaloneSteamPC => ("Steam", "PC"),
            Platforms.StandaloneEpicPC => ("Epic Games", "PC"),
            Platforms.StandaloneWin10 => ("Microsoft Store", "PC"),
            Platforms.StandaloneMac => ("Mac OS", "PC"),
            Platforms.StandaloneItch => ("Itch.io", "PC"),
            Platforms.Xbox => ("Xbox", "Console"),
            Platforms.Playstation => ("Playstation", "Console"),
            Platforms.Switch => ("Switch", "Console"),
            Platforms.Android => ("Android", "Mobile"),
            Platforms.IPhone => ("IPhone", "Mobile"),
            Platforms.Unknown => ("None", ""),
            _ => (string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(platformName))
            return string.Empty;

        return useTag && !string.IsNullOrEmpty(tag) ? $"{tag}: {platformName}" : platformName;
    }
    public static PlayerControl FindPlayerByNetId(uint netId)
    {
        if (PlayerControl.AllPlayerControls == null)
            return null;

        for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
        {
            var pc = PlayerControl.AllPlayerControls[i];
            if (pc != null && pc.NetId == netId)
                return pc;
        }

        return null;
    }
    public static List<NetworkedPlayerInfo> GetAllPlayerData()
    {
        var playerDataList = new List<NetworkedPlayerInfo>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null)
            {
                playerDataList.Add(player.Data);
            }
        }

        return playerDataList;
    }
    public class SavedOutfit
    {
        public string PlayerName;
        public int ColorId;
        public string HatId;
        public string SkinId;
        public string VisorId;
        public string PetId;
        public string NamePlateId;
    }
    public static SavedOutfit OriginalOutfit = null;
    public static void SaveOriginalOutfit(PlayerControl player)
    {
        if (player == null || player.Data == null) return;

        if (OriginalOutfit != null) return;

        var o = PlayerControl.LocalPlayer.Data.DefaultOutfit;

        OriginalOutfit = new SavedOutfit()
        {
            ColorId = o.ColorId,
            HatId = o.HatId,
            SkinId = o.SkinId,
            VisorId = o.VisorId,
            PetId = o.PetId,
            NamePlateId = o.NamePlateId
        };

        BMLogger.Info("[BanMod] Outfit originale salvato con successo.");
    }
    public static void CopyOutfit(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null) return;

        var o = source.Data.DefaultOutfit;


        if (target == AmongUsClient.Instance.AmHost)
        {
            target.RpcSetColor((byte)o.ColorId);
            target.RpcSetHat(o.HatId);
            target.RpcSetSkin(o.SkinId);
            target.RpcSetVisor(o.VisorId);
            target.RpcSetPet(o.PetId);
            target.RpcSetNamePlate(o.NamePlateId);

            var t = target.Data.DefaultOutfit;
            t.ColorId = o.ColorId;
            t.HatId = o.HatId;
            t.SkinId = o.SkinId;
            t.VisorId = o.VisorId;
            t.PetId = o.PetId;
            t.NamePlateId = o.NamePlateId;
        }
        if (target != AmongUsClient.Instance.AmHost)
        {
            target.RpcSetHat(o.HatId);
            target.RpcSetSkin(o.SkinId);
            target.RpcSetVisor(o.VisorId);
            target.RpcSetPet(o.PetId);
            target.RpcSetNamePlate(o.NamePlateId);

            var t = target.Data.DefaultOutfit;
            t.HatId = o.HatId;
            t.SkinId = o.SkinId;
            t.VisorId = o.VisorId;
            t.PetId = o.PetId;
            t.NamePlateId = o.NamePlateId;
        }

        target.Data.MarkDirty();
    }
    public static void CopyOutfit1(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null) return;

        var o = source.Data.DefaultOutfit;

        if (target == AmongUsClient.Instance.AmHost)
        {
            target.RpcSetColor((byte)o.ColorId);
            target.RpcSetHat(o.HatId);
            target.RpcSetSkin(o.SkinId);
            target.RpcSetVisor(o.VisorId);

            var t = target.Data.DefaultOutfit;
            t.ColorId = o.ColorId;
            t.HatId = o.HatId;
            t.SkinId = o.SkinId;
            t.VisorId = o.VisorId;
        }
    }
    public static void RestoreOriginalOutfit(PlayerControl player)
    {
        if (player == null || player.Data == null) return;
        if (OriginalOutfit == null) return;

        var o = OriginalOutfit;

        if (AmongUsClient.Instance.AmHost)
        {
            player.RpcSetColor((byte)o.ColorId);
            player.RpcSetHat(o.HatId);
            player.RpcSetSkin(o.SkinId);
            player.RpcSetVisor(o.VisorId);
            player.RpcSetPet(o.PetId);
            player.RpcSetNamePlate(o.NamePlateId);

            var info = player.Data.DefaultOutfit;
            info.ColorId = o.ColorId;
            info.HatId = o.HatId;
            info.SkinId = o.SkinId;
            info.VisorId = o.VisorId;
            info.PetId = o.PetId;
            info.NamePlateId = o.NamePlateId;
        }
        else
        {
            player.RpcSetHat(o.HatId);
            player.RpcSetSkin(o.SkinId);
            player.RpcSetVisor(o.VisorId);
            player.RpcSetPet(o.PetId);
            player.RpcSetNamePlate(o.NamePlateId);

            var info = player.Data.DefaultOutfit;
            info.HatId = o.HatId;
            info.SkinId = o.SkinId;
            info.VisorId = o.VisorId;
            info.PetId = o.PetId;
            info.NamePlateId = o.NamePlateId;
        }

        player.Data.MarkDirty();
    }

    public static bool stringToPlatformType(string platformStr, out Platforms? platform)
    {

        if (!string.IsNullOrEmpty(platformStr))
        { 

            try
            {
                platform = (Platforms)System.Enum.Parse(typeof(Platforms), platformStr, true);

                return true; 

            }
            catch { }

        }

        platform = null;
        return false; 
    }

    public static string PlatformTypeToString(Platforms platform)
    {
        return platform switch
        {
            Platforms.StandaloneEpicPC => "Epic",
            Platforms.StandaloneSteamPC => "Steam",
            Platforms.StandaloneMac => "Mac",
            Platforms.StandaloneWin10 => "Microsoft Store",
            Platforms.StandaloneItch => "Itch.io",
            Platforms.IPhone => "iPhone / iPad",
            Platforms.Android => "Android",
            Platforms.Switch => "Nintendo Switch",
            Platforms.Xbox => "Xbox",
            Platforms.Playstation => "PlayStation",
            _ => "Unknown"
        };
    }
    public static void SpawnLobby()
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            Debug.LogWarning("[MapCheats] Solo l'host può creare la lobby.");
            return;
        }
        if (!GameStates.isLobby)
        {
            return;
        }
        if (LobbyBehaviour.Instance == null)
        {
            var prefab = DestroyableSingleton<GameStartManager>.Instance.LobbyPrefab;
            if (prefab != null)
            {
                LobbyBehaviour.Instance = UnityEngine.Object.Instantiate<LobbyBehaviour>(prefab);
                AmongUsClient.Instance.Spawn(LobbyBehaviour.Instance, -2, SpawnFlags.None);
                BMLogger.Info("[MapCheats] LobbyBehaviour creato dal prefab.");
            }
            else
            {
                Debug.LogWarning("[MapCheats] LobbyPrefab non trovato in GameStartManager.");
            }
        }
        else
        {
            Debug.LogWarning("[MapCheats] LobbyBehaviour esiste già.");
        }

    }
    public static void ResetLobby()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var gsm = DestroyableSingleton<GameStartManager>.Instance;
        if (gsm == null || gsm.LobbyPrefab == null) return;

        var oldLobby = LobbyBehaviour.Instance;

        if (oldLobby != null)
        {
            (oldLobby as InnerNetObject)?.Despawn();
            LobbyBehaviour.Instance = null;
        }

        var newLobby = UnityEngine.Object.Instantiate(gsm.LobbyPrefab);
        LobbyBehaviour.Instance = newLobby;
        AmongUsClient.Instance.Spawn(newLobby, -2, SpawnFlags.None);
    }

    public static class CheatUtils
    {
        public static void CompletaTutteLeTask()
        {
            var player = PlayerControl.LocalPlayer;
            var client = AmongUsClient.Instance;

            if (player == null || player.Data == null)
            {
                Debug.LogWarning("Giocatore locale non disponibile.");
                return;
            }

            if (client == null)
            {
                Debug.LogWarning("AmongUsClient non inizializzato.");
                return;
            }

            var taskList = player.Data.Tasks;
            if (taskList == null || taskList.Count == 0)
            {
                Debug.LogWarning("Nessuna task da completare.");
                return;
            }

            HudManager.Instance.StartCoroutine(CompletaTutteLeTaskConDelay(1.5f));
        }
        public static IEnumerator CompletaTutteLeTaskConDelay(float delayPerTask = 1.5f)
        {
            var player = PlayerControl.LocalPlayer;
            var client = AmongUsClient.Instance;

            if (player == null || player.Data == null || client == null) yield break;

            bool isHost = client.AmHost;
            var taskList = player.Data.Tasks;

            if (taskList == null || taskList.Count == 0) yield break;

            var idsDaCompletare = new List<int>(taskList.Count);
            foreach (var taskInfo in taskList)
            {
                if (taskInfo != null && !taskInfo.Complete)
                    idsDaCompletare.Add((int)taskInfo.Id);
            }

            foreach (int id in idsDaCompletare)
            {
                var listaCorrente = player.Data.Tasks;
                if (listaCorrente == null) break;

                TaskInfo task = null;
                for (int i = 0; i < listaCorrente.Count; i++)
                {
                    if (listaCorrente[i] != null && (int)listaCorrente[i].Id == id)
                    {
                        task = listaCorrente[i];
                        break;
                    }
                }

                if (task == null) continue;

                var writer = client.StartRpcImmediately(
                    player.NetId,
                    (byte)RpcCalls.CompleteTask,
                    SendOption.Reliable,
                    client.HostId
                );

                writer.WritePacked(id);
                client.FinishRpcImmediately(writer);

                task.Complete = true;

                yield return new WaitForSeconds(Mathf.Max(1.5f, delayPerTask));
            }

            var taskSnapshot = player.myTasks.ToArray();
            foreach (var t in taskSnapshot)
            {
                if (t == null || t.IsComplete) continue;

                if (t is NormalPlayerTask normal)
                {
                    while (normal.TaskStep < normal.MaxStep)
                    {
                        normal.NextStep();
                        yield return new WaitForSeconds(1.25f);
                    }
                }

                t.Complete();
                yield return new WaitForSeconds(0.05f);
            }

            player.Data.MarkDirty();

        }

        public static void BypassScanner(bool value)
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null)
                    return;

                byte scannerCount = (byte)(PlayerControl.LocalPlayer.scannerCount + 1);
                PlayerControl.LocalPlayer.scannerCount = scannerCount;
                PlayerControl.LocalPlayer.SetScanner(value, scannerCount);

                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, 15, SendOption.Reliable, p.OwnerId);

                    writer.Write(value);
                    writer.Write(scannerCount);

                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Errore in BypassScanner: " + e.Message);
            }
        }

        public static void HandleScannerRPC(PlayerControl instance, byte callId, MessageReader reader)
        {
            if (callId != 15) return;

            try
            {
                bool scanValue = reader.ReadBoolean();
                byte scanCount = reader.ReadByte();

                instance.SetScanner(scanValue, scanCount);
            }
            catch (Exception e)
            {
                Debug.LogError("Errore in HandleScannerRPC: " + e.Message);
            }
        }

        public static IEnumerator BypassScannerWithTimeout(float duration)
        {
            BypassScanner(true);
            yield return new WaitForSeconds(duration);
            BypassScanner(false); 
        }
        public static void PlayAnimation(byte animationType)
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            player.PlayAnimation(animationType);

            RpcPlayAnimationMessage rpc = new(player.NetId, animationType);
            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpc));
        }

        public static void AnimShields() => PlayAnimation((byte)TaskTypes.PrimeShields);
        public static void AnimAsteroids() => PlayAnimation((byte)TaskTypes.ClearAsteroids);
        public static void AnimEmptyGarbage() => PlayAnimation((byte)TaskTypes.EmptyGarbage);

        public static void CamsOn()
        {
            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
        }

        public static void CamsOff()
        {
            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
        }
    }

    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", string.Empty);
    public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    private static readonly Dictionary<string, Sprite> CachedSprites = [];
    private static readonly DateTime TimeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
       GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);

    public static class SpamDetector
    {
        private class PlayerSpamData
        {
            public int MessageCount;
            public float LastResetTime;
        }

        private static Dictionary<int, PlayerSpamData> _playerData = new Dictionary<int, PlayerSpamData>();
        private const int MaxMessagesPerSecond = 60;

        public static bool IsSpamming(int playerId)
        {
            if (!_playerData.TryGetValue(playerId, out var data))
            {
                data = new PlayerSpamData { MessageCount = 0, LastResetTime = Time.time };
                _playerData[playerId] = data;
            }

            float now = Time.time;

            if (now - data.LastResetTime > 1f)
            {
                data.MessageCount = 0;
                data.LastResetTime = now;
            }

            data.MessageCount++;
            return data.MessageCount > MaxMessagesPerSecond;
        }

        public static void ResetPlayer(int playerId)
        {
            _playerData.Remove(playerId);
        }

        public static void ResetAll()
        {
            _playerData.Clear();
        }
    }
    public static void BanPlayer(int playerId)
    {
        NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"[NetworkBan] Player {playerId} banned for spamming.");
        BMLogger.LogWarning($"[NetworkBan] Player {playerId} banned for spamming.");
        AmongUsClient.Instance.KickPlayer(playerId, true);
        SpamDetector.ResetPlayer(playerId); 
    }
    public static class RpcMap
    {
        public static readonly Dictionary<byte, string> RpcNames = new Dictionary<byte, string>()
    {
        { 0, "PlayAnimation" },
        { 1, "CompleteTask" },
        { 2, "SyncSettings" },
        { 3, "SetInfected" },
        { 4, "Exiled" },
        { 5, "CheckName" },
        { 6, "SetName" },
        { 7, "CheckColor" },
        { 8, "SetColor" },
        { 9, "SetHat_Deprecated" },
        { 10, "SetSkin_Deprecated" },
        { 11, "ReportDeadBody" },
        { 12, "MurderPlayer" },
        { 13, "SendChat" },
        { 14, "StartMeeting" },
        { 15, "SetScanner" },
        { 16, "SendChatNote" },
        { 17, "SetPet_Deprecated" },
        { 18, "SetStartCounter" },
        { 19, "EnterVent" },
        { 20, "ExitVent" },
        { 21, "SnapTo" },
        { 22, "CloseMeeting" },
        { 23, "VotingComplete" },
        { 24, "CastVote" },
        { 25, "ClearVote" },
        { 26, "AddVote" },
        { 27, "CloseDoorsOfType" },
        { 29, "SetTasks" },
        { 31, "ClimbLadder" },
        { 32, "UsePlatform" },
        { 33, "SendQuickChat" },
        { 34, "BootFromVent" },
        { 35, "UpdateSystem" },
        { 36, "SetVisor_Deprecated" },
        { 37, "SetNamePlate_Deprecated" },
        { 38, "SetLevel" },
        { 39, "SetHatStr" },
        { 40, "SetSkinStr" },
        { 41, "SetPetStr" },
        { 42, "SetVisorStr" },
        { 43, "SetNamePlateStr" },
        { 44, "SetRole" },
        { 45, "ProtectPlayer" },
        { 46, "Shapeshift" },
        { 47, "CheckMurder" },
        { 48, "CheckProtect" },
        { 49, "Pet" },
        { 50, "CancelPet" },
        { 51, "CheckZipline" },
        { 52, "UseZipline" },
        { 53, "TriggerSpores" },
        { 54, "CheckSpore" },
        { 55, "CheckShapeshift" },
        { 56, "RejectShapeshift" },
        { 60, "LobbyTimeExpiring" },
        { 61, "ExtendLobbyTimer" },
        { 62, "CheckVanish" },
        { 63, "StartVanish" },
        { 64, "CheckAppear" },
        { 65, "StartAppear" }
    };

        public static string GetRpcName(byte id)
        {
            return RpcNames.TryGetValue(id, out var name)
                ? name
                : $"UnknownRpc_{id}";
        }
    }
    public static void AddChatBypass(PlayerControl sourcePlayer, string chatText, bool censor = true)
    {
        if (sourcePlayer == null || PlayerControl.LocalPlayer == null) return;

        ChatController chat = DestroyableSingleton<ChatController>.Instance;
        if (chat == null) return;

        var dataSelf = PlayerControl.LocalPlayer.Data;
        var dataSource = sourcePlayer.Data;
        if (dataSelf == null || dataSource == null) return;

        ChatBubble bubble = chat.GetPooledBubble();
        try
        {
            bubble.transform.SetParent(chat.scroller.Inner);
            bubble.transform.localScale = Vector3.one;

            bool isSelf = (sourcePlayer == PlayerControl.LocalPlayer);
            if (isSelf) bubble.SetRight();
            else bubble.SetLeft();

            bool didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);

            bubble.SetCosmetics(dataSource);
            chat.SetChatBubbleName(bubble, dataSource, dataSource.IsDead, didVote, PlayerNameColor.Get(dataSource), null);

            if (censor && DataManager.Settings.Multiplayer.CensorChat)
                chatText = BlockedWords.CensorWords(chatText, false);

            bubble.SetText(chatText);
            bubble.AlignChildren();
            chat.AlignAllBubbles();

            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }

            if (!isSelf && !chat.IsOpenOrOpening)
            {
                SoundManager.Instance.PlaySound(chat.messageSound, false, 1f, null)
                    .pitch = 0.5f + (float)sourcePlayer.PlayerId / 15f;
                chat.chatNotification.SetUp(sourcePlayer, chatText);
            }
        }
        catch (Exception)
        {
            BMLogger.Error(null);
            chat.chatBubblePool.Reclaim(bubble);
        }
    }
    public static int GetCurrentLobbyPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < GameData.Instance.PlayerCount; i++)
        {
            var player = GameData.Instance.AllPlayers[i];
            if (player != null && !player.Disconnected)
            {
                count++;
            }
        }
        return count;
    }

    public static string GetCurrentLobbyMode()
    {
        string lobbymode = "";
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        bool hideAndSeek = GameManager.Instance.IsHideAndSeek();
        bool Normal = GameManager.Instance.IsNormal();
        if (hideAndSeek)
        {
            lobbymode = "HideAndSeek";
        }
        else if (Normal && gameMode == GameModeType.KaitoRun)
        {
            lobbymode = "KaitoRun";
        }
        else if (Normal && gameMode == GameModeType.SnS)
        {
            lobbymode = "SnS";
        }
        else if (Normal && gameMode == GameModeType.BanMod)
        {
            lobbymode = "BanMod";
        }
        else if (Normal && gameMode == GameModeType.Default)
        {
            lobbymode = "Default";
        }
        else if (Normal && gameMode == GameModeType.TaskRun)
        {
            lobbymode = "TaskRun";
        }
        else if (Normal && gameMode == GameModeType.JBMode)
        {
            lobbymode = "JBMode";
        }
        else if (Normal && gameMode == GameModeType.FFA)
        {
            lobbymode = "FFA";
        }
        else 
        {
            lobbymode = "UnkNow";
        }
        return lobbymode;
    }
    public static string GetCurrentStatus()
    {
        string status = "";

        if (GameStates.InGame)
        {
            status = "In_Game";
        }
        else if (GameStates.isLobby)
        {
            status = "In_Lobby";
        }

        return status;
    }
    public static MapNames GetActiveMapName() => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
    public static MapNames GetCurrentMap()
    {
        if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null)
        {
            return (MapNames)(-1);
        }

        byte mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;

        switch (mapId)
        {
            case 0: return MapNames.Skeld;
            case 1: return MapNames.MiraHQ;
            case 2: return MapNames.Polus;
            case 3: return MapNames.Dleks;     
            case 4: return MapNames.Airship;
            case 5: return MapNames.Fungle;
            default:
                return (MapNames)(-1);
        }
    }
    public static string GetRegionName(IRegionInfo region = null)
    {
        region ??= ServerManager.Instance.CurrentRegion;

        string name = region.Name;

        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            name = "Local Games";
            return name;
        }

        if (region.PingServer.EndsWith("among.us", StringComparison.Ordinal))
        {
            if (name == "North America") name = "NA";
            else if (name == "Europe") name = "EU";
            else if (name == "Asia") name = "AS";

            return name;
        }
        return name;
    }

    public static class LanguageUtils
    {
        public static string GetLanguageName(IGameOptions options)
        {
            if (options == null) return "Unknown";

            GameKeywords keywords = (GameKeywords)options.Keywords;

            if (keywords.HasFlag(GameKeywords.English)) return "English";
            if (keywords.HasFlag(GameKeywords.Italian)) return "Italian";
            if (keywords.HasFlag(GameKeywords.French)) return "French";
            if (keywords.HasFlag(GameKeywords.German)) return "German";
            if (keywords.HasFlag(GameKeywords.SpanishLA)) return "Spanish (LA)";
            if (keywords.HasFlag(GameKeywords.SpanishEU)) return "Spanish (EU)";
            if (keywords.HasFlag(GameKeywords.Brazilian)) return "Brazilian Portuguese";
            if (keywords.HasFlag(GameKeywords.Portuguese)) return "Portuguese";
            if (keywords.HasFlag(GameKeywords.Korean)) return "Korean";
            if (keywords.HasFlag(GameKeywords.Russian)) return "Russian";
            if (keywords.HasFlag(GameKeywords.Dutch)) return "Dutch";
            if (keywords.HasFlag(GameKeywords.Filipino)) return "Filipino";
            if (keywords.HasFlag(GameKeywords.Japanese)) return "Japanese";
            if (keywords.HasFlag(GameKeywords.Arabic)) return "Arabic";
            if (keywords.HasFlag(GameKeywords.Polish)) return "Polish";
            if (keywords.HasFlag(GameKeywords.SChinese)) return "Simplified Chinese";
            if (keywords.HasFlag(GameKeywords.TChinese)) return "Traditional Chinese";
            if (keywords.HasFlag(GameKeywords.Irish)) return "Irish";

            if (keywords == GameKeywords.All) return "All";
            if (keywords == GameKeywords.Other) return "Other";

            return "Unknown";
        }

        public static IGameOptions GetCurrentGameOptions()
        {
            if (GameOptionsManager.Instance == null)
                return null;

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                return GameOptionsManager.Instance.GameHostOptions;
            }
            else
            {
                return GameOptionsManager.Instance.GameSearchOptions;
            }
        }
    }

    public class ButtonData
    {
        public string Title { get; set; } 
        public string Message { get; set; } 
    }
    public static string ColorString(UnityEngine.Color c, string s)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
    }
    public static class FancyText
    {
        private static readonly Dictionary<char, string> fancyMap = new Dictionary<char, string>
    {
        {'A', "卂"}, {'B', "乃"}, {'C', "匚"}, {'D', "刀"}, {'E', "乇"},
        {'F', "下"}, {'G', "厶"}, {'H', "卄"}, {'I', "丨"}, {'J', "ﾌ"},
        {'K', "ズ"}, {'L', "ㄥ"}, {'M', "爪"}, {'N', "𠘨"}, {'O', "ㄖ"},
        {'P', "卩"}, {'Q', "㔿"}, {'R', "尺"}, {'S', "丂"}, {'T', "ㄒ"},
        {'U', "ㄩ"}, {'V', "ᐯ"}, {'W', "山"}, {'X', "メ"}, {'Y', "ㄚ"}, {'Z', "乙"}
    };

        public static string ToFancy(string input)
        {
            var sb = new StringBuilder();

            foreach (char c in input)
            {
                char upper = char.ToUpper(c);

                if (fancyMap.ContainsKey(upper))
                    sb.Append(fancyMap[upper]);
                else
                    sb.Append(c); 
            }

            return sb.ToString();
        }
    }
    private static readonly System.Random Rand = new System.Random();
    public static readonly List<string> InsultiDisponibili = new List<string>();

    public static string PrendiInsulto()
    {
        if (InsultiDisponibili.Count == 0)
        {
            InsultiDisponibili.AddRange(Insulti);

            for (int i = InsultiDisponibili.Count - 1; i > 0; i--)
            {
                int j = Rand.Next(i + 1);
                string temp = InsultiDisponibili[i];
                InsultiDisponibili[i] = InsultiDisponibili[j];
                InsultiDisponibili[j] = temp;
            }
        }

        string insulto = InsultiDisponibili[InsultiDisponibili.Count - 1];
        InsultiDisponibili.RemoveAt(InsultiDisponibili.Count - 1);

        return insulto;
    }
    public static readonly string[] Insulti = new[]
    {
        "sei inutile come il tutorial di Among Us",
        "Non mi aspettavo niente da te e sono rimasto comunque deluso",
        "sei cosi brutto/a che se ti vede il gatto nero si gratta le palle e gira l'angolo",
        "sei così vacca che in India ti fanno sacra",
        "sei talmente zoccola che se ti dicono batti il 5 controlli subito l'agenda",
        "sei fastidioso/a come un chiodo nel culo",
        "sei talmente sfigato che se ti cade l'uccello rimbalza e ti picchia nel culo",
        "sei simpatico come un dito in culo e puzzi pure peggio",
        "fai talmente schifo che devi dare il viagra al tuo vibratore",
        "non ti picchio solo perchè la merda schizza!",
        "Sei raro come una figura mitologica: il corpo di uomo e la testa di cazzo",
        "Sei come Unieuro: batti, forte, sempre",
        "Quando Dio diede l'intelligenza all'umanità tu dov'eri? Al cesso!?",
        "sei cretino di tuo oppure ci hai studiato per esserlo?",
        "Meglio se non pensi, altrimenti il tuo cervello va in carenza d'ossigeno.",
        "sei come una nuvola, se ti levi dalle palle è una bella giornata.",
        "cagati in mano e poi prenditi a sberle.",
        "Sei simpatico come un riccio nelle mutande!",
        "Sei simpatico come un gatto attaccato ai maroni",
        "Essere stupidi è un diritto ma tu ne hai fatto un ABUSO !!",
        "Sei talmente scemo da essere secondo anche nella tua categoria!",
        "Hai più problemi tu che un libro di matematica.",
        "Credevo che la cosa più brutta esistente fosse la fame nel mondo, poi ho visto te!",
        "Partecipa a un concorso di intelligenza, qualcuno deve pur arrivare ultimo!",
        "E' vero che la natura fa brutti scherzi, ma a te t'ha preso proprio per il culo.",
        "Ma quando Dio ha donato il cervello all' umanità, tu perchè l' hai rifiutato?",
        "Sei talmente stupido che riusciresti a farti investire da un auto parcheggiata.",
        "Hai il quoziente intellettivo di un comodino!!!",
        "Se sei in giro con la bici e ti senti felice, guarda bene, forse hai dimenticato il sellino!",
        "Hai la faccia giusta per lavorare nella pubblicita'... dei lassativi.",
        "Vorrei poterti entrare nella testa, per provare l'ebbrezza del vuoto",
        "I tuoi silenzi sono più intelligenti delle tue parole",
        "Sei utile come un culo senza il buco",
        "Se lo scemo fosse un mestiere tu avresti una ditta e molti dipendenti!!!",
        "sei come un coltello svizzero... rompi i coglioni in 36 modi diversi",
        "Sei cosi scemo che se vai al cinema e leggi vietato ai minori di 18 torni con 17 amici",
        "Non prendertela se ti considerano mezzo scemo. Si vede che ti conoscono solo a metà.",
        "Sei talmente scemo che quando fai il cretino sembri normale!",
        "Sei così stupido che non troveresti una spina in una foresta di cactus.",
        "Ti farei tanti applausi… ma con la tua faccia in mezzo!",
        "Oggi ho sentito che è stato ritrovato un corpo senza cervello, ti prego dimmi che stai bene!",
        "Hai il cervello così piccolo che per cambiare idea devi fare manovra!",
        "Ti darei cinque minuti di intelligenza solo per farti capire quanto sei idiota!",
        "Meriti l’Oscar come Miglior cervello non protagonista",
        "Se sei intelligente, lo nascondi molto bene.",
        "Tu non sei scemo, sei solo diversamente intelligente.",
        "Come fai ad amare la natura dopo tutto quello che ti ha fatto?",
        "Sei la prova che Dio ha un gran senso dell’umorismo.",
        "Lo sai come si tiene sulle spine un imbecille? \n\n\n\n\n\n\n\n\n\nDomani te lo dico…",
        "Non sei stupido! Hai solo sfortuna quando pensi.",
        "Sei talmente stupido che accenderesti la luce per vedere se è buio.",
        "Non sto dicendo che hai problemi, ma hai provato a spegnere e riavviarti?",
        "vuoi che ci siano meno stronzi in circolazione? Resta a casa.",
        "Se ti sto dando fastidio dimmelo, che continuo!",
        "Se ti ho offeso con queste battute, ti chiedo scusa. Non pensavo sapessi leggere.",
        "Parlare con te è come aggiornare Windows: inutile, frustrante e sempre fuori tempo",
        "Hai un cervello così liscio che se ci versi l’acqua scivola via senza toccare niente",
        "Se l’idiozia fosse un virus, tu saresti il paziente zero",
        "Non saprei se insultarti o compatirti, quindi faccio entrambe le cose",
        "La tua presenza è meno gradita di un pop-up con scritto 'virus rilevato'",
        "Il tuo livello di inutilità sfida ogni logica conosciuta",
        "Se la stupidità fosse un’arte, tu saresti un museo intero",
        "Hai la consistenza mentale di una gelatina scaduta",
        "Quando parli, anche il silenzio si vergogna",
        "Ti impegni a essere così o ti viene naturale?",
        "Sei l’unico che potrebbe perdere contro un tutorial",
        "Quando fai una scelta, anche il destino sbuffa",
        "Vorrei capire cosa ti passa per la testa, ma temo il deserto",
        "Di’ qualcosa di intelligente ogni tanto, fai felice anche il tuo cervello",
        "Sei la versione umana delle notifiche fastidiose",
        "Ogni volta che parli, un neurone muore… e non hai molta riserva",
        "Il tuo cervello è così vuoto che l’eco si rifiuta pure di risponderti.",
        "Non so se mi fai pena o ridere, quindi per sicurezza evito entrambe le cose.",
        "Se sei la versione migliore di te stesso… non voglio vedere l’alpha.",
        "Quando parli sembri un aggiornamento fallito: rumore, errori e nessun miglioramento.",
        "Non hai la facoltà di pensare: hai un fastidioso rumore di fondo.",
        "Non dico che sei inutile, ma se fossi un caricatore sarebbe quello della stampante.",
        "Ti impegni a essere così o ti svegli già programmato male?",
        "Sei la prova che non tutti i processi mentali hanno successo.",
        "Parli tanto, ma il tuo cervello continua a non essere disponibile.",
        "Quando ragioni sembra di vedere un vecchio PC che fuma.",
        "Non sei antipatico: è la tua esistenza che fa crashare l’ambiente.",
        "Sei il tipo di persona che riesce a perdere anche contro un personaggio AFK.",
        "Non sei un problema da risolvere: sei proprio un errore da ignorare.",
        "Sei talmente limitato che sembri la demo gratuita di una persona vera.",
        "Quando provi a fare sarcasmo, sembra di guardare un criceto stanco che tenta di correre.",
        "Non serve insultarti: ti ci pensi da solo ogni volta che apri bocca.",
        "Il tuo cervello non è in ritardo: è proprio rimasto all’installazione.",
        "Vorrei cimentarmi in un duello intellettuale con te, ma vedo che sei disarmato.",
        "quando eri piccolo la tua altalena era probabilmente troppo vicina al muro!",
        "L'intelligenza sembra inseguirti - ma sfortunatamente sei più veloce.",
        "Uno di noi due è più stupido di me.",
        "Ricordo di essere stato al tuo livello di ignoranza, ero solo un bambino all'epoca.",
        "fai un clistere nelle orecchie perchè è il tuo cervello che è pieno di cacca non l'intestino.",
        "sei così inutile che pure tua madre ti mette tra le notifiche silenziate",
"hai la faccia di uno che è nato durante un errore di sistema",
"sei talmente scemo che il tuo cervello ha il cartello vendesi",
"hai più vuoto in testa che dignità in corpo",
"sei il risultato di quando la natura clicca su invia per sbaglio",
"sei così coglione che se piove zuppa esci col cucchiaio bucato",
"hai la simpatia di una scoreggia in ascensore pieno",
"sei così brutto che lo specchio ti guarda e cambia stanza",
"hai il cervello più liscio del culo di un neonato",
"sei talmente inutile che pure il tuo angelo custode ha dato le dimissioni",
"hai la faccia di uno che ha litigato con la bellezza e ha perso male",
"sei così stupido che se ti danno corda ti impicchi metaforicamente da solo",
"hai più merda in testa che un cesso dell'autogrill",
"sei il tipo di persona che fa bestemmiare anche gli atei",
"sei così sfigato che se compri un boomerang non torna per scelta",
"hai il fascino di un rutto dopo il kebab",
"sei talmente scemo che il QI ti manda gli auguri da lontano",
"hai la grazia di un bidone che rotola giù per le scale",
"sei così inutile che se fossi un organo saresti l'appendice infiammata",
"hai il cervello in offerta, ma nessuno lo prende manco gratis",
"sei la prova che due neuroni possono litigare e perderli entrambi",
"sei così brutto che la fotocamera frontale si mette in pausa",
"hai la dignità di una ciabatta trovata in spiaggia a novembre",
"sei talmente coglione che se ti perdi ti cercano per obbligo morale",
"hai il carisma di un calzino bagnato dentro una scarpa chiusa",
"sei così inutile che pure il cestino ti chiede la differenziata",
"hai la faccia di uno che ha preso schiaffi pure dal destino",
"sei talmente scarso che la mediocrità ti fa da tutor",
"hai il cervello che fa eco e pure l'eco se ne va",
"sei così fastidioso che pure le zanzare ti mutano",
"hai la finezza di un rutto durante una confessione",
"sei talmente vuoto che potresti affittarti come cantina",
"hai il fascino di una gomma masticata sotto il banco",
"sei così scemo che se ti dicono 'sali' scendi per protesta",
"hai la simpatia di una diarrea prima di un colloquio",
"sei più inutile di un preservativo bucato",
"hai la faccia di uno che ha perso a morra cinese contro uno specchio",
"sei talmente limitato che anche il recinto ti guarda male",
"hai il cervello con due neuroni: uno dorme e l'altro lo copre",
"sei così brutto che il filtro bellezza chiede il congedo",
"hai più difetti tu che scuse un traditore beccato",
"sei il motivo per cui il silenzio dovrebbe essere obbligatorio",
"sei così coglione che se ti clonano denunciano il laboratorio",
"hai il fascino di un water intasato il giorno di Ferragosto",
"sei talmente inutile che pure il tuo posto vuoto rende meglio",
"hai il cervello così spento che Enel ti manda il rimborso",
"sei la risposta sbagliata a una domanda mai fatta",
"sei così sfigato che se ti cade il pane resta in piedi per evitarti",
"hai la classe di uno sputo su una camicia bianca",
"sei talmente scemo che il tuo pensiero più profondo è una pozzanghera",
"hai il carisma di una multa presa sotto casa",
"sei così inutile che se fossi un bottone saresti quello finto",
"hai la faccia di chi è stato bocciato pure dalla fortuna",
"sei talmente coglione che il tuo cervello ti ha bloccato su WhatsApp",
"hai la simpatia di un call center mentre stai cagando",
"sei così vuoto che se ti scuotono fai rumore di monetine false",
"hai il fascino di una mutanda stesa male",
"sei più fuori posto di un bidet in cucina",
"hai la grazia di un mulo ubriaco in un negozio di vetri",
"sei talmente scemo che se studi peggiori",
"hai il cervello che lavora a nero e pure male",
"sei così inutile che il tuo CV lo usa il camino",
"hai la faccia di uno che è stato disegnato di lunedì",
"sei talmente brutto che il buio ti accende la luce",
"hai la dignità di un fazzoletto usato due volte",
"sei così fastidioso che pure il mal di testa ti evita",
"hai il cervello con la segreteria: lascia un pensiero dopo il bip",
"sei il tipo di persona che fa rimpiangere il silenzio imbarazzante",
"sei talmente inutile che il tuo superpotere è occupare spazio",
"hai la faccia di chi ha perso una gara di bellezza contro un citofono",
"sei così coglione che se ti danno ragione ti confondi",
"hai la simpatia di un’unghia incarnita in vacanza",
"sei più vuoto di una promessa fatta da ubriaco",
"hai il fascino di un frigo spento pieno di pesce",
"sei talmente scemo che il tuo cervello ti ha messo in spam",
"hai la classe di una scoreggia sotto le coperte",
"sei così inutile che anche il nulla ti dice levati",
"hai il cervello in modalità provvisoria da quando sei nato",
"sei la dimostrazione che respirare non basta per essere utili",
"sei talmente brutto che il tuo riflesso finge di non conoscerti",
"hai la delicatezza di una padella sui denti",
"sei così sfigato che se ti lanci nel vuoto il vuoto si sposta",
"hai più problemi tu che una lavatrice in centrifuga coi mattoni",
"sei il motivo per cui certi gruppi hanno il tasto espelli",
"sei talmente coglione che l'intelligenza ti usa come confine",
"hai il fascino di una crosta sul ginocchio",
"sei così inutile che se sparisci nessuno aggiorna la rubrica",
"hai la faccia di una sveglia rotta: fastidiosa e inutile",
"sei talmente scemo che se ti fanno una domanda si offende il punto interrogativo",
"hai il cervello più secco di un panino del distributore",
"sei così fastidioso che pure la pubblicità ti skippa",
"hai la personalità di una ciabatta spaiata",
"sei talmente vuoto che potresti fare da garage al vento",
"hai la simpatia di una puntura sul culo",
"sei così inutile che se fossi un mobile saresti il piedino zoppo",
"hai la faccia di uno che è stato assemblato senza istruzioni",
"sei talmente coglione che se pensi troppo ti viene il jet lag",
"hai il carisma di un tombino aperto",
"sei così brutto che pure il citofono abbassa lo sguardo",
"hai più imbarazzo addosso che vestiti puliti",
"sei talmente scemo che il tuo cervello ha chiesto il divorzio",
"hai la grazia di un maiale sui pattini",
"sei così inutile che pure la polvere ti scansa",
"hai il fascino di una macchia di sugo sul divano",
"sei il tipo di persona che fa sembrare intelligente una sedia",
"sei talmente scarso che anche l'ultimo posto ti evita",
"hai il cervello che funziona a bestemmie e comunque male",
"sei così fastidioso che il silenzio ti fa causa",
"hai la faccia di chi è nato già in modalità imbarazzo",
"sei talmente inutile che il tuo nome pesa più di te"
    };

    public static void Exile(NetworkedPlayerInfo playerToExileInfo)
    {
        VoteContextManager.IsForcedVote = true;
        List<MeetingHud.VoterState> statesList = new();
        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), playerToExileInfo, false);
        MeetingHud.Instance.Close();
        MeetingHud.Instance.RpcClose();
        VoteContextManager.IsForcedVote = false;
        playerToExileInfo.IsDead = true;
        playerToExileInfo.MarkDirty();

    }

    public static void KillPlayer(PlayerControl target)
    {
        if (GameStates.isLobby) return;

        var killer = PlayerControl.LocalPlayer;

        PreviousMatchPopupTracker.MarkMirrorKillTarget(target);

        if (!BanMod.UnreportableBodies.Contains(target.PlayerId))
        {
            BanMod.UnreportableBodies.Add(target.PlayerId);
        }
        if (Utils.IsShapeshifted(killer))
        {
            killer.CmdCheckRevertShapeshift(false);
        }
        if (target != PlayerControl.LocalPlayer && Utils.IsShapeshifted(target))
        {
            target.CmdCheckRevertShapeshift(false);
        }
        killer.MyPhysics.StartCoroutine(CoMirrorKill(killer, target));

        target.Data.IsDead = true;
        target.Data.MarkDirty();
    }

    private static IEnumerator CoMirrorKill(PlayerControl killer, PlayerControl target)
    {
        Utils.SaveOriginalOutfit(PlayerControl.LocalPlayer);

        Vector3 originalPosition = killer.transform.position;

        CopyOutfit(target, killer);

        yield return new WaitForSeconds(0.15f);

        killer.MurderPlayer(target, MurderResultFlags.Succeeded);
        target.Data.IsDead = true;
        target.Data.MarkDirty();
        DeadBody[] allBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        DeadBody body = allBodies.FirstOrDefault(b => b.ParentId == target.PlayerId);

        if (body != null)
            UnityEngine.Object.Destroy(body.gameObject);
        yield return new WaitForSeconds(0.06f);

        if (killer.NetTransform != null)
        {
            killer.NetTransform.RpcSnapTo(originalPosition);
            killer.transform.position = originalPosition;
        }

        RestoreOriginalOutfit(killer);

    }
    public static MapNames GetCurrentMapFromOptions()
    {
        if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null)
        {
            return (MapNames)(-1);
        }

        byte mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;

        switch (mapId)
        {
            case 0: return MapNames.Skeld;
            case 1: return MapNames.MiraHQ;
            case 2: return MapNames.Polus;
            case 3: return MapNames.Dleks;    
            case 4: return MapNames.Airship;
            case 5: return MapNames.Fungle;
            default:
                return (MapNames)(-1);
        }
    }
    public static class NameNormalizer
    {
        public static readonly Dictionary<char, char> SpecialCharMap = new Dictionary<char, char>
    {
        { '卂', 'A' }, { '卄', 'H' }, { '卩', 'P' },
        { '乃', 'B' }, { '丨', 'I' }, { 'Ɋ', 'Q' },
        { '匚', 'C' }, { 'ㄥ', 'L' }, { '尺', 'R' },
        { 'ᗪ', 'D' }, { '爪', 'M' }, { '丂', 'S' },
        { '乇', 'E' }, { '几', 'N' }, { 'ᐯ', 'V' },
        { '千', 'F' }, { 'ㄒ', 'T' }, { '乙', 'Z' },
        { 'Ꮆ', 'G' }, { 'ㄖ', 'O' }, { 'ㄩ', 'U' },

        { '4', 'a' }, { '3', 'e' }, { '5', 's' },
        { '1', 'i' }, { '7', 't' }, { '0', 'o' },

        { 'Ö', 'O' }, { 'Ø', 'O' }, { 'ö', 'o' }, { 'ø', 'o' },
        { 'à', 'a' }, { 'á', 'a' }, { 'â', 'a' }, { 'ã', 'a' },
        { 'è', 'e' }, { 'é', 'e' }, { 'ê', 'e' }, { 'ë', 'e' },
        { 'ì', 'i' }, { 'í', 'i' }, { 'î', 'i' }, { 'ï', 'i' },
        { 'ò', 'o' }, { 'ó', 'o' }, { 'ô', 'o' }, { 'õ', 'o' },
        { 'ù', 'u' }, { 'ú', 'u' }, { 'û', 'u' }, { 'ü', 'u' },
        { 'ç', 'c' }, { 'ñ', 'n' }, { 'ﾑ', 'a' }, { 'ﾉ', 'i' },
        { 'À', 'A' }, { 'Á', 'A' }, { 'Â', 'A' }, { 'Ã', 'A' },
        { 'È', 'E' }, { 'É', 'E' }, { 'Ê', 'E' }, { 'Ë', 'E' },
        { 'Ì', 'I' }, { 'Í', 'I' }, { 'Î', 'I' }, { 'Ï', 'I' },
        { 'Ò', 'O' }, { 'Ó', 'O' }, { 'Ô', 'O' }, { 'Õ', 'O' },
        { 'Ù', 'U' }, { 'Ú', 'U' }, { 'Û', 'U' }, { 'Ü', 'U' },
        { 'Ç', 'C' }, { 'Ñ', 'N' }, { 'ä', 'a' }, { 'å', 'a' },

        { 'а', 'a' }, { 'б', 'b' }, { 'в', 'v' }, { 'г', 'g' }, { 'д', 'd' }, { 'е', 'e' }, { 'ё', 'e' },
        { 'з', 'z' }, { 'и', 'i' }, { 'й', 'y' }, { 'к', 'k' }, { 'л', 'l' }, { 'м', 'm' },
        { 'н', 'n' }, { 'о', 'o' }, { 'п', 'p' }, { 'р', 'r' }, { 'с', 's' }, { 'т', 't' }, { 'у', 'u' },
        { 'ф', 'f' }, { 'х', 'h' }, { 'Ä', 'A' }, { 'Å', 'A' }, { 'ы', 'y' }, { 'э', 'e' },
        { 'А', 'A' }, { 'Б', 'B' }, { 'В', 'V' }, { 'Г', 'G' }, { 'Д', 'D' }, { 'Е', 'E' }, { 'Ё', 'E' },
        { 'З', 'Z' }, { 'И', 'I' }, { 'Й', 'Y' }, { 'К', 'K' }, { 'Л', 'L' }, { 'М', 'M' },
        { 'Н', 'N' }, { 'О', 'O' }, { 'П', 'P' }, { 'Р', 'R' }, { 'С', 'S' }, { 'Т', 'T' }, { 'У', 'U' },
        { 'Ф', 'F' }, { 'Х', 'H' },
        { 'Ы', 'Y' }, { 'Э', 'E' },

        { 'Ð', 'D' }, { 'ð', 'd' }
    };

        public static string NormalizeInputName(string inputName)
        {
            if (string.IsNullOrEmpty(inputName))
            {
                return inputName;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(inputName.Length);

            foreach (char c in inputName)
            {
                if (SpecialCharMap.TryGetValue(c, out char normalizedChar))
                {
                    sb.Append(normalizedChar);
                }
                else if (char.IsLetterOrDigit(c)) 
                {
                    sb.Append(char.ToLowerInvariant(c)); 
                }
            }
            return sb.ToString();
        }
    }
    public static Sprite CreateSprite(Color color, int width = 64, int height = 64)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot);
    }

    private static Stack<NetworkedPlayerInfo.PlayerOutfit> savedOutfits = new Stack<NetworkedPlayerInfo.PlayerOutfit>();
    private static Stack<string> savedNames = new Stack<string>();

    public static void SendInfo()
    {
        string title = TemplateLoader.LoadTemplate("InfoTemplate");
        Utils.SendMessage(title); 
        MessageBlocker.UpdateLastMessageTime();
    }

    public static void SendRules()
    {
        string mode = Options.GameMode.GetString();
        string templateName = mode switch
        {
            "SnS" => "RulesInfoSns",
            "KaitoRun" => "RulesInfoKaitoRun",
            "Default" => "RulesInfo",
            "TaskRun" => "RulesInfoTaskRun",
            "JBMode" => "RulesInfoJBMode",
            "FFA" => "RulesInfoFFA",
            _ => "WelcomeTemplate"
        };

        string title = TemplateLoader.FormatTemplate(templateName);
        Utils.SendMessage(title, 255);
        MessageBlocker.UpdateLastMessageTime();
    }
    public static string GradientText(string text, Color start, Color end)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (text.Length == 1)
            return ColorString(start, text);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            float t = (float)i / (text.Length - 1);
            Color c = Color.Lerp(start, end, t);
            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>");
        }

        return sb.ToString();
    }
    private static readonly System.Random RainbowNameRandom = new System.Random();

    public static string MakeRainbowName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        Color[][] palettes =
        {
        new Color[]
        {
            new Color(1f, 0f, 0f),
            new Color(1f, 0.45f, 0f),
            new Color(1f, 1f, 0f),
            new Color(0f, 1f, 0.35f),
            new Color(0f, 0.75f, 1f),
            new Color(0.65f, 0f, 1f)
        },
        new Color[]
        {
            new Color(1f, 0f, 0.45f),
            new Color(1f, 0.25f, 0.85f),
            new Color(0.65f, 0f, 1f),
            new Color(0f, 0.55f, 1f),
            new Color(0f, 1f, 0.85f)
        },
        new Color[]
        {
            new Color(1f, 0.9f, 0f),
            new Color(1f, 0.45f, 0f),
            new Color(1f, 0.1f, 0.1f),
            new Color(1f, 0f, 0.65f),
            new Color(0.75f, 0f, 1f)
        },
        new Color[]
        {
            new Color(0f, 1f, 0.55f),
            new Color(0f, 0.85f, 1f),
            new Color(0.05f, 0.25f, 1f),
            new Color(0.55f, 0f, 1f),
            new Color(1f, 0f, 0.85f)
        },
        new Color[]
        {
            new Color(1f, 0.25f, 0.25f),
            new Color(1f, 0.75f, 0.15f),
            new Color(0.65f, 1f, 0.15f),
            new Color(0.15f, 1f, 0.75f),
            new Color(0.15f, 0.75f, 1f)
        }
    };

        Color[] palette = palettes[RainbowNameRandom.Next(palettes.Length)];

        int offset = RainbowNameRandom.Next(palette.Length);
        bool reverse = RainbowNameRandom.Next(0, 2) == 1;

        string result = "<b>";

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (char.IsWhiteSpace(ch))
            {
                result += ch;
                continue;
            }

            int index = reverse
                ? (offset - i + palette.Length * 10) % palette.Length
                : (offset + i) % palette.Length;

            string size = i == 0 ? "120%" : "100%";

            result += "<size=" + size + ">";
            result += ColorString(palette[index], ch.ToString());
            result += "</size>";
        }

        result += "</b>";

        return result;
    }
    public static PlayerBodyTypes GetPlayerBodyType(PlayerControl target)
    {
        if (target?.Data == null || target.MyPhysics == null)
            return PlayerBodyTypes.Normal;

        return target.MyPhysics.bodyType;
    }

    public static void SetPlayerBodyType(PlayerControl target, PlayerBodyTypes bodyType)
    {
        if (target?.Data == null || target.MyPhysics == null)
            return;

        target.MyPhysics.SetBodyType(bodyType);
    }

    public static PlayerBodyTypes GetNextBodyType(PlayerControl target)
    {
        if (target?.Data == null || target.MyPhysics == null)
            return PlayerBodyTypes.Normal;

        return target.MyPhysics.bodyType switch
        {
            PlayerBodyTypes.Normal => PlayerBodyTypes.Horse,
            PlayerBodyTypes.Horse => PlayerBodyTypes.Seeker,
            PlayerBodyTypes.Seeker => PlayerBodyTypes.Normal,
            _ => PlayerBodyTypes.Normal
        };
    }

    public static void RefreshPlayerBodyType(PlayerControl target)
    {
        SetPlayerBodyType(target, GetNextBodyType(target));
    }
    public static void CloseMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        List<MeetingHud.VoterState> statesList = [];
        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
        MeetingHud.Instance.Close();
        MeetingHud.Instance.RpcClose();
        GuessManager.CleanupAfterMeeting();
        ExilerManager.CleanupAfterMeeting();
        CloseMeetingManager.CleanupAfterMeeting();
    }
    public static IEnumerator DelayedCloseMeeting()
    {
        while (MeetingHud.Instance == null || !MeetingHud.Instance.gameObject.activeInHierarchy)
            yield return null;

        yield return null;

        yield return new WaitForSeconds(0.1f);

        CloseMeeting();
    }
    public static bool isClientModded = false;
    public static void SendModdedHandshake()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }
        bool isClientModded = true;
        var sender = CustomRpcSender.Create("ModdedHandshakeSender", SendOption.Reliable);
        sender.StartMessage(-1)
              .StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ModdedHandshake)
              .Write($"BanMod {BanMod.PluginVersion}")
              .Write(isClientModded)
              .EndRpc()
              .SendMessage();

    }
    public static class HostOptionStatus
    {
        public static bool ImmortalAdded { get; set; } = false;
        public static bool ImmortalEnabled { get; set; } = false;
        public static bool EngineerEnabled { get; set; } = false;

        public static void UpdateHostRules(bool added, bool enabled, bool engineer)
        {
            ImmortalAdded = added;
            ImmortalEnabled = enabled;
            EngineerEnabled = engineer;
        }
    }
    public static void SendHostTripleBoolRpc()
    {
        if (PlayerControl.LocalPlayer == null || !AmongUsClient.Instance.AmHost || !GameStates.isOnlineGame)
        {
            return;
        }

        bool bool1_ImmortalAdded = ImmortalManager.immortalAssigned;
        bool bool2_ImmortalEnabled = Options.EnableImmortal.GetBool();
        bool bool3_EngineerEnabled = Options.EngineerFixer.GetBool();

        var sender = CustomRpcSender.Create("HostTripleBoolSender", SendOption.Reliable);

        sender.StartMessage(-1)
              .StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.HostTripleBoolUpdate)
              .Write(bool1_ImmortalAdded)
              .Write(bool2_ImmortalEnabled)
              .Write(bool3_EngineerEnabled)
              .EndRpc()
              .SendMessage();

        HostOptionStatus.UpdateHostRules(bool1_ImmortalAdded, bool2_ImmortalEnabled, bool3_EngineerEnabled);
    }
    public static class TemplateLoader
    {
        private static readonly string TemplatesFolder = "./BAN_DATA/TEMPLATE";

        public static void InitTemplates()
        {
            Directory.CreateDirectory(TemplatesFolder);
            //Welcome
            CreateTemplate("WelcomeTemplate",
                "Welcome {player} to BanMod");
            CreateTemplate("WelcomeTemplateSns",
                "Welcome {player} to BanMod\n Here we're playing SNS mode.");
            CreateTemplate("WelcomeTemplateKaitoRun",
                "Welcome {player} to BanMod\n Here we're playing KaitoRun mode.");
            CreateTemplate("WelcomeTemplateTaskRun",
                "Welcome {player} to BanMod\n Here we're playing TaskRun mode.");
            CreateTemplate("WelcomeTemplateJBMode",
                "Welcome {player} to BanMod\n Here we're playing JBMode.");
            CreateTemplate("WelcomeTemplateFFA",
                "Welcome {player} to BanMod\n Here we're playing FFA mode.");
            //Rules
            CreateTemplate("RulesInfo",
                "Add Rules for NormalMod");
            CreateTemplate("RulesInfoSns",
                "Add Rules for SNS");
            CreateTemplate("RulesInfoKaitoRun",
                "Add Rules for KaitoRun");
            CreateTemplate("RulesInfoTaskRun",
                "Add Rules for TaskRun");
            CreateTemplate("RulesInfoJBMode",
                "Add Rules for JBMode");
            CreateTemplate("RulesInfoFFA",
                "Add Rules for FFA");

        }

        private static void CreateTemplate(string name, string content)
        {
            string path = Path.Combine(TemplatesFolder, name + ".txt");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, content);
            }
        }

        public static string LoadTemplate(string templateName)
        {
            string filePath = Path.Combine(TemplatesFolder, templateName + ".txt");

            if (!File.Exists(filePath))
            {
                return $"<color=red>Missing template: {templateName}</color>";
            }

            return File.ReadAllText(filePath).Replace("\\n", "\n");
        }

        private static string ApplyPlaceholders(string template, string playerName = "")
        {
            int level = Options.KickLevelLevel.GetInt();

            return template
                .Replace("{player}", playerName)
                .Replace("{level}", level.ToString());
        }

        public static string FormatTemplate(string templateName, string playerName)
        {
            string template = LoadTemplate(templateName);
            return ApplyPlaceholders(template, playerName);
        }

        public static string FormatTemplate(string templateName)
        {
            string template = LoadTemplate(templateName);
            return ApplyPlaceholders(template);
        }
    }




    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            BMLogger.Error($"Error loading texture from: {path}", "LoadImage");
        }
        return null;
    }
    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream?.CopyTo(ms);
            texture.LoadImage(ms.ToArray(), false);
            return texture;
        }
        catch
        {
            BMLogger.Error($"读入Texture失败：{path}", "LoadImage");
        }
        return null;
    }
    public static Texture2D LoadExternalTexture(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                BMLogger.Warn($"Cursor file not found: {filePath}");
                return null;
            }

            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            if (!texture.LoadImage(fileData))
            {
                BMLogger.Error("LoadImage failed");
                return null;
            }

            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return texture;
        }
        catch (Exception ex)
        {
            BMLogger.Error($"Failed to load external texture: {ex}");
        }

        return null;
    }

    public static class TracersHandler
    {
        private static Dictionary<byte, GameObject> arrows = new();

        public static void drawPlayerArrow(PlayerControl target)
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || target == null || target.Data == null)
                    return;

                if (target == PlayerControl.LocalPlayer) return; 

                if (!target.Data.Role.IsImpostor || target.Data.IsDead)
                    return;
                if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor || PlayerControl.LocalPlayer.Data.IsDead)
                    return;

                if (!arrows.TryGetValue(target.PlayerId, out var arrowObj) || arrowObj == null)
                {
                    arrowObj = new GameObject("ImpostorArrow");
                    var spriteRenderer = arrowObj.AddComponent<SpriteRenderer>();

                    spriteRenderer.sprite = ArrowSprite;
                    spriteRenderer.color = target.Data.Role.TeamColor;
                    spriteRenderer.sortingOrder = 9999;

                    arrows[target.PlayerId] = arrowObj;
                }

                Vector3 localPos = PlayerControl.LocalPlayer.transform.position;
                Vector3 targetPos = target.transform.position;

                Vector3 direction = (targetPos - localPos).normalized;

                Vector3 offset = direction * 0.7f;

                arrowObj.transform.position = localPos + offset;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrowObj.transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);

                arrowObj.SetActive(true);
            }
            catch { }
        }

        public static void HideAllArrows()
        {
            foreach (var arrow in arrows.Values)
            {
                if (arrow != null)
                    arrow.SetActive(false);
            }
        }

        public static Sprite ArrowSprite;


    }
    public static void ShowCommand()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/dn (name)</b></color> {Translator.GetString("Command.dn")}"
            + $"\n  ○ <color=#FF0000><b>/ddn (name)</b></color> {Translator.GetString("Command.ddn")}"
            + $"\n  ○ <color=#FF0000><b>/dw (word)</b></color> {Translator.GetString("Command.dw")}"
            + $"\n  ○ <color=#FF0000><b>/ddw (word)</b></color> {Translator.GetString("Command.ddw")}"
            + $"\n  ○ <color=#FF0000><b>/ds (start)</b></color> {Translator.GetString("Command.ds")}"
            + $"\n  ○ <color=#FF0000><b>/dds (start)</b></color> {Translator.GetString("Command.dds")}"
            + $"\n  ○ <color=#FF0000><b>/f (id)</b></color> {Translator.GetString("Command.f")}"
            + $"\n  ○ <color=#FF0000><b>/df (id)</b></color> {Translator.GetString("Command.df")}"
            ));

    }
    public static void ShowCommand3()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/level (num)</b></color> {Translator.GetString("Command.level")}"
            + $"\n  ○ <color=#FF0000><b>/id</b></color> {Translator.GetString("Command.id")}"
            + $"\n  ○ <color=#FF0000><b>/m</b></color> {Translator.GetString("Command.role")}"
            + $"\n  ○ <color=#FF0000><b>/bm (playercolor)</b></color> {Translator.GetString("Command.bm")}"
            + $"\n  ○ <color=#FF0000><b>/role</b></color> {Translator.GetString("Command.roles")}"
            ));

    }
    public static void ShowCommand4()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  <color=#3535cc><b>Host Command</b></color>"
            + $"\n  ○ <color=#83b325><b>/copy</color><color=#b02eb0> (playercolor)</b></color> : copy outfit"
            + $"\n  ○ <color=#83b325><b>/reset</b></color> : reset outfit"
            + $"\n  ○ <color=#83b325><b>/every</color><color=#b02eb0> (color)</b></color> : all player same color"
            + $"\n  ○ <color=#83b325><b>/setname</color><color=#b02eb0> (id) (name)</b></color> : save the name in customnames.txt"
            + $"\n  ○ <color=#83b325><b>/start</color><color=#b02eb0> (sec)</b></color> : start with sec countdown"
            + $"\n  <color=#3535cc><b>Moderator Command</b></color>"
            + $"\n  ○ <color=#83b325><b>/instantstart</b></color> : start without countdown"
            + $"\n  ○ <color=#83b325><b>/start</color><color=#b02eb0> (sec)</b></color> : start with sec countdown"
            + $"\n  ○ <color=#83b325><b>/meeting</b></color> : call meeting"
            + $"\n  ○ <color=#83b325><b>/every</color><color=#b02eb0> (color)</b></color> : all player same color"
            + $"\n  ○ <color=#83b325><b>/setname</color><color=#b02eb0> (id) (name)</b></color>: save the name in customnames.txt"
            + $"\n  ○ <color=#83b325><b>/destroy</b></color> : destroy lobby"
            + $"\n  ○ <color=#83b325><b>/lobby</b></color> : recreate lobby"
            + $"\n  ○ <color=#83b325><b>/endgame</b></color> : Force GameEnd"
            + $"\n  ○ <color=#83b325><b>/endmeeting</b></color> : Close Meeting"
            + $"\n  ○ <color=#83b325><b>/ban</color><color=#b02eb0> (id)</b></color> : Ban"
            + $"\n  ○ <color=#83b325><b>/Kick</color><color=#b02eb0> (id)</b></color> : Kick"

            + $"\n  ○ <color=#83b325><b>/all</b></color> {Translator.GetString("Command.all")}"
            ));

    }

    public static readonly Dictionary<string, Vector2> DevicePos = new()
    {
        ["SkeldAdmin"] = new(3.48f, -8.62f),
        ["SkeldCamera"] = new(-13.06f, -2.45f),
        ["MiraHQAdmin"] = new(21.02f, 19.09f),
        ["MiraHQDoorLog"] = new(16.22f, 5.82f),
        ["PolusLeftAdmin"] = new(22.80f, -21.52f),
        ["PolusRightAdmin"] = new(24.66f, -21.52f),
        ["PolusCamera"] = new(2.96f, -12.74f),
        ["PolusVital"] = new(26.70f, -15.94f),
        ["DleksAdmin"] = new(-3.48f, -8.62f),
        ["DleksCamera"] = new(13.06f, -2.45f),
        ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
        ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
        ["AirshipCamera"] = new(8.10f, -9.63f),
        ["AirshipVital"] = new(25.24f, -7.94f),
        ["FungleCamera"] = new(6.20f, 0.10f),
        ["FungleVital"] = new(-2.50f, -9.80f)
    };
    public static readonly Dictionary<string, string> colorMap = new()
{
    { "gold2",       "#FFD700" },
    { "silver",      "#C0C0C0" },
    { "bronze",      "#CD7F32" },
    { "copper",      "#B87333" },
    { "platinum",    "#E5E4E2" },
    { "steel",       "#71797E" },
    { "rose_gold",   "#B76E79" },
    { "gunmetal",    "#2A3439" },
    { "chrome",      "#E5E4E2" },

    { "neonyellow",  "#CCFF00" },
    { "electriclime","#CCFF00" },
    { "neongreen",   "#39FF14" },
    { "neonblue",    "#1F51FF" },
    { "neonpink",    "#FF10F0" },
    { "neonorange",  "#FF5F1F" },
    { "neonpurple",  "#BC13FE" },
    { "neonred",     "#FF073A" },
    { "neoncyan",    "#0FF0FC" },
    { "hotpink",     "#FF69B4" },

    { "null",        "#FFFFFF00" },
    { "gold",        "#C6A25A" },
    { "brown2",      "#7F582D" },
    { "beige",       "#B6AA9E" },
    { "bluegreen",   "#00CEC8" },
    { "maize",       "#FBEC5D" },
    { "candy",       "#FF004D" },
    { "wine",        "#722F37" },

    { "white",       "#FFFFFF" },
    { "bianco",      "#FFFFFF" },
    { "weiß",        "#FFFFFF" },
    { "белый",       "#FFFFFF" },
    { "blanc",       "#FFFFFF" },

    { "blu",         "#0000FF" },
    { "blue",        "#0000FF" },
    { "blau",        "#0000FF" },
    { "синий",       "#0000FF" },
    { "bleu",        "#0000FF" },

    { "verde",       "#00FF00" },
    { "green",       "#00FF00" },
    { "grün",        "#00FF00" },
    { "зелёный",     "#00FF00" },
    { "vert",        "#00FF00" },

    { "fucsia",      "#FF00FF" },
    { "fuchsia",     "#FF00FF" },
    { "fuchsie",     "#FF00FF" },
    { "фуксия",      "#FF00FF" },

    { "pink",        "#FFC0CB" },
    { "rosa",        "#FFC0CB" },
    { "confetto",    "#FFC0CB" },
    { "розовый",     "#FFC0CB" },
    { "rose",        "#FFC0CB" },

    { "arancio",     "#FFA500" },
    { "arancione",   "#FFA500" },
    { "orange",      "#FFA500" },
    { "оранжевый",   "#FFA500" },

    { "giallo",      "#FFFF00" },
    { "gialla",      "#FFFF00" },
    { "yellow",      "#FFFF00" },
    { "gelb",        "#FFFF00" },
    { "жёлтый",      "#FFFF00" },
    { "jaune",       "#FFFF00" },

    { "nero",        "#000000" },
    { "nera",        "#000000" },
    { "black",       "#000000" },
    { "schwarz",     "#000000" },
    { "чёрный",      "#000000" },
    { "noir",        "#000000" },

    { "viola",       "#800080" },
    { "purple",      "#800080" },
    { "lila",        "#800080" },
    { "фиолетовый",  "#800080" },
    { "violet",      "#800080" },

    { "marrone",     "#8B4513" },
    { "brown",       "#8B4513" },
    { "braun",       "#8B4513" },
    { "коричневый",  "#8B4513" },
    { "marron",      "#8B4513" },

    { "ciano",       "#00FFFF" },
    { "azzurro",     "#00FFFF" },
    { "azzurra",     "#00FFFF" },
    { "cyan",        "#00FFFF" },
    { "hellblau",    "#00FFFF" },
    { "голубой",     "#00FFFF" },
    { "bleu clair",  "#00FFFF" },

    { "bordo",       "#800000" },
    { "bordeaux",    "#800000" },
    { "maroon",      "#800000" },
    { "kastanienbraun", "#800000" },
    { "бордовый",    "#800000" },

    { "crema",       "#FFFACD" },
    { "cream",       "#FFFACD" },
    { "creme",       "#FFFACD" },
    { "кремовый",    "#FFFACD" },
    { "crème",       "#FFFACD" },
    { "banana",      "#FFFACD" },
    { "banan",       "#FFFACD" },

    { "lime",        "#BFFF00" },
    { "limette",     "#BFFF00" },
    { "лайм",        "#BFFF00" },
    { "citron vert", "#BFFF00" },

    { "grigio",      "#808080" },
    { "grigia",      "#808080" },
    { "gray",        "#808080" },
    { "grau",        "#808080" },
    { "серый",       "#808080" },
    { "gris",        "#808080" },

    { "tortora",     "#D2B48C" },
    { "taupe",       "#D2B48C" },
    { "таупе",       "#D2B48C" },
    { "tan",         "#D2B48C" },

    { "corallo",     "#FF7F50" },
    { "coral",       "#FF7F50" },
    { "koralle",     "#FF7F50" },
    { "коралловый",  "#FF7F50" },
    { "corail",      "#FF7F50" },

    { "rosso",       "#FF0000" },
    { "rossa",       "#FF0000" },
    { "red",         "#FF0000" },
    { "rot",         "#FF0000" },
    { "красный",     "#FF0000" },
    { "rouge",       "#FF0000" }
};
    public static readonly Dictionary<string, string> colorMap1 = new(StringComparer.OrdinalIgnoreCase)
{
    { "white", "white" }, { "bianco", "white" }, { "weiß", "white" },
    { "белый", "white" }, { "blanc", "white" },

    { "black", "black" }, { "nero", "black" }, { "nera", "black" },
    { "schwarz", "black" }, { "чёрный", "black" }, { "noir", "black" },

    { "red", "red" }, { "rosso", "red" }, { "rossa", "red" },
    { "rot", "red" }, { "красный", "red" }, { "rouge", "red" },

    { "green", "green" }, { "verde", "green" }, { "grün", "green" },
    { "зелёный", "green" }, { "vert", "green" },

    { "yellow", "yellow" }, { "giallo", "yellow" }, { "gialla", "yellow" },
    { "gelb", "yellow" }, { "жёлтый", "yellow" }, { "jaune", "yellow" },

    { "blue", "blue" }, { "blu", "blue" }, { "blau", "blue" },
    { "синий", "blue" }, { "bleu", "blue" },

    { "cyan", "cyan" }, { "ciano", "cyan" }, { "azzurro", "cyan" },
    { "azzurra", "cyan" }, { "hellblau", "cyan" }, { "голубой", "cyan" }, { "bleu clair", "cyan" },

    { "magenta", "magenta" }, { "fucsia", "magenta" }, { "fuchsia", "magenta" },
    { "fuchsie", "magenta" }, { "фуксия", "magenta" },

    { "gray", "gray" }, { "grey", "gray" }, { "grigio", "gray" },
    { "grigia", "gray" }, { "grau", "gray" }, { "серый", "gray" }, { "gris", "gray" }
};


    public static readonly Dictionary<string, string> symbolMap = new()
{
    { "umbrella", "☂" },
    { "lines", "ミ" },
    { "lines1", "彡" },
    { "bracket", "『" },
    { "bracket1", "』" },
    { "spades", "♤" },
    { "spades1", "♠︎" },
    { "diamonds1", "♢" },
    { "bracket2", "〘" },
    { "bracket3", "〙" },
    { "cross", "†" },
    { "heart", "♥" },
    { "heart1", "♡" },
    { "infinity", "∞" },
    { "note", "♫" },
    { "note1", "♪" },
    { "star", "★" },
    { "star1", "☆" },
    { "true", "✓" },
    { "ying", "☯" },
    { "warning", "⚠" },
    { "clubs", "♣" },
    { "diamonds", "♦" },
    { "cloud", "☁" },
    { "divider", "┇" },
    { "flower", "✿" },
    { "flower1", "❀" },
    { "sun", "☀" },
    { "smile", "㋡" },
    { "smilea", "㋛" },
    { "smileb", "ッ" },
    { "smilec", "シ" },
    { "smiled", "ツ" },
    { "smilee", "ヅ" },
    { "smilef", "웃" }
};

    //private static int _messageSplitCounter = 0;
    //private static string NormalizeChatMessagePreserveLines(string message, bool removeHtml = true)
    //{
    //    if (string.IsNullOrWhiteSpace(message))
    //        return "";

    //    if (removeHtml)
    //        message = message.RemoveHtmlTags();

    //    message = message
    //        .Replace("\\r\\n", "\n")
    //        .Replace("\\n", "\n")
    //        .Replace("\\r", "\n")
    //        .Replace("\r\n", "\n")
    //        .Replace("\r", "\n");

    //    return message.Trim();
    //}

    //private static PlayerControl GetAvailableModdedProxy()
    //{
    //    try
    //    {
    //        if (BanMod.AllPlayerControls == null)
    //            return null;

    //        return BanMod.AllPlayerControls.FirstOrDefault(p =>
    //            p != null &&
    //            p.Data != null &&
    //            !p.Data.Disconnected &&
    //            !p.Data.IsDead &&
    //            p != PlayerControl.LocalPlayer &&
    //            UnifiedRPCHandlerPatch.IsClientModded(p.PlayerId));
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    //private static PlayerControl GetSelectedModdedProxy()
    //{
    //    return GetAvailableModdedProxy();
    //}

    //private static bool ShouldSendWithProxy(out PlayerControl proxy)
    //{
    //    proxy = GetSelectedModdedProxy();

    //    if (proxy == null)
    //        return false;

    //    bool useProxy = (_messageSplitCounter % 2) == 1;

    //    _messageSplitCounter++;

    //    return useProxy;
    //}

    //public static void RequestProxyMessage(string message, byte target = byte.MaxValue)
    //{
    //    try
    //    {
    //        message = NormalizeChatMessagePreserveLines(message, false);

    //        if (string.IsNullOrWhiteSpace(message))
    //            return;

    //        if (AmongUsClient.Instance == null)
    //            return;

    //        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
    //            return;

    //        if (message.Length > 120)
    //        {
    //            try
    //            {
    //                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
    //                    HudManager.Instance.Notifier,
    //                    $"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{message}\" (Lunghezza: {message.Length})");
    //            }
    //            catch
    //            {
    //            }

    //            return;
    //        }

    //        PlayerControl proxy = GetAvailableModdedProxy();

    //        if (proxy == null)
    //            return;

    //        int proxyClientId = proxy.GetClientId();

    //        if (proxyClientId < 0)
    //            return;

    //        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
    //            PlayerControl.LocalPlayer.NetId,
    //            (byte)CustomRPC.ProxySendChat,
    //            SendOption.Reliable,
    //            proxyClientId);

    //        writer.Write(message);

    //        writer.Write("");

    //        writer.Write(target);

    //        AmongUsClient.Instance.FinishRpcImmediately(writer);

    //    }
    //    catch
    //    {
    //    }
    //}

    //public static class ProxyMessageQueue
    //{
    //    private static readonly Queue<(string msg, int targetClientId, bool sendToAll)> queue = new();

    //    public static void Enqueue(string msg, int targetClientId, bool sendToAll = false)
    //    {
    //        msg = NormalizeChatMessagePreserveLines(msg);

    //        if (string.IsNullOrWhiteSpace(msg))
    //            return;

    //        queue.Enqueue((msg, targetClientId, sendToAll));
    //    }

    //    public static void TrySendNext()
    //    {
    //        if (queue.Count == 0)
    //            return;

    //        if (!MessageBlocker.CanSendMessage())
    //            return;

    //        var localPlayer = PlayerControl.LocalPlayer;

    //        if (localPlayer == null ||
    //            localPlayer.Data == null ||
    //            localPlayer.Data.IsDead ||
    //            localPlayer.Data.Disconnected)
    //        {
    //            return;
    //        }

    //        var (msg, targetClientId, sendToAll) = queue.Peek();

    //        msg = NormalizeChatMessagePreserveLines(msg);

    //        if (string.IsNullOrWhiteSpace(msg))
    //        {
    //            queue.Dequeue();
    //            return;
    //        }

    //        if (msg.Length > 120)
    //        {
    //            queue.Dequeue();
    //            return;
    //        }

    //        try
    //        {
    //            var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);

    //            writer.StartMessage(targetClientId);
    //            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
    //                .Write(msg)
    //                .EndRpc();
    //            writer.EndMessage();
    //            writer.SendMessage();

    //            if (sendToAll)
    //                ShowProxyMessageLocally(localPlayer, msg);

    //            queue.Dequeue();

    //            MessageBlocker.UpdateLastMessageTime();
    //        }
    //        catch
    //        {
    //        }
    //    }

    //    public static void ClearQueue()
    //    {
    //        queue.Clear();
    //    }
    //}

    //private static void ShowProxyMessageLocally(PlayerControl localPlayer, string message)
    //{
    //    try
    //    {
    //        if (localPlayer == null)
    //            return;

    //        message = NormalizeChatMessagePreserveLines(message);

    //        if (string.IsNullOrWhiteSpace(message))
    //            return;

    //        if (HudManager.Instance != null && HudManager.Instance.Chat != null)
    //        {
    //            HudManager.Instance.Chat.AddChat(localPlayer, message);
    //            return;
    //        }
    //    }
    //    catch
    //    {
    //    }

    //    try
    //    {
    //        if (localPlayer == null)
    //            return;

    //        message = NormalizeChatMessagePreserveLines(message);

    //        if (string.IsNullOrWhiteSpace(message))
    //            return;

    //        if (DestroyableSingleton<HudManager>.Instance != null &&
    //            DestroyableSingleton<HudManager>.Instance.Chat != null)
    //        {
    //            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(localPlayer, message);
    //        }
    //    }
    //    catch
    //    {
    //    }
    //}
    //public static void HandleProxySendChatRequest(string message, byte target)
    //{
    //    try
    //    {
    //        PlayerControl localPlayer = PlayerControl.LocalPlayer;

    //        if (localPlayer == null || localPlayer.Data == null)
    //            return;

    //        if (localPlayer.Data.IsDead || localPlayer.Data.Disconnected)
    //            return;

    //        message = NormalizeChatMessagePreserveLines(message);

    //        if (string.IsNullOrWhiteSpace(message))
    //            return;

    //        if (message.Length > 120)
    //            message = message.Substring(0, 120);

    //        bool sendToAll = target == byte.MaxValue || target == 255;
    //        bool targetIsProxySelf = target == localPlayer.PlayerId;

    //        int targetClientId = -1;

    //        if (!sendToAll && !targetIsProxySelf)
    //        {
    //            PlayerControl targetPlayer = Utils.GetPlayerById(target);

    //            if (targetPlayer == null || targetPlayer.Data == null || targetPlayer.Data.Disconnected)
    //                return;

    //            targetClientId = targetPlayer.GetClientId();

    //            if (targetClientId < 0)
    //                return;
    //        }

    //        if (targetIsProxySelf)
    //        {
    //            ShowProxyMessageLocally(localPlayer, message);
    //            MessageBlocker.UpdateLastMessageTime();
    //            return;
    //        }

    //        if (!MessageBlocker.CanSendMessage())
    //        {
    //            ProxyMessageQueue.Enqueue(message, targetClientId, sendToAll);
    //            return;
    //        }

    //        try
    //        {
    //            var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);

    //            writer.StartMessage(targetClientId);
    //            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
    //                .Write(message)
    //                .EndRpc();
    //            writer.EndMessage();
    //            writer.SendMessage();

    //            if (sendToAll)
    //                ShowProxyMessageLocally(localPlayer, message);

    //            MessageBlocker.UpdateLastMessageTime();
    //        }
    //        catch
    //        {
    //        }
    //    }
    //    catch
    //    {
    //    }
    //}

    //public static void SendMessage(string text, byte sendTo = byte.MaxValue)
    //{
    //    text = NormalizeChatMessagePreserveLines(text, false);

    //    if (string.IsNullOrWhiteSpace(text))
    //        return;

    //    if (text.Length > 120)
    //    {
    //        NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
    //            HudManager.Instance.Notifier,
    //            $"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{text}\" (Lunghezza: {text.Length})");

    //        Debug.LogError($"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{text}\" (Lunghezza: {text.Length})");
    //        return;
    //    }

    //    if (sendTo != byte.MaxValue)
    //    {
    //        var targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
    //            p != null &&
    //            p.PlayerId == sendTo &&
    //            p.Data != null &&
    //            !p.Data.Disconnected);

    //        if (targetPlayer == null)
    //            return;
    //    }

    //    if (ShouldSendWithProxy(out PlayerControl proxy) && proxy != null)
    //    {
    //        RequestProxyMessage(text, sendTo);
    //        return;
    //    }

    //    if (!MessageBlocker.CanSendMessage())
    //    {
    //        MessageRetryHandler.QueueMessage(text, sendTo);
    //        MessageRetryHandler.TrySendPending();
    //        return;
    //    }

    //    BanMod.MessagesToSend.Add((text, sendTo));
    //    MessageBlocker.UpdateLastMessageTime();
    //}

    //public static class MessageBlocker
    //{
    //    public static float lastMessageTime = -3.15f;
    //    public static float timeToWait = 3.15f;

    //    public static bool CanSendMessage()
    //    {
    //        return Time.time - lastMessageTime >= timeToWait;
    //    }

    //    public static void UpdateLastMessageTime()
    //    {
    //        lastMessageTime = Time.time;
    //    }

    //    public static void Reset()
    //    {
    //        lastMessageTime = -timeToWait;
    //    }
    //}

    //public static class MessageRetryHandler
    //{
    //    private static readonly object queueLock = new object();
    //    private static Queue<(string text, byte sendTo)> pendingMessages = new();

    //    public static void ClearQueue()
    //    {
    //        lock (queueLock)
    //        {
    //            pendingMessages.Clear();
    //        }
    //    }

    //    public static void TrySendPending()
    //    {
    //        int safetyCounter = 50;

    //        lock (queueLock)
    //        {
    //            while (pendingMessages.Count > 0 && safetyCounter-- > 0)
    //            {
    //                var msg = pendingMessages.Peek();

    //                msg.text = NormalizeChatMessagePreserveLines(msg.text, false);

    //                if (string.IsNullOrWhiteSpace(msg.text))
    //                {
    //                    pendingMessages.Dequeue();
    //                    continue;
    //                }

    //                if (msg.text.Length > 120)
    //                {
    //                    pendingMessages.Dequeue();
    //                    continue;
    //                }

    //                if (msg.sendTo != byte.MaxValue)
    //                {
    //                    var player = BanMod.AllPlayerControls.FirstOrDefault(p =>
    //                        p != null &&
    //                        p.PlayerId == msg.sendTo &&
    //                        p.Data != null &&
    //                        !p.Data.Disconnected);

    //                    if (player == null)
    //                    {
    //                        pendingMessages.Dequeue();
    //                        continue;
    //                    }
    //                }

    //                if (ShouldSendWithProxy(out PlayerControl proxy) && proxy != null)
    //                {
    //                    pendingMessages.Dequeue();
    //                    RequestProxyMessage(msg.text, msg.sendTo);
    //                    continue;
    //                }

    //                if (!MessageBlocker.CanSendMessage())
    //                    break;

    //                pendingMessages.Dequeue();
    //                BanMod.MessagesToSend.Add((msg.text, msg.sendTo));
    //                MessageBlocker.UpdateLastMessageTime();
    //            }
    //        }
    //    }

    //    public static void QueueMessage(string text, byte sendTo)
    //    {
    //        text = NormalizeChatMessagePreserveLines(text, false);

    //        if (string.IsNullOrWhiteSpace(text))
    //            return;

    //        if (text.Length > 120)
    //            return;

    //        lock (queueLock)
    //        {
    //            pendingMessages.Enqueue((text, sendTo));
    //        }
    //    }
    //}

    //public static void AbortAllMessages()
    //{
    //    MessageRetryHandler.ClearQueue();

    //    ProxyMessageQueue.ClearQueue();

    //    BanMod.MessagesToSend.Clear();

    //    _messageSplitCounter = 0;

    //    MessageBlocker.Reset();
    //}
    private static int _messageSplitCounter = 0;
    private static string NormalizeChatMessagePreserveLines(string message, bool removeHtml = true)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "";

        if (removeHtml)
            message = message.RemoveHtmlTags();

        message = message
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n")
            .Replace("\\r", "\n")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        return message.Trim();
    }

    private static bool IsAliveAvailableModdedSender(PlayerControl p)
    {
        try
        {
            return p != null &&
                   p.Data != null &&
                   !p.Data.Disconnected &&
                   !p.Data.IsDead &&
                   UnifiedRPCHandlerPatch.IsClientModded(p.PlayerId);
        }
        catch
        {
            return false;
        }
    }

    private static List<PlayerControl> GetAliveModdedMessageSenders()
    {
        List<PlayerControl> senders = new List<PlayerControl>();

        try
        {
            PlayerControl local = PlayerControl.LocalPlayer;

            // Il local host conta come uno degli sender, se è vivo.
            if (IsAliveAvailableModdedSender(local))
                senders.Add(local);

            if (BanMod.AllPlayerControls != null)
            {
                foreach (PlayerControl p in BanMod.AllPlayerControls
                    .Where(p => IsAliveAvailableModdedSender(p))
                    .OrderBy(p => p.PlayerId))
                {
                    if (local != null && p.PlayerId == local.PlayerId)
                        continue;

                    senders.Add(p);
                }
            }
        }
        catch
        {
        }

        return senders;
    }

    private static PlayerControl GetAvailableModdedProxy()
    {
        try
        {
            return GetAliveModdedMessageSenders()
                .FirstOrDefault(p =>
                    p != null &&
                    PlayerControl.LocalPlayer != null &&
                    p.PlayerId != PlayerControl.LocalPlayer.PlayerId);
        }
        catch
        {
            return null;
        }
    }

    private static PlayerControl GetSelectedModdedProxy()
    {
        return GetAvailableModdedProxy();
    }

    private static bool ShouldSendWithProxy(out PlayerControl proxy)
    {
        proxy = null;

        try
        {
            // La divisione dei messaggi la fa solo l'host.
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return false;

            List<PlayerControl> senders = GetAliveModdedMessageSenders();

            // Se c'è solo il local player, manda normale.
            if (senders == null || senders.Count <= 1)
                return false;

            int index = _messageSplitCounter % senders.Count;
            _messageSplitCounter = (_messageSplitCounter + 1) % 1000000;

            PlayerControl selected = senders[index];

            if (selected == null)
                return false;

            // Se il turno è del local host, non usare proxy.
            if (PlayerControl.LocalPlayer != null &&
                selected.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                return false;
            }

            proxy = selected;
            return true;
        }
        catch
        {
            proxy = null;
            return false;
        }
    }

    public static void RequestProxyMessage(string message, byte target = byte.MaxValue, PlayerControl forcedProxy = null)
    {
        try
        {
            message = NormalizeChatMessagePreserveLines(message, false);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (AmongUsClient.Instance == null)
                return;

            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                return;

            if (message.Length > 120)
            {
                try
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                        HudManager.Instance.Notifier,
                        $"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{message}\" (Lunghezza: {message.Length})");
                }
                catch
                {
                }

                return;
            }

            PlayerControl proxy = forcedProxy;

            if (proxy == null ||
                proxy.Data == null ||
                proxy.Data.Disconnected ||
                proxy.Data.IsDead ||
                proxy == PlayerControl.LocalPlayer)
            {
                proxy = GetAvailableModdedProxy();
            }

            if (proxy == null)
                return;

            int proxyClientId = proxy.GetClientId();

            if (proxyClientId < 0)
                return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ProxySendChat,
                SendOption.Reliable,
                proxyClientId);

            writer.Write(message);

            writer.Write("");

            writer.Write(target);

            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }
        catch
        {
        }
    }

    public static class ProxyMessageQueue
    {
        private static readonly Queue<(string msg, int targetClientId, bool sendToAll)> queue = new();

        public static void Enqueue(string msg, int targetClientId, bool sendToAll = false)
        {
            msg = NormalizeChatMessagePreserveLines(msg);

            if (string.IsNullOrWhiteSpace(msg))
                return;

            queue.Enqueue((msg, targetClientId, sendToAll));
        }

        public static void TrySendNext()
        {
            if (queue.Count == 0)
                return;

            if (!MessageBlocker.CanSendMessage())
                return;

            var localPlayer = PlayerControl.LocalPlayer;

            if (localPlayer == null ||
                localPlayer.Data == null ||
                localPlayer.Data.IsDead ||
                localPlayer.Data.Disconnected)
            {
                return;
            }

            var (msg, targetClientId, sendToAll) = queue.Peek();

            msg = NormalizeChatMessagePreserveLines(msg);

            if (string.IsNullOrWhiteSpace(msg))
            {
                queue.Dequeue();
                return;
            }

            if (msg.Length > 120)
            {
                queue.Dequeue();
                return;
            }

            try
            {
                var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);

                writer.StartMessage(targetClientId);
                writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();

                if (sendToAll)
                    ShowProxyMessageLocally(localPlayer, msg);

                queue.Dequeue();

                MessageBlocker.UpdateLastMessageTime();
            }
            catch
            {
            }
        }

        public static void ClearQueue()
        {
            queue.Clear();
        }
    }

    private static void ShowProxyMessageLocally(PlayerControl localPlayer, string message)
    {
        try
        {
            if (localPlayer == null)
                return;

            message = NormalizeChatMessagePreserveLines(message);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (HudManager.Instance != null && HudManager.Instance.Chat != null)
            {
                HudManager.Instance.Chat.AddChat(localPlayer, message);
                return;
            }
        }
        catch
        {
        }

        try
        {
            if (localPlayer == null)
                return;

            message = NormalizeChatMessagePreserveLines(message);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (DestroyableSingleton<HudManager>.Instance != null &&
                DestroyableSingleton<HudManager>.Instance.Chat != null)
            {
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(localPlayer, message);
            }
        }
        catch
        {
        }
    }
    public static void HandleProxySendChatRequest(string message, byte target)
    {
        try
        {
            PlayerControl localPlayer = PlayerControl.LocalPlayer;

            if (localPlayer == null || localPlayer.Data == null)
                return;

            if (localPlayer.Data.IsDead || localPlayer.Data.Disconnected)
                return;

            message = NormalizeChatMessagePreserveLines(message);

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (message.Length > 120)
                message = message.Substring(0, 120);

            bool sendToAll = target == byte.MaxValue || target == 255;
            bool targetIsProxySelf = target == localPlayer.PlayerId;

            int targetClientId = -1;

            if (!sendToAll && !targetIsProxySelf)
            {
                PlayerControl targetPlayer = Utils.GetPlayerById(target);

                if (targetPlayer == null || targetPlayer.Data == null || targetPlayer.Data.Disconnected)
                    return;

                targetClientId = targetPlayer.GetClientId();

                if (targetClientId < 0)
                    return;
            }

            if (targetIsProxySelf)
            {
                ShowProxyMessageLocally(localPlayer, message);
                MessageBlocker.UpdateLastMessageTime();
                return;
            }

            if (!MessageBlocker.CanSendMessage())
            {
                ProxyMessageQueue.Enqueue(message, targetClientId, sendToAll);
                return;
            }

            try
            {
                var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);

                writer.StartMessage(targetClientId);
                writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                    .Write(message)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();

                if (sendToAll)
                    ShowProxyMessageLocally(localPlayer, message);

                MessageBlocker.UpdateLastMessageTime();
            }
            catch
            {
            }
        }
        catch
        {
        }
    }

    public static void SendMessage(string text, byte sendTo = byte.MaxValue)
    {
        text = NormalizeChatMessagePreserveLines(text, false);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (text.Length > 120)
        {
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                HudManager.Instance.Notifier,
                $"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{text}\" (Lunghezza: {text.Length})");

            Debug.LogError($"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{text}\" (Lunghezza: {text.Length})");
            return;
        }

        if (sendTo != byte.MaxValue)
        {
            var targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null &&
                p.PlayerId == sendTo &&
                p.Data != null &&
                !p.Data.Disconnected);

            if (targetPlayer == null)
                return;
        }

        if (ShouldSendWithProxy(out PlayerControl proxy) && proxy != null)
        {
            RequestProxyMessage(text, sendTo, proxy);
            return;
        }

        if (!MessageBlocker.CanSendMessage())
        {
            MessageRetryHandler.QueueMessage(text, sendTo);
            MessageRetryHandler.TrySendPending();
            return;
        }

        BanMod.MessagesToSend.Add((text, sendTo));
        MessageBlocker.UpdateLastMessageTime();
    }

    public static class MessageBlocker
    {
        public static float lastMessageTime = -3.15f;
        public static float timeToWait = 3.15f;

        public static bool CanSendMessage()
        {
            return Time.time - lastMessageTime >= timeToWait;
        }

        public static void UpdateLastMessageTime()
        {
            lastMessageTime = Time.time;
        }

        public static void Reset()
        {
            lastMessageTime = -timeToWait;
        }
    }

    public static class MessageRetryHandler
    {
        private static readonly object queueLock = new object();
        private static Queue<(string text, byte sendTo)> pendingMessages = new();

        public static void ClearQueue()
        {
            lock (queueLock)
            {
                pendingMessages.Clear();
            }
        }

        public static void TrySendPending()
        {
            int safetyCounter = 50;

            lock (queueLock)
            {
                while (pendingMessages.Count > 0 && safetyCounter-- > 0)
                {
                    var msg = pendingMessages.Peek();

                    msg.text = NormalizeChatMessagePreserveLines(msg.text, false);

                    if (string.IsNullOrWhiteSpace(msg.text))
                    {
                        pendingMessages.Dequeue();
                        continue;
                    }

                    if (msg.text.Length > 120)
                    {
                        pendingMessages.Dequeue();
                        continue;
                    }

                    if (msg.sendTo != byte.MaxValue)
                    {
                        var player = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null &&
                            p.PlayerId == msg.sendTo &&
                            p.Data != null &&
                            !p.Data.Disconnected);

                        if (player == null)
                        {
                            pendingMessages.Dequeue();
                            continue;
                        }
                    }

                    if (ShouldSendWithProxy(out PlayerControl proxy) && proxy != null)
                    {
                        pendingMessages.Dequeue();
                        RequestProxyMessage(msg.text, msg.sendTo, proxy);
                        continue;
                    }

                    if (!MessageBlocker.CanSendMessage())
                        break;

                    pendingMessages.Dequeue();
                    BanMod.MessagesToSend.Add((msg.text, msg.sendTo));
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
        }

        public static void QueueMessage(string text, byte sendTo)
        {
            text = NormalizeChatMessagePreserveLines(text, false);

            if (string.IsNullOrWhiteSpace(text))
                return;

            if (text.Length > 120)
                return;

            lock (queueLock)
            {
                pendingMessages.Enqueue((text, sendTo));
            }
        }
    }

    public static void AbortAllMessages()
    {
        MessageRetryHandler.ClearQueue();

        ProxyMessageQueue.ClearQueue();

        BanMod.MessagesToSend.Clear();

        _messageSplitCounter = 0;

        MessageBlocker.Reset();
    }

    public static string NumberToWords(int number)
    {
        if (number < 0) return Translator.GetString("minus") + " " + NumberToWords(Math.Abs(number));
        if (number == 0) return Translator.GetString("zero");

        SupportedLangs currentLang = Translator.GetUserTrueLang();

        string[] words = {
            Translator.GetString("zero"), Translator.GetString("one"), Translator.GetString("two"),
            Translator.GetString("three"), Translator.GetString("four"), Translator.GetString("five"),
            Translator.GetString("six"), Translator.GetString("seven"), Translator.GetString("eight"),
            Translator.GetString("nine"), Translator.GetString("ten"), Translator.GetString("eleven"),
            Translator.GetString("twelve"), Translator.GetString("thirteen"), Translator.GetString("fourteen"),
            Translator.GetString("fifteen"), Translator.GetString("sixteen"), Translator.GetString("seventeen"),
            Translator.GetString("eighteen"), Translator.GetString("nineteen")
        };

        string[] tens = {
            "", "", Translator.GetString("twenty"), Translator.GetString("thirty"),
            Translator.GetString("forty"), Translator.GetString("fifty"), Translator.GetString("sixty"),
            Translator.GetString("seventy"), Translator.GetString("eighty"), Translator.GetString("ninety")
        };

        if (number < 20)
        {
            if (currentLang == SupportedLangs.French && number >= 17)
            {
                string unitPartKey = "";
                switch (number % 10)
                {
                    case 7: unitPartKey = "seven"; break;
                    case 8: unitPartKey = "eight"; break;
                    case 9: unitPartKey = "nine"; break;
                }
                return Translator.GetString("ten") + "-" + Translator.GetString(unitPartKey);
            }
            return words[number];
        }

        if (number < 100)
        {
            int tensDigit = number / 10;
            int unitDigit = number % 10;
            string result = tens[tensDigit]; 

            if (unitDigit > 0) 
            {
                switch (currentLang)
                {
                    case SupportedLangs.Italian:
                        if (unitDigit == 1) result = result.Substring(0, result.Length - 1) + Translator.GetString("one");
                        else if (unitDigit == 8) result = result.Substring(0, result.Length - 1) + Translator.GetString("eight");
                        else if (unitDigit == 3) result = result.Substring(0, result.Length - 1) + Translator.GetString("three");
                        else result += words[unitDigit]; 
                        break;

                    case SupportedLangs.English:
                        result += "-" + words[unitDigit];
                        break;

                    case SupportedLangs.French:
                        if (tensDigit == 7) 
                        {
                            result = tens[6];
                            if (unitDigit == 1) result += " " + Translator.GetString("french_and_eleven"); 
                            else result += "-" + words[10 + unitDigit]; 
                        }
                        else if (tensDigit == 8 && unitDigit == 0) 
                        {
                        }
                        else if (tensDigit == 8) 
                        {
                            result += "-" + words[unitDigit];
                        }
                        else if (tensDigit == 9)
                        {
                            result = tens[8]; 
                            result += "-" + words[10 + unitDigit]; 
                        }
                        else 
                        {
                            if (unitDigit == 1) result += " " + Translator.GetString("french_and_one"); 
                            else result += "-" + words[unitDigit];
                        }
                        break;

                    case SupportedLangs.German:
                        if (unitDigit == 0)
                        {
                        }
                        else
                        {
                            string unitWord = (unitDigit == 1) ? Translator.GetString("german_unit_one") : words[unitDigit];
                            result = unitWord + Translator.GetString("german_and") + tens[tensDigit];
                        }
                        break;

                    case SupportedLangs.Russian:
                        result += " " + words[unitDigit]; 
                        break;

                    default:
                        result = number.ToString();
                        break;
                }
            }
            return result;
        }

        return number.ToString();
    }

    public static Dictionary<byte, float> playerDeathTimes = new Dictionary<byte, float>();
    public static List<PlayerControl> AllPlayerControls; 
    public static float MeetingStartTime = 0f;
    public static bool Scientist(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Scientist;
    }
    public static bool Angel(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.GuardianAngel;
    }
    public static bool Engineer(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Engineer;
    }
    public static bool Detective(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Detective;
    }
    public static bool Noisemaker(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Noisemaker;
    }
    public static bool Cobra(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Viper;
    }
    public static bool Tracker(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Tracker;
    }
    public static bool Impostor(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Impostor;
    }
    public static bool IsShapeshifted(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player != null && player.shapeshiftTargetPlayerId != -1;
    }
    public static bool Shapeshifter(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Shapeshifter;
    }
    public static bool Phantom(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Phantom;
    }
    public static bool Crewmate(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.RoleType == RoleTypes.Crewmate;
    }
    public static bool ImpostorTeam(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.Role.TeamType == RoleTeamTypes.Impostor;
    }
    public static bool CrewTeam(PlayerControl player)
    {
        if (player == null) return false;
        if (player.Data == null) return false;
        if (player.Data.Role == null) return false;
        return player.Data.Role.TeamType == RoleTeamTypes.Crewmate;
    }
    public static void OnPlayerDeath(PlayerControl player)
    {
        if (player != null)
        {
            BanMod.playerDeathTimes[player.PlayerId] = Time.time;
        }
    }

    public static class SabotageManager
    {
        public static bool IsSabotageActive = false;

        public static float GameSabotageCooldownRemaining = 0f;

        public static bool TryActivateSabotage(SystemTypes sabotageType, byte value, bool closeCafeteriaDoors = true)
        {
            if (IsSabotageActive)
            {
                return false;
            }

            IsSabotageActive = true;


            if (closeCafeteriaDoors)
            {
                ShipStatus.Instance.RpcCloseDoorsOfType(SystemTypes.Cafeteria);
            }

            ShipStatus.Instance.RpcUpdateSystem(sabotageType, value);

            return true;
        }
        public static void SetSabotageActiveState(bool active)
        {
            IsSabotageActive = active;
        }

        public static void SetGameSabotageCooldown(float remainingTime)
        {
            GameSabotageCooldownRemaining = remainingTime;
        }
    }

    public static void Exeme()
    {
        PlayerControl playerToExile = PlayerControl.LocalPlayer;

        if (playerToExile == null || !AmongUsClient.Instance.AmHost) return;

        NetworkedPlayerInfo playerToExileInfo = GameData.Instance.GetPlayerById(playerToExile.PlayerId);

        List<MeetingHud.VoterState> statesList = new();

        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), playerToExileInfo, false);
        MeetingHud.Instance.Close();
        MeetingHud.Instance.RpcClose();
    }
    public static ClientData GetClientById(int id)
    {
        try { return AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == id); }
        catch { return null; }
    }
    public static unsafe class FastDestroyableSingleton<T> where T : MonoBehaviour
    {
        private static readonly IntPtr FieldPtr;
        private static readonly Func<IntPtr, T> CreateObject;
        static FastDestroyableSingleton()
        {
            FieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DestroyableSingleton<T>>.NativeClassPtr, nameof(DestroyableSingleton<T>._instance));
            var constructor = typeof(T).GetConstructor([typeof(IntPtr)]);
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            CreateObject = lambda.Compile();
        }
        public static T Instance
        {
            get
            {
                IntPtr objectPointer;
                IL2CPP.il2cpp_field_static_get_value(FieldPtr, &objectPointer);
                return objectPointer == IntPtr.Zero ? DestroyableSingleton<T>.Instance : CreateObject(objectPointer);
            }
        }
    }
    public static bool fullBrightActive()
    {

        return GameStates.IsDead || Camera.main.orthographicSize > 3f || Camera.main.gameObject.GetComponent<FollowerCamera>().Target != PlayerControl.LocalPlayer;
    }
    public static bool IsPlayerActive(byte playerId)
    {
        return BanMod.AllPlayerControls.Any(p => p.PlayerId == playerId && !p.Data.IsDead);
    }
    public static bool chatUiActive()
    {
        try
        {
            bool isImpostor = PlayerControl.LocalPlayer != null &&
                              PlayerControl.LocalPlayer.Data != null &&
                              PlayerControl.LocalPlayer.Data.Role.TeamType == RoleTeamTypes.Impostor;

            bool chatOffIfImpostorEnabled = BanMod.ChatOffIfImpostor.Value;

            bool activeChatValue = BanMod.AktiveChat.Value;

            if (isImpostor && chatOffIfImpostorEnabled)
            {
                activeChatValue = false;
            }

            return activeChatValue || MeetingHud.Instance || !ShipStatus.Instance || PlayerControl.LocalPlayer.Data.IsDead;
        }
        catch
        {
            return false;
        }
    }
    public static void openChat()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.chatScreen.SetActive(true);
            PlayerControl.LocalPlayer.NetTransform.Halt();
            DestroyableSingleton<HudManager>.Instance.Chat.StartCoroutine(DestroyableSingleton<HudManager>.Instance.Chat.CoOpen());
            if (DestroyableSingleton<FriendsListManager>.InstanceExists)
            {
                DestroyableSingleton<FriendsListManager>.Instance.SetFriendButtonColor(true);
            }
        }

    }
    public static void closeChat()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
        }

    }

    public static string getColoredPingText(int ping)
    {

        if (ping <= 100)
        { 
            return $"<color=#00ff00ff>PING: {ping} ms</color>";
        }
        else if (ping < 400)
        { 
            return $"<color=#ffff00ff>PING: {ping} ms</color>";
        }
        else
        { 
            return $"<color=#ff0000ff>PING: {ping} ms</color>";
        }
    }
    public static bool IsVip(string friendCode)
    {
        return AllowedManager.IsVip(friendCode);
    }

    public static bool IsModerator(string friendCode)
    {
        return AllowedManager.IsModerator(friendCode);
    }
    private static string TryRemove(this string text) => text.Length >= 1200 ? text.Remove(0, 1200) : string.Empty;
    public class PlayerState(byte playerId)
    {
        public readonly byte PlayerId = playerId;
        public bool IsDead { get; set; } = false;
        public bool Disconnected { get; set; } = false;
        public NetworkedPlayerInfo.PlayerOutfit NormalOutfit { get; internal set; }
    }
    public static PlayerControl GetPlayerById(int PlayerId, bool fast = true)
    {

        if (PlayerId is > byte.MaxValue or < byte.MinValue) return null;
        return BanMod.AllPlayerControls.FirstOrDefault(x => x.PlayerId == PlayerId);
    }

    public static byte MsgToColor(string text, bool isHost = false)
    {
        text = text.ToLowerInvariant();
        text = text.Replace("色", string.Empty);
        int color;
        try { color = int.Parse(text); } catch { color = -1; }
        switch (text)
        {
            case "0":
            case "rosso":
            case "Rosso":
            case "rouge":
            case "Rouge":
            case "red":
            case "Red":
            case "rot":
            case "Rot":
            case "красный":
            case "Красный":
                color = 0; break;
            case "1":
            case "blu":
            case "Blu":
            case "bleu":
            case "Bleu":
            case "blue":
            case "Blue":
            case "blau":
            case "Blau":
            case "синий":
            case "Синий":
                color = 1; break;
            case "2":
            case "verde":
            case "Verde":
            case "vert":
            case "Vert":
            case "green":
            case "Green":
            case "grün":
            case "Grün":
            case "зеленый":
            case "Зеленый":
                color = 2; break;
            case "3":
            case "rosa":
            case "Rosa":
            case "pink":
            case "Pink":
            case "fucsia":
            case "Fucsia":
            case "розовый":
            case "Розовый":
                color = 3; break;
            case "4":
            case "arancione":
            case "Arancione":
            case "orange":
            case "Orange":
            case "arancio":
            case "Arancio":
            case "оранжевый":
            case "Оранжевый":
                color = 4; break;
            case "5":
            case "giallo":
            case "Giallo":
            case "jaune":
            case "Jaune":
            case "yellow":
            case "Yellow":
            case "gelb":
            case "Gelb":
            case "желтый":
            case "Желтый":
                color = 5; break;
            case "6":
            case "nero":
            case "Nero":
            case "noir":
            case "Noir":
            case "black":
            case "Black":
            case "schwarz":
            case "Schwarz":
            case "черный":
            case "Черный":
                color = 6; break;
            case "7":
            case "bianco":
            case "Bianco":
            case "blanc":
            case "Blanc":
            case "white":
            case "White":
            case "weiss":
            case "Weiss":
            case "белый":
            case "Белый":
                color = 7; break;
            case "8":
            case "viola":
            case "Viola":
            case "violet":
            case "Violet":
            case "purple":
            case "Purple":
            case "lila":
            case "Lila":
            case "фиолетовый":
            case "Фиолетовый":
                color = 8; break;
            case "9":
            case "marrone":
            case "Marrone":
            case "marron":
            case "Marron":
            case "brown":
            case "Brown":
            case "braun":
            case "Braun":
            case "коричневый":
            case "Коричневый":
                color = 9; break;
            case "10":
            case "ciano":
            case "Ciano":
            case "cyan":
            case "Cyan":
            case "голубой":
            case "Голубой":
                color = 10; break;
            case "11":
            case "lime":
            case "Lime":
            case "лайм":
            case "Лайм":
                color = 11; break;
            case "12":
            case "bordeaux":
            case "Bordeaux":
            case "bordo":
            case "Bordo":
            case "maroon":
            case "Maroon":
            case "бордовый":
            case "Бордовый":
                color = 12; break;
            case "13":
            case "confetto":
            case "Confetto":
            case "rose":
            case "Rose":
                color = 13; break;
            case "14":
            case "banana":
            case "Banana":
            case "banane":
            case "Banane":
            case "crema":
            case "Crema":
            case "банановый":
            case "Банановый":
                color = 14; break;
            case "15":
            case "grigio":
            case "Grigio":
            case "gris":
            case "Gris":
            case "gray":
            case "Gray":
            case "grau":
            case "Grau":
            case "серый":
            case "Серый":
                color = 15; break;
            case "16":
            case "beige":
            case "Beige":
            case "Tortora":
            case "tortora":
            case "tan":
            case "Tan":
            case "бежевый":
            case "Бежевый":
                color = 16; break;
            case "17":
            case "corallo":
            case "Corallo":
            case "corail":
            case "Corail":
            case "coral":
            case "Coral":
            case "koralle":
            case "Koralle":
            case "коралловый":
            case "Коралловый":
                color = 17; break;

            case "18": case "隐藏": case "?": color = 18; break;
        }
        if (color == 18)
            return byte.MaxValue;
        return !isHost && color == 18 ? byte.MaxValue : color is < 0 or > 18 ? byte.MaxValue : Convert.ToByte(color);
    }

    public static string ColorIdToName(int colorId)
    {
        string key = colorId switch
        {
            0 => "ColorRed",
            1 => "ColorBlue",
            2 => "ColorGreen",
            3 => "ColorPink",
            4 => "ColorOrange",
            5 => "ColorYellow",
            6 => "ColorBlack",
            7 => "ColorWhite",
            8 => "ColorPurple",
            9 => "ColorBrown",
            10 => "ColorCyan",
            11 => "ColorLime",
            12 => "ColorBordeaux",
            13 => "ColorConfetto",
            14 => "ColorBanana",
            15 => "ColorGray",
            16 => "ColorBeige",
            17 => "ColorCoral",
            18 => "ColorHidden",
            _ => "ColorRed"
        };

        return Translator.GetString(key);
    }
    public static Color ColorIdToColor(int colorId)
    {
        return colorId switch
        {
            0 => Color.red,
            1 => Color.blue,
            2 => Color.green,
            3 => new Color(1f, 0.4f, 0.7f), 
            4 => new Color(1f, 0.5f, 0f),   
            5 => Color.yellow,
            6 => Color.black,
            7 => Color.white,
            8 => new Color(0.5f, 0f, 0.5f), 
            9 => new Color(0.36f, 0.26f, 0.2f), 
            10 => Color.cyan,
            11 => new Color(0.5f, 1f, 0f),
            12 => new Color(0.5f, 0f, 0f), 
            13 => new Color(1f, 0.8f, 0.9f), 
            14 => new Color(1f, 1f, 0.5f), 
            15 => Color.gray,
            16 => new Color(0.96f, 0.96f, 0.86f), 
            17 => new Color(1f, 0.5f, 0.31f), 
            18 => new Color(0.2f, 0.2f, 0.2f), 
            _ => Color.red
        };
    }
    public static void SendEngineerMessage()
    {
        var engineerPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Engineer);

        if (engineerPlayer == null)
            return;

        byte engineerId = engineerPlayer.PlayerId;

        string msg = GetString("EngineerMessage");

        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, engineerId); 
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, engineerId);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendShapeshifterMessage()
    {
        var ShapeshifterPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Shapeshifter);

        if (ShapeshifterPlayer == null)
            return;

        byte ShapeshifterId = ShapeshifterPlayer.PlayerId;

        string msg = string.Format(GetString("ShapeshifterMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, ShapeshifterId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, ShapeshifterId);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendPhantomMessage()
    {
        var PhantomPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Phantom);

        if (PhantomPlayer == null)
            return;

        byte PhantomId = PhantomPlayer.PlayerId;
        string msg = string.Format(GetString("PhantomMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendPhantomNBMessage()
    {
        var PhantomPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Phantom);

        if (PhantomPlayer == null)
            return;

        byte PhantomId = PhantomPlayer.PlayerId;
        string msg = string.Format(GetString("PhantomNBMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static bool AnySabotageIsActive()
   => IsActive(SystemTypes.Electrical)
      || IsActive(SystemTypes.Comms)
      || IsActive(SystemTypes.MushroomMixupSabotage)
      || IsActive(SystemTypes.Laboratory)
      || IsActive(SystemTypes.LifeSupp)
      || IsActive(SystemTypes.Reactor)
      || IsActive(SystemTypes.HeliSabotage);

    public static bool IsActive(SystemTypes type)
    {

        if (!ShipStatus.Instance.Systems.ContainsKey(type))
        {
            return false;
        }

        switch (type)
        {
            case SystemTypes.Electrical:
                {
                    var SwitchSystem = ShipStatus.Instance.Systems[type].TryCast<SwitchSystem>();
                    return SwitchSystem != null && SwitchSystem.IsActive;
                }
            case SystemTypes.Reactor:
                {
                    {
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].TryCast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                }
            case SystemTypes.Laboratory:
                {
                    var ReactorSystemType = ShipStatus.Instance.Systems[type].TryCast<ReactorSystemType>();
                    return ReactorSystemType != null && ReactorSystemType.IsActive;
                }
            case SystemTypes.HeliSabotage:
                {
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                }
            case SystemTypes.LifeSupp:
                {
                    var LifeSuppSystemType = ShipStatus.Instance.Systems[type].TryCast<LifeSuppSystemType>();
                    return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                }
            case SystemTypes.Comms:
                {
                    var hqHud = ShipStatus.Instance.Systems[type].TryCast<HqHudSystemType>();
                    var hudOverride = ShipStatus.Instance.Systems[type].TryCast<HudOverrideSystemType>();

                    return (hqHud != null && hqHud.IsActive) || (hudOverride != null && hudOverride.IsActive);
                }
            case SystemTypes.MushroomMixupSabotage:
                {
                    var MushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                    return MushroomMixupSabotageSystem != null && MushroomMixupSabotageSystem.IsActive;
                }
            default:
                return false;
        }
    }
    public static class VoteContextManager
    {
        public static bool IsForcedVote = false;
    }
    public static string ToFullWidthNumbers(string input)
    {
        return new string(input.Select(c =>
        {
            if (char.IsDigit(c))
                return (char)('０' + (c - '0'));
            return c;
        }).ToArray());
    }

}

public static class MatchSummary1
{
    public static bool ImpostorWin = false;
    public static bool CrewmateWin = false;
    public static bool JesterWin = false;
    public static bool TaskWin = false;
    public static List<string> ReportHistory = new List<string>();
    public static string ImpostorName = "";
    public static string TaskWinnerName = "";
    public static string JesterName = "";
    private static float MatchStartTime = 0f;
    private static float MatchEndTime = 0f;
    private static bool MatchTimerRunning = false;
    public static void StartMatchTimer()
    {
        MatchStartTime = UnityEngine.Time.realtimeSinceStartup;
        MatchEndTime = 0f;
        MatchTimerRunning = true;
    }

    public static void StopMatchTimer()
    {
        if (!MatchTimerRunning)
            return;

        MatchEndTime = UnityEngine.Time.realtimeSinceStartup;
        MatchTimerRunning = false;
    }

    public static string GetMatchTime()
    {
        float totalTime = MatchTimerRunning
            ? UnityEngine.Time.realtimeSinceStartup - MatchStartTime
            : MatchEndTime - MatchStartTime;

        int minutes = (int)(totalTime / 60f);
        int seconds = (int)(totalTime % 60f);

        return $"{minutes:D2}:{seconds:D2}";
    }
    public static void Reset()
    {
        ImpostorWin = false;
        CrewmateWin = false;
        JesterWin = false;
        TaskWin = false;
        ImpostorName = "";
        TaskWinnerName = "";
        JesterName = "";
        ReportHistory.Clear();
        MatchStartTime = 0f;
        MatchEndTime = 0f;
        MatchTimerRunning = false;
    }

    public static string GetSummaryReport()
    {
        var report = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(ImmortalManager.LastImmortalPlayerName))
        {
            report.AppendLine(string.Format(GetString("ImmortalPlayerReport"), ImmortalManager.LastImmortalPlayerName));
        }
        if (TaskWin)
        {
            string TaskWinnerName = TaskManager.WinnerName;
            float winnerTime = TaskManager.WinnerTotalTime;

            report.AppendLine(GetString("TaskWins"));
            report.AppendLine($"{GetString("WinnerIs")}: {TaskWinnerName}");
            int minutes = (int)(winnerTime / 60);
            int seconds = (int)(winnerTime % 60);
            string timeFormatted = $"{minutes:D2}:{seconds:D2}";

            report.AppendLine($"{GetString("ScientistPlayerDiedTime")}: {ToFullWidthNumbers(timeFormatted)}");
        }
        else if (JesterWin)
        {
            string JesterName = PreviousMatchPopupTracker.JesterName;
            report.AppendLine($"{GetString("JesterWins")}: {JesterName}");
            int totalDone = 0;
            int totalTasks = 0;
            foreach (var data in TaskTracker.GetAllTaskData())
            {
                totalDone += data.Done;
                totalTasks += data.Total;
            }

            report.AppendLine($"Task: {ToFullWidthNumbers(totalDone.ToString())} / {ToFullWidthNumbers(totalTasks.ToString())}");
        }
        else if (ImpostorWin)
        {
            report.AppendLine(GetString("ImpostorWins"));
            var impostors = ImpostorTracker.GetImpostors();
            if (impostors.Count == 0)
            {
                report.AppendLine(GetString("NoImpostorsPresent"));
            }
            else
            {
                foreach (var imp in impostors)
                {
                    int kills = KillTracker.GetKills(imp.PlayerId);
                    report.AppendLine($"{imp.PlayerName}: {ToFullWidthNumbers(kills.ToString())} kill");
                }
            }
            int totalDone = 0;
            int totalTasks = 0;
            foreach (var data in TaskTracker.GetAllTaskData())
            {
                totalDone += data.Done;
                totalTasks += data.Total;
            }

            report.AppendLine($"Task: {ToFullWidthNumbers(totalDone.ToString())} / {ToFullWidthNumbers(totalTasks.ToString())}");
        }
        else if (CrewmateWin)
        {
            report.AppendLine(GetString("CrewmateWins"));
            var impostors = ImpostorTracker.GetImpostors();
            if (impostors.Count == 0)
            {
                report.AppendLine(GetString("NoImpostorsPresent"));
            }
            else
            {
                foreach (var imp in impostors)
                {
                    int kills = KillTracker.GetKills(imp.PlayerId);
                    report.AppendLine($"{imp.PlayerName}: {ToFullWidthNumbers(kills.ToString())} kill");
                }
            }
            int totalDone = 0;
            int totalTasks = 0;
            foreach (var data in TaskTracker.GetAllTaskData())
            {
                totalDone += data.Done;
                totalTasks += data.Total;
            }

            report.AppendLine($"Task: {ToFullWidthNumbers(totalDone.ToString())} / {ToFullWidthNumbers(totalTasks.ToString())}");
        }
        report.AppendLine($"Match Time: {ToFullWidthNumbers(GetMatchTime())}");
        return report.ToString();
    }

    public static void SaveToHistory()
    {
        var report = GetSummaryReport();
        if (!string.IsNullOrWhiteSpace(report))
        {
            ReportHistory.Add(report);
        }
    }

    public static string GetLastSavedReport()
    {
        return ReportHistory.LastOrDefault();
    }
}
public static class TaskTracker
{
    public class TaskData
    {
        public byte PlayerId;
        public string Name;
        public int Done;
        public int Total;
    }

    public static Dictionary<byte, TaskData> TaskState = new Dictionary<byte, TaskData>();

    public static void UpdatePlayerTask(PlayerControl player)
    {
        if (player?.Data == null || player.Data.Tasks == null)
            return;
        if (player.Data.Role?.TeamType == RoleTeamTypes.Impostor)
            return;
        int total = player.Data.Tasks.Count; 
        int done = 0;
        foreach (var task in player.Data.Tasks)
        {
            if (task != null && task.Complete)
                done++;
        }

        TaskState[player.PlayerId] = new TaskData
        {
            PlayerId = player.PlayerId,
            Name = player.Data.PlayerName,
            Done = done,
            Total = total
        };
        PreviousMatchPopupTracker.UpdatePlayerTask(player);
    }

    public static void Clear() => TaskState.Clear();

    public static List<TaskData> GetAllTaskData() => TaskState.Values.ToList();
}
public static class ImpostorTracker
{
    public class ImpostorData
    {
        public byte PlayerId;
        public string PlayerName;
    }

    private static readonly List<ImpostorData> impostors = new();

    public static void DetectImpostors()
    {
        impostors.Clear();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!GameStates.isHideNSeek)
            {
                if (player?.Data?.Role?.TeamType == RoleTeamTypes.Impostor)
                {
                    impostors.Add(new ImpostorData
                    {
                        PlayerId = player.PlayerId,
                        PlayerName = player.Data.PlayerName
                    });
                }
            }
            else
            {
                if (player.Data.RoleType == RoleTypes.Impostor)
                {
                    impostors.Add(new ImpostorData
                    {
                        PlayerId = player.PlayerId,
                        PlayerName = player.Data.PlayerName
                    });
                }
            }
        }
    }
    public static List<ImpostorData> GetImpostors() => new(impostors);

    public static void Clear() => impostors.Clear();

    public static bool IsImpostor(byte playerId) => impostors.Any(i => i.PlayerId == playerId);
}
public static class KillTracker
{
    private static readonly Dictionary<byte, int> kills = new();

    public static void RegisterKill(byte killerId)
    {

        if (!kills.ContainsKey(killerId))
            kills[killerId] = 0;

        kills[killerId]++;
    }

    public static int GetKills(byte playerId)
    {
        return kills.TryGetValue(playerId, out var count) ? count : 0;
    }

    public static void Clear() => kills.Clear();
}

//public class PositionLogger : MonoBehaviour
//{
//    private List<Vector2> savedPositions = new List<Vector2>();

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.F2))
//        {
//            if (PlayerControl.LocalPlayer != null)
//            {
//                Vector2 pos = PlayerControl.LocalPlayer.transform.position;
//                savedPositions.Add(pos);
//                BMLogger.Info($"[PositionLogger] Saved position: {pos}");
//            }
//        }

//        if (Input.GetKeyDown(KeyCode.F3))
//        {
//            BMLogger.Info($"[PositionLogger] --------- SAVED POSITIONS ---------");
//            StringBuilder sb = new StringBuilder();
//            foreach (Vector2 pos in savedPositions)
//            {
//                sb.AppendLine($"new Vector2({pos.x}f, {pos.y}f),");
//            }
//            BMLogger.Info(sb.ToString());
//        }

//    }
//}

public class SpawnProtectionChecker : MonoBehaviour
{
    private SpawnZoneManager zoneManager;
    public MapNames currentMap;

    private void Start()
    {
        zoneManager = new SpawnZoneManager();

        if (ShipStatus.Instance == null)
        {
            currentMap = (MapNames)(-1);  
            return;
        }

        currentMap = GetCurrentMapFromOptions();
    }

    private void Update()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (gameMode == GameModeType.KaitoRun)
        {
            if (currentMap == (MapNames)(-1) && ShipStatus.Instance != null)
            {
                currentMap = GetCurrentMapFromOptions();
            }

            if (Time.time > Block.ShieldEndTime) return;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead) continue;

                Vector2 pos = player.GetTruePosition();
                bool isInSpawnZone = zoneManager.IsPlayerInAnyZone(pos, currentMap);
                bool isProtected = BanMod.ShieldedPlayers.Contains(player.PlayerId);

                if (isInSpawnZone && !isProtected && !GameStates.isLobby)
                {
                    BanMod.ShieldedPlayers.Add(player.PlayerId);
                }
                else if (!isInSpawnZone && isProtected)
                {
                    BanMod.ShieldedPlayers.Remove(player.PlayerId);
                    player.RemoveProtection();
                    player.protectedByGuardianId = -1;
                    player.Data.MarkDirty();
                    AFKDetector.EnsureTrackedPlayers();
                }
            }
        }
    }

}
public class SpawnProtectionChecker1 : MonoBehaviour
{
    private SpawnZoneManager zoneManager;
    public MapNames currentMap;

    private void Start()
    {
        zoneManager = new SpawnZoneManager();

        if (ShipStatus.Instance == null)
        {
            currentMap = (MapNames)(-1);  
            return;
        }

        currentMap = GetCurrentMapFromOptions();
    }

    private void Update()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        if (!Options.Protection10Sec.GetBool()) return;
        if (Options.Protection10Sec.GetBool())
        {
            if (currentMap == (MapNames)(-1) && ShipStatus.Instance != null)
            {
                currentMap = GetCurrentMapFromOptions();
            }

            if (Time.time > Block.ShieldEndTime) return;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead) continue;

                Vector2 pos = player.GetTruePosition();
                bool isInSpawnZone = zoneManager.IsPlayerInAnyZone(pos, currentMap);
                bool isProtected = BanMod.ShieldedPlayers.Contains(player.PlayerId);
                bool hasLostProtection = Block.PlayersLostProtection.Contains(player.PlayerId);

                if (isInSpawnZone && !isProtected && !hasLostProtection)
                {
                    if (Block.InitialProtectedPlayers.Contains(player.PlayerId) && !GameStates.isLobby)
                    {
                        BanMod.ShieldedPlayers.Add(player.PlayerId);
                    }
                }
                else if (!isInSpawnZone && isProtected)
                {
                    BanMod.ShieldedPlayers.Remove(player.PlayerId);
                    player.RemoveProtection();
                    player.protectedByGuardianId = -1;
                    player.Data.MarkDirty();
                    Block.PlayersLostProtection.Add(player.PlayerId); 

                    AFKDetector.EnsureTrackedPlayers();
                }
            }
        }
    }
}

public static class BMImage
{
    private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            string key = path + "|" + pixelsPerUnit;
            if (CachedSprites.TryGetValue(key, out Sprite sprite)) return sprite;

            Texture2D texture = LoadTextureFromResources(path);
            if (texture == null)
            {
                BMLogger.Error("Texture is null: " + path, "LoadImage");
                return null;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );

            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            CachedSprites[key] = sprite;
            return sprite;
        }
        catch (Exception ex)
        {
            BMLogger.Error("Error loading texture from: " + path + " | " + ex, "LoadImage");
        }

        return null;
    }

    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = OpenEmbeddedResourceFlexible(assembly, path);

            if (stream == null)
            {
                BMLogger.Error("Embedded resource not found: " + path, "LoadImage");
                LogAvailableResources(assembly);
                return null;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                if (!texture.LoadImage(ms.ToArray(), false))
                {
                    BMLogger.Error("LoadImage failed for resource: " + path, "LoadImage");
                    return null;
                }
            }

            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return texture;
        }
        catch (Exception ex)
        {
            BMLogger.Error("读入Texture失败：" + path + " | " + ex, "LoadImage");
        }

        return null;
    }

    private static Stream OpenEmbeddedResourceFlexible(Assembly assembly, string path)
    {
        if (assembly == null || string.IsNullOrEmpty(path))
            return null;

        Stream exact = assembly.GetManifestResourceStream(path);
        if (exact != null)
            return exact;

        string normalized = NormalizeResourcePath(path);
        string fileName = Path.GetFileName(path.Replace('\\', '/'));

        string[] resources = assembly.GetManifestResourceNames();

        for (int i = 0; i < resources.Length; i++)
        {
            string resource = resources[i];
            string normalizedResource = NormalizeResourcePath(resource);

            if (normalizedResource.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
                return assembly.GetManifestResourceStream(resource);
        }

        string resourcesSuffix = normalized;
        int idx = normalized.IndexOf("Resources.", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            resourcesSuffix = normalized.Substring(idx);

        for (int i = 0; i < resources.Length; i++)
        {
            string resource = resources[i];
            string normalizedResource = NormalizeResourcePath(resource);

            if (normalizedResource.EndsWith(resourcesSuffix, StringComparison.OrdinalIgnoreCase))
                return assembly.GetManifestResourceStream(resource);
        }

        for (int i = 0; i < resources.Length; i++)
        {
            string resource = resources[i];

            if (resource.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase) ||
                resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                return assembly.GetManifestResourceStream(resource);
        }

        return null;
    }

    private static string NormalizeResourcePath(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", ".")
            .Replace("/", ".")
            .Replace("..", ".")
            .Trim('.');
    }

    private static void LogAvailableResources(Assembly assembly)
    {
        try
        {
            string[] names = assembly.GetManifestResourceNames();

            for (int i = 0; i < names.Length && i < 40; i++)
            {
                BMLogger.LogDebug("[LoadImage] Embedded resource: " + names[i]);
            }

            if (names.Length > 40)
            {
                BMLogger.LogDebug("[LoadImage] Showing first 40 of " + names.Length + " embedded resources.");
            }
        }
        catch (Exception ex)
        {
            BMLogger.Error("[LoadImage] Could not list embedded resources: " + ex);
        }
    }

    public static Sprite LoadExternalSprite(string filePath, float pixelsPerUnit = 1f)
    {
        try
        {
            string key = filePath + "|" + pixelsPerUnit;
            if (CachedSprites.TryGetValue(key, out Sprite sprite)) return sprite;

            Texture2D texture = LoadExternalTexture(filePath);
            if (texture == null)
                return null;

            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );

            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            CachedSprites[key] = sprite;
            return sprite;
        }
        catch (Exception ex)
        {
            BMLogger.Error("Failed to load external sprite: " + ex);
        }

        return null;
    }

    public static Texture2D LoadExternalTexture(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            if (!texture.LoadImage(fileData))
            {
                BMLogger.Error("LoadImage failed");
                return null;
            }

            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return texture;
        }
        catch (Exception ex)
        {
            BMLogger.Error("Failed to load external texture: " + ex);
        }

        return null;
    }
}
