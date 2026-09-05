// credits and licenses in the resources folder
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BanMod;

public static class MatchChatLogger
{
    private sealed class ChatEntry
    {
        public float Time;
        public byte PlayerId;
        public string PlayerName;
        public string ColorName;
        public bool WasDead;
        public string Context;
        public string Message;
    }

    private static readonly List<ChatEntry> Entries = new();

    private static bool recording;
    private static bool finished;
    private static bool suppressRecording;
    private static byte lastPlayerId;
    private static string lastMessage;
    private static float lastMessageTime;
    private static float startTime;
    private static DateTime startDate;
    private static DateTime endDate;
    private static string gameModeName;
    private static string currentFilePath;

    private static bool IsHost =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.AmHost;

    public static void StartMatch()
    {
        if (!IsHost)
            return;

        Entries.Clear();
        recording = true;
        finished = false;
        startTime = Time.realtimeSinceStartup;
        startDate = DateTime.Now;
        endDate = default;
        gameModeName = GetGameModeName();
        lastPlayerId = byte.MaxValue;
        lastMessage = null;
        lastMessageTime = -1f;

        string fileName =
            $"Chat_{startDate:yyyy-MM-dd_HH-mm-ss-fff}.txt";

        currentFilePath =
            Path.Combine(GetLogDirectory(), fileName);

        WriteLog();

        BMLogger.Info(
            "[MatchChatLogger] Chat recording started.");
    }

    public static void RecordChat(
        PlayerControl player,
        string message)
    {
        if (!IsHost ||
            !recording ||
            suppressRecording ||
            player == null ||
            player.Data == null ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        string cleanedMessage = CleanText(message);

        // RpcSendChat can be echoed back through HandleRpc on some game
        // versions. Ignore only that immediate echo, not later messages.
        if (lastPlayerId == player.PlayerId &&
            string.Equals(
                lastMessage,
                cleanedMessage,
                StringComparison.Ordinal) &&
            now - lastMessageTime < 0.25f)
        {
            return;
        }

        lastPlayerId = player.PlayerId;
        lastMessage = cleanedMessage;
        lastMessageTime = now;

        Entries.Add(new ChatEntry
        {
            Time = GetElapsedTime(),
            PlayerId = player.PlayerId,
            PlayerName = CleanText(player.Data.PlayerName),
            ColorName = GetColorName(player.Data),
            WasDead = player.Data.IsDead,
            Context = GetChatContext(),
            Message = cleanedMessage
        });

        // Keep the on-disk file current after every message, so a crash or
        // forced disconnect does not lose the chat collected so far.
        try
        {
            WriteLog();
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchChatLogger] Failed to update chat log: {exception}",
                "MatchChatLogger");
        }
    }

    public static void FinishMatch()
    {
        if (!IsHost || !recording)
            return;

        try
        {
            endDate = DateTime.Now;
            finished = true;
            WriteLog();
            recording = false;

            BMLogger.Info(
                $"[MatchChatLogger] Chat log saved: {currentFilePath}");
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchChatLogger] Failed to save chat log: {exception}",
                "MatchChatLogger");
        }
    }

    public static string GetChatLogLocation()
    {
        if (!BanMod.EnableChatLog.Value)
            return "Chat logging is disabled.";

        if (!IsHost)
            return "The chat log is available only to the host.";

        if (recording)
        {
            bool hostIsDead =
                PlayerControl.LocalPlayer != null &&
                PlayerControl.LocalPlayer.Data != null &&
                PlayerControl.LocalPlayer.Data.IsDead;

            if (!hostIsDead)
            {
                return
                    "During a match, you can view the chat log only while dead.";
            }

            try
            {
                WriteLog();

                return
                    $"Chat log saved at: {currentFilePath}";
            }
            catch (Exception exception)
            {
                BMLogger.Error(
                    $"[MatchChatLogger] Failed to update chat log: {exception}",
                    "MatchChatLogger");

                return "The chat log could not be updated.";
            }
        }

        if (!GameStates.isLobby)
        {
            return
                "The chat log can be viewed in the lobby after the match.";
        }

        try
        {
            string filePath = ResolveLatestLog();

            if (string.IsNullOrWhiteSpace(filePath))
                return "No chat logs were found.";

            return
                $"Chat log saved at: {filePath}";
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchChatLogger] Failed to locate chat log: {exception}",
                "MatchChatLogger");

            return "The chat log could not be located.";
        }
    }

    public static void ShowCommandResult(string message)
    {
        suppressRecording = true;

        try
        {
            ChatCommands.ShowChat(message);
        }
        finally
        {
            suppressRecording = false;
        }
    }

    private static void WriteLog()
    {
        if (string.IsNullOrWhiteSpace(currentFilePath))
            return;

        Directory.CreateDirectory(GetLogDirectory());

        var text = new StringBuilder();

        text.AppendLine("========================================");
        text.AppendLine("               CHAT LOG");
        text.AppendLine("========================================");
        text.AppendLine();
        text.AppendLine($"Started: {startDate:yyyy-MM-dd HH:mm:ss}");

        if (finished)
            text.AppendLine($"Ended:   {endDate:yyyy-MM-dd HH:mm:ss}");
        else
            text.AppendLine("Status: match in progress");

        text.AppendLine($"Game mode: {gameModeName}");
        text.AppendLine();

        if (Entries.Count == 0)
        {
            text.AppendLine("No messages were recorded.");
        }
        else
        {
            foreach (ChatEntry entry in Entries)
            {
                string status = entry.WasDead ? "DEAD" : "ALIVE";

                text.AppendLine(
                    $"[{FormatTime(entry.Time)}] " +
                    $"[{entry.Context}] " +
                    $"[{status}] " +
                    $"{entry.ColorName} ({entry.PlayerName}) " +
                    $"[ID {entry.PlayerId}]: {entry.Message}");
            }
        }

        text.AppendLine();
        text.AppendLine("========================================");
        text.AppendLine("            END OF CHAT LOG");
        text.AppendLine("========================================");

        File.WriteAllText(
            currentFilePath,
            text.ToString(),
            new UTF8Encoding(false));
    }

    private static string ResolveLatestLog()
    {
        if (!string.IsNullOrWhiteSpace(currentFilePath) &&
            File.Exists(currentFilePath))
        {
            return currentFilePath;
        }

        string directory = GetLogDirectory();

        if (!Directory.Exists(directory))
            return null;

        return Directory
            .GetFiles(directory, "Chat_*.txt")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static float GetElapsedTime()
    {
        return Math.Max(
            0f,
            Time.realtimeSinceStartup - startTime);
    }

    private static string GetChatContext()
    {
        return MeetingHud.Instance != null
            ? "MEETING"
            : "GAME";
    }

    private static string GetGameModeName()
    {
        try
        {
            return Options.GameMode != null
                ? Options.GameMode.Selected.ToString()
                : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string CleanText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        return value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static string GetColorName(
        NetworkedPlayerInfo player)
    {
        try
        {
            int colorId = player.DefaultOutfit.ColorId;

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
        catch
        {
            return "Unknown";
        }
    }

    private static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        int remainingSeconds = (int)(seconds % 60f);
        int milliseconds =
            (int)((seconds * 1000f) % 1000f);

        return
            $"{minutes:D2}:{remainingSeconds:D2}.{milliseconds:D3}";
    }

    private static string GetLogDirectory()
    {
        const string ChatPath = "./BAN_DATA/LOG/";
        return Path.Combine(
            ChatPath,
            "BanMod Chat Logs");
    }

}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
internal static class MatchChatLoggerStartPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!FakeMapLobbyUtility.Active &&
            BanMod.EnableChatLog.Value)
        {
            MatchChatLogger.StartMatch();
        }
    }
}

[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.RpcSendChat),
    typeof(string))]
internal static class MatchChatLoggerSendChatPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(
        PlayerControl __instance,
        [HarmonyArgument(0)] string chatText)
    {
        if (BanMod.EnableChatLog.Value)
        {
            MatchChatLogger.RecordChat(
                __instance,
                chatText);
        }
    }
}

[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.HandleRpc),
    typeof(byte),
    typeof(MessageReader))]
internal static class MatchChatLoggerReceiveChatPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(
        PlayerControl __instance,
        [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!BanMod.EnableChatLog.Value ||
            callId != (byte)RpcCalls.SendChat ||
            reader == null)
        {
            return;
        }

        var originalPosition = reader.Position;

        try
        {
            // Read the RPC payload before ChatController applies its
            // alive/dead visibility rules. Restoring Position leaves the
            // original game handler completely untouched.
            string chatText = reader.ReadString();

            MatchChatLogger.RecordChat(
                __instance,
                chatText);
        }
        catch (Exception exception)
        {
            BMLogger.Error(
                $"[MatchChatLogger] Failed to read chat RPC: {exception}",
                "MatchChatLogger");
        }
        finally
        {
            reader.Position = originalPosition;
        }
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
internal static class MatchChatLoggerEndPatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (BanMod.EnableChatLog.Value)
        {
            MatchChatLogger.FinishMatch();
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal static class MatchChatLoggerCommandPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ChatController __instance)
    {
        if (__instance == null ||
            __instance.freeChatField == null)
        {
            return true;
        }

        string text = __instance.freeChatField.Text;

        if (!string.Equals(
                text?.Trim(),
                "/chatlog",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        __instance.freeChatField.Clear();

        MatchChatLogger.ShowCommandResult(
            MatchChatLogger.GetChatLogLocation());

        return false;
    }
}