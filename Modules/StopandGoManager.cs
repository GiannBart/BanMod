//credits and licenses in the resources folder
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public class StopandGoManager : MonoBehaviour
    {
        private bool wasInVent = false;
        private Dictionary<byte, Vector2> lastPositions = new Dictionary<byte, Vector2>();
        private const float MOVEMENT_THRESHOLD = 0.05f;
        public static bool gameEnded = false;

        private float sabotageActivationTime = 0f;
        private const float REACTION_DELAY = 1.1f; 

        void Update()
        {
            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();

            if (gameMode != GameModeType.StopOrDeath || gameEnded) return;
            if (GameStates.isLobby) return;
            if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost || ShipStatus.Instance == null) return;

            PlayerControl impostor = PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(p => p.Data?.Role != null && p.Data.Role.IsImpostor && !p.Data.IsDead);

            if (impostor == null) return;

            bool currentlyInVent = impostor.inVent;
            if (wasInVent && !currentlyInVent)
            {
                ToggleSabotage();
            }
            wasInVent = currentlyInVent;

            if (IsReactorActuallyActive())
            {
                if (Time.time - sabotageActivationTime >= REACTION_DELAY)
                {
                    CheckForMovingPlayers();
                }
            }

            CheckTaskVictory();
            UpdateLastPositions();
        }

        private bool IsReactorActuallyActive()
        {
            if (ShipStatus.Instance == null) return false;
            var reactor = ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>();
            return reactor != null && reactor.IsActive;
        }

        private void ToggleSabotage()
        {
            bool active = IsReactorActuallyActive();

            if (!active)
            {
                var sabotage = ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
                if (sabotage != null) sabotage.MarkClean();

                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 128);

                sabotageActivationTime = Time.time;

                BMLogger.Info("[STELLA] SABOTAGGIO ATTIVATO - INIZIO TOLERANZA");
            }
            else
            {
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                BMLogger.Info("[STELLA] SABOTAGGIO SPENTO");
            }
        }

        private void CheckForMovingPlayers()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data.IsDead || player.Data.Role.IsImpostor) continue;

                if (lastPositions.TryGetValue(player.PlayerId, out Vector2 lastPos))
                {
                    float distanceMoved = Vector2.Distance(player.transform.position, lastPos);

                    if (distanceMoved > MOVEMENT_THRESHOLD && IsReactorActuallyActive())
                    {
                        player.RpcSetRole(RoleTypes.CrewmateGhost);
                    }
                }
            }
        }

        private void UpdateLastPositions()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                lastPositions[player.PlayerId] = player.transform.position;
            }
        }

        private void CheckTaskVictory()
        {
            var aliveCrew = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p != null && !p.Data.IsDead && !p.Data.Role.IsImpostor).ToList();

            if (aliveCrew.Count == 0) return;

            bool everyCrewFinished = true;

            foreach (var player in aliveCrew)
            {
                if (player.Data.Tasks == null || player.Data.Tasks.Count == 0) continue;

                bool playerDone = player.Data.Tasks.ToArray().All(t => t.Complete);

                if (!playerDone)
                {
                    everyCrewFinished = false;
                    break;
                }
            }

            if (everyCrewFinished)
            {
                gameEnded = true;
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
            }
        }

        public void ResetState()
        {
            if (ShipStatus.Instance != null) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
            wasInVent = false;
            gameEnded = false;
            lastPositions.Clear();
            sabotageActivationTime = 0f;
        }
    }
}