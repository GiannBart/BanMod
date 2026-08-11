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
    public static class Exiler
    {
        public static byte ExilerId = 255;
        public static bool ExilerSelected = false;

        public static void OnStart()
        {
            if (Options.ExilerExe.GetBool() && !Exiler.ExilerSelected)
            {
                Exiler.SelectExiler();

                if (!Exiler.ExilerSelected)
                    BMLogger.Info("[Exiler] Exiler non assegnato.");
            }
        }

        public static void SelectExiler()
        {
            if (!Options.ExilerExe.GetBool())
                return;

            var allPlayers = BanMod.AllPlayerControls;

            var alivePlayers = allPlayers
                .Where(p => p.Data != null && !p.Data.IsDead
                            && p.PlayerId != Guesser.SpecialKillerId
                            && p.PlayerId != Jester.JesterId
                            && p.PlayerId != Watcher.WatcherId
                            && !Scientist(p)
                            && !Engineer(p)
                            && !Tracker(p)
                            && (!BanMod.forceImpostor || !BanMod.forcedImpostorIds.Contains(p.PlayerId))
                            && !(Options.PhantomGuess.GetBool() && Phantom(p))
                            && !(Options.ViperGuess.GetBool() && Cobra(p))
                            && !(Options.ImpostorGuess.GetBool() && Impostor(p))
                            && !(Options.ShapeGuess.GetBool() && Shapeshifter(p)))
                .ToList();

            if (alivePlayers.Count == 0)
            {
                ExilerId = 255;
                ExilerSelected = false;
                BMLogger.Info("[Exiler] Nessun candidato valido per Exiler trovato.");
                return;
            }

            var randomPlayer = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];

            ExilerId = randomPlayer.PlayerId;
            ExilerSelected = true;

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.SetExiler,
                    SendOption.Reliable,
                    -1);

                writer.Write(ExilerId);

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

        public static void SendExilerMessage()
        {
            if (ExilerId == 255)
                return;

            var allPlayers = BanMod.AllPlayerControls;
            var killer = allPlayers.FirstOrDefault(p => p.PlayerId == ExilerId);

            if (killer == null || killer.Data == null || killer.Data.IsDead)
                return;

            string msg = string.Format(GetString("ExilerInfo"));

            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, ExilerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, ExilerId);
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

        public static void ResetExiler()
        {
            ExilerId = 255;
            ExilerSelected = false;

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