using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod;

public static class FirstMeetingProtectionManager
{
    private static readonly HashSet<byte> ProtectedPlayerIds = new();

    public static bool FirstMeetingStarted { get; private set; }

    public static void ResetForNewGame()
    {
        ProtectedPlayerIds.Clear();
        FirstMeetingStarted = false;

        BMLogger.Info(
            "[FirstMeetingProtection] Stato resettato per la nuova partita."
        );
    }

    public static bool IsProtected(byte playerId)
    {
        return !FirstMeetingStarted &&
               ProtectedPlayerIds.Contains(playerId);
    }

    public static bool AddPlayer(
        PlayerControl player,
        string source = "Unknown")
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        if (FirstMeetingStarted)
            return false;

        if (player == null ||
            player.Data == null ||
            player.Data.IsDead ||
            player.Data.Disconnected)
        {
            return false;
        }

        bool added =
            ProtectedPlayerIds.Add(player.PlayerId);

        if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
        {
            BanMod.ShieldedPlayers.Add(player.PlayerId);
        }

        ApplyShield(player);

        BMLogger.Info(
            $"[FirstMeetingProtection] Protetto " +
            $"{player.Data.PlayerName} ({player.PlayerId}). " +
            $"Origine: {source}. Nuovo: {added}"
        );

        return true;
    }

    public static bool AddPlayerByName(
        string playerName,
        string source = "Manual")
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        PlayerControl player =
            BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.Disconnected &&
                string.Equals(
                    p.Data.PlayerName,
                    playerName,
                    System.StringComparison.OrdinalIgnoreCase
                ));

        if (player == null)
        {
            BMLogger.LogWarning(
                $"[FirstMeetingProtection] Player non trovato: {playerName}"
            );

            return false;
        }

        return AddPlayer(player, source);
    }

    public static void ReapplyAfterProtectedKill(
        PlayerControl target)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (target == null || target.Data == null)
            return;

        if (!IsProtected(target.PlayerId))
            return;

        if (MeetingHud.Instance != null)
            return;

        if (target.Data.IsDead ||
            target.Data.Disconnected)
        {
            return;
        }

        ApplyShield(target);

        byte playerId = target.PlayerId;

        LateTask.New(
            () =>
            {
                if (FirstMeetingStarted)
                    return;

                if (MeetingHud.Instance != null)
                    return;

                PlayerControl currentPlayer =
                    BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p != null &&
                        p.Data != null &&
                        p.PlayerId == playerId);

                if (currentPlayer == null ||
                    currentPlayer.Data.IsDead ||
                    currentPlayer.Data.Disconnected)
                {
                    return;
                }

                if (!IsProtected(playerId))
                    return;

                ApplyShield(currentPlayer);
            },
            0.05f,
            $"ReapplyFirstMeetingShield-{playerId}"
        );

        BMLogger.Info(
            $"[FirstMeetingProtection] Riapplicazione scudo richiesta per " +
            $"{target.Data.PlayerName}."
        );
    }

    private static void ApplyShield(
        PlayerControl target)
    {
        if (target == null ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected)
        {
            return;
        }

        PlayerControl guardian =
            PlayerControl.LocalPlayer;

        if (guardian == null ||
            guardian.Data == null)
        {
            return;
        }

        if (target.protectedByGuardianId > -1)
            return;

        int colorId =
            guardian.Data.DefaultOutfit.ColorId;

        guardian.ProtectPlayer(
            target,
            colorId
        );
    }

    private static bool MustKeepShield(
        byte playerId)
    {
        if (ImmortalManager.IsImmortal(playerId))
            return true;

        if (Watcher.IsWatcher(playerId))
            return true;

        if (PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.PlayerId == playerId &&
            Options.ProtectFirstHost.GetBool())
        {
            return true;
        }

        return false;
    }

    public static void EndAtFirstMeeting()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (FirstMeetingStarted)
            return;

        FirstMeetingStarted = true;

        foreach (byte playerId in ProtectedPlayerIds.ToArray())
        {
            PlayerControl player =
                BanMod.AllPlayerControls.FirstOrDefault(p =>
                    p != null &&
                    p.Data != null &&
                    p.PlayerId == playerId);

            bool keepShield =
                MustKeepShield(playerId);

            if (keepShield)
            {
                BMLogger.Info(
                    $"[FirstMeetingProtection] Protezione temporanea terminata " +
                    $"per {player?.Data?.PlayerName ?? playerId.ToString()}, " +
                    $"ma lo scudo permanente viene mantenuto."
                );

                continue;
            }

            BanMod.ShieldedPlayers.Remove(playerId);

            if (player == null ||
                player.Data == null ||
                player.Data.IsDead)
            {
                continue;
            }

            if (player.protectedByGuardianId > -1)
            {
                player.RemoveProtection();
                player.protectedByGuardianId = -1;
                player.Data.MarkDirty();
            }

            BMLogger.Info(
                $"[FirstMeetingProtection] Scudo temporaneo rimosso da " +
                $"{player.Data.PlayerName}."
            );
        }

        ProtectedPlayerIds.Clear();

        BanMod.ProtectedPlayerIdThisMatch = 255;
        BanMod.InitiallyProtectedFriendCode = null;

        BMLogger.Info(
            "[FirstMeetingProtection] Primo meeting iniziato: " +
            "protezioni temporanee concluse."
        );
    }
}