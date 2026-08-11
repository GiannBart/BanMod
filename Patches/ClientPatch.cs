////credits and licenses in the resources folder
//using BepInEx.Unity.IL2CPP.Utils.Collections;
//using HarmonyLib;
//using InnerNet;
//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection;
//using UnityEngine;

//namespace BanMod
//{
//    public static class VoteBanTracker
//    {
//        private static readonly Dictionary<int, HashSet<int>> VoteDetails = new();

//        public static bool AddVote(int targetId, int voterId, out int totalVotes)
//        {
//            if (!VoteDetails.ContainsKey(targetId))
//                VoteDetails[targetId] = new HashSet<int>();

//            bool isNewVote = VoteDetails[targetId].Add(voterId);

//            totalVotes = VoteDetails[targetId].Count;
//            return isNewVote;
//        }

//        public static int GetVoteCount(int clientId)
//        {
//            if (VoteDetails.TryGetValue(clientId, out var voters))
//                return voters.Count;

//            return 0;
//        }

//        public static void Reset()
//        {
//            VoteDetails.Clear();
//        }
//    }

//    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
//    public static class VoteBanSystem_AddVote_NotifyPatch
//    {
//        public static void Postfix(int srcClient, int clientId)
//        {
//            if (HudManager.Instance?.Notifier == null || AmongUsClient.Instance == null)
//                return;

//            bool isHost = AmongUsClient.Instance.AmHost;

//            if (isHost && BanMod.VoteLockEnabled)
//                return;

//            if (!VoteBanTracker.AddVote(clientId, srcClient, out int voteCount))
//                return;

//            ClientData targetClient = AmongUsClient.Instance.GetClient(clientId);
//            ClientData voterClient = AmongUsClient.Instance.GetClient(srcClient);

//            string targetName = targetClient?.PlayerName ?? "Player";
//            string voterName = voterClient?.PlayerName ?? "Someone";
//            int localClientId = AmongUsClient.Instance.ClientId;

//            string message;

//            if (clientId == localClientId)
//            {
//                message = $"<color=red>{voterName}</color> voted to kick <color=yellow>YOU</color>.\n" +
//                          $"Votes: {voteCount}/3";
//            }
//            else if (srcClient == localClientId)
//            {
//                message = $"You voted to kick <color=red>{targetName}</color>.\n" +
//                          $"Total votes: {voteCount}/3";
//            }
//            else
//            {
//                message = $"<color=yellow>{voterName}</color> voted to kick <color=red>{targetName}</color>.\n" +
//                          $"Total votes: {voteCount}/3";
//            }

//            BanMod.ShowChat(message);

//            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
//                HudManager.Instance.Notifier,
//                message
//            );

//            RefocusChatInput();
//        }

//        private static void RefocusChatInput()
//        {
//            try
//            {
//                ChatController chat = HudManager.Instance?.Chat;

//                if (chat == null)
//                    return;

//                chat.StartCoroutine(CoRefocusChatInput(chat).WrapToIl2Cpp());
//            }
//            catch
//            {
//            }
//        }

//        private static IEnumerator CoRefocusChatInput(ChatController chat)
//        {
//            yield return null;

//            try
//            {
//                if (chat == null)
//                    yield break;

//                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
//                    yield break;

//                if (chat.freeChatField == null)
//                    yield break;

//                chat.freeChatField.SetVisible(true);
//                chat.freeChatField.Focus();
//            }
//            catch
//            {
//            }

//            yield return null;

//            try
//            {
//                if (chat != null &&
//                    chat.chatScreen != null &&
//                    chat.chatScreen.activeInHierarchy &&
//                    chat.freeChatField != null)
//                {
//                    chat.freeChatField.SetVisible(true);
//                    chat.freeChatField.Focus();
//                }
//            }
//            catch
//            {
//            }
//        }
//    }

//    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
//    public static class BanMenuSetVisiblePatch
//    {
//        public static bool Prefix(BanMenu __instance, ref bool show)
//        {
//            bool hasPlayer = PlayerControl.LocalPlayer != null &&
//                             PlayerControl.LocalPlayer.Data != null;

//            __instance.MenuButton.gameObject.SetActive(show && hasPlayer);

//            if (AmongUsClient.Instance != null)
//            {
//                __instance.BanButton.gameObject.SetActive(
//                    AmongUsClient.Instance.CanBan()
//                );

//                __instance.KickButton.gameObject.SetActive(
//                    AmongUsClient.Instance.CanKick()
//                );
//            }
//            else
//            {
//                __instance.BanButton.gameObject.SetActive(false);
//                __instance.KickButton.gameObject.SetActive(false);
//            }

//            return false;
//        }
//    }

//    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
//    public static class InnerNetClientCanBanPatch
//    {
//        public static bool Prefix(InnerNetClient __instance, ref bool __result)
//        {
//            __result = __instance.AmHost;
//            return false;
//        }
//    }

//    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
//    public static class KickPlayerPatch
//    {
//        public static bool Prefix(int clientId, bool ban)
//        {
//            if (!AmongUsClient.Instance.AmHost)
//                return true;

//            if (AmongUsClient.Instance.ClientId == clientId)
//                return false;

//            if (ban)
//                BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));

//            return true;
//        }
//    }

//    [HarmonyPatch]
//    public static class VoteBanSystemAddVotePatch
//    {
//        static MethodBase TargetMethod()
//        {
//            return AccessTools.Method(
//                typeof(VoteBanSystem),
//                "AddVote",
//                new[] { typeof(int), typeof(int) }
//            );
//        }

//        static bool Prefix(int srcClient, int clientId)
//        {
//            try
//            {
//                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
//                    return true;

//                int hostClientId = AmongUsClient.Instance.HostId;

//                ClientData voter = AmongUsClient.Instance.GetClient(srcClient);
//                ClientData target = AmongUsClient.Instance.GetClient(clientId);

//                string voterName = voter?.PlayerName ?? $"Client {srcClient}";
//                string targetName = target?.PlayerName ?? $"Client {clientId}";

//                if (clientId == hostClientId)
//                {
//                    BMLogger.LogInfo($"[BanMod] Vote from {voterName} against Host ({targetName}) was automatically blocked.");
//                    BanMod.ShowChat($"<color=red>{voterName}</color> tried to kick you!");
//                    RefocusChatInput();
//                    return false;
//                }

//                if (BanMod.VoteLockEnabled && srcClient != hostClientId)
//                {
//                    BanMod.ShowChat($"<color=yellow>[VoteLock]</color> {voterName} tried to vote {targetName}, but voting is disabled.");
//                    BMLogger.LogInfo($"[VoteLock] Blocked attempt by {voterName} to vote {targetName}.");
//                    RefocusChatInput();
//                    return false;
//                }

//                BMLogger.LogInfo($"[VoteBan] {voterName} is voting to kick {targetName}");

//                return true;
//            }
//            catch (System.Exception ex)
//            {
//                BMLogger.LogError("[VoteLock] Error in Prefix AddVote: " + ex);
//                return true;
//            }
//        }

//        private static void RefocusChatInput()
//        {
//            try
//            {
//                ChatController chat = HudManager.Instance?.Chat;

//                if (chat == null)
//                    return;

//                chat.StartCoroutine(CoRefocusChatInput(chat).WrapToIl2Cpp());
//            }
//            catch
//            {
//            }
//        }

//        private static IEnumerator CoRefocusChatInput(ChatController chat)
//        {
//            yield return null;

//            try
//            {
//                if (chat == null)
//                    yield break;

//                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
//                    yield break;

//                if (chat.freeChatField == null)
//                    yield break;

//                chat.freeChatField.SetVisible(true);
//                chat.freeChatField.Focus();
//            }
//            catch
//            {
//            }

//            yield return null;

//            try
//            {
//                if (chat != null &&
//                    chat.chatScreen != null &&
//                    chat.chatScreen.activeInHierarchy &&
//                    chat.freeChatField != null)
//                {
//                    chat.freeChatField.SetVisible(true);
//                    chat.freeChatField.Focus();
//                }
//            }
//            catch
//            {
//            }
//        }
//    }

//    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
//    public static class ChatController_SendChat_RefocusPatch
//    {
//        public static void Postfix(ChatController __instance)
//        {
//            try
//            {
//                if (__instance == null)
//                    return;

//                __instance.StartCoroutine(CoRefocusAfterSend(__instance).WrapToIl2Cpp());
//            }
//            catch
//            {
//            }
//        }

//        private static IEnumerator CoRefocusAfterSend(ChatController chat)
//        {
//            yield return null;

//            try
//            {
//                if (chat == null)
//                    yield break;

//                if (chat.chatScreen == null || !chat.chatScreen.activeInHierarchy)
//                    yield break;

//                if (chat.freeChatField == null)
//                    yield break;

//                chat.freeChatField.SetVisible(true);
//                chat.freeChatField.Focus();
//            }
//            catch
//            {
//            }

//            yield return null;

//            try
//            {
//                if (chat != null &&
//                    chat.chatScreen != null &&
//                    chat.chatScreen.activeInHierarchy &&
//                    chat.freeChatField != null)
//                {
//                    chat.freeChatField.SetVisible(true);
//                    chat.freeChatField.Focus();
//                }
//            }
//            catch
//            {
//            }
//        }
//    }
//}
//credits and licenses in the resources folder
// Credits and licenses in the resources folder.

using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;

namespace BanMod
{
    /// <summary>
    /// Memorizza chi ha votato per espellere ogni client.
    /// Impedisce di contare più volte lo stesso votante.
    /// </summary>
    public static class VoteBanTracker
    {
        private static readonly Dictionary<int, HashSet<int>> VoteDetails = new();

        public static bool AddVote(
            int targetId,
            int voterId,
            out int totalVotes)
        {
            if (!VoteDetails.TryGetValue(
                    targetId,
                    out HashSet<int> voters))
            {
                voters = new HashSet<int>();
                VoteDetails[targetId] = voters;
            }

            bool isNewVote = voters.Add(voterId);

            totalVotes = voters.Count;
            return isNewVote;
        }

        public static int GetVoteCount(int clientId)
        {
            if (VoteDetails.TryGetValue(
                    clientId,
                    out HashSet<int> voters))
            {
                return voters.Count;
            }

            return 0;
        }

        public static void Reset()
        {
            VoteDetails.Clear();
        }
    }

    /// <summary>
    /// Mostra localmente un messaggio nella chat.
    /// Non usa notifiche, RPC, SendMessage o refocus.
    /// </summary>
    internal static class VoteBanChat
    {
        public static void AddChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            HudManager hud = HudManager.Instance;
            PlayerControl localPlayer = PlayerControl.LocalPlayer;

            if (hud == null ||
                hud.Chat == null ||
                localPlayer == null)
            {
                BMLogger.LogInfo(
                    "[VoteBan] Chat non disponibile; " +
                    "messaggio locale non visualizzato."
                );

                return;
            }

            try
            {
                hud.Chat.AddChat(localPlayer, message);
            }
            catch (Exception ex)
            {
                BMLogger.LogError(
                    "[VoteBan] Errore durante Chat.AddChat: " + ex
                );
            }
        }
    }

    /// <summary>
    /// Gestisce il blocco dei voti contro l'host,
    /// VoteLock e le notifiche locali dei voti validi.
    ///
    /// Prefix e Postfix sono nella stessa classe per poter
    /// condividere __state:
    ///
    /// - __state = false: voto bloccato, il Postfix non notifica;
    /// - __state = true: voto consentito, il Postfix può notificare.
    /// </summary>
    [HarmonyPatch(
        typeof(VoteBanSystem),
        nameof(VoteBanSystem.AddVote),
        new Type[] { typeof(int), typeof(int) }
    )]
    public static class VoteBanSystemAddVotePatch
    {
        public static bool Prefix(
            int srcClient,
            int clientId,
            out bool __state)
        {
            // Il Postfix non deve considerare il voto valido
            // finché il Prefix non lo autorizza esplicitamente.
            __state = false;

            try
            {
                AmongUsClient client = AmongUsClient.Instance;

                // Se il client non è ancora disponibile,
                // non blocchiamo il metodo originale.
                if (client == null)
                {
                    __state = true;
                    return true;
                }

                // Il blocco dei voti viene deciso solamente dall'host.
                // Sugli altri client lasciamo eseguire normalmente AddVote.
                if (!client.AmHost)
                {
                    __state = true;
                    return true;
                }

                int hostClientId = client.HostId;

                ClientData voter =
                    client.GetClient(srcClient);

                ClientData target =
                    client.GetClient(clientId);

                string voterName =
                    voter?.PlayerName ??
                    $"Client {srcClient}";

                string targetName =
                    target?.PlayerName ??
                    $"Client {clientId}";

                /*
                 * Impedisce di votare contro l'host.
                 */
                if (clientId == hostClientId)
                {
                    BMLogger.LogInfo(
                        $"[VoteBan] Il voto di {voterName} " +
                        $"contro l'host ({targetName}) è stato bloccato."
                    );

                    VoteBanChat.AddChat(
                        $"<color=red>{voterName}</color> " +
                        "tried to kick you!"
                    );

                    return false;
                }

                /*
                 * Con VoteLock attivo, i client non host
                 * non possono votare per espellere altri giocatori.
                 *
                 * L'host può continuare a usare il sistema.
                 */
                if (BanMod.VoteLockEnabled.Value)
                {
                    if (srcClient != hostClientId)
                    {
                        BMLogger.LogInfo(
                            $"[VoteLock] Il voto di {voterName} " +
                            $"contro {targetName} è stato bloccato."
                        );

                        VoteBanChat.AddChat(
                            "<color=yellow>[VoteLock]</color> " +
                            $"{voterName} tried to vote {targetName}, " +
                            "but voting is disabled."
                        );

                        return false;
                    }
                }

                BMLogger.LogInfo(
                    $"[VoteBan] {voterName} is voting " +
                    $"to kick {targetName}."
                );

                // Il metodo originale può essere eseguito.
                __state = true;
                return true;
            }
            catch (Exception ex)
            {
                BMLogger.LogError(
                    "[VoteBan] Errore nel Prefix di AddVote: " + ex
                );

                // In caso di errore nella mod, non blocchiamo
                // il comportamento originale del gioco.
                __state = true;
                return true;
            }
        }

        public static void Postfix(
            int srcClient,
            int clientId,
            bool __state)
        {
            try
            {
                /*
                 * Se il Prefix ha bloccato il voto,
                 * non registrarlo e non mostrare una seconda notifica.
                 */
                if (!__state)
                    return;

                AmongUsClient client = AmongUsClient.Instance;

                if (client == null)
                    return;

                /*
                 * Impedisce di mostrare due volte lo stesso voto
                 * se AddVote viene ricevuto o elaborato più volte.
                 */
                if (!VoteBanTracker.AddVote(
                        clientId,
                        srcClient,
                        out int voteCount))
                {
                    return;
                }

                ClientData targetClient =
                    client.GetClient(clientId);

                ClientData voterClient =
                    client.GetClient(srcClient);

                string targetName =
                    targetClient?.PlayerName ??
                    $"Client {clientId}";

                string voterName =
                    voterClient?.PlayerName ??
                    $"Client {srcClient}";

                int localClientId = client.ClientId;

                string message;

                /*
                 * Il giocatore locale è il bersaglio del voto.
                 */
                if (clientId == localClientId)
                {
                    message =
                        $"<color=red>{voterName}</color> " +
                        "voted to kick " +
                        "<color=yellow>YOU</color>.\n" +
                        $"Votes: {voteCount}/3";
                }
                /*
                 * Il giocatore locale ha effettuato il voto.
                 */
                else if (srcClient == localClientId)
                {
                    message =
                        "You voted to kick " +
                        $"<color=red>{targetName}</color>.\n" +
                        $"Total votes: {voteCount}/3";
                }
                /*
                 * Il voto riguarda altri due giocatori.
                 */
                else
                {
                    message =
                        $"<color=yellow>{voterName}</color> " +
                        "voted to kick " +
                        $"<color=red>{targetName}</color>.\n" +
                        $"Total votes: {voteCount}/3";
                }

                VoteBanChat.AddChat(message);
            }
            catch (Exception ex)
            {
                BMLogger.LogError(
                    "[VoteBan] Errore nel Postfix di AddVote: " + ex
                );
            }
        }
    }

    /// <summary>
    /// Pulisce i voti registrati quando si entra in una lobby.
    /// </summary>
    [HarmonyPatch(
        typeof(AmongUsClient),
        nameof(AmongUsClient.OnGameJoined)
    )]
    public static class VoteBanTrackerResetPatch
    {
        public static void Postfix()
        {
            VoteBanTracker.Reset();
        }
    }

    /// <summary>
    /// Lascia eseguire il metodo originale di BanMenu.SetVisible
    /// e modifica i pulsanti solamente dopo la logica vanilla.
    ///
    /// Non usa più un Prefix con return false, perché potrebbe
    /// impedire al gioco di aggiornare correttamente menu e input.
    /// </summary>
    [HarmonyPatch(
        typeof(BanMenu),
        nameof(BanMenu.SetVisible)
    )]
    public static class BanMenuSetVisiblePatch
    {
        public static void Postfix(
            BanMenu __instance,
            bool show)
        {
            if (__instance == null)
                return;

            try
            {
                PlayerControl localPlayer =
                    PlayerControl.LocalPlayer;

                bool hasPlayer =
                    localPlayer != null &&
                    localPlayer.Data != null;

                if (__instance.MenuButton != null &&
                    __instance.MenuButton.gameObject != null)
                {
                    __instance.MenuButton.gameObject.SetActive(
                        show && hasPlayer
                    );
                }

                AmongUsClient client =
                    AmongUsClient.Instance;

                bool canBan =
                    show &&
                    client != null &&
                    client.CanBan();

                bool canKick =
                    show &&
                    client != null &&
                    client.CanKick();

                if (__instance.BanButton != null &&
                    __instance.BanButton.gameObject != null)
                {
                    __instance.BanButton.gameObject.SetActive(
                        canBan
                    );
                }

                if (__instance.KickButton != null &&
                    __instance.KickButton.gameObject != null)
                {
                    __instance.KickButton.gameObject.SetActive(
                        canKick
                    );
                }
            }
            catch (Exception ex)
            {
                BMLogger.LogError(
                    "[BanMenu] Errore in SetVisible Postfix: " + ex
                );
            }
        }
    }

    /// <summary>
    /// Permette solamente all'host di usare il ban diretto.
    /// </summary>
    [HarmonyPatch(
        typeof(InnerNetClient),
        nameof(InnerNetClient.CanBan)
    )]
    public static class InnerNetClientCanBanPatch
    {
        public static bool Prefix(
            InnerNetClient __instance,
            ref bool __result)
        {
            __result =
                __instance != null &&
                __instance.AmHost;

            return false;
        }
    }

    /// <summary>
    /// Impedisce all'host di espellere o bannare sé stesso.
    /// Registra inoltre il ban nel BanManager prima
    /// dell'esecuzione del metodo originale.
    /// </summary>
    [HarmonyPatch(
        typeof(InnerNetClient),
        nameof(InnerNetClient.KickPlayer)
    )]
    public static class KickPlayerPatch
    {
        public static bool Prefix(
            int clientId,
            bool ban)
        {
            try
            {
                AmongUsClient client =
                    AmongUsClient.Instance;

                if (client == null)
                    return true;

                // La modifica interessa solamente l'host.
                if (!client.AmHost)
                    return true;

                // Impedisce all'host di espellere sé stesso.
                if (client.ClientId == clientId)
                    return false;

                if (ban)
                {
                    ClientData recentClient =
                        client.GetRecentClient(clientId);

                    if (recentClient != null)
                    {
                        BanManager.AddBanPlayer(recentClient);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                BMLogger.LogError(
                    "[KickPlayer] Errore nel Prefix: " + ex
                );

                // In caso di errore nella mod, lascia funzionare
                // il metodo originale del gioco.
                return true;
            }
        }
    }
}