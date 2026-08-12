using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BanMod;

public static class MatchTextLogger
{
    private sealed class VoteEntry
    {
        public string Text;
    }

    private sealed class MeetingEntry
    {
        public int Number;
        public bool Finished;
        public readonly Dictionary<byte, string> AlivePlayers = new();
        public readonly HashSet<byte> PlayersWhoVoted = new();
        public readonly List<VoteEntry> Votes = new();
    }

    private sealed class TaskEntry
    {
        public uint TaskId;
        public string TaskName;
        public float CompletionTime;
    }

    private sealed class PlayerTaskEntry
    {
        public byte PlayerId;
        public string PlayerName;
        public string ColorName;
        public int TotalTasks;
        public readonly HashSet<uint> CompletedTaskIds = new();
        public readonly List<TaskEntry> Tasks = new();
    }

    private static readonly List<MeetingEntry> Meetings = new();
    private static readonly Dictionary<byte, PlayerTaskEntry> PlayersTasks = new();
    private static MeetingEntry currentMeeting;
    private static float matchStartTime;
    private static DateTime matchStartDate;
    private static bool matchActive;
    private static bool reportSaved;

    private static bool IsHost =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.AmHost;

    public static void StartMatch()
    {
        if (!IsHost)
            return;

        Meetings.Clear();
        PlayersTasks.Clear();
        currentMeeting = null;
        matchStartTime = Time.realtimeSinceStartup;
        matchStartDate = DateTime.Now;
        matchActive = true;
        reportSaved = false;

        BMLogger.Info("[MatchTextLogger] Match recording started.");
    }

    public static void StartMeeting(MeetingHud meetingHud)
    {
        if (!IsHost || !matchActive || meetingHud == null)
            return;

        FinishMeeting();

        currentMeeting = new MeetingEntry
        {
            Number = Meetings.Count + 1
        };

        if (meetingHud.playerStates != null && GameData.Instance != null)
        {
            foreach (var voteArea in meetingHud.playerStates)
            {
                if (!voteArea)
                    continue;

                byte playerId = voteArea.TargetPlayerId;
                var playerData = GameData.Instance.GetPlayerById(playerId);

                if (playerData == null || playerData.Disconnected || playerData.IsDead)
                    continue;

                currentMeeting.AlivePlayers[playerId] = GetColorName(playerData);
            }
        }

        Meetings.Add(currentMeeting);
    }

    public static void RecordVote(MeetingHud meetingHud, byte voterId)
    {
        if (!IsHost ||
            !matchActive ||
            currentMeeting == null ||
            currentMeeting.Finished ||
            meetingHud == null ||
            meetingHud.playerStates == null)
        {
            return;
        }

        if (!currentMeeting.AlivePlayers.ContainsKey(voterId))
            return;

        if (currentMeeting.PlayersWhoVoted.Contains(voterId))
            return;

        PlayerVoteArea voterArea = null;

        foreach (var area in meetingHud.playerStates)
        {
            if (area && area.TargetPlayerId == voterId)
            {
                voterArea = area;
                break;
            }
        }

        if (!voterArea || !voterArea.DidVote)
            return;

        byte votedForId = voterArea.VotedFor;

        if (votedForId == PlayerVoteArea.HasNotVoted ||
            votedForId == PlayerVoteArea.MissedVote ||
            votedForId == PlayerVoteArea.DeadVote)
        {
            return;
        }

        string voterColor = currentMeeting.AlivePlayers[voterId];
        string voteText;

        if (votedForId == PlayerVoteArea.SkippedVote)
        {
            voteText = $"{voterColor} skipped";
        }
        else
        {
            if (!currentMeeting.AlivePlayers.TryGetValue(votedForId, out string targetColor))
            {
                var target = GameData.Instance?.GetPlayerById(votedForId);
                targetColor = target != null
                    ? GetColorName(target)
                    : $"Player {votedForId}";
            }

            voteText = $"{voterColor} voted {targetColor}";
        }

        currentMeeting.PlayersWhoVoted.Add(voterId);
        currentMeeting.Votes.Add(new VoteEntry { Text = voteText });
    }

    public static void FinishMeeting()
    {
        if (currentMeeting == null || currentMeeting.Finished)
            return;

        currentMeeting.Finished = true;
        currentMeeting = null;
    }

    public static void RecordTask(PlayerControl player, uint taskId)
    {
        if (!IsHost || !matchActive || player == null || player.Data == null)
            return;

        if (player.Data.Role != null &&
            player.Data.Role.TeamType == RoleTeamTypes.Impostor)
        {
            return;
        }

        PlayerTaskEntry playerEntry = GetOrCreateTaskPlayer(player);

        if (!playerEntry.CompletedTaskIds.Add(taskId))
            return;

        float elapsed = Math.Max(
            0f,
            Time.realtimeSinceStartup - matchStartTime
        );

        playerEntry.Tasks.Add(new TaskEntry
        {
            TaskId = taskId,
            TaskName = GetTaskName(player, taskId),
            CompletionTime = elapsed
        });

        playerEntry.TotalTasks = Math.Max(
            playerEntry.TotalTasks,
            GetTotalTasks(player)
        );
    }

    public static void SaveReport()
    {
        if (!IsHost || !matchActive || reportSaved)
            return;

        try
        {
            FinishMeeting();
            CapturePlayersWithoutCompletedTasks();

            string directory = GetLogDirectory();

            Directory.CreateDirectory(directory);

            string fileName =
                $"Match_{matchStartDate:yyyy-MM-dd_HH-mm-ss-fff}.txt";

            string filePath = Path.Combine(directory, fileName);

            File.WriteAllText(
                filePath,
                BuildReport(),
                new UTF8Encoding(false)
            );

            reportSaved = true;
            matchActive = false;

            BMLogger.Info(
                $"[MatchTextLogger] Match report saved: {filePath}"
            );
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchTextLogger] Failed to save report: {exception}",
                "MatchTextLogger"
            );
        }
    }

    public static string OpenLatestReport()
    {
        try
        {
            string directory = GetLogDirectory();

            if (!Directory.Exists(directory))
                return "No match logs were found.";

            string latestReport = Directory
                .GetFiles(directory, "Match_*.txt")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(latestReport))
                return "No match logs were found.";

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = latestReport,
                    UseShellExecute = true
                }
            );

            return $"Opened match log: {Path.GetFileName(latestReport)}";
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchTextLogger] Failed to open report: {exception}",
                "MatchTextLogger"
            );

            return "The match log could not be opened.";
        }
    }

    private static string GetLogDirectory()
    {
        string desktop = Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory
        );

        return Path.Combine(desktop, "BanMod Match Logs");
    }

    private static string BuildReport()
    {
        var text = new StringBuilder();

        text.AppendLine("========================================");
        text.AppendLine("              MATCH REPORT");
        text.AppendLine("========================================");
        text.AppendLine($"Started: {matchStartDate:yyyy-MM-dd HH:mm:ss}");
        text.AppendLine();
        text.AppendLine("=============== VOTING =================");
        text.AppendLine();

        if (Meetings.Count == 0)
        {
            text.AppendLine("No meetings were held.");
            text.AppendLine();
        }
        else
        {
            foreach (var meeting in Meetings)
            {
                text.AppendLine($"Voting - Meeting {meeting.Number}");
                text.AppendLine("----------------------------------------");

                int position = 1;

                foreach (var vote in meeting.Votes)
                {
                    text.AppendLine($"{position}. {vote.Text}");
                    position++;
                }

                var nonVoters = meeting.AlivePlayers
                    .Where(player =>
                        !meeting.PlayersWhoVoted.Contains(player.Key))
                    .OrderBy(player => player.Value);

                foreach (var player in nonVoters)
                    text.AppendLine($"{player.Value} did not vote");

                text.AppendLine();
            }
        }

        var ranking = PlayersTasks.Values
            .OrderByDescending(player => player.Tasks.Count)
            .ThenBy(player =>
                player.Tasks.Count > 0
                    ? player.Tasks[player.Tasks.Count - 1].CompletionTime
                    : float.MaxValue)
            .ThenBy(player => player.PlayerName)
            .ToList();

        text.AppendLine("============= TASK RANKING =============");
        text.AppendLine();

        if (ranking.Count == 0)
        {
            text.AppendLine("No players had assigned tasks.");
            return text.ToString();
        }

        for (int i = 0; i < ranking.Count; i++)
        {
            PlayerTaskEntry player = ranking[i];

            string lastTaskTime = player.Tasks.Count > 0
                ? FormatTime(
                    player.Tasks[player.Tasks.Count - 1].CompletionTime)
                : "--:--.---";

            text.AppendLine(
                $"{i + 1}. {player.ColorName} " +
                $"({player.PlayerName}) - " +
                $"{player.Tasks.Count}/{player.TotalTasks} tasks - " +
                $"last task: {lastTaskTime}"
            );
        }

        text.AppendLine();
        text.AppendLine("============= TASK DETAILS =============");
        text.AppendLine();

        foreach (PlayerTaskEntry player in ranking)
        {
            text.AppendLine($"{player.ColorName} ({player.PlayerName})");
            text.AppendLine(
                $"Completed tasks: {player.Tasks.Count}/{player.TotalTasks}"
            );
            text.AppendLine("----------------------------------------");

            if (player.Tasks.Count == 0)
            {
                text.AppendLine("No tasks completed.");
            }
            else
            {
                for (int taskIndex = 0;
                     taskIndex < player.Tasks.Count;
                     taskIndex++)
                {
                    TaskEntry task = player.Tasks[taskIndex];

                    text.AppendLine(
                        $"{taskIndex + 1}. " +
                        $"{task.TaskName} - " +
                        $"{FormatTime(task.CompletionTime)}"
                    );
                }
            }

            text.AppendLine();
        }

        return text.ToString();
    }

    private static PlayerTaskEntry GetOrCreateTaskPlayer(
        PlayerControl player)
    {
        if (PlayersTasks.TryGetValue(
                player.PlayerId,
                out PlayerTaskEntry entry))
        {
            return entry;
        }

        entry = new PlayerTaskEntry
        {
            PlayerId = player.PlayerId,
            PlayerName = CleanText(player.Data.PlayerName),
            ColorName = GetColorName(player.Data),
            TotalTasks = GetTotalTasks(player)
        };

        PlayersTasks[player.PlayerId] = entry;
        return entry;
    }

    private static void CapturePlayersWithoutCompletedTasks()
    {
        if (PlayerControl.AllPlayerControls == null)
            return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null)
                continue;

            if (player.Data.Role != null &&
                player.Data.Role.TeamType == RoleTeamTypes.Impostor)
            {
                continue;
            }

            if (GetTotalTasks(player) <= 0)
                continue;

            GetOrCreateTaskPlayer(player);
        }
    }

    private static int GetTotalTasks(PlayerControl player)
    {
        if (player?.Data?.Tasks == null)
            return 0;

        return player.Data.Tasks.Count;
    }

    private static string GetTaskName(
        PlayerControl player,
        uint taskId)
    {
        try
        {
            if (player.myTasks != null)
            {
                foreach (var playerTask in player.myTasks)
                {
                    if (playerTask == null || playerTask.Id != taskId)
                        continue;

                    if (playerTask is NormalPlayerTask normalTask)
                        return normalTask.TaskType.ToString();

                    return playerTask.GetType().Name;
                }
            }
        }
        catch
        {
        }

        return $"Task ID {taskId}";
    }

    private static string GetColorName(NetworkedPlayerInfo player)
    {
        try
        {
            if (player?.DefaultOutfit != null)
            {
                return GetEnglishColorName(
                    player.DefaultOutfit.ColorId
                );
            }
        }
        catch
        {
        }

        return CleanText(player?.PlayerName ?? "Unknown");
    }

    private static string GetEnglishColorName(int colorId)
    {
        return colorId switch
        {
            0 => "Red",
            1 => "Blue",
            2 => "Green",
            3 => "Pink",
            4 => "Orange",
            5 => "Yellow",
            6 => "Black",
            7 => "White",
            8 => "Purple",
            9 => "Brown",
            10 => "Cyan",
            11 => "Lime",
            12 => "Maroon",
            13 => "Rose",
            14 => "Banana",
            15 => "Gray",
            16 => "Tan",
            17 => "Coral",
            18 => "Hidden",
            _ => $"Color {colorId}"
        };
    }

    private static string CleanText(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unknown"
            : value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
    }

    private static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        int remainingSeconds = (int)(seconds % 60f);
        int milliseconds = (int)((seconds * 1000f) % 1000f);

        return
            $"{minutes:D2}:" +
            $"{remainingSeconds:D2}." +
            $"{milliseconds:D3}";
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
internal static class MatchTextLoggerStartPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (FakeMapLobbyUtility.Active)
            return;

        MatchTextLogger.StartMatch();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
internal static class MatchTextLoggerTaskPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerControl __instance,
        [HarmonyArgument(0)] uint idx)
    {
        MatchTextLogger.RecordTask(__instance, idx);
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
internal static class MatchTextLoggerMeetingStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(MeetingHud __instance)
    {
        MatchTextLogger.StartMeeting(__instance);
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
internal static class MatchTextLoggerVotePatch
{
    [HarmonyPostfix]
    private static void Postfix(
        MeetingHud __instance,
        [HarmonyArgument(0)] byte srcPlayerId)
    {
        MatchTextLogger.RecordVote(__instance, srcPlayerId);
    }
}

[HarmonyPatch(typeof(MeetingHud), "VotingComplete")]
internal static class MatchTextLoggerVotingCompletePatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        MatchTextLogger.FinishMeeting();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
internal static class MatchTextLoggerMeetingDestroyPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        MatchTextLogger.FinishMeeting();
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
internal static class MatchTextLoggerEndGamePatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        MatchTextLogger.SaveReport();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
internal static class MatchTextLoggerShipDestroyPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        MatchTextLogger.SaveReport();
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal static class MatchTextLoggerChatCommandPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ChatController __instance)
    {
        if (__instance == null ||
            __instance.freeChatField == null ||
            __instance.freeChatField.textArea == null)
        {
            return true;
        }

        string text = __instance.freeChatField.textArea.text;

        if (!string.Equals(
                text?.Trim(),
                "/matchlog",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        __instance.freeChatField.textArea.SetText("");
        ChatCommands.ShowChat(MatchTextLogger.OpenLatestReport());
        return false;
    }
}
