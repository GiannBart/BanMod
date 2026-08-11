//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace BanMod
{
    public static class JesterWinState
    {
        public static bool JesterWon = false;
        public static byte WinnerId = byte.MaxValue;
        public static readonly Color JesterWinColor = new Color(1f, 0.35f, 0.9f);
        public static bool endgame = false;
        public static void Reset()
        {
            JesterWon = false;
            endgame = false;
            WinnerId = byte.MaxValue;
        }

        public static void SetWinner(byte playerId)
        {
            JesterWon = true;
            WinnerId = playerId;

            MatchSummary1.JesterWin = true;
            MatchSummary1.CrewmateWin = false;
            MatchSummary1.ImpostorWin = false;

            PreviousMatchPopupTracker.JesterWin = true;
            PreviousMatchPopupTracker.JesterName = Jester.LastJesterName;
        }

        public static bool IsActive()
        {
            return JesterWon && WinnerId != byte.MaxValue;
        }
    }

    public static class JesterWinHelper
    {
        public static void TryTriggerJesterWin()
        {
            if (Jester.JesterId == 255) return;
            if (JesterWinState.JesterWon) return;

            JesterWinState.SetWinner(Jester.JesterId);
            JesterWinState.endgame = false;
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && GameManager.Instance != null)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.Role == null)
                        continue;
                    if (player.PlayerId != Jester.JesterId)
                    {
                        player.RpcSetRole(RoleTypes.ImpostorGhost);
                    }
                }
            }
        }
        public static PlayerControl GetJesterPlayer()
        {
            if (Jester.JesterId == 255) return null;
            if (PlayerControl.AllPlayerControls == null) return null;

            for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
            {
                var pc = PlayerControl.AllPlayerControls[i];
                if (pc != null && pc.PlayerId == Jester.JesterId)
                    return pc;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    public static class MeetingHud_CheckForEndVoting_JesterCancelPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!Options.Jester.GetBool()) return true;

            if (!__instance.playerStates.All(ps => ps.AmDead || ps.DidVote)) return true;

            var voteDict = __instance.CalculateVotes();
            if (voteDict.Count == 0) return true;

            bool tie;
            var max = voteDict.MaxPair(out tie);

            if (!tie && max.Key == Jester.JesterId)
            {
                MeetingHud.VoterState[] array = new MeetingHud.VoterState[__instance.playerStates.Length];
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    array[i] = new MeetingHud.VoterState
                    {
                        VoterId = playerVoteArea.TargetPlayerId,
                        VotedForId = playerVoteArea.VotedFor
                    };
                }

                NetworkedPlayerInfo exiled = null;
                foreach (var p in GameData.Instance.AllPlayers)
                {
                    if (p.PlayerId == Jester.JesterId)
                    {
                        exiled = p;
                        break;
                    }
                }
                JesterWinHelper.TryTriggerJesterWin();
                JesterWinState.SetWinner(Jester.JesterId);
                __instance.RpcVotingComplete(array, exiled, false);

                LateTask.New(() =>
                {
                    GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByVote, false);
                }, 11f, "Jester EndGame");

                return false; 
            }

            return true;
        }
    }
}