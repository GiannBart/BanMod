//credits and licenses in the resources folder/

using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Data;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using UnityEngine;
using static BanMod.ImmortalManager;
using static BanMod.Utils;

namespace BanMod;




[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
static class OnPlayerJoined_ClientData_Patch
{
    public static void Postfix([HarmonyArgument(0)] ClientData client)
    {
        if (client == null)
            return;

        if (AmongUsClient.Instance == null)
            return;

        if (AmongUsClient.Instance.AmHost &&
            Options.CheckBlockList.GetBool() &&
            DestroyableSingleton<FriendsListManager>.Instance != null &&
            DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode))
        {
            AmongUsClient.Instance.KickPlayer(client.Id, true);
            BanManager.AddBanPlayer(client, "Blocked List");

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    $"{client.PlayerName} {Translator.GetString("Blocked")}"
                );
            }

            return;
        }

        BanManager.CheckBanPlayer(client);

        if (AmongUsClient.Instance != null)
            AmongUsClient.Instance.StartCoroutine(BanManager.WaitAndCheckAll(client));

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            BanMod.SendModDetectionRPC();
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
public static class PlayerControlStartUnifiedPatch
{
    public static System.Collections.Generic.Dictionary<string, string> PlayerNamesByFriendCode = new System.Collections.Generic.Dictionary<string, string>();

    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;
        int playerId = __instance.PlayerId;
        string friendCode = __instance.Data.FriendCode;
        if (playerId > 15 || playerId < -2)
        {
            AmongUsClient.Instance.KickPlayer(playerId, true);
        }

        if (__instance.AmOwner && GameStates.isOnlineGame)
        {
            __instance.StartCoroutine(InitialHandshake());
        }
        if (AmongUsClient.Instance.AmHost && __instance.AmOwner && (GameModeType)Options.GameMode.GetValue() == GameModeType.FFA)
        {
            FfaExternalBridge.SyncGameMode();
            FfaExternalBridge.SyncVentSeconds();
            FfaExternalBridge.SyncVentMode();
            FfaExternalBridge.SyncTeamMode();
            FfaExternalBridge.SyncTeamCount();
        }
    }
    private static IEnumerator InitialHandshake()
    {
        yield return new WaitForSeconds(5f);
        if (GameStates.isLobby)
        {
            Utils.SendModdedHandshake();
        }
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
internal static class AntiLeaveSpamPatch
{
    public static void Postfix([HarmonyArgument(0)] ClientData data)
    {
        try
        {
            if (data == null)
                return;

            int clientId = data.Id;
            string friendCode = data.FriendCode ?? "";

            if (!string.IsNullOrEmpty(friendCode) &&
                PlayerControlStartUnifiedPatch.PlayerNamesByFriendCode.ContainsKey(friendCode))
            {
                PlayerControlStartUnifiedPatch.PlayerNamesByFriendCode.Remove(friendCode);
                BMLogger.LogInfo($"[Cleanup] Rimossi dati per friendCode {friendCode}");
            }

        }
        catch (Exception ex)
        {
            BMLogger.LogError($"[Cleanup Error] OnPlayerLeft cleanup fallita: {ex}");
        }
    }
}