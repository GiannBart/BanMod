//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class MeetingHud_Update
{
    public static List<int> votedPlayers = new List<int>();
    public static void Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (__instance.state < MeetingHud.VoteStates.Results)
        {
            foreach (var playerVoteArea in __instance.playerStates)
            {
                if (!playerVoteArea) continue;
                var playerData = GameData.Instance.GetPlayerById(playerVoteArea.TargetPlayerId);
                if (playerData != null && !playerData.Disconnected && playerVoteArea.VotedFor != PlayerVoteArea.HasNotVoted && playerVoteArea.VotedFor != PlayerVoteArea.MissedVote && playerVoteArea.VotedFor != PlayerVoteArea.DeadVote && !votedPlayers.Contains(playerVoteArea.TargetPlayerId))
                {
                    votedPlayers.Add(playerVoteArea.TargetPlayerId);
                    if (playerVoteArea.VotedFor != PlayerVoteArea.SkippedVote)
                    {
                        foreach (var votedForArea in __instance.playerStates)
                        {
                            if (votedForArea.TargetPlayerId == playerVoteArea.VotedFor)
                            {
                                __instance.BloopAVoteIcon(playerData, 0, votedForArea.transform);
                                break;
                            }
                        }
                    }
                    else if (__instance.SkippedVoting)
                    {
                        __instance.BloopAVoteIcon(playerData, 0, __instance.SkippedVoting.transform);
                    }
                }
            }

            foreach (var votedForArea in __instance.playerStates)
            {
                if (!votedForArea) continue;
                var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
                if (!voteSpreader) continue;
                foreach (var spriteRenderer in voteSpreader.Votes)
                {
                    spriteRenderer.gameObject.SetActive(BanMod.revealVotes);
                }
            }

            if (__instance.SkippedVoting)
            {
                __instance.SkippedVoting.SetActive(BanMod.revealVotes);
            }
        }
    }

}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
public static class MeetingHud_PopulateResults
{
    public static void Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var votedForArea in __instance.playerStates)
        {
            if (!votedForArea) continue;
            var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
            if (!voteSpreader) continue;
            var length = voteSpreader.Votes.Count;
            if (length == 0) continue;
            foreach (var spriteRenderer in voteSpreader.Votes)
            {
                Object.DestroyImmediate(spriteRenderer);
            }
            voteSpreader.Votes.Clear();
        }
        if (__instance.SkippedVoting)
        {
            var voteSpreader = __instance.SkippedVoting.transform.GetComponent<VoteSpreader>();
            foreach (var spriteRenderer in voteSpreader.Votes)
            {
                Object.DestroyImmediate(spriteRenderer);
            }
            voteSpreader.Votes.Clear();
        }
        MeetingHud_Update.votedPlayers.Clear();
    }
}
