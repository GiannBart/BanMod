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

    private static bool EndGameSent;

    public static void Start()
    {
        if (!EnableGameTimer.GetBool())
        {
            Stop();
            return;
        }

        TotalTime = GameTimerMinutes.GetInt() * 60f;
        RemainingTime = TotalTime;

        IsRunning = true;
        IsPaused = false;
        EndGameSent = false;
    }

    public static void Update(float deltaTime)
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (!IsRunning || IsPaused || EndGameSent)
            return;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        RemainingTime -= deltaTime;

        if (RemainingTime > 0f)
            return;

        RemainingTime = 0f;
        IsRunning = false;
        EndGameSent = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RpcEndGame(
                GameOverReason.ImpostorsByKill,
                false
            );
        }
    }

    public static void Pause()
    {
        if (!EnableGameTimer.GetBool())
            return;

        if (!IsRunning)
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

    public static void Stop()
    {
        IsRunning = false;
        IsPaused = false;
        EndGameSent = false;

        TotalTime = 0f;
        RemainingTime = 0f;
    }

    public static float GetElapsedTime()
    {
        return Mathf.Max(0f, TotalTime - RemainingTime);
    }

    public static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(
            Mathf.Max(0f, seconds)
        );

        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        if (minutes == 0)
            return $"{secs} sec";

        if (secs == 0)
            return $"{minutes} min";

        return $"{minutes}:{secs:00}";
    }

    public static void SendTimeMessage()
    {
        if (!EnableGameTimer.GetBool())
            return;

        string elapsed = ToFullWidthNumbers(FormatTime(GetElapsedTime()));
        string remaining = ToFullWidthNumbers(FormatTime(RemainingTime));

        string messages =
            $"Game Timer\n" +
            $"Elapsed: {elapsed}\n" +
            $"Remaining: {remaining}";

        if (AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(messages);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(messages);
            MessageBlocker.UpdateLastMessageTime();
        }
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class GameTimerUpdatePatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        GameTimeLimit.Update(Time.deltaTime);
    }
}


