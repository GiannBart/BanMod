//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Linq;
using MS.Internal.Xml.XPath;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;
using static FilterPopUp.FilterInfoUI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace BanMod;

public static class RolesCommand
{
    public static bool Cmd(byte srcPlayerId, byte suspectPlayerId)
    {
        PlayerControl player = Utils.GetPlayerById(srcPlayerId);
        PlayerControl targetPlayer = Utils.GetPlayerById(suspectPlayerId);

        if (player == null || targetPlayer == null) return false;

        bool isSpecialKiller = player.PlayerId == Guesser.SpecialKillerId;
        bool isJester = player.PlayerId == Jester.JesterId;
        bool isPresident = player.PlayerId == Exiler.ExilerId;
        bool isJudge= player.PlayerId == Judge.JudgeId;
        bool isProfiler = player.PlayerId == Profiler.ProfilerId;
        bool isScientist = Scientist(player);
        bool isTracker = Tracker(player);
        bool isPhantom = Phantom(player) && Options.EnableImmortal.GetBool();
        bool isViper = Cobra(player) && Options.EnableImmortal.GetBool();
        bool isShape = Shapeshifter(player) && Options.EnableImmortal.GetBool();
        bool isImpostor = Impostor(player) && Options.EnableImmortal.GetBool();

        string killerName = player.Data.PlayerName;
        string targetName = targetPlayer.Data.PlayerName;

        NetworkedPlayerInfo targetToExileInfo = targetPlayer.Data;
        NetworkedPlayerInfo playerToExileInfo = player.Data;
        NetworkedPlayerInfo playerToProtectInfo = player.Data;
        var action1 = Options.GuesserAction.GetValue();
        var action2 = Options.ExilerAction.GetValue();

        if (isSpecialKiller && Options.Guess.GetBool())
        {
            bool isImpostortrue =
    targetPlayer.Data != null &&
    targetPlayer.Data.Role != null &&
    targetPlayer.Data.Role.IsImpostor;
            if (!isImpostortrue)
            {
                if (action1 == 1)
                {
                    Utils.Exile(playerToExileInfo);
                }
                else if (action1 == 0)
                {
                    KillPlayerAndNotify(player);
                }

                PreviousMatchPopupTracker.RegisterGuesserAttempt(
                    player,
                    targetPlayer,
                    GetString("Impostor"),
                    false
                );
            }
            else
            {
                if (action1 == 1)
                {
                    Utils.Exile(targetToExileInfo);
                }
                else if (action1 == 0)
                {
                    KillPlayerAndNotify(targetPlayer);
                }

                PreviousMatchPopupTracker.RegisterGuesserAttempt(
                    player,
                    targetPlayer,
                    GetString("Impostor"),
                    true
                );
            }

            return true;
        }

        if (isPresident && Options.ExilerExe.GetBool())
        {
            if (ChatCommands.ComandoExeUsed) return false;

            if (playerToExileInfo == null || playerToExileInfo.IsDead || playerToExileInfo.Disconnected)
                return false;

            BMLogger.Info($"[BBM] Target trovato: {targetPlayer?.Data?.PlayerName ?? "Nessuno"} con ID {targetPlayer?.PlayerId}");

            ChatCommands.ComandoExeUsed = true;

            if (action2 == 1)
            {
                Utils.Exile(targetToExileInfo);
                PreviousMatchPopupTracker.RegisterPresidentExile(player, targetPlayer);
            }
            else if (action2 == 0)
            {
                KillPlayerAndNotify(targetPlayer);
                PreviousMatchPopupTracker.RegisterPresidentKill(player, targetPlayer);
            }
            else
            {
                PreviousMatchPopupTracker.RegisterPresidentFail(player, targetPlayer);
            }

            if (Options.killexiler.GetBool())
            {
                KillPlayerAndNotify(player);
            }

            return true;
        }
        if (isScientist && Options.ScientistTime.GetBool())
        {
            Scientist.ScientistCommand(player);
            return true;
        }

        if (isPhantom && Options.PhantomGuess.GetBool())
        {
            if (!ImmortalManager.immortalAssigned && !Options.aktive_notimmplayer.GetBool()) return false;

            bool success = ImmortalManager.IsImmortal(targetPlayer.PlayerId);

            if (!success)
            {
                KillPlayerAndNotify(player);
                PreviousMatchPopupTracker.RegisterPhantomAttempt(player, targetPlayer, false);
            }
            else
            {
                KillPlayerAndNotify(targetPlayer);
                PreviousMatchPopupTracker.RegisterPhantomAttempt(player, targetPlayer, true);
            }

            return true;
        }

        if (isViper && Options.ViperGuess.GetBool())
        {
            if (!ImmortalManager.immortalAssigned && !Options.aktive_notimmplayer.GetBool()) return false;

            bool success = ImmortalManager.IsImmortal(targetPlayer.PlayerId);

            if (!success)
            {
                KillPlayerAndNotify(player);
                PreviousMatchPopupTracker.RegisterViperAttempt(player, targetPlayer, false);
            }
            else
            {
                KillPlayerAndNotify(targetPlayer);
                PreviousMatchPopupTracker.RegisterViperAttempt(player, targetPlayer, true);
            }

            return true;
        }

        if (isShape && Options.ShapeGuess.GetBool())
        {
            if (!ImmortalManager.immortalAssigned && !Options.aktive_notimmplayer.GetBool()) return false;

            bool success = ImmortalManager.IsImmortal(targetPlayer.PlayerId);

            if (!success)
            {
                KillPlayerAndNotify(player);
                PreviousMatchPopupTracker.RegisterShapeAttempt(player, targetPlayer, false);
            }
            else
            {
                KillPlayerAndNotify(targetPlayer);
                PreviousMatchPopupTracker.RegisterShapeAttempt(player, targetPlayer, true);
            }

            return true;
        }

        if (isImpostor && Options.ImpostorGuess.GetBool())
        {
            if (!ImmortalManager.immortalAssigned && !Options.aktive_notimmplayer.GetBool()) return false;

            bool success = ImmortalManager.IsImmortal(targetPlayer.PlayerId);

            if (!success)
            {
                KillPlayerAndNotify(player);
                PreviousMatchPopupTracker.RegisterImpostorAttempt(player, targetPlayer, false);
            }
            else
            {
                KillPlayerAndNotify(targetPlayer);
                PreviousMatchPopupTracker.RegisterImpostorAttempt(player, targetPlayer, true);
            }

            return true;
        }

        return false;
    }
    private static void KillPlayerAndNotify(PlayerControl victim)
    {
        if (victim == null || victim.Data == null)
            return;

        string victimName = GetRealPlayerName(victim);
        string msg = $"{victimName} {GetString("PlayerIsDead")}";
        SendDeathMessage(msg);
        Utils.KillPlayer(victim);

    }
    private static string GetRealPlayerName(PlayerControl player)
    {
        try
        {
            if (player?.Data != null && !string.IsNullOrWhiteSpace(player.Data.PlayerName))
                return player.Data.PlayerName;
        }
        catch
        {
        }

        return "Unknown";
    }
    private static void SendDeathMessage(string msg)
    {
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg);
            MessageBlocker.UpdateLastMessageTime();
        }
    }
}
public static class RoleCommandActionRpc
{
    public static void Send(byte targetPlayerId)
    {
        PlayerControl localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null)
            return;

        byte srcPlayerId = localPlayer.PlayerId;

        if (AmongUsClient.Instance.AmHost)
        {
            ExecuteOnHost(srcPlayerId, targetPlayerId);
            return;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
            localPlayer.NetId,
            (byte)CustomRPC.RoleCommandAction,
            SendOption.Reliable,
            AmongUsClient.Instance.HostId
        );

        writer.Write(srcPlayerId);
        writer.Write(targetPlayerId);

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void Receive(PlayerControl senderObject, MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        byte srcPlayerId = reader.ReadByte();
        byte targetPlayerId = reader.ReadByte();

        if (senderObject == null || senderObject.PlayerId != srcPlayerId)
        {
            BMLogger.Warn(
                $"RoleCommandAction spoof bloccato. Sender={senderObject?.PlayerId} Src={srcPlayerId}",
                "RoleCommandAction"
            );
            return;
        }

        ExecuteOnHost(srcPlayerId, targetPlayerId);
    }

    private static void ExecuteOnHost(byte srcPlayerId, byte targetPlayerId)
    {
        PlayerControl src = Utils.GetPlayerById(srcPlayerId);
        PlayerControl target = Utils.GetPlayerById(targetPlayerId);

        if (src == null || target == null)
            return;

        if (src.Data == null || target.Data == null)
            return;

        if (src.Data.IsDead || src.Data.Disconnected)
            return;

        if (target.Data.IsDead || target.Data.Disconnected)
            return;

        bool success = RolesCommand.Cmd(srcPlayerId, targetPlayerId);

        if (!success)
        {
            Utils.SendMessage(Translator.GetString("NeutralInfo"), srcPlayerId);
        }
    }
}
public static class RoleButtonRefresh
{
    private static float lastRefreshTime = -10f;

    public static void RefreshNow()
    {
        try
        {
            if (MeetingHud.Instance == null)
                return;

            if (Time.realtimeSinceStartup - lastRefreshTime < 0.20f)
                return;

            lastRefreshTime = Time.realtimeSinceStartup;

            MeetingHud.Instance.StartCoroutine(RefreshRoutine(MeetingHud.Instance));
        }
        catch
        {
        }
    }

    private static IEnumerator RefreshRoutine(MeetingHud meetingHud)
    {
        if (meetingHud == null)
            yield break;

        yield return null;
        yield return null;

        try
        {
            ExilerManager.CreateExilerButton(meetingHud);
        }
        catch
        {
        }

        try
        {
            GuessManager.CreateGuesserButton(meetingHud);
        }
        catch
        {
        }

        try
        {
            ImpGuessManager.CreateImpGuesserButton(meetingHud);
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MeetingHudRoleButtonsStartPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        try
        {
            if (__instance == null)
                return;

            __instance.StartCoroutine(DelayedRefresh(__instance));
        }
        catch
        {
        }
    }

    private static IEnumerator DelayedRefresh(MeetingHud meetingHud)
    {
        if (meetingHud == null)
            yield break;

        yield return null;
        yield return null;
        yield return null;

        RoleButtonRefresh.RefreshNow();
    }
}
