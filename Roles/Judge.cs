using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class Judge
    {
        public static byte JudgeId = byte.MaxValue;
        public static bool JudgeSelected = false;

        public static bool JudgeCanUseEnd = false;
        public static int JudgeEndUses = 0;

        internal static byte JudgeVoteTargetId = byte.MaxValue;

        public static int JudgeEndMaxUses
        {
            get
            {
                return Options.JudgeEndUse.GetInt();
            }
        }

        public static int JudgeEndRemainingUses
        {
            get
            {
                return Mathf.Max(0, JudgeEndMaxUses - JudgeEndUses);
            }
        }

        public static void OnStart()
        {
            JudgeEndUses = 0;
            JudgeCanUseEnd = false;
            JudgeVoteTargetId = byte.MaxValue;

            if (Options.Judge.GetBool() && !JudgeSelected)
            {
                SelectJudge();

                if (!JudgeSelected)
                    BMLogger.Info("[Judge] Judge non assegnato.");
            }

            JudgeCanUseEnd =
                Options.Judge.GetBool() &&
                JudgeSelected &&
                JudgeEndMaxUses > 0;

            BMLogger.Info(
                $"[Judge] Utilizzi /end inizializzati: " +
                $"{JudgeEndUses}/{JudgeEndMaxUses}. " +
                $"Abilitato: {JudgeCanUseEnd}");
        }

        public static void SelectJudge()
        {
            if (!Options.Judge.GetBool())
                return;

            var allPlayers = BanMod.AllPlayerControls;

            var alivePlayers = allPlayers
                .Where(p => p.Data != null && !p.Data.IsDead
                            && p.PlayerId != Guesser.SpecialKillerId
                            && p.PlayerId != Jester.JesterId
                            && p.PlayerId != Watcher.WatcherId
                            && p.PlayerId != Exiler.ExilerId
                            && p.PlayerId != Profiler.ProfilerId
                            && !Scientist(p)
                            && !Engineer(p)
                            && !Tracker(p)
                            && !Detective(p)
                            && (!BanMod.forceImpostor || !BanMod.forcedImpostorIds.Contains(p.PlayerId))
                            && !(Options.PhantomGuess.GetBool() && Phantom(p))
                            && !(Options.ViperGuess.GetBool() && Cobra(p))
                            && !(Options.ImpostorGuess.GetBool() && Impostor(p))
                            && !(Options.ShapeGuess.GetBool() && Shapeshifter(p)))
                .ToList();

            if (alivePlayers.Count == 0)
            {
                JudgeId = byte.MaxValue;
                JudgeSelected = false;
                JudgeCanUseEnd = false;

                BMLogger.Info("[Judge] Nessun candidato valido per Judge trovato.");
                return;
            }

            var randomPlayer =
                alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];

            JudgeId = randomPlayer.PlayerId;
            JudgeSelected = true;
            JudgeCanUseEnd = JudgeEndMaxUses > 0;

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.SetJudge,
                    SendOption.Reliable,
                    -1);

                writer.Write(JudgeId);

                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            BMLogger.Info(
                $"[Judge] Judge assegnato al PlayerId {JudgeId}. " +
                $"Utilizzi /end: {JudgeEndMaxUses}");
        }

        public static void SendJudgeMessage()
        {
            if (JudgeId == byte.MaxValue)
                return;

            var allPlayers = BanMod.AllPlayerControls;
            var judgePlayer =
                allPlayers.FirstOrDefault(p => p.PlayerId == JudgeId);

            if (judgePlayer == null ||
                judgePlayer.Data == null ||
                judgePlayer.Data.IsDead)
            {
                return;
            }

            string msg = string.Format(GetString("JudgeInfo"));

            if (AmongUsClient.Instance.AmHost &&
                PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, JudgeId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, JudgeId);
                MessageBlocker.UpdateLastMessageTime();
            }
        }

        public static void ResetEndUses()
        {
            JudgeEndUses = 0;

            JudgeCanUseEnd =
                Options.Judge.GetBool() &&
                JudgeSelected &&
                JudgeEndMaxUses > 0;

            BMLogger.Info(
                $"[Judge] Utilizzi /end ripristinati: " +
                $"{JudgeEndUses}/{JudgeEndMaxUses}. " +
                $"Abilitato: {JudgeCanUseEnd}");
        }

        public static bool TryUseEndCommand()
        {
            int maxUses = JudgeEndMaxUses;

            if (maxUses <= 0)
            {
                JudgeCanUseEnd = false;
                return false;
            }

            if (!JudgeCanUseEnd || JudgeEndUses >= maxUses)
            {
                JudgeCanUseEnd = false;
                return false;
            }

            JudgeEndUses++;
            JudgeCanUseEnd = JudgeEndUses < maxUses;

            BMLogger.Info(
                $"[Judge] /end usato: {JudgeEndUses}/{maxUses}. " +
                $"Rimasti: {JudgeEndRemainingUses}. " +
                $"Abilitato: {JudgeCanUseEnd}");

            return true;
        }

        public static bool TryHandleEndCommand(
            PlayerControl player,
            string command,
            out bool canceled)
        {
            canceled = false;

            if (string.IsNullOrWhiteSpace(command))
                return false;

            command = command.ToLowerInvariant();

            if (command != "/end" && command != "/close")
                return false;

            canceled = true;

            if (!AmongUsClient.Instance.AmHost)
                return true;

            if (!Options.Judge.GetBool())
                return true;

            if (player == null ||
                !JudgeSelected ||
                JudgeId == byte.MaxValue ||
                player.PlayerId != JudgeId)
            {
                return true;
            }

            if (MeetingHud.Instance == null)
            {
                Utils.SendMessage(
                    "Puoi usare /end soltanto durante una riunione.",
                    player.PlayerId);

                return true;
            }

            MeetingHud.VoteStates meetingState =
                MeetingHud.Instance.CurrentState;

            bool meetingCanBeClosed =
                meetingState == MeetingHud.VoteStates.Discussion ||
                meetingState == MeetingHud.VoteStates.NotVoted ||
                meetingState == MeetingHud.VoteStates.Voted;

            if (!meetingCanBeClosed)
            {
                Utils.SendMessage(
                    "La riunione non può essere terminata in questo momento.",
                    player.PlayerId);

                return true;
            }

            if (JudgeEndMaxUses <= 0)
            {
                JudgeCanUseEnd = false;

                Utils.SendMessage(
                    "Il comando /end è disabilitato nelle opzioni.",
                    player.PlayerId);

                return true;
            }

            if (!TryUseEndCommand())
            {
                Utils.SendMessage(
                    $"Hai esaurito gli utilizzi di /end. " +
                    $"Utilizzi: {JudgeEndUses}/{JudgeEndMaxUses}.",
                    player.PlayerId);

                return true;
            }

            if (JudgeEndRemainingUses > 0)
            {
                Utils.SendMessage(
                    $"Riunione terminata. Utilizzi /end rimasti: " +
                    $"{JudgeEndRemainingUses}.",
                    player.PlayerId);
            }
            else
            {
                Utils.SendMessage(
                    "Riunione terminata. Hai esaurito gli utilizzi di /end.",
                    player.PlayerId);
            }

            PlayerControl.LocalPlayer.StartCoroutine(
                Utils.DelayedCloseMeeting());

            return true;
        }

        public static void ResetJudge()
        {
            JudgeId = byte.MaxValue;
            JudgeSelected = false;

            JudgeEndUses = 0;
            JudgeCanUseEnd = false;
            JudgeVoteTargetId = byte.MaxValue;
        }
    }

    [HarmonyPatch(typeof(MeetingHud))]
    [HarmonyPatch(nameof(MeetingHud.CheckForEndVoting))]
    public static class JudgeCheckForEndVotingPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost)
                return true;

            if (!Options.Judge.GetBool())
                return true;

            if (!Judge.JudgeSelected ||
                Judge.JudgeId == byte.MaxValue)
            {
                return true;
            }

            if (!__instance.playerStates.All(
                    state => state.AmDead || state.DidVote))
            {
                return true;
            }

            PlayerVoteArea judgeVoteArea =
                __instance.playerStates.FirstOrDefault(
                    state =>
                        state != null &&
                        state.TargetPlayerId == Judge.JudgeId);

            if (judgeVoteArea == null ||
                judgeVoteArea.AmDead ||
                !judgeVoteArea.DidVote)
            {
                return true;
            }

            byte judgeTarget = judgeVoteArea.VotedFor;

            // Non raddoppiare mancato voto, skip o voto forzato.
            if (judgeTarget == 252 ||
                judgeTarget == 253 ||
                judgeTarget == 254 ||
                judgeTarget == 255)
            {
                return true;
            }

            var voteDict = __instance.CalculateVotes();

            if (voteDict == null ||
                voteDict.Count == 0 ||
                !voteDict.TryGetValue(
                    judgeTarget,
                    out int normalVotes))
            {
                return true;
            }

            voteDict[judgeTarget] = normalVotes * 2;

            bool tie;
            var max = voteDict.MaxPair(out tie);

            NetworkedPlayerInfo exiled = null;

            if (!tie)
            {
                for (int i = 0; i < GameData.Instance.AllPlayers.Count; i++)
                {
                    NetworkedPlayerInfo player =
                        GameData.Instance.AllPlayers[i];

                    if (player != null &&
                        player.PlayerId == max.Key)
                    {
                        exiled = player;
                        break;
                    }
                }
            }

            MeetingHud.VoterState[] normalStates =
                new MeetingHud.VoterState[
                    __instance.playerStates.Length];

            for (int i = 0;
                 i < __instance.playerStates.Length;
                 i++)
            {
                PlayerVoteArea area =
                    __instance.playerStates[i];

                normalStates[i] =
                    new MeetingHud.VoterState
                    {
                        VoterId = area.TargetPlayerId,
                        VotedForId = area.VotedFor
                    };
            }

            MeetingHud.VoterState[] duplicatedStates =
                normalStates
                    .Where(state =>
                        state.VotedForId == judgeTarget)
                    .ToArray();

            MeetingHud.VoterState[] finalStates =
                normalStates
                    .Concat(duplicatedStates)
                    .ToArray();

            BMLogger.Info(
                $"[Judge] Voti e icone per PlayerId " +
                $"{judgeTarget}: {normalVotes} -> " +
                $"{normalVotes * 2}");

            __instance.RpcVotingComplete(
                finalStates,
                exiled,
                tie);

            return false;
        }
    }

}