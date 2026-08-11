//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
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

namespace BanMod
{
    public static class Guesser
    {
        public static byte SpecialKillerId = 255; 
        public static bool SpecialKillerSelected = false;

        public static void OnStart()
        {
            if (!Options.Guess.GetBool())
                return;

            if (!SpecialKillerSelected)
            {
                SelectSpecialKiller();

                if (!SpecialKillerSelected)
                {
                    BMLogger.Info("[Guesser] SpecialKiller non assegnato: nessun candidato valido.");
                }
            }
        }

        public static void SelectSpecialKiller()
        {
            if (!Options.Guess.GetBool())
                return;

            var allPlayers = BanMod.AllPlayerControls;

            var guesserPlayers = allPlayers
                .Where(p => p != null
                            && p.Data != null
                            && p.Data.Role != null
                            && p.Data.Role.TeamType == RoleTeamTypes.Crewmate
                            && !p.Data.IsDead
                            && p.PlayerId != Exiler.ExilerId
                            && p.PlayerId != Judge.JudgeId
                            && p.PlayerId != Profiler.ProfilerId
                            && p.PlayerId != Jester.JesterId
                            && p.PlayerId != Watcher.WatcherId
                            && (!BanMod.forceImpostor || !BanMod.forcedImpostorIds.Contains(p.PlayerId))
                            && !Utils.Scientist(p)
                            && !Utils.Tracker(p)
                            && !Utils.Engineer(p)
                            && !Utils.Detective(p))
                .ToList();

            if (guesserPlayers.Count == 0)
            {
                SpecialKillerId = 255;
                SpecialKillerSelected = false;

                BMLogger.Info("[Guesser] Nessun candidato valido per SpecialKiller trovato.");

                try
                {
                    RoleButtonRefresh.RefreshNow();
                }
                catch
                {
                }

                return;
            }

            var randomPlayer = guesserPlayers[UnityEngine.Random.Range(0, guesserPlayers.Count)];

            SpecialKillerId = randomPlayer.PlayerId;
            SpecialKillerSelected = true;

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.SetSpecialKiller,
                    SendOption.Reliable,
                    -1);

                writer.Write(SpecialKillerId);

                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            try
            {
                RoleButtonRefresh.RefreshNow();
            }
            catch
            {
            }
        }

        public static bool IsGuesser(byte playerId)
        {
            return Options.Guess.GetBool() &&
                   SpecialKillerSelected &&
                   playerId == SpecialKillerId;
        }

        public static bool IsGuesser(PlayerControl player)
        {
            return player != null && IsGuesser(player.PlayerId);
        }

        public static void SendKillerMessage()
        {
            if (SpecialKillerId == 255)
                return;

            var allPlayers = BanMod.AllPlayerControls;
            var killer = allPlayers.FirstOrDefault(p => p != null && p.PlayerId == SpecialKillerId);

            if (killer == null || killer.Data == null || killer.Data.IsDead)
                return;

            string msg = string.Format(GetString("GuesserInfo"));

            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, SpecialKillerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, SpecialKillerId);
                MessageBlocker.UpdateLastMessageTime();
            }

            try
            {
                RoleButtonRefresh.RefreshNow();
            }
            catch
            {
            }
        }

        public static void SendKillerMessageTest()
        {
            string msg = string.Format(GetString("GuesserInfo"));

            Utils.SendMessage(msg, 255);
            MessageBlocker.UpdateLastMessageTime();

            try
            {
                RoleButtonRefresh.RefreshNow();
            }
            catch
            {
            }
        }

        public static void ResetSpecialKiller()
        {
            SpecialKillerId = 255;
            SpecialKillerSelected = false;

            try
            {
                RoleButtonRefresh.RefreshNow();
            }
            catch
            {
            }
        }
    }
}