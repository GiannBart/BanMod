//credits and licenses in the resources folder
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod
{
    public class RunManager : MonoBehaviour
    {
        private float stopTimer = 0f;
        private bool canKillThisStop = true;
        private bool hasStartedRunning = false;

        private List<byte> arrivalOrder = new List<byte>();

        private const float STOP_THRESHOLD = 2.0f;
        private const float DANGER_DURATION = 5.0f;
        private const float VELOCITY_EPSILON = 0.1f;
        private const float SAFE_RADIUS = 1.0f;

        void Update()
        {
            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
            if (GameStates.isLobby || gameMode != GameModeType.FollowOrDeath) return;
            if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost || GameData.Instance == null) return;

            PlayerControl impostor = PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(p => p.Data?.Role != null && p.Data.Role.IsImpostor && !p.Data.IsDead);

            if (impostor == null) return;

            bool isMoving = impostor.MyPhysics.Velocity.magnitude > VELOCITY_EPSILON;

            if (!hasStartedRunning)
            {
                if (isMoving) hasStartedRunning = true;
                return;
            }

            if (!isMoving)
            {
                stopTimer += Time.deltaTime;

                TrackArrivals(impostor);

                if (stopTimer >= (STOP_THRESHOLD + DANGER_DURATION) && canKillThisStop)
                {
                    ExecuteAdvancedDeadlyLogic(impostor);
                    canKillThisStop = false;
                }
            }
            else
            {
                stopTimer = 0f;
                canKillThisStop = true;
                arrivalOrder.Clear(); 
            }
        }

        private void TrackArrivals(PlayerControl impostor)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data.IsDead || player.Data.Role.IsImpostor) continue;

                float dist = Vector2.Distance(player.transform.position, impostor.transform.position);

                if (dist <= SAFE_RADIUS && !arrivalOrder.Contains(player.PlayerId))
                {
                    arrivalOrder.Add(player.PlayerId);
                    BMLogger.Info($"[RUN MOD] {player.Data.PlayerName} è arrivato! Posizione in lista: {arrivalOrder.Count}");
                }
                else if (dist > SAFE_RADIUS && arrivalOrder.Contains(player.PlayerId))
                {
                    arrivalOrder.Remove(player.PlayerId);
                }
            }
        }

        private void ExecuteAdvancedDeadlyLogic(PlayerControl impostor)
        {
            var allLivingCrewmates = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Role.IsImpostor)
                .ToList();

            if (allLivingCrewmates.Count == 0) return;

            PlayerControl target = null;

            bool everyoneIsSafe = allLivingCrewmates.All(p => arrivalOrder.Contains(p.PlayerId));

            if (everyoneIsSafe)
            {
                byte lastId = arrivalOrder.Last();
                target = allLivingCrewmates.FirstOrDefault(p => p.PlayerId == lastId);
                BMLogger.Info($"[RUN MOD] Tutti salvi! Ma l'ultimo ad arrivare è stato {target?.Data.PlayerName}. ELIMINATO.");
            }
            else
            {
                target = allLivingCrewmates
                    .Where(p => !arrivalOrder.Contains(p.PlayerId))
                    .OrderByDescending(p => Vector2.Distance(p.transform.position, impostor.transform.position))
                    .FirstOrDefault();

                BMLogger.Info($"[RUN MOD] Qualcuno non è arrivato. Il più lontano fuori raggio era {target?.Data.PlayerName}. ELIMINATO.");
            }

            if (target != null)
            {
                target.RpcSetRole(RoleTypes.CrewmateGhost);
            }

            arrivalOrder.Clear();
        }

        public void ResetState()
        {
            stopTimer = 0f;
            canKillThisStop = true;
            hasStartedRunning = false;
            arrivalOrder.Clear();
        }
    }
}