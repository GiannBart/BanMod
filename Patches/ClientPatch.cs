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
                if (BanMod.VoteLockEnabled.Value &&
                    srcClient != hostClientId)
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
