//https://github.com/GiannBart/BanMod
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace BanMod
{
    public static class HotPotatoModeController
    {
        private const byte BlackColorId = 6;
        private const byte WhiteColorId = 7;
        private const byte RedColorId = 0;
        private const byte YellowColorId = 5;
        private const byte NoPlayerId = byte.MaxValue;

        private const float DefaultExplosionSeconds = 20f;
        private const float DefaultFirstPotatoDelaySeconds = 3f;
        private const float TouchRadius = 0.50f;
        private const float NextRoundDelay = 3f;
        private const float HolderVisionFactor = 0.75f;

        private static readonly HashSet<byte> Eliminated =
            new HashSet<byte>();
        private static readonly Dictionary<byte, byte> OriginalColors =
            new Dictionary<byte, byte>();

        private static bool _active;
        private static bool _ending;
        private static PlayerControl _holder;
        private static byte _blockedReturnPlayerId = NoPlayerId;
        private static float _roundElapsed;
        private static float _roundDuration = DefaultExplosionSeconds;
        private static float _nextRoundDelay;
        private static float _blinkAccumulator;
        private static bool _blinkShowsWhite;
        private static bool _baseVisionCaptured;
        private static float _baseCrewVision;
        private static float _baseImpostorVision;
        private static float _visionResyncAccumulator;

        internal static bool IsLobby => GameStates.isLobby;

        public static bool IsRunning =>
            !IsLobby && _active && !_ending;

        public static bool IsHolder(byte playerId)
        {
            return IsRunning &&
                   _holder != null &&
                   _holder.PlayerId == playerId;
        }

        private static bool IsModeSelected()
        {
            try
            {
                return Options.GameMode != null &&
                       Options.GameMode.Selected == GameModeType.HotPotato;
            }
            catch
            {
                return false;
            }
        }

        internal static void Begin()
        {
            if (IsLobby)
                return;

            Stop();

            if (!IsModeSelected() ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null ||
                ShipStatus.Instance == null)
                return;

            CaptureBaseVision();

            _active = true;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsConnectedPlayer(player))
                    continue;

                OriginalColors[player.PlayerId] =
                    (byte)player.Data.DefaultOutfit.ColorId;

                player.RpcSetColor(WhiteColorId);

                Utils.ClearTasks(player);
            }

            _nextRoundDelay = GetFirstPotatoDelaySeconds();

            if (_nextRoundDelay <= 0f)
                StartNewRound();

            BMLogger.Info(
                $"Hot Potato avviata. Prima patata tra {_nextRoundDelay:0.0}s.",
                "HotPotato");
        }

        internal static void Stop()
        {
            RestoreHolderVision(_holder);
            RestoreOriginalColor(_holder);

            if (_baseVisionCaptured)
            {
                ApplyLocalVision(
                    _baseCrewVision,
                    _baseImpostorVision);
            }

            _active = false;
            _ending = false;
            _holder = null;
            _blockedReturnPlayerId = NoPlayerId;
            _roundElapsed = 0f;
            _roundDuration = DefaultExplosionSeconds;
            _nextRoundDelay = 0f;
            _blinkAccumulator = 0f;
            _blinkShowsWhite = false;
            _baseVisionCaptured = false;
            _baseCrewVision = 0f;
            _baseImpostorVision = 0f;
            _visionResyncAccumulator = 0f;
            Eliminated.Clear();
            OriginalColors.Clear();
        }

        internal static void Tick(float deltaTime)
        {
            if (!IsRunning ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null ||
                ShipStatus.Instance == null)
                return;

            if (MeetingHud.Instance != null)
                return;

            float safeDelta = Mathf.Clamp(deltaTime, 0f, 0.25f);

            if (_nextRoundDelay > 0f)
            {
                _nextRoundDelay -= safeDelta;

                if (_nextRoundDelay <= 0f)
                    StartNewRound();

                return;
            }

            if (!IsAliveParticipant(_holder))
            {
                RestoreHolderVision(_holder);
                SetHotPotatoBaseColor(_holder);
                _holder = null;
                StartNewRound();
                return;
            }

            _visionResyncAccumulator += safeDelta;
            if (_visionResyncAccumulator >= 1f)
            {
                _visionResyncAccumulator = 0f;
                ApplyHolderVision(_holder);
            }

            _roundElapsed += safeDelta;
            TickHolderBlink(safeDelta);

            if (_roundElapsed >= _roundDuration)
            {
                ExplodeHolder();
                return;
            }

            TickPassByContact();
        }

        private static void StartNewRound()
        {
            if (!IsRunning)
                return;

            List<PlayerControl> alivePlayers = GetAliveParticipants();

            if (alivePlayers.Count <= 1)
            {
                FinishGame(
                    alivePlayers.Count == 1
                        ? alivePlayers[0]
                        : null);
                return;
            }

            _holder = alivePlayers[
                UnityEngine.Random.Range(0, alivePlayers.Count)];
            _blockedReturnPlayerId = NoPlayerId;
            _roundElapsed = 0f;
            _roundDuration = GetExplosionSeconds();
            _nextRoundDelay = 0f;
            _blinkAccumulator = 0f;
            _blinkShowsWhite = false;
            _visionResyncAccumulator = 0f;

            _holder.RpcSetColor(BlackColorId);
            ApplyHolderVision(_holder);

            BMLogger.Info(
                $"Nuovo possessore: PlayerId={_holder.PlayerId}, nome={_holder.Data.PlayerName}. Esplosione tra {_roundDuration:0.0}s.",
                "HotPotato");
        }

        private static void TickPassByContact()
        {
            if (!IsAliveParticipant(_holder) || _holder.inVent)
                return;

            float radiusSquared = TouchRadius * TouchRadius;
            Vector2 holderPosition = _holder.GetTruePosition();
            PlayerControl nearestTarget = null;
            float nearestDistanceSquared = float.MaxValue;
            bool blockedPlayerStillTouching = false;

            foreach (PlayerControl target in PlayerControl.AllPlayerControls)
            {
                if (!IsAliveParticipant(target) ||
                    target.PlayerId == _holder.PlayerId ||
                    target.inVent)
                    continue;

                Vector2 difference =
                    target.GetTruePosition() - holderPosition;
                float distanceSquared = difference.sqrMagnitude;

                if (target.PlayerId == _blockedReturnPlayerId)
                {
                    if (distanceSquared <= radiusSquared)
                        blockedPlayerStillTouching = true;

                    continue;
                }

                if (distanceSquared > radiusSquared ||
                    distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestTarget = target;
                nearestDistanceSquared = distanceSquared;
            }

            if (_blockedReturnPlayerId != NoPlayerId &&
                !blockedPlayerStillTouching)
            {
                _blockedReturnPlayerId = NoPlayerId;
            }

            if (nearestTarget != null)
                PassPotato(nearestTarget);
        }

        private static void PassPotato(PlayerControl newHolder)
        {
            if (!IsAliveParticipant(_holder) ||
                !IsAliveParticipant(newHolder) ||
                newHolder.PlayerId == _holder.PlayerId)
                return;

            PlayerControl previousHolder = _holder;
            RestoreHolderVision(previousHolder);
            SetHotPotatoBaseColor(previousHolder);

            _holder = newHolder;
            _blockedReturnPlayerId = previousHolder.PlayerId;
            _blinkAccumulator = 0f;
            _blinkShowsWhite = false;
            _visionResyncAccumulator = 0f;

            _holder.RpcSetColor(BlackColorId);
            ApplyHolderVision(_holder);

            BMLogger.Info(
                $"Patata passata da PlayerId={previousHolder.PlayerId} a PlayerId={_holder.PlayerId}. Tempo rimanente={Mathf.Max(0f, _roundDuration - _roundElapsed):0.0}s.",
                "HotPotato");
        }

        private static void TickHolderBlink(float deltaTime)
        {
            if (!IsAliveParticipant(_holder))
                return;

            float remaining = Mathf.Max(
                0f,
                _roundDuration - _roundElapsed);

            // Più di 10 secondi: possessore nero fisso.
            if (remaining > 10f)
            {
                _blinkAccumulator = 0f;
                _blinkShowsWhite = false;

                _holder.RpcSetColor(BlackColorId);
                return;
            }

            float blinkInterval;

            // Da 10 a 5 secondi: cambio ogni 1 secondo.
            if (remaining > 5f)
            {
                blinkInterval = 1f;
            }
            // Da 5 a 3 secondi: cambio ogni 0.5 secondi.
            else if (remaining > 3f)
            {
                blinkInterval = 0.5f;
            }
            // Da 3 secondi allo scoppio: cambio rapidissimo.
            else
            {
                blinkInterval = 0.12f;
            }

            _blinkAccumulator += deltaTime;

            while (_blinkAccumulator >= blinkInterval)
            {
                _blinkAccumulator -= blinkInterval;
                _blinkShowsWhite = !_blinkShowsWhite;

                _holder.RpcSetColor(
                    _blinkShowsWhite
                        ? YellowColorId
                        : RedColorId);
            }
        }

        private static void ExplodeHolder()
        {
            PlayerControl explodedPlayer = _holder;
            RestoreHolderVision(explodedPlayer);
            _holder = null;
            _blockedReturnPlayerId = NoPlayerId;
            _roundElapsed = 0f;
            _blinkAccumulator = 0f;
            _blinkShowsWhite = false;
            _visionResyncAccumulator = 0f;

            if (!IsAliveParticipant(explodedPlayer))
            {
                StartNewRound();
                return;
            }

            Eliminated.Add(explodedPlayer.PlayerId);
            SetHotPotatoBaseColor(explodedPlayer);
            explodedPlayer.RpcSetRole(
                RoleTypes.CrewmateGhost,
                true);

            BMLogger.Info(
                $"Patata esplosa: PlayerId={explodedPlayer.PlayerId}, nome={explodedPlayer.Data.PlayerName}.",
                "HotPotato");

            List<PlayerControl> survivors = GetAliveParticipants();

            if (survivors.Count <= 1)
            {
                FinishGame(
                    survivors.Count == 1
                        ? survivors[0]
                        : null);
                return;
            }

            _nextRoundDelay = NextRoundDelay;
        }

        private static void FinishGame(PlayerControl winner)
        {
            if (!IsRunning || GameManager.Instance == null)
                return;

            RestoreHolderVision(_holder);
            SetHotPotatoBaseColor(_holder);

            MatchSummary1.HotPotatoWin = winner != null;
            MatchSummary1.HotPotatoWinnerName =
                winner?.Data?.PlayerName ?? "";

            _ending = true;

            // Ripristina gli impostori a 1 mentre siamo ancora in game,
            // prima dell'RpcEndGame e quindi prima del rientro in lobby.
            ForceSingleImpostorBeforeEnd();

            BMLogger.Info(
                winner != null
                    ? $"Vittoria Hot Potato: PlayerId={winner.PlayerId}, nome={winner.Data.PlayerName}."
                    : "Hot Potato terminata senza vincitore.",
                "HotPotato");

            GameManager.Instance.RpcEndGame(
                GameOverReason.CrewmatesByTask,
                false);
        }

        private static float GetExplosionSeconds()
        {
            float value = DefaultExplosionSeconds;

            try
            {
                if (Options.HotPotatoExplosionSeconds != null)
                {
                    value =
                        Options.HotPotatoExplosionSeconds.GetInt();
                }
            }
            catch (Exception exception)
            {
                BMLogger.Info(
                    $"Lettura opzione HotPotatoExplosionSeconds fallita: {exception}",
                    "HotPotato");
            }

            return Mathf.Clamp(value, 1f, 300f);
        }

        private static float GetFirstPotatoDelaySeconds()
        {
            float value = DefaultFirstPotatoDelaySeconds;

            try
            {
                if (Options.HotPotatoFirstDelaySeconds != null)
                {
                    value =
                        Options.HotPotatoFirstDelaySeconds.GetInt();
                }
            }
            catch (Exception exception)
            {
                BMLogger.Info(
                    $"Lettura opzione HotPotatoFirstDelaySeconds fallita: {exception}",
                    "HotPotato");
            }

            return Mathf.Clamp(value, 0f, 120f);
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

                // Aggiorna anche le opzioni host/globali: questo evita
                // di rientrare in lobby con un numero impostori alterato.
                IGameOptions hostOptions =
                    GameOptionsManager.Instance?.CurrentGameOptions;

                if (hostOptions != null)
                {
                    hostOptions.SetInt(
                        Int32OptionNames.NumImpostors,
                        1);
                }

                BMLogger.Info(
                    "Numero impostori ripristinato e sincronizzato a 1 prima della fine partita.",
                    "HotPotato");
            }
            catch (Exception exception)
            {
                BMLogger.Info(
                    $"Ripristino impostori fallito: {exception}",
                    "HotPotato");
            }
        }

        private static void CaptureBaseVision()
        {
            IGameOptions options =
                GameManager.Instance?.LogicOptions?.currentGameOptions;

            if (options == null)
                return;

            _baseCrewVision = options.GetFloat(
                FloatOptionNames.CrewLightMod);
            _baseImpostorVision = options.GetFloat(
                FloatOptionNames.ImpostorLightMod);
            _baseVisionCaptured = true;
        }

        private static float GetReducedVision(float originalVision)
        {
            return Mathf.Clamp(
                originalVision - 0.25f,
                0.05f,
                5f);
        }

        private static void ApplyHolderVision(PlayerControl player)
        {
            if (!IsRunning || !IsConnectedPlayer(player))
                return;

            float crewVision = GetReducedVision(
                _baseVisionCaptured ? _baseCrewVision : 1f);
            float impostorVision = GetReducedVision(
                _baseVisionCaptured ? _baseImpostorVision : 1f);

            if (player.AmOwner)
            {
                ApplyLocalVision(
                    crewVision,
                    impostorVision);
                return;
            }

            int clientId =
                AmongUsClient.Instance.GetClientIdFromCharacter(player);

            HotPotatoTargetedVisionSender.SendVision(
                clientId,
                crewVision,
                impostorVision);
        }

        private static void RestoreHolderVision(PlayerControl player)
        {
            if (!_baseVisionCaptured || !IsConnectedPlayer(player))
                return;

            if (player.AmOwner)
            {
                ApplyLocalVision(
                    _baseCrewVision,
                    _baseImpostorVision);
                return;
            }

            if (!IsRunning)
                return;

            int clientId =
                AmongUsClient.Instance.GetClientIdFromCharacter(player);

            HotPotatoTargetedVisionSender.SendVision(
                clientId,
                _baseCrewVision,
                _baseImpostorVision);
        }

        private static void ApplyLocalVision(
            float crewVision,
            float impostorVision)
        {
            IGameOptions options =
                GameManager.Instance?.LogicOptions?.currentGameOptions;

            if (options == null)
                return;

            options.SetFloat(
                FloatOptionNames.CrewLightMod,
                Mathf.Clamp(crewVision, 0.05f, 5f));

            options.SetFloat(
                FloatOptionNames.ImpostorLightMod,
                Mathf.Clamp(impostorVision, 0.05f, 5f));
        }

        private static void SetHotPotatoBaseColor(PlayerControl player)
        {
            if (!IsConnectedPlayer(player))
                return;

            player.RpcSetColor(WhiteColorId);
        }

        private static void RestoreOriginalColor(PlayerControl player)
        {
            if (!IsConnectedPlayer(player))
                return;

            byte originalColor;

            if (!OriginalColors.TryGetValue(
                    player.PlayerId,
                    out originalColor))
                return;

            player.RpcSetColor(originalColor);
        }

        private static List<PlayerControl> GetAliveParticipants()
        {
            var result = new List<PlayerControl>();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (IsAliveParticipant(player))
                    result.Add(player);
            }

            return result;
        }

        private static bool IsConnectedPlayer(PlayerControl player)
        {
            return player != null &&
                   player.Data != null &&
                   !player.Data.Disconnected;
        }

        private static bool IsAliveParticipant(PlayerControl player)
        {
            return IsConnectedPlayer(player) &&
                   !player.Data.IsDead &&
                   !Eliminated.Contains(player.PlayerId);
        }
    }

    internal static class HotPotatoTargetedVisionSender
    {
        private static MethodInfo _sendOrDisconnectMethod;

        internal static void SendVision(
            int targetClientId,
            float crewVision,
            float impostorVision)
        {
            if (!HotPotatoModeController.IsRunning ||
                HotPotatoModeController.IsLobby ||
                ShipStatus.Instance == null ||
                targetClientId < 0 ||
                AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                GameManager.Instance == null)
                return;

            MethodInfo sendOrDisconnectMethod =
                _sendOrDisconnectMethod;

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
                    "HotPotato");
                return;
            }

            LogicOptions logic = GameManager.Instance.LogicOptions;
            IGameOptions options = logic?.currentGameOptions;

            if (logic == null || options == null)
                return;

            byte componentIndex = GameManager.Instance.IsNormal()
                ? (byte)4
                : (byte)5;

            float currentCrewVision = options.GetFloat(
                FloatOptionNames.CrewLightMod);
            float currentImpostorVision = options.GetFloat(
                FloatOptionNames.ImpostorLightMod);
            MessageWriter writer = null;

            try
            {
                options.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    Mathf.Clamp(crewVision, 0.05f, 5f));

                options.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    Mathf.Clamp(impostorVision, 0.05f, 5f));

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
                    $"Invio visuale Hot Potato fallito: {exception}",
                    "HotPotato");
            }
            finally
            {
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
    internal static class HotPotatoIntroCutsceneOnDestroyPatch
    {
        private static void Postfix()
        {
            if (HotPotatoModeController.IsLobby ||
                ShipStatus.Instance == null)
                return;

            HotPotatoModeController.Begin();
        }
    }

    [HarmonyPatch(typeof(HudManager), "Update")]
    internal static class HotPotatoHudManagerUpdatePatch
    {
        private static void Postfix()
        {
            if (HotPotatoModeController.IsLobby)
                return;

            HotPotatoModeController.Tick(Time.deltaTime);
        }
    }


    [HarmonyPatch(typeof(GameManager), "EndGame")]
    internal static class HotPotatoGameManagerEndGamePatch
    {
        private static void Prefix()
        {
            if (HotPotatoModeController.IsLobby)
                return;

            HotPotatoModeController.Stop();
        }
    }
}
