////credits and licenses in the resources folder
//using BepInEx.Unity.IL2CPP.Utils;
//using HarmonyLib;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace BanMod;

//[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
//public static class MeetingHud_Update
//{
//    public static List<int> votedPlayers = new List<int>();
//    public static void Prefix(MeetingHud __instance)
//    {
//        if (!AmongUsClient.Instance.AmHost) return;
//        if (__instance.state < MeetingHud.VoteStates.Results)
//        {
//            foreach (var playerVoteArea in __instance.playerStates)
//            {
//                if (!playerVoteArea) continue;
//                var playerData = GameData.Instance.GetPlayerById(playerVoteArea.TargetPlayerId);
//                if (playerData != null && !playerData.Disconnected && playerVoteArea.VotedFor != PlayerVoteArea.HasNotVoted && playerVoteArea.VotedFor != PlayerVoteArea.MissedVote && playerVoteArea.VotedFor != PlayerVoteArea.DeadVote && !votedPlayers.Contains(playerVoteArea.TargetPlayerId))
//                {
//                    votedPlayers.Add(playerVoteArea.TargetPlayerId);
//                    if (playerVoteArea.VotedFor != PlayerVoteArea.SkippedVote)
//                    {
//                        foreach (var votedForArea in __instance.playerStates)
//                        {
//                            if (votedForArea.TargetPlayerId == playerVoteArea.VotedFor)
//                            {
//                                __instance.BloopAVoteIcon(playerData, 0, votedForArea.transform);
//                                break;
//                            }
//                        }
//                    }
//                    else if (__instance.SkippedVoting)
//                    {
//                        __instance.BloopAVoteIcon(playerData, 0, __instance.SkippedVoting.transform);
//                    }
//                }
//            }

//            foreach (var votedForArea in __instance.playerStates)
//            {
//                if (!votedForArea) continue;
//                var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
//                if (!voteSpreader) continue;
//                foreach (var spriteRenderer in voteSpreader.Votes)
//                {
//                    spriteRenderer.gameObject.SetActive(BanMod.revealVotes);
//                }
//            }

//            if (__instance.SkippedVoting)
//            {
//                __instance.SkippedVoting.SetActive(BanMod.revealVotes);
//            }
//        }
//    }

//}

//[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
//public static class MeetingHud_PopulateResults
//{
//    public static void Prefix(MeetingHud __instance)
//    {
//        if (!AmongUsClient.Instance.AmHost) return;
//        foreach (var votedForArea in __instance.playerStates)
//        {
//            if (!votedForArea) continue;
//            var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
//            if (!voteSpreader) continue;
//            var length = voteSpreader.Votes.Count;
//            if (length == 0) continue;
//            foreach (var spriteRenderer in voteSpreader.Votes)
//            {
//                Object.DestroyImmediate(spriteRenderer);
//            }
//            voteSpreader.Votes.Clear();
//        }
//        if (__instance.SkippedVoting)
//        {
//            var voteSpreader = __instance.SkippedVoting.transform.GetComponent<VoteSpreader>();
//            foreach (var spriteRenderer in voteSpreader.Votes)
//            {
//                Object.DestroyImmediate(spriteRenderer);
//            }
//            voteSpreader.Votes.Clear();
//        }
//        MeetingHud_Update.votedPlayers.Clear();
//    }
//}
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class MeetingHud_Update
{
    public static readonly HashSet<byte> VotedPlayers = new();

    public static void Prefix(MeetingHud __instance)
    {
        if (__instance == null)
            return;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (__instance.playerStates == null ||
            GameData.Instance == null)
        {
            return;
        }

        if (__instance.state >= MeetingHud.VoteStates.Results)
            return;

        foreach (var playerVoteArea in __instance.playerStates)
        {
            if (!playerVoteArea)
                continue;

            byte voterId = playerVoteArea.TargetPlayerId;
            byte votedForId = playerVoteArea.VotedFor;

            var playerData = GameData.Instance.GetPlayerById(voterId);

            if (playerData == null || playerData.Disconnected)
                continue;

            if (votedForId == PlayerVoteArea.HasNotVoted ||
                votedForId == PlayerVoteArea.MissedVote ||
                votedForId == PlayerVoteArea.DeadVote)
            {
                continue;
            }

            if (!VotedPlayers.Add(voterId))
                continue;

            if (votedForId == PlayerVoteArea.SkippedVote)
            {
                if (__instance.SkippedVoting)
                {
                    __instance.BloopAVoteIcon(
                        playerData,
                        0,
                        __instance.SkippedVoting.transform
                    );
                }

                continue;
            }

            foreach (var votedForArea in __instance.playerStates)
            {
                if (!votedForArea)
                    continue;

                if (votedForArea.TargetPlayerId != votedForId)
                    continue;

                if (votedForArea.transform != null)
                {
                    __instance.BloopAVoteIcon(
                        playerData,
                        0,
                        votedForArea.transform
                    );
                }

                break;
            }
        }

        foreach (var votedForArea in __instance.playerStates)
        {
            if (!votedForArea || votedForArea.transform == null)
                continue;

            var voteSpreader =
                votedForArea.transform.GetComponent<VoteSpreader>();

            if (!voteSpreader || voteSpreader.Votes == null)
                continue;

            foreach (var spriteRenderer in voteSpreader.Votes)
            {
                if (!spriteRenderer)
                    continue;

                if (spriteRenderer.gameObject != null)
                {
                    spriteRenderer.gameObject.SetActive(
                        Options.revealVotes.GetBool()
                    );
                }
            }
        }

        if (__instance.SkippedVoting)
        {
            var skipSpreader =
                __instance.SkippedVoting.transform
                    .GetComponent<VoteSpreader>();

            if (skipSpreader != null &&
                skipSpreader.Votes != null)
            {
                foreach (var spriteRenderer in skipSpreader.Votes)
                {
                    if (spriteRenderer &&
                        spriteRenderer.gameObject != null)
                    {
                        spriteRenderer.gameObject.SetActive(
                            Options.revealVotes.GetBool()
                        );
                    }
                }
            }
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
public static class MeetingHud_PopulateResults
{
    public static void Prefix(MeetingHud __instance)
    {
        if (__instance == null)
            return;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (__instance.playerStates != null)
        {
            foreach (var votedForArea in __instance.playerStates)
            {
                if (!votedForArea ||
                    votedForArea.transform == null)
                {
                    continue;
                }

                ClearVotes(
                    votedForArea.transform.GetComponent<VoteSpreader>()
                );
            }
        }

        if (__instance.SkippedVoting)
        {
            ClearVotes(
                __instance.SkippedVoting.transform
                    .GetComponent<VoteSpreader>()
            );
        }

        MeetingHud_Update.VotedPlayers.Clear();
    }

    private static void ClearVotes(VoteSpreader voteSpreader)
    {
        if (!voteSpreader || voteSpreader.Votes == null)
            return;

        foreach (var spriteRenderer in voteSpreader.Votes)
        {
            if (!spriteRenderer)
                continue;

            if (spriteRenderer.gameObject != null)
            {
                Object.Destroy(spriteRenderer.gameObject);
            }
        }

        voteSpreader.Votes.Clear();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MeetingHudVoteResetStartPatch
{
    public static void Prefix()
    {
        MeetingHud_Update.VotedPlayers.Clear();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
public static class MeetingHudVoteResetDestroyPatch
{
    public static void Prefix()
    {
        MeetingHud_Update.VotedPlayers.Clear();
    }
}