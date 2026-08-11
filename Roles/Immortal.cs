//credits and licenses in the resources folder
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

public static class ImmortalManager
{
    public static readonly HashSet<byte> ImmortalPlayers = new();
    public static readonly Dictionary<byte, float> TaskCompletionTimes = new(); 
    public static bool immortalAssigned = false;
    public static string LastImmortalPlayerName = null;


    public static byte? ImmortalPlayerId = null;

    public static void OnPlayerCompletedTasks(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player == null || player.Data == null || player.Data.IsDead) return;
        if (!Options.EnableImmortal.GetBool()) return;
        if (immortalAssigned) return;

        if (Utils.Engineer(player)) return;
        if (Utils.Detective(player)) return;
        if (Utils.Noisemaker(player)) return;
        if (player.PlayerId == Guesser.SpecialKillerId) return;
        if (player.PlayerId == Watcher.WatcherId) return;
        if (player.PlayerId == Jester.JesterId) return;

        if (PlayerTask.AllTasksCompleted(player))
        {
            if (!TaskCompletionTimes.ContainsKey(player.PlayerId))
            {
                TaskCompletionTimes[player.PlayerId] = Time.time; 
                BMLogger.Info($"[ImmortalManager] Player {player.PlayerId} finished all tasks at time {Time.time}");
            }

            TryAssignImmortal();
        }
    }

    private static void TryAssignImmortal()
    {
        if (!Options.EnableImmortal.GetBool()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (immortalAssigned) return;
        if (TaskCompletionTimes.Count == 0) return;

        var orderedFinishers = TaskCompletionTimes
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .Where(pid =>
            {
                var p = BanMod.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == pid);
                if (p == null) return false;

                if (Utils.Engineer(p)) return false;
                if (pid == Guesser.SpecialKillerId) return false;
                if (pid == Jester.JesterId) return false;

                return true;
            })
            .ToList();

        if (orderedFinishers.Count == 0) return;

        byte immortalCandidate = orderedFinishers[0];

        ImmortalPlayers.Add(immortalCandidate);
        ImmortalPlayerId = immortalCandidate;
        immortalAssigned = true;

        if (AmongUsClient.Instance.AmHost) SendHostTripleBoolRpc();
        BMLogger.Info($"[ImmortalManager] Player {immortalCandidate} assigned as Immortal.");

        NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, "Immortal Added");

        if (Options.sendtoAll.GetBool())
        {
            string msgAll = GetString("immortaladded");
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                Utils.RequestProxyMessage(msgAll);
            else
                Utils.SendMessage(msgAll, 255);
            MessageBlocker.UpdateLastMessageTime();
        }

        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == immortalCandidate);
        if (player != null)
        {
            if (Options.sendtoimmortal.GetBool())
            {
                string msgPriv = GetString("ImmortalSelfMessage");
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    Utils.RequestProxyMessage(msgPriv, immortalCandidate);
                else
                    Utils.SendMessage(msgPriv, immortalCandidate);
                MessageBlocker.UpdateLastMessageTime();
            }

            if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
            {
                BanMod.ShieldedPlayers.Add(player.PlayerId);
                LastImmortalPlayerName = player.Data?.PlayerName;
            }
        }
    }
    public static bool IsImmortalEnabledAndAktive(byte playerId)
    {
        {
            return Options.EnableImmortal.GetBool() && ImmortalPlayers.Contains(playerId);
        }
    }

    public static bool IsImmortal(byte playerId)
    {
        return ImmortalPlayers.Contains(playerId);
    }
    public static void ResetImmortal()
    {
        ImmortalPlayers.Clear();
        TaskCompletionTimes.Clear();
        immortalAssigned = false;
        ImmortalPlayerId = null;
        LastImmortalPlayerName = null;
        BMLogger.Info("[ImmortalManager] Reset for new session.");
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class TaskCompleteImmortalPatch
{
    public static void Postfix(PlayerControl __instance, uint idx)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Options.EnableImmortal.GetBool()) return;
        var player = __instance;

        ImmortalManager.OnPlayerCompletedTasks(player);

        if (ImmortalManager.IsImmortal(player.PlayerId))
        {
            if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
            {
                BanMod.ShieldedPlayers.Add(player.PlayerId);
            }
        }
    }
}

[HarmonyPatch(typeof(MeetingHud))]
[HarmonyPatch(nameof(MeetingHud.CheckForEndVoting))]
public static class MeetingHud_CheckForEndVoting_Patch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (!Options.Immortalesentvote.GetBool())
            return true;

        if (VoteContextManager.IsForcedVote)
            return true;

        if (!__instance.playerStates.All(ps => ps.AmDead || ps.DidVote))
            return true;

        var voteDict = __instance.CalculateVotes();

        if (voteDict.Count == 0) return true;


        bool tieOriginal;
        var max = voteDict.MaxPair(out tieOriginal);

        byte exiledId;
        bool isTie;

        if (tieOriginal)
        {
            exiledId = byte.MaxValue;
            isTie = true;
        }
        else
        {
            exiledId = max.Key;

            if (ImmortalManager.IsImmortal(exiledId))
            {
                exiledId = byte.MaxValue;
                isTie = false; 
            }
            else
            {
                isTie = false; 
            }
        }

        NetworkedPlayerInfo exiled =
            exiledId == byte.MaxValue ? null : GameData.Instance.GetPlayerById(exiledId);

        var states = new MeetingHud.VoterState[__instance.playerStates.Length];
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            var area = __instance.playerStates[i];
            states[i] = new MeetingHud.VoterState
            {
                VoterId = area.TargetPlayerId,
                VotedForId = area.VotedFor
            };
        }

        __instance.RpcVotingComplete(states, exiled, isTie);
        return false;
    }
}