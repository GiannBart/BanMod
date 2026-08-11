//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Rewired.UI.ControlMapper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BanMod.Translator;
using static BanMod.Utils;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public static class Watcher
    {
        public static byte WatcherId = 255;
        public static byte WatcherLover = 255;
        public static bool WatcherSelected = false;

        public static void OnStart()
        {
            if (!Options.Watcher.GetBool())
                return;

            if (WatcherSelected && WatcherId != 255)
            {
                BMLogger.Info($"[Watcher] Watcher già selezionato: {WatcherId}. Non lo cambio.");
                return;
            }

            SelectWatcher();

            if (!WatcherSelected)
                BMLogger.Info("[Watcher] Watcher non assegnato.");
        }

        public static void SelectWatcher()
        {
            if (!Options.Watcher.GetBool())
                return;

            var allPlayers = BanMod.AllPlayerControls;

            PlayerControl watcherPlayer = null;

            if (WatcherId != 255)
            {
                watcherPlayer = allPlayers
                    .FirstOrDefault(p =>
                        p != null &&
                        p.Data != null &&
                        !p.Data.IsDead &&
                        !p.Data.Disconnected &&
                        p.PlayerId == WatcherId);

                if (watcherPlayer == null)
                {
                    BMLogger.Info($"[Watcher] WatcherId già impostato ({WatcherId}) ma player non trovato/non valido.");
                    WatcherId = 255;
                    WatcherLover = 255;
                    WatcherSelected = false;
                    return;
                }

                WatcherSelected = true;
                WatcherLover = 255;

                BMLogger.Info($"[Watcher] Watcher manuale confermato: {WatcherId}. Lover non ancora scelto.");
            }
            else
            {
                var alivePlayers = allPlayers
                    .Where(p => p != null &&
                                p.Data != null &&
                                !p.Data.IsDead &&
                                !p.Data.Disconnected &&
                                Crewmate(p) &&
                                p.PlayerId != Guesser.SpecialKillerId &&
                                p.PlayerId != Jester.JesterId &&
                                p.PlayerId != Exiler.ExilerId &&
                                p.PlayerId != Judge.JudgeId && 
                                p.PlayerId != Profiler.ProfilerId &&
                                !Scientist(p) &&
                                !Engineer(p) &&
                                !Tracker(p) &&
                                (!BanMod.forceImpostor || !BanMod.forcedImpostorIds.Contains(p.PlayerId)) &&
                                !(Options.PhantomGuess.GetBool() && Phantom(p)) &&
                                !(Options.ViperGuess.GetBool() && Cobra(p)) &&
                                !(Options.ImpostorGuess.GetBool() && Impostor(p)) &&
                                !(Options.ShapeGuess.GetBool() && Shapeshifter(p)))
                    .ToList();

                if (alivePlayers.Count == 0)
                {
                    WatcherId = 255;
                    WatcherLover = 255;
                    WatcherSelected = false;
                    BMLogger.Info("[Watcher] Nessun candidato valido per Watcher trovato.");
                    return;
                }

                watcherPlayer = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];
                WatcherId = watcherPlayer.PlayerId;
                WatcherLover = 255;
                WatcherSelected = true;

                BMLogger.Info($"[Watcher] Watcher random assegnato: {WatcherId}. Lover non ancora scelto.");
            }

            if (AmongUsClient.Instance.AmHost)
            {
                SendWatcherRpc();
            }
        }

        public static void SelectWatcherLover()
        {
            if (!AmongUsClient.Instance.AmHost)
                return;

            if (!Options.Watcher.GetBool())
                return;

            if (!WatcherSelected || WatcherId == 255)
            {
                BMLogger.Info("[Watcher] Lover non scelto: Watcher non valido.");
                return;
            }

            if (WatcherLover != 255)
            {
                BMLogger.Info($"[Watcher] Lover già selezionato: {WatcherLover}. Non lo cambio.");
                return;
            }

            var loverPlayers = BanMod.AllPlayerControls
                .Where(p => p != null &&
                            p.Data != null &&
                            !p.Data.IsDead &&
                            !p.Data.Disconnected &&
                            Crewmate(p) &&
                            p.PlayerId != WatcherId)
                .ToList();

            if (loverPlayers.Count == 0)
            {
                WatcherLover = 255;
                BMLogger.Info("[Watcher] Nessun compagno crew valido trovato come lover.");
                return;
            }

            var randomLover = loverPlayers[UnityEngine.Random.Range(0, loverPlayers.Count)];
            WatcherLover = randomLover.PlayerId;

            BMLogger.Info($"[Watcher] Lover scelto a gioco iniziato: {WatcherLover}");

            SendWatcherRpc();
        }

        public static void ApplyWatcherShield()
        {
            if (!AmongUsClient.Instance.AmHost)
                return;

            if (!Options.Watcher.GetBool())
                return;

            if (!WatcherSelected || WatcherId == 255)
                return;

            var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == WatcherId);

            if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected)
            {
                BMLogger.Info($"[Watcher] Shield non assegnato: player non valido. WatcherId={WatcherId}");
                return;
            }

            if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
            {
                BanMod.ShieldedPlayers.Add(player.PlayerId);
                BMLogger.Info($"[Watcher] Shield assegnato al Watcher {player.PlayerId}.");
            }
            else
            {
                BMLogger.Info($"[Watcher] Watcher {player.PlayerId} era già in ShieldedPlayers.");
            }
        }

        private static void SendWatcherRpc()
        {
            if (!AmongUsClient.Instance.AmHost)
                return;

            if (PlayerControl.LocalPlayer == null)
                return;

            var writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.SetWatcher,
                SendOption.Reliable,
                -1);

            writer.Write(WatcherId);
            writer.Write(WatcherLover);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static bool IsWatcher(byte playerId)
        {
            return Options.Watcher.GetBool()
                   && WatcherSelected
                   && WatcherId != 255
                   && WatcherId == playerId;
        }

        public static bool IsWatcher(PlayerControl player)
        {
            return player != null && IsWatcher(player.PlayerId);
        }

        public static bool IsWatcherLover(byte playerId)
        {
            return WatcherSelected
                   && WatcherLover != 255
                   && WatcherLover == playerId;
        }

        public static bool IsWatcherLover(PlayerControl player)
        {
            return player != null && IsWatcherLover(player.PlayerId);
        }

        public static PlayerControl GetWatcherPlayer()
        {
            if (WatcherId == 255)
                return null;

            return BanMod.AllPlayerControls
                .FirstOrDefault(p => p != null && p.PlayerId == WatcherId);
        }

        public static PlayerControl GetWatcherLoverPlayer()
        {
            if (WatcherLover == 255)
                return null;

            return BanMod.AllPlayerControls
                .FirstOrDefault(p => p != null && p.PlayerId == WatcherLover);
        }

        public static void SendWatcherMessage()
        {
            if (WatcherId == 255)
            {
                BMLogger.Info("[Watcher] Messaggio non inviato: WatcherId = 255.");
                return;
            }

            if (WatcherLover == 255)
            {
                BMLogger.Info("[Watcher] Messaggio non inviato: WatcherLover = 255.");
                return;
            }

            var watcher = GetWatcherPlayer();

            if (watcher == null || watcher.Data == null || watcher.Data.IsDead)
            {
                BMLogger.Info("[Watcher] Messaggio non inviato: Watcher nullo o morto.");
                return;
            }

            string loverName = GetWatcherLoverNameSafe();

            string msg = string.Format(GetString("WatcherInfo"), loverName);

            Utils.SendMessage(msg, WatcherId);
            MessageBlocker.UpdateLastMessageTime();

            BMLogger.Info($"[Watcher] Messaggio inviato al Watcher {WatcherId}: {msg}");

        }

        private static string GetWatcherLoverNameSafe()
        {
            try
            {
                PlayerControl lover = BanMod.AllPlayerControls
                    .FirstOrDefault(p => p != null && p.PlayerId == WatcherLover);

                if (lover != null &&
                    lover.Data != null &&
                    !string.IsNullOrWhiteSpace(lover.Data.PlayerName))
                {
                    return lover.Data.PlayerName;
                }
            }
            catch
            {
            }

            try
            {
                PlayerControl lover = PlayerControl.AllPlayerControls
                    .ToArray()
                    .FirstOrDefault(p => p != null && p.PlayerId == WatcherLover);

                if (lover != null &&
                    lover.Data != null &&
                    !string.IsNullOrWhiteSpace(lover.Data.PlayerName))
                {
                    return lover.Data.PlayerName;
                }
            }
            catch
            {
            }

            return $"Player {WatcherLover}";
        }

        public static void OnPlayerDied(byte deadPlayerId)
        {
            if (!AmongUsClient.Instance.AmHost)
                return;

            if (!WatcherSelected)
                return;

            if (WatcherId == 255 || WatcherLover == 255)
                return;

            if (deadPlayerId != WatcherLover)
                return;

            KillWatcherBecauseLoverDied();
        }

        public static void KillWatcherBecauseLoverDied()
        {
            if (!AmongUsClient.Instance.AmHost)
                return;

            if (!WatcherSelected)
                return;

            if (WatcherId == 255 || WatcherLover == 255)
                return;

            var watcher = GetWatcherPlayer();

            if (watcher == null || watcher.Data == null)
                return;

            if (watcher.Data.IsDead)
                return;

            try
            {
                BanMod.ShieldedPlayers.Remove(watcher.PlayerId);
            }
            catch
            {
            }

            try
            {
                watcher.RemoveProtection();
            }
            catch
            {
            }

            try
            {
                watcher.protectedByGuardianId = -1;
            }
            catch
            {
            }

            try
            {
                watcher.RpcSetRole(RoleTypes.CrewmateGhost);
            }
            catch
            {
            }

            try
            {
                if (watcher.Data != null)
                {
                    watcher.Data.IsDead = true;
                    watcher.Data.MarkDirty();
                }
            }
            catch
            {
            }

            BMLogger.Info("[Watcher] Il WatcherLover è morto. Uccido anche il Watcher.");
        }

        public static void ResetWatcher()
        {
            WatcherId = 255;
            WatcherLover = 255;
            WatcherSelected = false;
        }
    }
}

[HarmonyPatch]
public static class WatcherMeetingPatch
{
    private const byte SkipVoteId = 253;

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    [HarmonyPrefix]
    public static void CastVotePrefix(byte srcPlayerId, ref byte suspectPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        if (!Watcher.WatcherSelected)
            return;

        if (Watcher.WatcherId == 255)
            return;

        if (suspectPlayerId == Watcher.WatcherId)
        {
            BMLogger.Info(
                $"[Watcher] {srcPlayerId} ha votato il Watcher {Watcher.WatcherId}. Voto convertito in Skip."
            );

            suspectPlayerId = SkipVoteId;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), "VotingComplete")]
    [HarmonyPostfix]
    public static void VotingCompletePostfix(NetworkedPlayerInfo exiled)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        if (!Watcher.WatcherSelected)
            return;

        if (Watcher.WatcherId == 255 || Watcher.WatcherLover == 255)
            return;

        if (exiled == null)
            return;

        if (exiled.PlayerId == Watcher.WatcherLover)
        {
            BMLogger.Info(
                $"[Watcher] WatcherLover {Watcher.WatcherLover} è stato votato/esiliato. Uccido il Watcher {Watcher.WatcherId}."
            );

            Watcher.KillWatcherBecauseLoverDied();
        }
    }
}