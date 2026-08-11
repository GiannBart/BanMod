//credits and licenses in the resources folder
using AmongUs.GameOptions;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod;

public static class TaskManager
{
    public static readonly Dictionary<byte, float> TaskCompletionTimes = new();
    public static bool taskAssigned = false;
    public static byte? TaskPlayerId = null;
    public static string WinnerName = "Unknown";
    public static float WinnerTotalTime = 0f;
    public static float GameStartTime = 0f;

    public static void OnPlayerCompletedTasks(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player == null || player.Data == null || player.Data.IsDead) return;
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (gameMode != GameModeType.TaskRun) return;
        if (taskAssigned) return;
        if (!PlayerTask.AllTasksCompleted(player)) return;

        if (!TaskCompletionTimes.ContainsKey(player.PlayerId))
        {
            float now = Time.time;
            TaskCompletionTimes[player.PlayerId] = now;

            taskAssigned = true;
            TaskPlayerId = player.PlayerId;
            WinnerName = player.Data.PlayerName;
            WinnerTotalTime = now - GameStartTime;

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && GameManager.Instance != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Role == null)
                        continue;

                    if (pc.PlayerId != TaskPlayerId)
                    {
                        pc.RpcSetRole(RoleTypes.ImpostorGhost);
                    }
                }

                LateTask.New(() =>
                {
                    GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByVote, false);
                }, 0.5f, "TaskWin EndGame");
            }

            BMLogger.Info($"[TaskManager] Player {WinnerName} (ID: {TaskPlayerId}) won in {WinnerTotalTime:F2}s.");
            MatchSummary1.TaskWin = true;

        }
    }


    public static void ResetTaskManager()
    {
        TaskCompletionTimes.Clear();
        taskAssigned = false;
        TaskPlayerId = null;
        WinnerName = "Unknown";
        WinnerTotalTime = 0f;
        GameStartTime = Time.time;
        BMLogger.Info("[TaskManager] Reset for new session.");
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class TaskManager_CompleteTask_Patch
{
    public static void Postfix(PlayerControl __instance, uint idx)
    {
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (!AmongUsClient.Instance.AmHost) return;
        if (gameMode != GameModeType.TaskRun) return;

        TaskManager.OnPlayerCompletedTasks(__instance);
    }
}
