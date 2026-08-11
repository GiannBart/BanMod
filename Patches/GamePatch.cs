//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Data;
using Il2CppSystem.Linq;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static BanMod.ExtendedPlayerControl;
using static BanMod.Translator;
using static BanMod.Utils;
using Math = System.Math;

namespace BanMod;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
public static class GameEndPatch
{
    private static void Postfix()
    {
        if (BanMod.IsBanModDisabled) return;

        VoteBanTracker.Reset();

        var NoisemakerRunManager = UnityEngine.Object.FindObjectOfType<NoisemakerRunManager>();
        if (NoisemakerRunManager != null) NoisemakerRunManager.ResetState();
        var StopandGoManager = UnityEngine.Object.FindObjectOfType<StopandGoManager>();
        if (StopandGoManager != null) StopandGoManager.ResetState();
        if (!AmongUsClient.Instance.AmHost) return;
        JesterWinState.Reset();
        UnifiedRPCHandlerPatch.ModdedClients.Clear();
        UnifiedRPCHandlerPatch.ResetModdedNotifications();
        Guesser.ResetSpecialKiller();
        Jester.ResetJester();
        Exiler.ResetExiler();
        Judge.ResetJudge();
        Profiler.ResetProfiler();
        Watcher.ResetWatcher();
        MurderPlayerCombinedPatch.misfireCount.Clear();
        BanMod.RoomZoneManagerInstance.ClearAllData();
        GuessManager.ResetForNewGame();
        ExilerManager.ResetForNewGame();
        CloseMeetingManager.ResetForNewGame();
        BanMod.playerDeathTimes.Clear();
        BanMod.ShieldedPlayers.Clear();
        BanMod.UnreportableBodies.Clear();
        ChatCommands.ComandoExeUsed = false;
        ChatCommands.ComandoRoomUsed = false;
        LobbyStartPatch.hasSentSummary = false;
        LobbyStartPatch.hasSentSummary1 = false;
        BanMod.forcedImpostorIds.Clear();
        ForcedRoleSystem.Clear();
        BanMod.forceImpostor = false;
        BanMod.hasSentHackWarning = false;
        BanMod.hasKilled = false;
        RpcSetTasksPatch.ResetGeneratedCommonTasks();
        SabotageSystemType_UpdateSystem_Patch.Clear();
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class GameStartPatch
{
    private static void Postfix()
    {
        if (BanMod.IsBanModDisabled) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (FakeMapLobbyUtility.Active) return;
        FirstMeetingProtectionManager.ResetForNewGame();
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
        if (Options.Jester.GetBool())
        {
            Jester.SelectJester();
        }
        if (gameMode == GameModeType.TaskRun)
        {
            TaskManager.GameStartTime = Time.time;
        }
        JesterWinState.endgame = false;
        AbortAllMessages();
        ImmortalManager.ResetImmortal();
        TaskManager.ResetTaskManager();
        MatchSummary1.Reset();
        TaskTracker.Clear();
        KillTracker.Clear();
        BanMod.InitiallyProtectedFriendCode = null;

        BanMod.IsFirstRound = true;
        BanMod.ProtectedPlayerIdThisMatch = 255;

        //if (Options.ProtectFirst.GetBool())
        //{
        //    if (BanMod.FirstDeadFriendCode != null)
        //    {
        //        PlayerControl playerToProtect = BanMod.AllPlayerControls
        //            .FirstOrDefault(p => p != null && p.Data != null && p.Data.FriendCode == BanMod.FirstDeadFriendCode);

        //        if (playerToProtect != null && !playerToProtect.Data.IsDead)
        //        {
        //            if (!BanMod.ShieldedPlayers.Contains(playerToProtect.PlayerId))
        //            {
        //                BanMod.ShieldedPlayers.Add(playerToProtect.PlayerId);
        //            }
        //            BanMod.InitiallyProtectedFriendCode = playerToProtect.Data.FriendCode;

        //            BanMod.ProtectedPlayerIdThisMatch = playerToProtect.PlayerId;
        //        }
        //    }
        //}

        //BanMod.FirstDeadFriendCode = null;
    
        if (Options.ProtectFirstHost.GetBool())
        {
            if (PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                if (!BanMod.ShieldedPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
                {
                    BanMod.ShieldedPlayers.Add(PlayerControl.LocalPlayer.PlayerId);
                }
            }
        }
        MatchSummary1.StartMatchTimer();
        GameTimeLimit.Start();
        AmongUsClient.Instance.StartCoroutine(WaitForLocalPlayerAndExecute());
    }
    private static void ApplyProtectFirst()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        string previousFirstDeadFriendCode =
            BanMod.FirstDeadFriendCode;

        BanMod.FirstDeadFriendCode = null;

        if (!Options.ProtectFirstDead.GetBool())
        {
            BMLogger.Info(
                "[ProtectFirst] Opzione disabilitata."
            );

            return;
        }

        if (string.IsNullOrEmpty(
                previousFirstDeadFriendCode))
        {
            BMLogger.Info(
                "[ProtectFirst] Nessun primo morto registrato " +
                "nella partita precedente."
            );

            return;
        }

        PlayerControl playerToProtect =
            BanMod.AllPlayerControls.FirstOrDefault(player =>
                player != null &&
                player.Data != null &&
                !player.Data.IsDead &&
                !player.Data.Disconnected &&
                player != PlayerControl.LocalPlayer &&
                player.Data.FriendCode ==
                    previousFirstDeadFriendCode);

        if (playerToProtect == null)
        {
            BMLogger.Info(
                $"[ProtectFirst] Il giocatore non è presente " +
                $"nella nuova partita. FriendCode: " +
                $"{previousFirstDeadFriendCode}"
            );

            return;
        }

        bool applied =
            FirstMeetingProtectionManager.AddPlayer(
                playerToProtect,
                "ProtectFirst"
            );

        if (!applied)
            return;

        BanMod.InitiallyProtectedFriendCode =
            playerToProtect.Data.FriendCode;

        BanMod.ProtectedPlayerIdThisMatch =
            playerToProtect.PlayerId;

        BMLogger.Info(
            $"[ProtectFirst] Selezionato " +
            $"{playerToProtect.Data.PlayerName}, " +
            $"PlayerId: {playerToProtect.PlayerId}"
        );
    }
    private static void ApplyManualFirstMeetingProtection()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (Options.ProtectFirstPlayer == null)
            return;

        string selectedPlayerName = "None";

        try
        {
            int selectedIndex =
                Options.ProtectFirstPlayer.GetValue();

            string[] selections =
                Options.ProtectFirstPlayer.Selections;

            if (selections != null &&
                selectedIndex >= 0 &&
                selectedIndex < selections.Length)
            {
                selectedPlayerName =
                    selections[selectedIndex];
            }
        }
        catch (System.Exception exception)
        {
            BMLogger.LogWarning(
                $"[ManualFirstProtection] Errore lettura selezione: " +
                $"{exception.Message}"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                selectedPlayerName) ||
            string.Equals(
                selectedPlayerName,
                "None",
                System.StringComparison.OrdinalIgnoreCase
            ))
        {
            BMLogger.Info(
                "[ManualFirstProtection] Nessun giocatore selezionato."
            );

            return;
        }

        bool applied =
            FirstMeetingProtectionManager.AddPlayerByName(
                selectedPlayerName,
                "ManualSelection"
            );

        if (!applied)
        {
            BMLogger.LogWarning(
                $"[ManualFirstProtection] Impossibile proteggere: " +
                $"{selectedPlayerName}"
            );

            return;
        }

        BMLogger.Info(
            $"[ManualFirstProtection] Protezione applicata a: " +
            $"{selectedPlayerName}"
        );
    }
    private static IEnumerator WaitForLocalPlayerAndExecute()
    {
        while (PlayerControl.LocalPlayer == null)
            yield return null;

        while (PlayerControl.LocalPlayer.Data == null)
            yield return null;

        while (!BanMod.AllPlayerControls.All(p => p != null && p.Data != null && (p.roleAssigned || p.Data.Disconnected)))
        yield return null;

        ApplyProtectFirst();
        ApplyManualFirstMeetingProtection();

        if (Options.Jester.GetBool())
        {
            Jester.SendJesterMessage();
        }
        if (BanMod.GM.Value)
        {
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.CrewmateGhost);
            HudManager.Instance.StartCoroutine(CheatUtils.CompletaTutteLeTaskConDelay(1f));
            BMLogger.Info("[BANMOD] GM Mode");
        }
        if (ForcedRoleSystem.GM)
        {
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.CrewmateGhost);
            HudManager.Instance.StartCoroutine(CheatUtils.CompletaTutteLeTaskConDelay(1f));
            BMLogger.Info("[BANMOD] GM Mode");
        }
        if (Options.EngineerFixer.GetBool())
            Engineer.SendEngineerMessage();

        if (Options.PhantomGuess.GetBool())
            ImpostorGuesser.SendPhantomPlayerMessage();

        if (Options.ShapeGuess.GetBool())
            ImpostorGuesser.SendShapePlayerMessage();

        if (Options.ViperGuess.GetBool())
            ImpostorGuesser.SendViperPlayerMessage();

        if (Options.ImpostorGuess.GetBool())
            ImpostorGuesser.SendImpostorPlayerMessage();

        if (Options.ScientistTime.GetBool())
            Scientist.SendScientistMessage();

        if (Options.Guess.GetBool())
        {
            Guesser.OnStart();
            Guesser.SendKillerMessage();
        }
        if (Options.ExilerExe.GetBool())
        {
            Exiler.OnStart();
            Exiler.SendExilerMessage();
        }
        if (Options.Judge.GetBool())
        {
            Judge.OnStart();
            Judge.SendJudgeMessage();
        }
        if (Options.Profiler.GetBool())
        {
            Profiler.OnStart();
            Profiler.SendProfilerMessage();
        }
        if (Options.Watcher.GetBool())
        {
            Watcher.OnStart();

            if (Watcher.WatcherSelected && Watcher.WatcherId != 255)
            {
                Watcher.SelectWatcherLover();
                Watcher.SendWatcherMessage();
                Watcher.ApplyWatcherShield();
            }
        }
        PreviousMatchPopupTracker.ResetCurrentMatch();
        PreviousMatchPopupTracker.CaptureInitialRoles();
        Jester.ForcedJesterSelected = false;
        if (AmongUsClient.Instance.AmHost) SendHostTripleBoolRpc();
        BMLogger.Info("[GamePatch] Operazioni completate dopo l'assegnazione dei ruoli!");

    }
    
}
[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public static class CheckEndCriteriaPatch
{
    public static bool Prefix(LogicGameFlowNormal __instance)
    {
        if (BanMod.IsBanModDisabled) return true;
        if (!AmongUsClient.Instance.AmHost) return true;

        TaskTracker.Clear();
        ImpostorTracker.Clear();
        ImpostorTracker.DetectImpostors();
        int impVivi = PlayerControl.AllPlayerControls.ToArray().Count(p =>
            p != null &&
            p.Data != null &&
            !p.Data.IsDead &&
            !p.Data.Disconnected &&
            p.Data.Role.IsImpostor);

        int crewVivi = PlayerControl.AllPlayerControls.ToArray().Count(p =>
            p != null &&
            p.Data != null &&
            !p.Data.IsDead &&
            !p.Data.Disconnected &&
            !p.Data.Role.IsImpostor);
        int impostoriVivi = 0;
        bool mutanteInPunizione = false;
        bool showAd = !DataManager.Player.Ads.HasPurchasedAdRemoval;
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null) continue;

            TaskTracker.UpdatePlayerTask(player);

        }
        if (BanMod.NoGameEnd.Value) return false;

        if (MeetingHud.Instance != null && impVivi == 0)
        {
            if (LastImpostorMeetingEndDelay.TryStart(__instance, showAd))
                return false;
        }
        if (gameMode == GameModeType.TaskRun)
        {
            {
                return false;
            }

        }
        if (gameMode == GameModeType.SnS) 
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.IsDead)
                {
                    if (player.Data.Role.IsImpostor)
                    {
                        impostoriVivi++;
                    }
                    else if (player.isNew && gameMode == GameModeType.SnS)
                    {
                        mutanteInPunizione = true;
                        BMLogger.Info($"[CHECK_WIN] Trovato mutante in punizione ({player.Data.PlayerName}). Blocco fine game.");
                    }
                }
            }
            if (impostoriVivi == 0 && mutanteInPunizione)
            {
                return false;
            }
            if (impostoriVivi == 0 && !mutanteInPunizione)
            {
                return true;
            }
        }
        if (gameMode == GameModeType.FollowOrDeath || gameMode == GameModeType.RunOrDeath ) 
        {
            if (gameMode == GameModeType.RunOrDeath && NoisemakerRunManager.gameEnded)
            {
                __instance.Manager.RpcEndGame(GameOverReason.CrewmatesByTask, showAd);
                return false;
            }
            if (impVivi == 0)
            {
                __instance.Manager.RpcEndGame(GameOverReason.CrewmatesByVote, showAd);
                return false;
            }

            if (crewVivi >= 2)
            {
                return false;
            }
            else if (crewVivi == 1)
            {
                __instance.Manager.RpcEndGame(GameOverReason.CrewmatesByVote, showAd);
                return false; 
            }
        }

        if (gameMode == GameModeType.StopOrDeath) 
        {
            if (impVivi == 0)
            {
                __instance.Manager.RpcEndGame(GameOverReason.CrewmatesByVote, showAd);
                return false;
            }

            if (StopandGoManager.gameEnded || crewVivi == 1)
            {
                __instance.Manager.RpcEndGame(GameOverReason.CrewmatesByVote, showAd);
                return false;
            }
            else if (crewVivi >= 2)
            {
                return false;
            }
        }
        return true; 
    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class EndGameSavePatch
{
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        HostAfkManager.IsHostAfk = false;
        var reason = EndGameResult.CachedGameOverReason;
        PreviousMatchPopupTracker.SaveCurrentMatch();

        if (GameManager.Instance.DidHumansWin(reason))
        {
            MatchSummary1.CrewmateWin = true;
            MatchSummary1.ImpostorWin = false;
        }
        else
        {
            MatchSummary1.CrewmateWin = false;
            MatchSummary1.ImpostorWin = true;
        }
        MatchSummary1.StopMatchTimer();
        MatchSummary1.SaveToHistory();
        UnifiedRPCHandlerPatch.AlreadyHandledCheaters.Clear();
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
class CheckTaskCompletionPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (BanMod.NoGameEnd.Value)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(LogicRoleSelectionHnS), nameof(LogicRoleSelectionHnS.AssignRolesForTeam))]
public static class RoleSelectionPatch
{
    public static bool Prefix(
        LogicRoleSelectionHnS __instance,
        Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players,
        IGameOptions opts,
        RoleTeamTypes team,
        ref int teamMax)
    {
        if (!Options.MoreSeek.GetBool())
            return true;

        if (team != RoleTeamTypes.Impostor)
            return true;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return true;

        if (players == null)
            return false;

        int totalSeekersNeeded = Options.NumSeekers.GetInt();

        if (totalSeekersNeeded < 1)
            totalSeekersNeeded = 1;

        if (totalSeekersNeeded > players.Count)
            totalSeekersNeeded = players.Count;

        if (totalSeekersNeeded > 14)
            totalSeekersNeeded = 14;

        var hnsOptions = GameOptionsManager.Instance.CurrentGameOptions.Cast<HideNSeekGameOptionsV10>();

        if (hnsOptions != null)
        {
            hnsOptions.NumImpostors = totalSeekersNeeded;

            hnsOptions.ImpostorPlayerID = -1;
        }

        teamMax = totalSeekersNeeded;

        for (int i = 0; i < totalSeekersNeeded; i++)
        {
            if (players.Count == 0)
                break;

            string choice = "Round-robin";

            if (Options.SeekerSelections != null && i < Options.SeekerSelections.Count && Options.SeekerSelections[i] != null)
            {
                try
                {
                    choice = Options.SeekerSelections[i].GetString();
                }
                catch
                {
                    choice = "Round-robin";
                }
            }

            NetworkedPlayerInfo picked = PickSeeker(players, choice, i);

            if (picked == null || picked.Object == null)
                continue;

            // Questo è il punto chiave:
            // Seeker 1 = indice 0.
            // Se scegli un nome in SetSeeker 1, quel player diventa il vero seeker principale.
            if (i == 0 && hnsOptions != null)
            {
                hnsOptions.ImpostorPlayerID = picked.PlayerId;
            }

            picked.Object.RpcSetRole(RoleTypes.Impostor, false);
            players.Remove(picked);
        }

        return false;
    }

    private static NetworkedPlayerInfo PickSeeker(
        Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players,
        string choice,
        int seekerIndex)
    {
        if (players == null || players.Count == 0)
            return null;

        if (!string.IsNullOrEmpty(choice) && choice != "Round-robin")
        {
            for (int i = 0; i < players.Count; i++)
            {
                NetworkedPlayerInfo p = players[i];

                if (p == null)
                    continue;

                if (p.PlayerName == choice)
                    return p;
            }

            return players[UnityEngine.Random.Range(0, players.Count)];
        }

        var pseudoRandomList = new PseudoRandomList<NetworkedPlayerInfo>(AmongUsClient.Instance.GameId);

        for (int i = 0; i < players.Count; i++)
        {
            NetworkedPlayerInfo p = players[i];

            if (p == null)
                continue;

            pseudoRandomList.Add(p);
        }

        // Round-robin stabile per round + indice seeker.
        int skips = GameData.RoundsPlayedInSession + seekerIndex;

        for (int r = 0; r < skips; r++)
        {
            pseudoRandomList.PickRandom();
        }

        return pseudoRandomList.PickRandom();
    }
}

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.IsGameOverDueToDeath))]
public static class VictoryLogicPatch
{
    public static bool Prefix(LogicGameFlowHnS __instance, ref bool __result)
    {
        if (!Options.MoreSeek.GetBool())
            return true;


        var counts = __instance.GetPlayerCounts();
        int seekers = counts.Item1;
        int crewmates = counts.Item2;
        int ghosts = counts.Item3;

        __result = crewmates <= 0 && (!DestroyableSingleton<TutorialManager>.InstanceExists || ghosts > 0)
                   || seekers <= 0;

        return false;
    }
}

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.CheckEndCriteria))]
public static class EndCriteriaPatch
{
    public static bool Prefix(LogicGameFlowHnS __instance)
    {
        if (BanMod.NoGameEnd.Value)
            return false;

        if (GameData.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return true;

        if (__instance == null || __instance.Manager == null || AmongUsClient.Instance.IsGameOver)
            return false;

        if (!Options.MoreSeek.GetBool())
            return true;

        bool showAd = false;

        try
        {
            showAd = !DataManager.Player.Ads.HasPurchasedAdRemoval;
        }
        catch
        {
            showAd = false;
        }


        int seekersAlive = 0;
        int crewmatesAlive = 0;

        foreach (var player in GameData.Instance.AllPlayers)
        {
            if (player == null || player.Disconnected)
                continue;

            if (player.Role.IsImpostor)
            {
                if (!player.IsDead)
                    seekersAlive++;
            }
            else
            {
                if (!player.IsDead)
                    crewmatesAlive++;
            }
        }

        if (crewmatesAlive <= 0)
        {
            __instance.Manager.RpcEndGame(GameOverReason.HideAndSeek_ImpostorsByKills, false);
            return false;
        }

        if (seekersAlive <= 0)
        {
            __instance.Manager.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
            return false;
        }

        if (__instance.AllTimersExpired())
        {
            __instance.Manager.RpcEndGame(GameOverReason.HideAndSeek_CrewmatesByTimer, false);
            return false;
        }

        return false;
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
public static class UpdateSeekerNamesPatch
{
    private static float timer = 0f;

    public static void Postfix()
    {
        timer += Time.deltaTime;
        if (timer < 2f) return;
        timer = 0f;

        if (AmongUsClient.Instance == null || GameData.Instance == null) return;

        var namesList = new List<string> { "Round-robin" };
        foreach (var player in GameData.Instance.AllPlayers)
        {
            if (player != null && !player.Disconnected)
                namesList.Add(player.PlayerName);
        }

        string[] namesArray = namesList.ToArray();

        foreach (var opt in Options.SeekerSelections)
        {
            opt.Selections = namesArray;
            opt.Rule = (0, namesArray.Length - 1, 1);
        }
    }
}