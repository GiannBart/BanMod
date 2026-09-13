// Credits and licenses in the resources folder.
using HarmonyLib;
using UnityEngine;
using static BanMod.Options;
using static BanMod.Utils;

namespace BanMod;

public static class GameTimeLimit
{
    public static float TotalTime { get; private set; }
    public static float RemainingTime { get; private set; }

    public static bool IsRunning { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool EndedByTimer { get; private set; }
    public static float MeetingTime { get; private set; }

    private static bool MeetingActive;
    private static float MeetingStartedAt;
    private static bool EndGameSent;
    private static bool TimerWinMessageSent;
    private static bool EndGamePendingAfterMeeting;

    public static void Start()
    {
        if (!EnableGameTimer.GetBool())
        {
            Stop();
            return;
        }

        TotalTime = GameTimerMinutes.GetFloat() * 60f;
        RemainingTime = TotalTime;

        IsRunning = true;
        IsPaused = false;

        EndGameSent = false;
        EndGamePendingAfterMeeting = false;
        EndedByTimer = false;
        TimerWinMessageSent = false;

        MeetingTime = 0f;
        MeetingStartedAt = 0f;
        MeetingActive = false;
    }

    public static void Update(float deltaTime)
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (!IsRunning || IsPaused || EndGameSent)
            return;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (PauseGameTimerDuringMeetings.GetBool() &&
            MeetingActive)
        {
            return;
        }

        RemainingTime -= deltaTime;

        if (RemainingTime > 0f)
            return;

        RemainingTime = 0f;

        // Se il timer scade durante un meeting,
        // aspetta la fine del meeting prima di chiudere il game.
        if (MeetingActive)
        {
            IsRunning = false;
            EndGamePendingAfterMeeting = true;
            return;
        }

        EndGameByTimer();
    }

    private static void EndGameByTimer()
    {
        if (EndGameSent)
            return;

        if (GameManager.Instance == null)
            return;

        EndGameSent = true;
        EndGamePendingAfterMeeting = false;
        IsRunning = false;
        EndedByTimer = true;

        MatchSummary1.CaptureGameTimer();

        GameManager.Instance.RpcEndGame(
            GameOverReason.ImpostorsByKill,
            false
        );
    }
    public static string FormatMinutesSeconds(float seconds)
    {
        int totalSeconds = Mathf.RoundToInt(
            Mathf.Max(0f, seconds)
        );

        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        if (minutes > 0 && secs > 0)
            return $"{minutes}m{secs}s";

        if (minutes > 0)
            return $"{minutes}m";

        return $"{secs}s";
    }

    //private static void SendTimerWinMessageOnce()
    //{
    //    if (!GameTimerMessage.GetBool())
    //        return;

    //    if (TimerWinMessageSent)
    //        return;

    //    TimerWinMessageSent = true;

    //    string message =
    //        Translator.GetString("ImpostorWinsTimer");

    //    if (AmongUsClient.Instance != null &&
    //        AmongUsClient.Instance.AmHost &&
    //        PlayerControl.LocalPlayer?.Data != null &&
    //        PlayerControl.LocalPlayer.Data.IsDead)
    //    {
    //        Utils.RequestProxyMessage(message);
    //    }
    //    else
    //    {
    //        Utils.SendMessage(message);
    //    }

    //    MessageBlocker.UpdateLastMessageTime();
    //}

    public static void Pause()
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (!IsRunning || EndGameSent)
            return;

        IsPaused = true;
    }

    public static void Resume()
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (!IsRunning || EndGameSent)
            return;

        IsPaused = false;
    }

    public static void OnMeetingStarted()
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (MeetingActive)
            return;

        MeetingActive = true;
        MeetingStartedAt = Time.realtimeSinceStartup;
    }

    public static void OnMeetingEnded()
    {
        if (!MeetingActive)
            return;

        MeetingTime += Mathf.Max(
            0f,
            Time.realtimeSinceStartup - MeetingStartedAt
        );

        MeetingActive = false;
        MeetingStartedAt = 0f;

        if (EndGamePendingAfterMeeting)
        {
            EndGameByTimer();
        }
    }
    public static float GetMeetingTime()
    {
        float total = MeetingTime;

        if (MeetingActive)
        {
            total += Mathf.Max(
                0f,
                Time.realtimeSinceStartup - MeetingStartedAt
            );
        }

        return total;
    }
    public static void Stop()
    {
        IsRunning = false;
        IsPaused = false;

        EndGameSent = false;
        EndGamePendingAfterMeeting = false;
        EndedByTimer = false;
        TimerWinMessageSent = false;

        TotalTime = 0f;
        RemainingTime = 0f;
        MeetingTime = 0f;
        MeetingStartedAt = 0f;
        MeetingActive = false;
    }

    public static float GetElapsedTime()
    {
        return Mathf.Max(0f, TotalTime - RemainingTime);
    }

    // Formato per countdown e tempi effettivi.
    // Esempio: 4.5 minuti -> 04:30.
    public static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(
            Mathf.Max(0f, seconds)
        );

        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        return $"{minutes:D2}:{secs:D2}";
    }

    // Formato in minuti decimali.
    // Esempi: 4, 4.5, 5, 5.5.
    public static string FormatMinutes(float seconds)
    {
        float minutes = Mathf.Max(0f, seconds) / 60f;

        return minutes.ToString(
            "0.#",
            System.Globalization.CultureInfo.InvariantCulture
        );
    }

    public static void SendTimeMessage()
    {
        if (!EnableGameTimer.GetBool())
            return;

        string configuredMinutes = ToFullWidthNumbers(
            FormatMinutesSeconds(TotalTime)
        );

        string elapsed = ToFullWidthNumbers(
            FormatTime(GetElapsedTime())
        );

        string remaining = ToFullWidthNumbers(
            FormatTime(RemainingTime)
        );

        string message =
            "Game Timer\n" +
            $"Duration: {configuredMinutes}\n" +
            $"Elapsed: {elapsed}\n" +
            $"Remaining: {remaining}";

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer?.Data != null &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(message);
        }
        else
        {
            Utils.SendMessage(message);
        }

        MessageBlocker.UpdateLastMessageTime();
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class GameTimerUpdatePatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        GameTimeLimit.Update(Time.unscaledDeltaTime);
    }
}
[HarmonyPatch(typeof(MeetingHud), "Start")]
public static class GameTimerMeetingStartPatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        GameTimeLimit.OnMeetingStarted();
    }
}

[HarmonyPatch(typeof(MeetingHud), "OnDestroy")]
public static class GameTimerMeetingEndPatch
{
    public static void Prefix()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        GameTimeLimit.OnMeetingEnded();
    }
}