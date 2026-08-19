//credits and licenses in the resources folder
using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class Jester
    {
        public static byte JesterId = 255; 
        public static byte ForcedJesterId = 255;
        public static bool ForcedJesterSelected = false;
        public static bool JesterSelected = false;
        public static string LastJesterName = string.Empty;


        public static void OnStart()
        {
            if (!Options.Jester.GetBool()) return;

            if (!JesterSelected)
            {
                SelectJester();

                if (!JesterSelected)
                {
                    BMLogger.Info("[Jester] Jester non assegnato: nessun candidato valido.");
                }
            }
        }

        public static void SelectJester()
        {
            if (!Options.Jester.GetBool()) return;

            var allPlayers = BanMod.AllPlayerControls;

            var jesterPlayers = allPlayers
                .Where(p => p.Data != null && !p.Data.IsDead
                            && Crewmate(p)
                            && p.PlayerId != Exiler.ExilerId
                            && p.PlayerId != Judge.JudgeId
                            && !Judge(p)
                            && p.PlayerId != Profiler.ProfilerId
                            && p.PlayerId != Watcher.WatcherId
                            && p.PlayerId != Guesser.SpecialKillerId)
                .ToList();

            if (jesterPlayers.Count == 0)
            {
                JesterId = 255;
                JesterSelected = false;
                BMLogger.Info("[Jester] Nessun candidato valido per Jester trovato.");
                return;
            }

            PlayerControl chosenPlayer = null;

            if (ForcedJesterId != 255)
            {
                chosenPlayer = jesterPlayers.FirstOrDefault(p => p.PlayerId == ForcedJesterId);

                if (chosenPlayer != null)
                {
                    BMLogger.Info($"[Jester] Jester impostato manualmente: {chosenPlayer.name}");
                }
                else
                {
                    BMLogger.Info("[Jester] Il player impostato manualmente non è valido, uso random.");
                    ForcedJesterId = 255;
                }
            }

            if (chosenPlayer == null)
            {
                chosenPlayer = jesterPlayers[UnityEngine.Random.Range(0, jesterPlayers.Count)];
            }

            JesterId = chosenPlayer.PlayerId;
            JesterSelected = true;

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.SetJester,
                    SendOption.Reliable,
                    -1
                );
                writer.Write(JesterId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            ForcedJesterId = 255;
        }

        public static void Update()
        {
            if (!JesterSelected || JesterId == 255) return;

            var jesterPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p != null && p.PlayerId == JesterId);

            if (jesterPlayer != null && jesterPlayer.Data != null)
            {
                string currentName = jesterPlayer.Data.PlayerName;

                if (LastJesterName != currentName)
                {
                    LastJesterName = currentName;
                }
            }
        }

        public static bool IsJester(byte playerId)
        {
            return Options.Jester.GetBool() && JesterSelected && playerId == JesterId;
        }

        public static bool IsJester(PlayerControl player)
        {
            return player != null && IsJester(player.PlayerId);
        }

        public static RoleTypes GetAssignedRole()
        {
            if (Options.Jester.GetBool() &&
                Options.JesterVent.GetBool() &&
                JesterSelected &&
                JesterId != 255)
            {
                return RoleTypes.Engineer;
            }

            return RoleTypes.Crewmate;
        }

        public static void SendJesterMessage()
        {
            if (JesterId == 255) return;

            var allPlayers = BanMod.AllPlayerControls;
            var killer = allPlayers.FirstOrDefault(p => p.PlayerId == JesterId);
            if (killer == null || killer.Data == null || killer.Data.IsDead) return;

            string msg = string.Format(GetString("JesterInfo"));
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, JesterId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, JesterId);
                MessageBlocker.UpdateLastMessageTime();
            }
        }

       
        public static void ResetJester()
        {
            JesterId = 255;
            ForcedJesterId = 255;
            ForcedJesterSelected = false;
            JesterSelected = false;
            LastJesterName = string.Empty;
        }
    }
}