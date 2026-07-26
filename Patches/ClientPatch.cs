//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using InnerNet;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BanMod
{
    public static class VoteBanTracker
    {
        private static readonly Dictionary<int, HashSet<int>> VoteDetails = new();

        public static bool AddVote(int targetId, int voterId, out int totalVotes)
        {
            if (!VoteDetails.ContainsKey(targetId))
                VoteDetails[targetId] = new HashSet<int>();

            bool isNewVote = VoteDetails[targetId].Add(voterId);

            totalVotes = VoteDetails[targetId].Count;
            return isNewVote;
        }

        public static int GetVoteCount(int clientId)
        {
            if (VoteDetails.TryGetValue(clientId, out var voters))
                return voters.Count;

            return 0;
        }

        public static void Reset()
        {
            VoteDetails.Clear();
        }
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    public static class VoteBanSystem_AddVote_NotifyPatch
    {
        public static void Postfix(int srcClient, int clientId)
        {
            if (HudManager.Instance?.Notifier == null || AmongUsClient.Instance == null)
                return;

            bool isHost = AmongUsClient.Instance.AmHost;

            if (isHost && BanMod.VoteLockEnabled)
                return;

            if (!VoteBanTracker.AddVote(clientId, srcClient, out int voteCount))
                return;

            ClientData targetClient = AmongUsClient.Instance.GetClient(clientId);
            ClientData voterClient = AmongUsClient.Instance.GetClient(srcClient);

            string targetName = targetClient?.PlayerName ?? "Player";
            string voterName = voterClient?.PlayerName ?? "Someone";
            int localClientId = AmongUsClient.Instance.ClientId;

            string message;

            if (clientId == localClientId)
            {
                message = $"<color=red>{voterName}</color> voted to kick <color=yellow>YOU</color>.\n" +
                          $"Votes: {voteCount}/3";
            }
            else if (srcClient == localClientId)
            {
                message = $"You voted to kick <color=red>{targetName}</color>.\n" +
                          $"Total votes: {voteCount}/3";
            }
            else
            {
                message = $"<color=yellow>{voterName}</color> voted to kick <color=red>{targetName}</color>.\n" +
                          $"Total votes: {voteCount}/3";
            }

            BanMod.ShowChat(message);

            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                HudManager.Instance.Notifier,
                message
            );

            RefocusChatInput();
        }

        private static void RefocusChatInput()
        {
            try
            {
                ChatController chat = HudManager.Instance?.Chat;

                if (chat == null)
                    return;

                chat.StartCoroutine(CoRefocusChatInput(chat).WrapToIl2Cpp());
            }
            catch
            {
            }
        }

        private static IEnumerator CoRefocusChatInput(ChatController chat)
        {
            yield return null;

            try
            {
                if (chat == null)
                    yield break;

                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
                    yield break;

                if (chat.freeChatField == null)
                    yield break;

                chat.freeChatField.SetVisible(true);
                chat.freeChatField.Focus();
            }
            catch
            {
            }

            yield return null;

            try
            {
                if (chat != null &&
                    chat.chatScreen != null &&
                    chat.chatScreen.activeInHierarchy &&
                    chat.freeChatField != null)
                {
                    chat.freeChatField.SetVisible(true);
                    chat.freeChatField.Focus();
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
    public static class BanMenuSetVisiblePatch
    {
        public static bool Prefix(BanMenu __instance, ref bool show)
        {
            bool hasPlayer = PlayerControl.LocalPlayer != null &&
                             PlayerControl.LocalPlayer.Data != null;

            __instance.MenuButton.gameObject.SetActive(show && hasPlayer);

            if (AmongUsClient.Instance != null)
            {
                __instance.BanButton.gameObject.SetActive(
                    AmongUsClient.Instance.CanBan()
                );

                __instance.KickButton.gameObject.SetActive(
                    AmongUsClient.Instance.CanKick()
                );
            }
            else
            {
                __instance.BanButton.gameObject.SetActive(false);
                __instance.KickButton.gameObject.SetActive(false);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
    public static class InnerNetClientCanBanPatch
    {
        public static bool Prefix(InnerNetClient __instance, ref bool __result)
        {
            __result = __instance.AmHost;
            return false;
        }
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
    public static class KickPlayerPatch
    {
        public static bool Prefix(int clientId, bool ban)
        {
            if (!AmongUsClient.Instance.AmHost)
                return true;

            if (AmongUsClient.Instance.ClientId == clientId)
                return false;

            if (ban)
                BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));

            return true;
        }
    }

    [HarmonyPatch]
    public static class VoteBanSystemAddVotePatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(VoteBanSystem),
                "AddVote",
                new[] { typeof(int), typeof(int) }
            );
        }

        static bool Prefix(int srcClient, int clientId)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return true;

                int hostClientId = AmongUsClient.Instance.HostId;

                ClientData voter = AmongUsClient.Instance.GetClient(srcClient);
                ClientData target = AmongUsClient.Instance.GetClient(clientId);

                string voterName = voter?.PlayerName ?? $"Client {srcClient}";
                string targetName = target?.PlayerName ?? $"Client {clientId}";

                if (clientId == hostClientId)
                {
                    BMLogger.LogInfo($"[BanMod] Vote from {voterName} against Host ({targetName}) was automatically blocked.");
                    BanMod.ShowChat($"<color=red>{voterName}</color> tried to kick you!");
                    RefocusChatInput();
                    return false;
                }

                if (BanMod.VoteLockEnabled && srcClient != hostClientId)
                {
                    BanMod.ShowChat($"<color=yellow>[VoteLock]</color> {voterName} tried to vote {targetName}, but voting is disabled.");
                    BMLogger.LogInfo($"[VoteLock] Blocked attempt by {voterName} to vote {targetName}.");
                    RefocusChatInput();
                    return false;
                }

                BMLogger.LogInfo($"[VoteBan] {voterName} is voting to kick {targetName}");

                return true;
            }
            catch (System.Exception ex)
            {
                BMLogger.LogError("[VoteLock] Error in Prefix AddVote: " + ex);
                return true;
            }
        }

        private static void RefocusChatInput()
        {
            try
            {
                ChatController chat = HudManager.Instance?.Chat;

                if (chat == null)
                    return;

                chat.StartCoroutine(CoRefocusChatInput(chat).WrapToIl2Cpp());
            }
            catch
            {
            }
        }

        private static IEnumerator CoRefocusChatInput(ChatController chat)
        {
            yield return null;

            try
            {
                if (chat == null)
                    yield break;

                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
                    yield break;

                if (chat.freeChatField == null)
                    yield break;

                chat.freeChatField.SetVisible(true);
                chat.freeChatField.Focus();
            }
            catch
            {
            }

            yield return null;

            try
            {
                if (chat != null &&
                    chat.chatScreen != null &&
                    chat.chatScreen.activeInHierarchy &&
                    chat.freeChatField != null)
                {
                    chat.freeChatField.SetVisible(true);
                    chat.freeChatField.Focus();
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    public static class ChatController_SendChat_RefocusPatch
    {
        public static void Postfix(ChatController __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                __instance.StartCoroutine(CoRefocusAfterSend(__instance).WrapToIl2Cpp());
            }
            catch
            {
            }
        }

        private static IEnumerator CoRefocusAfterSend(ChatController chat)
        {
            yield return null;

            try
            {
                if (chat == null)
                    yield break;

                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
                    yield break;

                if (chat.freeChatField == null)
                    yield break;

                chat.freeChatField.SetVisible(true);
                chat.freeChatField.Focus();
            }
            catch
            {
            }

            yield return null;

            try
            {
                if (chat != null &&
                    chat.chatScreen != null &&
                    chat.chatScreen.activeInHierarchy &&
                    chat.freeChatField != null)
                {
                    chat.freeChatField.SetVisible(true);
                    chat.freeChatField.Focus();
                }
            }
            catch
            {
            }
        }
    }
}