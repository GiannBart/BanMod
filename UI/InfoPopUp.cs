//credits and licenses in the resources folder
using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static BanMod.Translator;
using static UnityEngine.GraphicsBuffer;

namespace BanMod;

public static class PreviousMatchPopupTracker
{
    private static readonly Dictionary<byte, PlayerRoleStat> InitialRoles = new();

    public sealed class KillerStat
    {
        public byte PlayerId;
        public string RealName = "";
        public int KillCount;
    }

    public sealed class TaskStat
    {
        public byte PlayerId;
        public string Name = "";
        public int Done;
        public int Total;
        public long LastCompletionOrder;
        public float LastCompletionTimeSeconds = -1f;
    }

    public sealed class ProtectionStat
    {
        public byte PlayerId;
        public string Name = "";
        public int AssignedCount;
        public int EffectiveSaveCount;
        public bool IsImmortalPlayer;
    }

    public sealed class PlayerRoleStat
    {
        public byte PlayerId;
        public string Name = "";
        public string RoleName = "";
        public string VanillaRoleName = "";
        public string CustomRoleName = "";
    }

    public sealed class MatchSnapshot
    {
        public DateTime SavedAt = DateTime.Now;
        public string ReportText = "";

        public List<KillerStat> KillerStats = new();
        public List<TaskStat> TaskStats = new();
        public List<ProtectionStat> ProtectionStats = new();
        public List<PlayerRoleStat> PlayerRoles = new();

        public bool SpecialKillerFailed;
        public bool PhantomFailed;
        public bool ShapeFailed;
        public bool ViperFailed;
        public bool ImpostorFailed;
        public bool PresidentExeFailed;
        public bool PresidentKillFailed;
        public bool JesterWin;

        public string JesterName = "";

        public string GuesserName = "";
        public string GuessedTargetName = "";
        public string GuessedRoleName = "";

        public string ImpostorName = "";
        public string ImpostorTargetName = "";

        public string PresidentName = "";
        public string PresidentTargetName = "";

        public string PhantomName = "";
        public string PhantomTargetName = "";

        public string ViperName = "";
        public string ViperTargetName = "";

        public string ShapeName = "";
        public string ShapeTargetName = "";

        public string LastImmortalPlayerName = "";

        public string LastTaskCompleterName = "";
        public int LastTaskCompleterDone;
        public int LastTaskCompleterTotal;
    }

    private static readonly Dictionary<byte, KillerStat> CurrentKills = new();
    private static readonly Dictionary<byte, TaskStat> CurrentTasks = new();
    private static readonly Dictionary<byte, ProtectionStat> CurrentProtections = new();
    private static readonly HashSet<byte> IgnoredMirrorKillTargets = new();
    private static readonly Dictionary<byte, float> PendingProtectionUndoUntil = new();
    private static readonly List<MatchSnapshot> History = new();

    private static long _taskCompletionCounter = 0;
    private static float _matchStartedAt = -1f;
    private static string _lastTaskCompleterName = "";
    private static int _lastTaskCompleterDone = 0;
    private static int _lastTaskCompleterTotal = 0;

    public static MatchSnapshot LastSnapshot { get; private set; }

    public static Action<string> OpenPopup;

    public static bool SpecialKillerFailed = false;
    public static bool PhantomFailed = false;
    public static bool ShapeFailed = false;
    public static bool ViperFailed = false;
    public static bool ImpostorFailed = false;
    public static bool PresidentExeFailed = true;
    public static bool PresidentKillFailed = true;
    public static bool JesterWin = false;

    public static string JesterName = "";

    public static string GuesserName = "";
    public static string GuessedTargetName = "";
    public static string GuessedRoleName = "";

    public static string ImpostorName = "";
    public static string ImpostorTargetName = "";

    public static string PresidentName = "";
    public static string PresidentTargetName = "";

    public static string PhantomName = "";
    public static string PhantomTargetName = "";

    public static string ViperName = "";
    public static string ViperTargetName = "";

    public static string ShapeName = "";
    public static string ShapeTargetName = "";

    public static void CaptureInitialRoles()
    {
        _matchStartedAt = Time.time;

        InitialRoles.Clear();

        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null || player.Data == null)
                continue;

            string customRole = GetCustomRoleName(player) ?? "";
            string vanillaRole = GetVanillaRoleName(player);
            string roleName = GetRoleNameForSummary(player);

            InitialRoles[player.PlayerId] = new PlayerRoleStat
            {
                PlayerId = player.PlayerId,
                Name = GetPlayerName(player),
                RoleName = roleName,
                VanillaRoleName = vanillaRole,
                CustomRoleName = customRole
            };
        }

        BMLogger.Info($"[PreviousMatch] Salvati {InitialRoles.Count} ruoli iniziali della partita.", "PreviousMatch");

        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null || player.Data == null)
                continue;

            UpdatePlayerTask(player);
        }
    }

    public static void ResetCurrentMatch()
    {
        InitialRoles.Clear();
        CurrentKills.Clear();
        CurrentTasks.Clear();
        CurrentProtections.Clear();
        IgnoredMirrorKillTargets.Clear();
        PendingProtectionUndoUntil.Clear();

        _taskCompletionCounter = 0;
        _lastTaskCompleterName = "";
        _lastTaskCompleterDone = 0;
        _lastTaskCompleterTotal = 0;
        _matchStartedAt = -1f;

        SpecialKillerFailed = false;
        PhantomFailed = false;
        ShapeFailed = false;
        ViperFailed = false;
        ImpostorFailed = false;
        PresidentExeFailed = true;
        PresidentKillFailed = true;
        JesterWin = false;

        JesterName = "";

        GuesserName = "";
        GuessedTargetName = "";
        GuessedRoleName = "";

        ImpostorName = "";
        ImpostorTargetName = "";

        PresidentName = "";
        PresidentTargetName = "";

        PhantomName = "";
        PhantomTargetName = "";

        ViperName = "";
        ViperTargetName = "";

        ShapeName = "";
        ShapeTargetName = "";

    }

    public static void MarkMirrorKillTarget(PlayerControl target)
    {
        if (target == null) return;
        IgnoredMirrorKillTargets.Add(target.PlayerId);
    }

    public static bool ConsumeIgnoredMirrorKill(PlayerControl target)
    {
        if (target == null) return false;
        return IgnoredMirrorKillTargets.Remove(target.PlayerId);
    }

    public static void RegisterRealKill(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null)
            return;

        if (killer.Data == null || target.Data == null)
            return;

        KillerStat stat;
        if (!CurrentKills.TryGetValue(killer.PlayerId, out stat))
        {
            stat = new KillerStat
            {
                PlayerId = killer.PlayerId,
                RealName = GetPlayerName(killer),
                KillCount = 0
            };
            CurrentKills[killer.PlayerId] = stat;
        }

        stat.RealName = GetPlayerName(killer);
        stat.KillCount++;
    }

    public static void RegisterProtectionReceived(PlayerControl target)
    {
        if (target == null || target.Data == null)
            return;

        if (ImmortalManager.IsImmortal(target.PlayerId))
            return;

        if (Watcher.IsWatcher(target.PlayerId))
            return;

        if (BanMod.ShieldedPlayers.Contains(target.PlayerId))
            return;

        ProtectionStat stat;
        if (!CurrentProtections.TryGetValue(target.PlayerId, out stat))
        {
            stat = new ProtectionStat
            {
                PlayerId = target.PlayerId,
                Name = GetPlayerName(target),
                AssignedCount = 0,
                EffectiveSaveCount = 0,
                IsImmortalPlayer = false
            };
            CurrentProtections[target.PlayerId] = stat;
        }

        stat.Name = GetPlayerName(target);
        stat.AssignedCount++;
    }

    public static void RegisterEffectiveProtectionSave(PlayerControl target)
    {
        if (target == null || target.Data == null)
            return;

        ProtectionStat stat;
        if (!CurrentProtections.TryGetValue(target.PlayerId, out stat))
        {
            stat = new ProtectionStat
            {
                PlayerId = target.PlayerId,
                Name = GetPlayerName(target),
                AssignedCount = 0,
                EffectiveSaveCount = 0,
                IsImmortalPlayer = false
            };
            CurrentProtections[target.PlayerId] = stat;
        }

        stat.Name = GetPlayerName(target);

        if (ImmortalManager.IsImmortal(target.PlayerId))
            stat.IsImmortalPlayer = true;

        stat.EffectiveSaveCount++;
    }

    public static void MarkRecentProtection(PlayerControl target)
    {
        if (target == null) return;
        PendingProtectionUndoUntil[target.PlayerId] = Time.time + 1f;
    }

    public static void ValidateRecentProtection(PlayerControl target)
    {
        if (target == null || target.Data == null)
            return;

        if (!PendingProtectionUndoUntil.TryGetValue(target.PlayerId, out float until))
            return;

        if (ImmortalManager.IsImmortal(target.PlayerId))
        {
            RemoveLastProtection(target);
            PendingProtectionUndoUntil.Remove(target.PlayerId);
            return;
        }

        if (BanMod.ShieldedPlayers.Contains(target.PlayerId))
        {
            RemoveLastProtection(target);
            PendingProtectionUndoUntil.Remove(target.PlayerId);
            return;
        }

        if (Time.time > until)
        {
            PendingProtectionUndoUntil.Remove(target.PlayerId);
        }
    }

    private static void RemoveLastProtection(PlayerControl target)
    {
        if (target == null)
            return;

        if (!CurrentProtections.TryGetValue(target.PlayerId, out var stat))
            return;

        stat.Name = GetPlayerName(target);

        if (stat.AssignedCount > 0)
            stat.AssignedCount--;

        if (stat.AssignedCount <= 0 && stat.EffectiveSaveCount <= 0)
            CurrentProtections.Remove(target.PlayerId);
    }

    private static void RefreshImmortalProtections()
    {
        foreach (var stat in CurrentProtections.Values)
        {
            var player = Utils.GetPlayerById(stat.PlayerId);
            if (player != null)
                stat.Name = GetPlayerName(player);

            bool isImmortal = ImmortalManager.IsImmortal(stat.PlayerId);
            stat.IsImmortalPlayer = isImmortal;

            if (isImmortal)
                stat.AssignedCount = 0;
        }
    }

    public static void UpdatePlayerTask(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Tasks == null)
            return;

        if (player.Data.Role != null && player.Data.Role.TeamType == RoleTeamTypes.Impostor)
            return;

        int total = player.Data.Tasks.Count;
        int done = 0;

        foreach (var task in player.Data.Tasks)
        {
            if (task != null && task.Complete)
                done++;
        }

        TaskStat stat;
        bool existed = CurrentTasks.TryGetValue(player.PlayerId, out stat);

        if (!existed)
        {
            stat = new TaskStat
            {
                PlayerId = player.PlayerId,
                Name = GetPlayerName(player),
                Done = done,
                Total = total,
                LastCompletionOrder = 0,
                LastCompletionTimeSeconds = -1f
            };

            CurrentTasks[player.PlayerId] = stat;
            return;
        }

        stat.Name = GetPlayerName(player);
        stat.Done = done;
        stat.Total = total;
    }

    public static void RegisterTaskCompletion(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Tasks == null)
            return;

        if (player.Data.Role != null && player.Data.Role.TeamType == RoleTeamTypes.Impostor)
            return;

        int total = player.Data.Tasks.Count;
        int done = 0;

        foreach (var task in player.Data.Tasks)
        {
            if (task != null && task.Complete)
                done++;
        }

        TaskStat stat;
        bool existed = CurrentTasks.TryGetValue(player.PlayerId, out stat);

        if (!existed)
        {
            stat = new TaskStat
            {
                PlayerId = player.PlayerId,
                Name = GetPlayerName(player),
                Done = done,
                Total = total,
                LastCompletionOrder = 0,
                LastCompletionTimeSeconds = -1f
            };
            CurrentTasks[player.PlayerId] = stat;
        }

        stat.Name = GetPlayerName(player);
        stat.Done = done;
        stat.Total = total;

        if (_matchStartedAt < 0f)
            _matchStartedAt = Time.time;

        stat.LastCompletionTimeSeconds =
            Mathf.Max(0f, Time.time - _matchStartedAt);

        _taskCompletionCounter++;
        stat.LastCompletionOrder = _taskCompletionCounter;

        _lastTaskCompleterName = GetPlayerName(player);
        _lastTaskCompleterDone = done;
        _lastTaskCompleterTotal = total;
    }
    private static string GetTaskRankLabel(int position)
    {
        switch (position)
        {
            case 1:
                return "<color=#FFD700>1st</color>";

            case 2:
                return "<color=#C0C0C0>2nd</color>";

            case 3:
                return "<color=#CD7F32>3rd</color>";

            default:
                return position + GetOrdinalSuffix(position);
        }
    }

    private static string GetOrdinalSuffix(int number)
    {
        int lastTwoDigits = number % 100;

        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            return "th";

        switch (number % 10)
        {
            case 1:
                return "st";

            case 2:
                return "nd";

            case 3:
                return "rd";

            default:
                return "th";
        }
    }

    private static string FormatTaskCompletionTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return $"{minutes}:{remainingSeconds:00}";
    }
    public static void RegisterGuesserAttempt(PlayerControl guesser, PlayerControl target, string guessedRoleName, bool success)
    {
        GuesserName = GetPlayerName(guesser);
        GuessedTargetName = GetPlayerName(target);
        GuessedRoleName = string.IsNullOrWhiteSpace(guessedRoleName) ? GetString("GuessRoleUnknown") : guessedRoleName;
        SpecialKillerFailed = !success;
    }

    public static void RegisterPresidentExile(PlayerControl player, PlayerControl target)
    {
        PresidentName = GetPlayerName(player);
        PresidentTargetName = GetPlayerName(target);
        PresidentExeFailed = false;
        PresidentKillFailed = true;
    }

    public static void RegisterPresidentKill(PlayerControl player, PlayerControl target)
    {
        PresidentName = GetPlayerName(player);
        PresidentTargetName = GetPlayerName(target);
        PresidentExeFailed = true;
        PresidentKillFailed = false;
    }

    public static void RegisterPresidentFail(PlayerControl player, PlayerControl target)
    {
        PresidentName = GetPlayerName(player);
        PresidentTargetName = GetPlayerName(target);
        PresidentExeFailed = true;
        PresidentKillFailed = true;
    }
    public static void RegisterPhantomAttempt(PlayerControl player, PlayerControl target, bool success)
    {
        PhantomName = GetPlayerName(player);
        PhantomTargetName = GetPlayerName(target);
        PhantomFailed = !success;
    }

    public static void RegisterViperAttempt(PlayerControl player, PlayerControl target, bool success)
    {
        ViperName = GetPlayerName(player);
        ViperTargetName = GetPlayerName(target);
        ViperFailed = !success;
    }

    public static void RegisterShapeAttempt(PlayerControl player, PlayerControl target, bool success)
    {
        ShapeName = GetPlayerName(player);
        ShapeTargetName = GetPlayerName(target);
        ShapeFailed = !success;
    }

    public static void RegisterImpostorAttempt(PlayerControl player, PlayerControl target, bool success)
    {
        ImpostorName = GetPlayerName(player);
        ImpostorTargetName = GetPlayerName(target);
        ImpostorFailed = !success;
    }

    public static void SaveCurrentMatch()
    {
        RefreshImmortalProtections();

        MatchSnapshot snap = new MatchSnapshot();

        snap.KillerStats = CurrentKills.Values
            .Select(CloneKillerStat)
            .OrderByDescending(x => x.KillCount)
            .ThenBy(x => x.RealName)
            .ToList();

        snap.TaskStats = CurrentTasks.Values
            .Select(CloneTaskStat)
            .OrderByDescending(x => x.Done)
            .ThenBy(x =>
                x.LastCompletionTimeSeconds < 0f
                    ? float.MaxValue
                    : x.LastCompletionTimeSeconds)
            .ThenBy(x => x.Name)
            .ToList();

        snap.ProtectionStats = CurrentProtections.Values
            .Select(CloneProtectionStat)
            .OrderByDescending(x => x.IsImmortalPlayer)
            .ThenByDescending(x => x.AssignedCount)
            .ThenByDescending(x => x.EffectiveSaveCount)
            .ThenBy(x => x.Name)
            .ToList();

        snap.PlayerRoles = InitialRoles.Values
            .Select(ClonePlayerRoleStat)
            .OrderBy(x => x.Name)
            .ToList();

        snap.SpecialKillerFailed = SpecialKillerFailed;
        snap.PhantomFailed = PhantomFailed;
        snap.ShapeFailed = ShapeFailed;
        snap.ViperFailed = ViperFailed;
        snap.ImpostorFailed = ImpostorFailed;
        snap.PresidentExeFailed = PresidentExeFailed;
        snap.PresidentKillFailed = PresidentKillFailed;

        snap.JesterWin = JesterWin;
        snap.JesterName = JesterName;

        snap.GuesserName = GuesserName;
        snap.GuessedTargetName = GuessedTargetName;
        snap.GuessedRoleName = GuessedRoleName;

        snap.ImpostorName = ImpostorName;
        snap.ImpostorTargetName = ImpostorTargetName;

        snap.PresidentName = PresidentName;
        snap.PresidentTargetName = PresidentTargetName;

        snap.PhantomName = PhantomName;
        snap.PhantomTargetName = PhantomTargetName;

        snap.ViperName = ViperName;
        snap.ViperTargetName = ViperTargetName;

        snap.ShapeName = ShapeName;
        snap.ShapeTargetName = ShapeTargetName;

        snap.LastImmortalPlayerName = ImmortalManager.LastImmortalPlayerName ?? "";

        snap.LastTaskCompleterName = _lastTaskCompleterName;
        snap.LastTaskCompleterDone = _lastTaskCompleterDone;
        snap.LastTaskCompleterTotal = _lastTaskCompleterTotal;

        snap.ReportText = BuildReportText(snap);

        LastSnapshot = snap;
        History.Add(snap);
    }

    public static string GetLastSavedReport()
    {
        return LastSnapshot != null ? LastSnapshot.ReportText : "";
    }

    public static void ShowLastMatchPopup()
    {
        if (LastSnapshot == null)
            return;

        if (string.IsNullOrWhiteSpace(LastSnapshot.ReportText))
            return;

        if (OpenPopup != null)
        {
            OpenPopup.Invoke(LastSnapshot.ReportText);
            return;
        }

        if (HudManager.Instance != null)
        {
            HudManager.Instance.ShowPopUp(LastSnapshot.ReportText);
        }
    }

    private static string GetRoleNameForSummary(PlayerControl player)
    {
        if (player == null || player.Data == null)
            return $"<color=#FFFFFF>{GetString("UnknownRole")}</color>";

        string customRole = GetCustomRoleName(player);
        string vanillaRole = GetVanillaRoleName(player);

        if (!string.IsNullOrWhiteSpace(customRole))
        {
            if (!string.IsNullOrWhiteSpace(vanillaRole) && customRole != vanillaRole)
                return $"{customRole} <color=#FFFFFF>({vanillaRole})</color>";

            return customRole;
        }

        if (!string.IsNullOrWhiteSpace(vanillaRole))
            return vanillaRole;

        return $"<color=#00FFFF>{GetString("Crewmate")}</color>";
    }

    private static string GetCustomRoleName(PlayerControl player)
    {
        if (player == null || player.Data == null)
            return null;

        if (player.PlayerId == Jester.JesterId)
            return $"<color=#FFA500>{GetString("Jester")}</color>";

        if (player.PlayerId == Guesser.SpecialKillerId)
            return $"<color=#FFA500>{GetString("GuesserRole")}</color>";

        if (player.PlayerId == Exiler.ExilerId)
            return $"<color=#FFA500>{GetString("ExilerRole")}</color>";

        if (player.PlayerId == Judge.JudgeId)
            return $"<color=#FFA500>{GetString("JudgeRole")}</color>";

        if (player.PlayerId == Profiler.ProfilerId)
            return $"<color=#FFA500>{GetString("ProfilerRole")}</color>";

        if (player.PlayerId == ImmortalManager.ImmortalPlayerId)
            return $"<color=#FFA500>{GetString("Immortal")}</color>";

        if (player.PlayerId == Watcher.WatcherId)
            return $"<color=#FFA500>{GetString("Watcher")}</color>";

        return null;
    }

    private static string GetVanillaRoleName(PlayerControl player)
    {
        if (player == null || player.Data == null)
            return $"<color=#FFFFFF>{GetString("UnknownRole")}</color>";

        if (player.Data.Role != null)
        {
            switch (player.Data.Role.Role)
            {
                case RoleTypes.Impostor:
                    return $"<color=#FF4D4D>{GetString("ImpostorRole")}</color>";

                case RoleTypes.Shapeshifter:
                    return $"<color=#FF4D4D>{GetString("Shapeshifter")}</color>";

                case RoleTypes.Phantom:
                    return $"<color=#FF4D4D>{GetString("Phantom")}</color>";

                case RoleTypes.Viper:
                    return $"<color=#FF4D4D>{GetString("Viper")}</color>";

                case RoleTypes.Engineer:
                    return $"<color=#00FFFF>{GetString("Engineer")}</color>";

                case RoleTypes.Scientist:
                    return $"<color=#00FFFF>{GetString("Scientist")}</color>";

                case RoleTypes.Detective:
                    return $"<color=#00FFFF>{GetString("Detective")}</color>";

                case RoleTypes.Tracker:
                    return $"<color=#00FFFF>{GetString("Tracker")}</color>";

                case RoleTypes.Noisemaker:
                    return $"<color=#00FFFF>{GetString("Noisemaker")}</color>";

                case RoleTypes.Crewmate:
                    return $"<color=#00FFFF>{GetString("Crewmate")}</color>";
            }
        }

        if (player.Data.Role != null && player.Data.Role.TeamType == RoleTeamTypes.Impostor)
            return $"<color=#FF4D4D>{GetString("ImpostorRole")}</color>";

        return $"<color=#00FFFF>{GetString("Crewmate")}</color>";
    }

    private static string BuildReportText(MatchSnapshot snap)
    {
        var report = new StringBuilder();

        report.AppendLine(GetString("SummaryHeader"));
        report.AppendLine();

        report.AppendLine(GetString("Roles"));
        if (snap.PlayerRoles == null || snap.PlayerRoles.Count == 0)
        {
            report.AppendLine("Error");
        }
        else
        {
            foreach (var entry in snap.PlayerRoles.OrderBy(x => x.Name))
            {
                report.AppendLine($"{entry.Name} = {entry.RoleName}");
            }
        }
        report.AppendLine();

        if (snap.JesterWin)
        {
            if (!string.IsNullOrWhiteSpace(snap.JesterName))
                report.AppendLine($"{GetString("JesterWins")}: {snap.JesterName}");
            else
                report.AppendLine(GetString("JesterWins"));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.GuesserName) && !string.IsNullOrEmpty(snap.GuessedTargetName))
        {
            report.AppendLine(GetString("SummaryGuesser"));

            if (!string.IsNullOrWhiteSpace(snap.GuessedRoleName))
            {
                if (!snap.SpecialKillerFailed)
                    report.AppendLine(string.Format(GetString("GuesserSuccessRole"), snap.GuesserName, snap.GuessedTargetName, snap.GuessedRoleName));
                else
                    report.AppendLine(string.Format(GetString("GuesserFailRole"), snap.GuesserName, snap.GuessedTargetName, snap.GuessedRoleName));
            }
            else
            {
                if (!snap.SpecialKillerFailed)
                    report.AppendLine(string.Format(GetString("GuessSuccessMessage"), snap.GuesserName, snap.GuessedTargetName));
                else
                    report.AppendLine(string.Format(GetString("GuessFailMessage"), snap.GuesserName, snap.GuessedTargetName));
            }

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.PresidentName) && !string.IsNullOrEmpty(snap.PresidentTargetName))
        {
            report.AppendLine(GetString("SummaryPresident"));

            if (!snap.PresidentExeFailed)
                report.AppendLine(string.Format(GetString("PresidentExile"), snap.PresidentName, snap.PresidentTargetName));
            else if (!snap.PresidentKillFailed)
                report.AppendLine(string.Format(GetString("PresidentKill"), snap.PresidentName, snap.PresidentTargetName));
            else
                report.AppendLine(string.Format(GetString("PresidentFail"), snap.PresidentName, snap.PresidentTargetName));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.PhantomName) && !string.IsNullOrEmpty(snap.PhantomTargetName))
        {
            report.AppendLine(GetString("SummaryPhantom"));

            if (!snap.PhantomFailed)
                report.AppendLine(string.Format(GetString("GuessSuccessMessage"), snap.PhantomName, snap.PhantomTargetName));
            else
                report.AppendLine(string.Format(GetString("GuessFailMessage"), snap.PhantomName, snap.PhantomTargetName));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.ViperName) && !string.IsNullOrEmpty(snap.ViperTargetName))
        {
            report.AppendLine(GetString("SummaryViper"));

            if (!snap.ViperFailed)
                report.AppendLine(string.Format(GetString("GuessSuccessMessage"), snap.ViperName, snap.ViperTargetName));
            else
                report.AppendLine(string.Format(GetString("GuessFailMessage"), snap.ViperName, snap.ViperTargetName));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.ShapeName) && !string.IsNullOrEmpty(snap.ShapeTargetName))
        {
            report.AppendLine(GetString("SummaryShape"));

            if (!snap.ShapeFailed)
                report.AppendLine(string.Format(GetString("GuessSuccessMessage"), snap.ShapeName, snap.ShapeTargetName));
            else
                report.AppendLine(string.Format(GetString("GuessFailMessage"), snap.ShapeName, snap.ShapeTargetName));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.ImpostorName) && !string.IsNullOrEmpty(snap.ImpostorTargetName))
        {
            report.AppendLine(GetString("Impostor"));

            if (!snap.ImpostorFailed)
                report.AppendLine(string.Format(GetString("ImpostorRoleSuccess"), snap.ImpostorName, snap.ImpostorTargetName));
            else
                report.AppendLine(string.Format(GetString("ImpostorRoleFail"), snap.ImpostorName, snap.ImpostorTargetName));

            report.AppendLine();
        }

        if (!string.IsNullOrEmpty(snap.LastImmortalPlayerName))
        {
            report.AppendLine(string.Format(GetString("ImmortalPlayerReport"), snap.LastImmortalPlayerName));
            report.AppendLine();
        }

        report.AppendLine(GetString("KillSummaryHeader"));
        if (snap.KillerStats.Count == 0)
        {
            report.AppendLine(GetString("KillSummaryEmpty"));
        }
        else
        {
            foreach (var killer in snap.KillerStats)
            {
                report.AppendLine(string.Format(GetString("KillSummaryLine"), killer.RealName, killer.KillCount));
            }
        }

        report.AppendLine();
        report.AppendLine("TASK PODIUM");

        if (snap.TaskStats == null || snap.TaskStats.Count == 0)
        {
            report.AppendLine("No task data available.");
        }
        else
        {
            var rankedTasks = snap.TaskStats
                .OrderByDescending(t => t.Done)
                .ThenBy(t =>
                    t.LastCompletionTimeSeconds < 0f
                        ? float.MaxValue
                        : t.LastCompletionTimeSeconds)
                .ThenBy(t => t.Name)
                .ToList();

            for (int index = 0; index < rankedTasks.Count; index++)
            {
                TaskStat task = rankedTasks[index];
                int position = index + 1;

                string rankLabel = GetTaskRankLabel(position);
                string timeText = "";

                if (task.LastCompletionTimeSeconds >= 0f)
                {
                    string formattedTime =
                        FormatTaskCompletionTime(task.LastCompletionTimeSeconds);

                    if (task.Total > 0 && task.Done >= task.Total)
                    {
                        timeText = $" - completed in {formattedTime}";
                    }
                    else if (task.Done > 0)
                    {
                        timeText = $" - last task completed at {formattedTime}";
                    }
                }

                report.AppendLine(
                    $"{rankLabel} - {task.Name}: " +
                    $"{task.Done}/{task.Total} tasks{timeText}");
            }
        }

        report.AppendLine();
        report.AppendLine(GetString("ProtectionSummaryHeader"));

        report.AppendLine(GetString("ProtectionAssignedHeader"));
        var assignedList = snap.ProtectionStats
            .Where(x => x.AssignedCount > 0)
            .OrderByDescending(x => x.AssignedCount)
            .ThenBy(x => x.Name)
            .ToList();

        if (assignedList.Count == 0)
        {
            report.AppendLine(GetString("ProtectionAssignedEmpty"));
        }
        else
        {
            foreach (var p in assignedList)
            {
                report.AppendLine(string.Format(GetString("FormatNameValue"), p.Name, p.AssignedCount));
            }
        }

        report.AppendLine();

        report.AppendLine(GetString("ProtectionEffectiveHeader"));
        var effectiveList = snap.ProtectionStats
            .Where(x => x.EffectiveSaveCount > 0)
            .OrderByDescending(x => x.EffectiveSaveCount)
            .ThenBy(x => x.Name)
            .ToList();

        if (effectiveList.Count == 0)
        {
            report.AppendLine(GetString("ProtectionEffectiveEmpty"));
        }
        else
        {
            foreach (var p in effectiveList)
            {
                if (p.IsImmortalPlayer)
                    report.AppendLine(string.Format(GetString("ProtectionSummaryImmortalLine"), p.Name, p.EffectiveSaveCount));
                else
                    report.AppendLine(string.Format(GetString("FormatNameValue"), p.Name, p.EffectiveSaveCount));
            }
        }

        report.AppendLine();
        if (!string.IsNullOrWhiteSpace(snap.LastTaskCompleterName))
            report.AppendLine(string.Format(GetString("LastTaskCompleter"), snap.LastTaskCompleterName, snap.LastTaskCompleterDone, snap.LastTaskCompleterTotal));
        else
            report.AppendLine(GetString("LastTaskCompleterNone"));

        return report.ToString();
    }

    private static string GetPlayerName(PlayerControl player)
    {
        if (player == null)
            return GetString("UnknownPlayerName");

        if (player.Data != null && !string.IsNullOrWhiteSpace(player.Data.PlayerName))
            return player.Data.PlayerName;

        return GetString("UnknownPlayerName");
    }

    private static KillerStat CloneKillerStat(KillerStat s)
    {
        return new KillerStat
        {
            PlayerId = s.PlayerId,
            RealName = s.RealName,
            KillCount = s.KillCount
        };
    }

    private static TaskStat CloneTaskStat(TaskStat s)
    {
        return new TaskStat
        {
            PlayerId = s.PlayerId,
            Name = s.Name,
            Done = s.Done,
            Total = s.Total,
            LastCompletionOrder = s.LastCompletionOrder,
            LastCompletionTimeSeconds = s.LastCompletionTimeSeconds
        };
    }

    private static ProtectionStat CloneProtectionStat(ProtectionStat s)
    {
        return new ProtectionStat
        {
            PlayerId = s.PlayerId,
            Name = s.Name,
            AssignedCount = s.AssignedCount,
            EffectiveSaveCount = s.EffectiveSaveCount,
            IsImmortalPlayer = s.IsImmortalPlayer
        };
    }

    private static PlayerRoleStat ClonePlayerRoleStat(PlayerRoleStat s)
    {
        return new PlayerRoleStat
        {
            PlayerId = s.PlayerId,
            Name = s.Name,
            RoleName = s.RoleName,
            VanillaRoleName = s.VanillaRoleName,
            CustomRoleName = s.CustomRoleName
        };
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcProtectPlayer))]
public static class RpcProtectPlayerSummaryPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] int colorId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (BanMod.IsBanModDisabled) return;
        if (target == null || target.Data == null) return;

        if (BanMod.ShieldedPlayers.Contains(target.PlayerId))
            return;

        PreviousMatchPopupTracker.RegisterProtectionReceived(target);
        PreviousMatchPopupTracker.MarkRecentProtection(target);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class CompleteTaskSummaryPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (BanMod.IsBanModDisabled) return;
        if (__instance == null || __instance.Data == null) return;

        PreviousMatchPopupTracker.RegisterTaskCompletion(__instance);
    }
}