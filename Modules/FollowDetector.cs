//credits and licenses in the resources folder
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod
{
    public static class ProximityMonitor
    {
        public enum Phase { Detection, Warning, Consequence }

        private class ProximityData
        {
            public byte TargetId;
            public float Timer;
            public Phase CurrentPhase;
            public string OriginalName;
        }

        private static readonly Dictionary<byte, ProximityData> PlayersProximity = new();
        private static readonly Dictionary<byte, (float remaining, Vector2 pos)> FrozenPlayers = new();

        private static float ProximityDistance => Options.ProximityDistance.GetFloat();
        private static int ProximitySeconds => Options.ProximityTimeSeconds.GetInt();
        private static bool MonitorEnabled => Options.EnableProximityMonitor.GetBool();
        private static int Action => Options.ProximityAction.GetValue();

        public static void EnsureTrackedPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            PlayersProximity.Clear();
            FrozenPlayers.Clear();
            foreach (var p in BanMod.AllAlivePlayerControls)
            {
                if (p?.Data == null) continue;
                if (p.inVent) continue;
                TrackPlayerInit(p);
            }
        }

        private static void TrackPlayerInit(PlayerControl pc)
        {
            if (pc == null || pc.Data == null) return;
            if (pc.inVent) return;
            PlayersProximity[pc.PlayerId] = new ProximityData
            {
                TargetId = byte.MaxValue,
                Timer = ProximitySeconds,
                CurrentPhase = Phase.Detection,
                OriginalName = pc.Data.PlayerName
            };
            PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "proximity");
        }

        public static void OnFixedUpdate(PlayerControl pc)
        {
            if (!MonitorEnabled) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (!GameStates.IsInGameplay) return;
            if (pc == null || pc.Data == null) return;
            if (pc.Data.IsDead) return;
            if (pc.inVent) return;

            if (GameStates.IsMeeting)
            {
                if (PlayersProximity.ContainsKey(pc.PlayerId)) ResetPlayerData(pc.PlayerId, pc);
                return;
            }

            if (!PlayersProximity.TryGetValue(pc.PlayerId, out var pdata))
            {
                TrackPlayerInit(pc);
                pdata = PlayersProximity[pc.PlayerId];
            }

            PlayerControl target = null;
            float bestDist = float.MaxValue;
            foreach (var other in BanMod.AllAlivePlayerControls)
            {
                if (other == null || other.Data == null) continue;
                if (other.PlayerId == pc.PlayerId) continue;
                if (other.Data.IsDead) continue;
                if (other.inVent) continue;
                float d = Vector2.Distance(pc.Pos(), other.Pos());
                if (d <= ProximityDistance && d < bestDist)
                {
                    bestDist = d;
                    target = other;
                }
            }

            if (target == null)
            {
                if (pdata.TargetId != byte.MaxValue)
                {
                    pdata.CurrentPhase = Phase.Detection;
                    pdata.Timer = ProximitySeconds;
                    pdata.TargetId = byte.MaxValue;
                    PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "proximity");
                }
                return;
            }

            if (pdata.TargetId != target.PlayerId)
            {
                pdata.TargetId = target.PlayerId;
                pdata.Timer = ProximitySeconds;
                pdata.CurrentPhase = Phase.Detection;
                pdata.OriginalName = pc.Data.PlayerName;
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "proximity");
            }
            else
            {
                pdata.Timer -= Time.fixedDeltaTime;
                if (pdata.CurrentPhase == Phase.Detection && pdata.Timer <= 0f)
                {
                    pdata.CurrentPhase = Phase.Warning;
                    pdata.Timer = ProximitySeconds;
                    PlayerWarningMessenger.SendOnce(pc, "proximity", "DetectorProximityWarning");
                }
                else if (pdata.CurrentPhase == Phase.Warning && pdata.Timer <= 0f)
                {
                    pdata.CurrentPhase = Phase.Consequence;
                    HandleConsequence(pc, target, pdata);
                    pdata.CurrentPhase = Phase.Detection;
                    pdata.Timer = ProximitySeconds;
                    pdata.TargetId = byte.MaxValue;
                    PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "proximity");
                }
            }
        }

        private static void HandleConsequence(PlayerControl follower, PlayerControl followed, ProximityData pdata)
        {
            if (follower == null || followed == null || pdata == null) return;
            if (Utils.ImpostorTeam(follower)) return;
            if (Action == 1)
            {
                try { AmongUsClient.Instance.KickPlayer(follower.GetClientId(), false); }
                catch { }
            }
        }

        private static void ResetPlayerData(byte playerId, PlayerControl pc)
        {
            if (PlayersProximity.ContainsKey(playerId)) PlayersProximity.Remove(playerId);
            if (FrozenPlayers.ContainsKey(playerId)) FrozenPlayers.Remove(playerId);
            PlayerWarningMessenger.ClearForPlayer(playerId, "proximity");
        }

        public static bool IsFollowing(PlayerControl pc)
        {
            if (pc == null || !PlayersProximity.TryGetValue(pc.PlayerId, out var d)) return false;
            return d.CurrentPhase == Phase.Warning || d.CurrentPhase == Phase.Consequence;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.OnGameStart))]
    public static class Proximity_HudManagerOnGameStartPatch
    {
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            PlayerWarningMessenger.ResetAll();
            ProximityMonitor.EnsureTrackedPlayers();
        }
    }
}
