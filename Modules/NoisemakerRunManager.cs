//credits and licenses in the resources folder
using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod
{
    public class NoisemakerRunManager : MonoBehaviour
    {
        public static bool PendingRaceTrigger = false;
        public static Vector2 PendingKillPosition;
        public static NoisemakerRunManager Instance { get; private set; }

        private Vector2 lastKillPosition;
        private List<byte> playersReachedPos = new List<byte>();
        private bool isRaceActive = false;
        private float raceTimer = 0f;
        private const float RACE_DURATION = 10f;
        private const float REACH_DIST = 1.3f;
        public static bool gameEnded = false;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost || ShipStatus.Instance == null) return;
            if ((GameModeType)Options.GameMode.GetValue() != GameModeType.RunOrDeath) return;
            if (GameStates.isLobby)
            {
                if (gameEnded) gameEnded = false;
                return;
            }

            if (AmongUsClient.Instance.AmHost && GameStates.IsInGameplay && !gameEnded)
            {
                CheckTaskVictory();
            }

            if (PendingRaceTrigger)
            {
                PendingRaceTrigger = false;
                StartRace(PendingKillPosition);
            }

            if (!isRaceActive || !AmongUsClient.Instance.AmHost || !GameStates.IsInGameplay) return;

            raceTimer -= Time.deltaTime;
            CheckPlayersProximity();

            if (raceTimer <= 0)
            {
                EndRace();
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
                BMLogger.Info("[BanMod] Tutti i Crewmate vivi hanno terminato le task! Vittoria triggerata.");
            }
        }

        public void StartRace(Vector2 killPos)
        {
            lastKillPosition = killPos;
            playersReachedPos.Clear();
            raceTimer = RACE_DURATION;
            isRaceActive = true;
            BMLogger.Info($"[BanMod] Corsa avviata via Trigger! Destinazione: {killPos}");
        }

        private void CheckPlayersProximity()
        {
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data.IsDead || p.Data.Role.IsImpostor || playersReachedPos.Contains(p.PlayerId)) continue;

                if (Vector2.Distance(p.transform.position, lastKillPosition) < REACH_DIST)
                {
                    playersReachedPos.Add(p.PlayerId);
                    BMLogger.Info($"[BanMod] {p.Data.PlayerName} ha raggiunto il punto ed è salvo.");
                }
            }
        }

        private void EndRace()
        {
            isRaceActive = false;

            var aliveCrew = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p != null && !p.Data.IsDead && !p.Data.Role.IsImpostor).ToList();

            if (aliveCrew.Count == 0) return;

            PlayerControl targetToKill = null;
            var losers = aliveCrew.Where(p => !playersReachedPos.Contains(p.PlayerId)).ToList();

            if (losers.Count > 0)
            {
                targetToKill = losers.OrderByDescending(p => Vector2.Distance(p.transform.position, lastKillPosition)).FirstOrDefault();
            }
            else
            {
                byte lastId = playersReachedPos.LastOrDefault();
                targetToKill = aliveCrew.FirstOrDefault(p => p.PlayerId == lastId);
            }

            if (targetToKill != null)
            {
                targetToKill.RpcSetRole(RoleTypes.CrewmateGhost);
                BMLogger.Info($"[BanMod] Fine corsa! {targetToKill.Data.PlayerName} eliminato.");
            }
        }

        public void ResetState()
        {
            isRaceActive = false;
            raceTimer = 0f;
            gameEnded = false;
            playersReachedPos.Clear();
            PendingRaceTrigger = false;
        }
    }
}