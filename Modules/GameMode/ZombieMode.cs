using System;
using System.Collections.Generic;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace BanMod
{
    public static class ZombieModeController
    {
        private const byte BlueColorId = 1;
        private const float SecondaryInfectionBlinkDuration = 5f;
        private static readonly byte GreenColorId =
            unchecked((byte)Palette.PreviewGreenColorId);

        private sealed class PendingInfection
        {
            internal PlayerControl Player;
            internal float Elapsed;
            internal float BlinkAccumulator;
            internal bool ShowsGreen;
        }

        private static readonly HashSet<byte> Infected = new HashSet<byte>();
        private static readonly Dictionary<byte, PendingInfection>
            PendingInfections = new Dictionary<byte, PendingInfection>();
        private static readonly Dictionary<byte, float> ExposureByTarget =
            new Dictionary<byte, float>();

        private static readonly Dictionary<byte, int> TouchCountByTarget =
            new Dictionary<byte, int>();

        private static readonly HashSet<byte> ContactLatchedTargets =
            new HashSet<byte>();

        private static bool _active;
        private static bool _initialInfectionFinished;
        private static bool _resyncAfterMeeting;
        private static float _gameplayElapsed;
        private static float _blinkAccumulator;
        private static bool _blinkShowsGreen;
        private static PlayerControl _warningPlayer;

        private static bool _baseOptionsCaptured;
        private static float _baseSpeed;
        private static float _baseCrewVision;
        private static float _baseImpostorVision;

        private static bool _ending;
        private static float _winCheckAccumulator;
        private static float _optionsResyncAccumulator;

        internal static bool IsLobby => GameStates.isLobby;
        public static bool IsActive => !IsLobby && _active && !_ending;
        public static bool IsRunning => !IsLobby && _active;

        internal static void Begin()
        {
            if (IsLobby)
                return;

            Stop();

            if (!Options.IsZombieMode ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null ||
                ShipStatus.Instance == null)
                return;

            CaptureBaseOptions();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsConnectedPlayer(player))
                    continue;

                player.RpcSetRole(RoleTypes.Crewmate, true);
                player.RpcSetColor(BlueColorId);
            }

            _active = true;
            BMLogger.Info(
                $"Modalita zombie avviata: prima infezione={Options.ZombieInitialInfectionDelay.GetInt()}s, tocchi richiesti={Options.ZombieTouchesToInfect.GetInt()}, durata tocco={Options.ZombieTouchDurationSeconds.GetFloat():0.0}s.",
                "ZombieMode");
        }

        internal static void Stop()
        {
            if (_baseOptionsCaptured)
                RestoreLocalBaseOptions();

            _active = false;
            _initialInfectionFinished = false;
            _resyncAfterMeeting = false;
            _gameplayElapsed = 0f;
            _blinkAccumulator = 0f;
            _blinkShowsGreen = false;
            _warningPlayer = null;
            _baseOptionsCaptured = false;
            _ending = false;
            _winCheckAccumulator = 0f;
            _optionsResyncAccumulator = 0f;
            Infected.Clear();
            PendingInfections.Clear();
            ExposureByTarget.Clear();
            TouchCountByTarget.Clear();
            ContactLatchedTargets.Clear();
        }

        internal static void Tick(float deltaTime)
        {
            if (IsLobby ||
                !_active ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null ||
                ShipStatus.Instance == null)
                return;

            if (MeetingHud.Instance != null)
            {
                ExposureByTarget.Clear();
                ContactLatchedTargets.Clear();
                return;
            }

            if (_resyncAfterMeeting)
            {
                _resyncAfterMeeting = false;
                ReapplyZombieOptions();
            }

            float safeDelta = Mathf.Clamp(deltaTime, 0f, 0.25f);
            _gameplayElapsed += safeDelta;

            if (!_initialInfectionFinished)
                TickInitialInfection(safeDelta);
            else
            {
                TickPendingInfections(safeDelta);
                TickContactInfections(safeDelta);
            }

            if (_initialInfectionFinished)
            {
                _optionsResyncAccumulator += safeDelta;
                if (_optionsResyncAccumulator >= 1f)
                {
                    _optionsResyncAccumulator = 0f;
                    ReapplyZombieOptions();
                }
            }

            _winCheckAccumulator += safeDelta;
            if (_winCheckAccumulator >= 0.25f)
            {
                _winCheckAccumulator = 0f;
                CheckWinConditions();
            }
        }

        internal static void MarkMeetingFinished()
        {
            if (IsLobby || !_active)
                return;

            ExposureByTarget.Clear();
            ContactLatchedTargets.Clear();
            _resyncAfterMeeting = true;
        }

        private static void TickInitialInfection(float deltaTime)
        {
            if (IsLobby)
                return;

            float infectionAt = Mathf.Max(
                0f, Options.ZombieInitialInfectionDelay.GetInt());
            float warningDuration = Mathf.Min(5f, infectionAt);
            float warningAt = infectionAt - warningDuration;

            if (_warningPlayer == null && _gameplayElapsed >= warningAt)
            {
                _warningPlayer = SelectInitialZombie();
                _blinkAccumulator = 0f;
                _blinkShowsGreen = false;

                if (_warningPlayer != null)
                    _warningPlayer.RpcSetColor(BlueColorId);
            }

            if (_warningPlayer != null && !IsEligibleTarget(_warningPlayer))
            {
                _warningPlayer = SelectInitialZombie();
                _blinkAccumulator = 0f;
                _blinkShowsGreen = false;
            }

            if (_warningPlayer != null && _gameplayElapsed < infectionAt)
            {
                _blinkAccumulator += deltaTime;
                float interval = 0.20f;

                if (_blinkAccumulator >= interval)
                {
                    _blinkAccumulator -= interval;
                    _blinkShowsGreen = !_blinkShowsGreen;
                    _warningPlayer.RpcSetColor(
                        _blinkShowsGreen ? GreenColorId : BlueColorId);
                }
            }

            if (_gameplayElapsed < infectionAt)
                return;

            if (_warningPlayer == null)
            {
                BMLogger.Info(
                    "Nessun giocatore valido per la prima infezione.",
                    "ZombieMode");
                return;
            }

            Infect(_warningPlayer);
            _warningPlayer = null;
            _initialInfectionFinished = true;
        }

        private static PlayerControl SelectInitialZombie()
        {
            var candidates = new List<PlayerControl>();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (IsEligibleTarget(player))
                    candidates.Add(player);
            }

            if (candidates.Count == 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static void TickContactInfections(float deltaTime)
        {
            if (IsLobby)
                return;

            var newlyInfected = new List<PlayerControl>();
            float radius = 1f;
            float radiusSquared = radius * radius;

            int requiredTouches = Mathf.Clamp(
                Options.ZombieTouchesToInfect != null
                    ? Options.ZombieTouchesToInfect.GetInt()
                    : 1,
                1,
                20);

            float requiredContactSeconds = Mathf.Clamp(
                Options.ZombieTouchDurationSeconds != null
                    ? Options.ZombieTouchDurationSeconds.GetFloat()
                    : 0.2f,
                0.1f,
                10f);

            foreach (PlayerControl target in PlayerControl.AllPlayerControls)
            {
                if (!IsEligibleTarget(target))
                {
                    if (target != null)
                        ClearContactProgress(target.PlayerId, true);

                    continue;
                }

                byte targetId = target.PlayerId;
                bool nearZombie = false;
                Vector2 targetPosition = target.GetTruePosition();

                foreach (PlayerControl zombiePlayer
                    in PlayerControl.AllPlayerControls)
                {
                    if (!IsActiveZombie(zombiePlayer))
                        continue;

                    Vector2 difference =
                        zombiePlayer.GetTruePosition() - targetPosition;

                    if (difference.sqrMagnitude <= radiusSquared)
                    {
                        nearZombie = true;
                        break;
                    }
                }

                if (!nearZombie)
                {
                    ExposureByTarget.Remove(targetId);
                    ContactLatchedTargets.Remove(targetId);
                    continue;
                }

                if (ContactLatchedTargets.Contains(targetId))
                    continue;

                float exposure = 0f;
                ExposureByTarget.TryGetValue(targetId, out exposure);
                exposure += deltaTime;
                ExposureByTarget[targetId] = exposure;

                if (exposure < requiredContactSeconds)
                    continue;

                ExposureByTarget.Remove(targetId);
                ContactLatchedTargets.Add(targetId);

                int touches = 0;
                TouchCountByTarget.TryGetValue(targetId, out touches);
                touches++;
                TouchCountByTarget[targetId] = touches;

                BMLogger.Info(
                    $"Contatto Zombie: PlayerId={targetId}, nome={target.Data.PlayerName}, tocchi={touches}/{requiredTouches}.",
                    "ZombieMode");

                if (touches >= requiredTouches)
                    newlyInfected.Add(target);
            }

            foreach (PlayerControl player in newlyInfected)
                BeginSecondaryInfection(player);
        }

        private static void ClearContactProgress(
            byte playerId,
            bool clearCompletedTouches)
        {
            ExposureByTarget.Remove(playerId);
            ContactLatchedTargets.Remove(playerId);

            if (clearCompletedTouches)
                TouchCountByTarget.Remove(playerId);
        }

        private static void BeginSecondaryInfection(PlayerControl player)
        {
            if (IsLobby ||
                !IsConnectedPlayer(player) ||
                Infected.Contains(player.PlayerId) ||
                PendingInfections.ContainsKey(player.PlayerId))
                return;

            if (!Infected.Add(player.PlayerId))
                return;

            ClearContactProgress(player.PlayerId, true);
            Utils.ClearTasks(player);
            player.RpcSetColor(BlueColorId);

            PendingInfections[player.PlayerId] = new PendingInfection
            {
                Player = player,
                Elapsed = 0f,
                BlinkAccumulator = 0f,
                ShowsGreen = false
            };

            BMLogger.Info(
                $"Trasformazione avviata PlayerId={player.PlayerId}, nome={player.Data.PlayerName}.",
                "ZombieMode");
        }

        private static void TickPendingInfections(float deltaTime)
        {
            if (IsLobby || PendingInfections.Count == 0)
                return;

            var completed = new List<PlayerControl>();
            var cancelled = new List<byte>();
            float interval = 0.20f;

            foreach (KeyValuePair<byte, PendingInfection> entry
                in PendingInfections)
            {
                PendingInfection pending = entry.Value;
                PlayerControl player = pending.Player;

                if (!IsConnectedPlayer(player) || player.Data.IsDead)
                {
                    if (IsConnectedPlayer(player))
                        player.RpcSetColor(BlueColorId);

                    cancelled.Add(entry.Key);
                    continue;
                }

                pending.Elapsed += deltaTime;
                pending.BlinkAccumulator += deltaTime;

                while (pending.BlinkAccumulator >= interval)
                {
                    pending.BlinkAccumulator -= interval;
                    pending.ShowsGreen = !pending.ShowsGreen;
                    player.RpcSetColor(
                        pending.ShowsGreen ? GreenColorId : BlueColorId);
                }

                if (pending.Elapsed >= SecondaryInfectionBlinkDuration)
                    completed.Add(player);
            }

            foreach (byte playerId in cancelled)
            {
                PendingInfections.Remove(playerId);
                Infected.Remove(playerId);
                ClearContactProgress(playerId, true);
            }

            foreach (PlayerControl player in completed)
            {
                PendingInfections.Remove(player.PlayerId);
                ActivateZombie(player);
            }
        }

        private static void Infect(PlayerControl player)
        {
            if (IsLobby ||
                !IsConnectedPlayer(player) ||
                !Infected.Add(player.PlayerId))
                return;

            ClearContactProgress(player.PlayerId, true);
            Utils.ClearTasks(player);
            ActivateZombie(player);
        }

        private static void ActivateZombie(PlayerControl player)
        {
            if (IsLobby ||
                !IsConnectedPlayer(player) ||
                !Infected.Contains(player.PlayerId) ||
                PendingInfections.ContainsKey(player.PlayerId))
                return;

            ClearContactProgress(player.PlayerId, true);
            Utils.ClearTasks(player);
            player.RpcSetColor(GreenColorId);

            if (player.AmOwner)
            {
                ApplyLocalZombieOptions();
            }
            else
            {
                int clientId =
                    AmongUsClient.Instance.GetClientIdFromCharacter(player);
                TargetedOptionsSender.SendZombieOptions(clientId);
            }

            BMLogger.Info(
                $"Infettato PlayerId={player.PlayerId}, nome={player.Data.PlayerName}.",
                "ZombieMode");
        }

        private static void ReapplyZombieOptions()
        {
            if (IsLobby)
                return;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsActiveZombie(player))
                    continue;

                player.RpcSetColor(GreenColorId);
                Utils.ClearTasks(player);
                if (player.AmOwner)
                {
                    ApplyLocalZombieOptions();
                }
                else
                {
                    int clientId =
                        AmongUsClient.Instance.GetClientIdFromCharacter(player);
                    TargetedOptionsSender.SendZombieOptions(clientId);
                }
            }
        }

        private static bool IsConnectedPlayer(PlayerControl player)
        {
            return player != null &&
                   player.Data != null &&
                   !player.Data.Disconnected;
        }

        private static bool IsEligibleTarget(PlayerControl player)
        {
            if (!IsConnectedPlayer(player) ||
                player.Data.IsDead ||
                player.inVent ||
                Infected.Contains(player.PlayerId) ||
                PendingInfections.ContainsKey(player.PlayerId))
                return false;


            return true;
        }

        private static bool IsActiveZombie(PlayerControl player)
        {
            return IsConnectedPlayer(player) &&
                   !player.Data.IsDead &&
                   !player.inVent &&
                   Infected.Contains(player.PlayerId) &&
                   !PendingInfections.ContainsKey(player.PlayerId);
        }

        public static bool AreAllNonZombieTasksComplete(
            out int completedTasks,
            out int totalTasks)
        {
            completedTasks = 0;
            totalTasks = 0;

            if (IsLobby)
                return false;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsConnectedPlayer(player) ||
                    Infected.Contains(player.PlayerId) ||
                    player.Data.Tasks == null)
                    continue;

                foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
                {
                    totalTasks++;
                    if (task.Complete)
                        completedTasks++;
                }
            }

            return totalTasks > 0 && completedTasks >= totalTasks;
        }

        public static bool IsZombie(byte playerId)
        {
            return Infected.Contains(playerId);
        }

        public static bool IsFullyTransformedZombie(byte playerId)
        {
            return Infected.Contains(playerId) &&
                   !PendingInfections.ContainsKey(playerId);
        }

        private static float GetZombieSpeedPercentage()
        {
            return Mathf.Clamp(
                Options.ZombieSpeedMultiplier.GetFloat(),
                25f,
                100f) / 100f;
        }

        private static float GetZombieVisionPercentage()
        {
            return Mathf.Clamp(
                Options.ZombieVisionMultiplier.GetFloat(),
                25f,
                100f) / 100f;
        }

        internal static float GetZombieSpeed()
        {
            return _baseSpeed * GetZombieSpeedPercentage();
        }

        internal static float GetZombieCrewVision()
        {
            return _baseCrewVision * GetZombieVisionPercentage();
        }

        internal static float GetZombieImpostorVision()
        {
            return _baseImpostorVision * GetZombieVisionPercentage();
        }

        internal static void CheckWinConditions()
        {
            if (IsLobby ||
                !IsActive ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null)
                return;

            {
                int completedTasks;
                int totalTasks;
                if (AreAllNonZombieTasksComplete(
                    out completedTasks,
                    out totalTasks))
                {
                    FinishGame(false);
                    return;
                }
            }


            int zombies = 0;
            int nonZombies = 0;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsConnectedPlayer(player) || player.Data.IsDead)
                    continue;

                if (IsActiveZombie(player))
                    zombies++;
                else
                    nonZombies++;
            }

            if (zombies > 0 && zombies > nonZombies)
                FinishGame(true);
        }

        private static void FinishGame(bool zombiesWin)
        {
            if (IsLobby ||
                _ending ||
                GameManager.Instance == null)
                return;

            _ending = true;

            MatchSummary1.ZombieWin = zombiesWin;
            MatchSummary1.ZombieCrewmateWin = !zombiesWin;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsConnectedPlayer(player))
                    continue;

                player.RpcSetRole(
                    Infected.Contains(player.PlayerId)
                        ? RoleTypes.Impostor
                        : RoleTypes.Crewmate,
                    true);
            }

            ForceSingleImpostorBeforeEnd();

            GameOverReason reason = zombiesWin
                ? GameOverReason.ImpostorsByKill
                : GameOverReason.CrewmatesByTask;

            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
        }

        private static void ForceSingleImpostorBeforeEnd()
        {
            try
            {
                IGameOptions logicOptions =
                    GameManager.Instance?.LogicOptions?.currentGameOptions;

                if (logicOptions != null)
                {
                    logicOptions.SetInt(
                        Int32OptionNames.NumImpostors,
                        1);
                }

                IGameOptions hostOptions =
                    GameOptionsManager.Instance?.CurrentGameOptions;

                if (hostOptions != null)
                {
                    hostOptions.SetInt(
                        Int32OptionNames.NumImpostors,
                        1);
                }

                BMLogger.Info(
                    "Numero impostori Zombie ripristinato e sincronizzato a 1 prima della fine partita.",
                    "ZombieMode");
            }
            catch (Exception exception)
            {
                BMLogger.Info(
                    $"Ripristino impostori Zombie fallito: {exception}",
                    "ZombieMode");
            }
        }

        private static void CaptureBaseOptions()
        {
            if (IsLobby)
                return;

            IGameOptions options =
                GameManager.Instance?.LogicOptions?.currentGameOptions;

            if (options == null)
                return;

            _baseSpeed = options.GetFloat(FloatOptionNames.PlayerSpeedMod);
            _baseCrewVision = options.GetFloat(FloatOptionNames.CrewLightMod);
            _baseImpostorVision = options.GetFloat(
                FloatOptionNames.ImpostorLightMod);
            _baseOptionsCaptured = true;
        }

        private static void ApplyLocalZombieOptions()
        {
            if (IsLobby)
                return;

            IGameOptions options =
                GameManager.Instance?.LogicOptions?.currentGameOptions;

            if (options == null)
                return;

            options.SetFloat(
                FloatOptionNames.PlayerSpeedMod,
                GetZombieSpeed());
            options.SetFloat(
                FloatOptionNames.CrewLightMod,
                GetZombieCrewVision());
            options.SetFloat(
                FloatOptionNames.ImpostorLightMod,
                GetZombieImpostorVision());
        }

        private static void RestoreLocalBaseOptions()
        {
            if (IsLobby)
                return;

            IGameOptions options =
                GameManager.Instance?.LogicOptions?.currentGameOptions;

            if (options == null)
                return;

            options.SetFloat(FloatOptionNames.PlayerSpeedMod, _baseSpeed);
            options.SetFloat(FloatOptionNames.CrewLightMod, _baseCrewVision);
            options.SetFloat(
                FloatOptionNames.ImpostorLightMod,
                _baseImpostorVision);
        }
    }

    internal static class TargetedOptionsSender
    {
        private static MethodInfo _sendOrDisconnectMethod;

        internal static void SendZombieOptions(int targetClientId)
        {
            if (!ZombieModeController.IsActive ||
                ZombieModeController.IsLobby ||
                ShipStatus.Instance == null ||
                targetClientId < 0 ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null)
                return;

            MethodInfo sendOrDisconnectMethod = _sendOrDisconnectMethod;

            if (sendOrDisconnectMethod == null)
            {
                sendOrDisconnectMethod = AccessTools.Method(
                    typeof(InnerNetClient),
                    "SendOrDisconnect",
                    new[] { typeof(MessageWriter) });

                _sendOrDisconnectMethod = sendOrDisconnectMethod;
            }

            if (sendOrDisconnectMethod == null)
            {
                BMLogger.Info(
                    "InnerNetClient.SendOrDisconnect non trovato.",
                    "ZombieMode");
                return;
            }

            LogicOptions logic = GameManager.Instance.LogicOptions;
            IGameOptions options = logic?.currentGameOptions;
            if (logic == null || options == null)
                return;

            byte componentIndex = GameManager.Instance.IsNormal()
                ? (byte)4
                : (byte)5;

            float currentSpeed =
                options.GetFloat(FloatOptionNames.PlayerSpeedMod);
            float currentCrewVision =
                options.GetFloat(FloatOptionNames.CrewLightMod);
            float currentImpostorVision =
                options.GetFloat(FloatOptionNames.ImpostorLightMod);

            MessageWriter writer = null;

            try
            {
                options.SetFloat(
                    FloatOptionNames.PlayerSpeedMod,
                    ZombieModeController.GetZombieSpeed());
                options.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    ZombieModeController.GetZombieCrewVision());
                options.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    ZombieModeController.GetZombieImpostorVision());

                writer = MessageWriter.Get(SendOption.Reliable);

                writer.StartMessage(6);
                writer.Write(AmongUsClient.Instance.GameId);
                writer.WritePacked(targetClientId);

                writer.StartMessage(1);
                writer.WritePacked(GameManager.Instance.NetId);

                writer.StartMessage(componentIndex);
                logic.Serialize(writer);
                writer.EndMessage();

                writer.EndMessage();
                writer.EndMessage();

                sendOrDisconnectMethod.Invoke(
                    AmongUsClient.Instance,
                    new object[] { writer });
            }
            catch (Exception exception)
            {
                BMLogger.Info(
                    $"Invio opzioni mirate fallito: {exception}",
                    "ZombieMode");
            }
            finally
            {
                options.SetFloat(
                    FloatOptionNames.PlayerSpeedMod,
                    currentSpeed);
                options.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    currentCrewVision);
                options.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    currentImpostorVision);

                writer?.Recycle();
            }
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), "OnDestroy")]
    internal static class IntroCutsceneOnDestroyPatch
    {
        private static void Postfix()
        {
            if (ZombieModeController.IsLobby ||
                ShipStatus.Instance == null)
                return;

            ZombieModeController.Begin();
        }
    }

    [HarmonyPatch(typeof(HudManager), "Update")]
    internal static class HudManagerUpdatePatch
    {
        private static void Postfix()
        {
            if (ZombieModeController.IsLobby)
                return;

            ZombieModeController.Tick(Time.deltaTime);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), "get_SpeedMod")]
    internal static class ZombieHostSpeedPatch
    {
        private static void Postfix(
            PlayerPhysics __instance,
            ref float __result)
        {
            if (ZombieModeController.IsLobby)
                return;

            PlayerControl player =
                __instance.GetComponent<PlayerControl>();

            if (!ZombieModeController.IsActive ||
                player == null ||
                !player.AmOwner ||
                !ZombieModeController.IsFullyTransformedZombie(player.PlayerId))
                return;

            __result = ZombieModeController.GetZombieSpeed();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), "OnDestroy")]
    internal static class MeetingHudOnDestroyPatch
    {
        private static void Postfix()
        {
            if (ZombieModeController.IsLobby)
                return;

            ZombieModeController.MarkMeetingFinished();
        }
    }

    [HarmonyPatch(typeof(LogicGameFlowNormal), "CheckEndCriteria")]
    internal static class ZombieModeNormalEndCriteriaPatch
    {
        private static bool Prefix()
        {
            if (ZombieModeController.IsLobby ||
                !ZombieModeController.IsRunning)
                return true;

            ZombieModeController.CheckWinConditions();
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), "CheckEndGameViaTasks")]
    internal static class ZombieModeVanillaTaskWinPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (ZombieModeController.IsLobby ||
                !ZombieModeController.IsRunning)
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), "EndGame")]
    internal static class GameManagerEndGamePatch
    {
        private static void Prefix()
        {
            if (ZombieModeController.IsLobby)
                return;

            ZombieModeController.Stop();
        }
    }
}
