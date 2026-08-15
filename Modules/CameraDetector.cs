//credits and licenses in the resources folder
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class CamDetector
    {
        public static readonly Dictionary<byte, DataCam> PlayerDataCam = new();
        private static readonly List<PlayerControl> playersInRoomBuffer = new();
        private static float cachedFixedTime = float.MinValue;
        private static int cachedMaxAllowedInCam;

        public static void ResetAll()
        {
            foreach (byte playerId in PlayerDataCam.Keys)
                PlayerWarningMessenger.ClearForPlayer(playerId, "cam_crowd");

            PlayerDataCam.Clear();
            playersInRoomBuffer.Clear();
            cachedFixedTime = float.MinValue;
            cachedMaxAllowedInCam = 0;
            DetectorPlayerExclusions.Reset();
        }
        private static readonly Dictionary<MapNames, List<RoomZone>> RoomZonesByMap = new()
        {
            { MapNames.Skeld, new List<RoomZone> { new RoomZone("Security", new List<Vector2> { new Vector2(-12.0f,-1.80f), new Vector2(-12.0f,-7.40f), new Vector2(-15.0f,-7.40f), new Vector2(-15.0f,-1.80f) }) } },
            { MapNames.Polus, new List<RoomZone> { new RoomZone("Security", new List<Vector2> { new Vector2(1.8512791f,-11.1489315f), new Vector2(1.7445393f,-12.595622f), new Vector2(4.1805935f,-12.602555f), new Vector2(4.1892643f,-11.220876f) }) } },
            { MapNames.Airship, new List<RoomZone> { new RoomZone("Security", new List<Vector2> { new Vector2(6.8444366f,-9.876846f), new Vector2(6.8444366f,-10.751844f), new Vector2(9.175756f,-10.751841f), new Vector2(9.433342f,-9.944351f) }) } },
            { MapNames.Fungle, new List<RoomZone> { new RoomZone("Security", new List<Vector2> { new Vector2(6.49043f,-0.08541406f), new Vector2(8.414647f,0.19194058f), new Vector2(7.6020155f,1.8918657f), new Vector2(6.029099f,1.9040151f) }) } }
        };

        public static void RecordPosition(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableCamDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;
            if (DetectorPlayerExclusions.ShouldIgnore(pc)) return;

            if (!PlayerDataCam.ContainsKey(pc.PlayerId))
            {
                PlayerDataCam[pc.PlayerId] = new DataCam
                {
                    LastPosition = pc.Pos(),
                    Timer = Mathf.RoundToInt(Options.DetectionCamDelay.GetFloat() / Time.fixedDeltaTime),
                    CurrentPhase = DataCam.Phase.Detection,
                    OriginalName = pc.Data.PlayerName
                };
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "cam_crowd");
            }
            else
            {
                var data = PlayerDataCam[pc.PlayerId];
                data.LastPosition = pc.Pos();
                data.Timer = Mathf.RoundToInt(Options.DetectionCamDelay.GetFloat() / Time.fixedDeltaTime);
            }
        }

        public static void OnFixedUpdate(PlayerControl pc, bool force = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableCamDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;

            if (DetectorPlayerExclusions.ShouldIgnore(pc))
            {
                if (PlayerDataCam.Remove(pc.PlayerId))
                    PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "cam_crowd");
                return;
            }

            if (pc.Data.IsDead || pc.inVent) return;

            MapNames currentMap = GetCurrentMap();
            if (!RoomZonesByMap.TryGetValue(currentMap, out var rooms)) rooms = RoomZonesByMap[MapNames.Skeld];
            var targetRoom = rooms.Find(r => r.RoomName == "Security");
            if (targetRoom == null) return;

            if (!Mathf.Approximately(cachedFixedTime, Time.fixedTime))
            {
                cachedFixedTime = Time.fixedTime;
                playersInRoomBuffer.Clear();

                int impostorsAlive = 0;
                foreach (var player in BanMod.AllAlivePlayerControls)
                {
                    if (player == null || player.Data == null) continue;

                    if (player.Data.Role != null && player.Data.Role.IsImpostor)
                        impostorsAlive++;

                    if (DetectorPlayerExclusions.ShouldIgnore(player))
                        continue;

                    if (!player.inVent && targetRoom.Contains(player.Pos()))
                        playersInRoomBuffer.Add(player);
                }

                cachedMaxAllowedInCam = impostorsAlive == 1
                    ? 1
                    : Options.MaxCam.GetInt();

                if (playersInRoomBuffer.Count >= cachedMaxAllowedInCam)
                {
                    for (int i = 1; i < playersInRoomBuffer.Count; i++)
                    {
                        PlayerControl trackedPlayer = playersInRoomBuffer[i];
                        if (!PlayerDataCam.ContainsKey(trackedPlayer.PlayerId))
                            RecordPosition(trackedPlayer);
                    }

                    PlayerControl exemptPlayer = playersInRoomBuffer[0];
                    if (PlayerDataCam.Remove(exemptPlayer.PlayerId))
                    {
                        PlayerWarningMessenger.ClearForPlayer(
                            exemptPlayer.PlayerId,
                            "cam_crowd"
                        );
                    }
                }
            }

            if (playersInRoomBuffer.Count < cachedMaxAllowedInCam) return;

            if (!PlayerDataCam.TryGetValue(pc.PlayerId, out var data)) return;

            if (!targetRoom.Contains(pc.Pos()))
            {
                PlayerDataCam.Remove(pc.PlayerId);
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "cam_crowd");
                return;
            }

            data.Timer--;

            if (data.Timer <= 0)
            {
                switch (data.CurrentPhase)
                {
                    case DataCam.Phase.Detection:
                        data.CurrentPhase = DataCam.Phase.Warning;
                        data.Timer = Mathf.RoundToInt(Options.TimeToCamActivate.GetFloat() / Time.fixedDeltaTime);
                        PlayerWarningMessenger.SendOnce(pc, "cam_crowd", "DetectorCamCrowdWarning");
                        break;

                    case DataCam.Phase.Warning:
                        data.CurrentPhase = DataCam.Phase.Consequence;
                        goto case DataCam.Phase.Consequence;

                    case DataCam.Phase.Consequence:
                        if (Options.EnableCamKick.GetBool())
                        {
                            AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                            ChatCommands.ShowChat($"{pc.Data.PlayerName} {GetString("Camkicked")}");
                            PlayerDataCam.Remove(pc.PlayerId);
                            PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "cam_crowd");
                        }
                        return;
                }
            }
        }

        public class DataCam
        {
            public enum Phase { Detection, Warning, Consequence }
            public Vector2 LastPosition { get; set; }
            public int Timer { get; set; }
            public Phase CurrentPhase { get; set; }
            public string OriginalName { get; init; }
        }
    }

    public class RoomZone
    {
        public string RoomName;
        public List<Vector2> Bounds;
        public RoomZone(string roomName, List<Vector2> bounds) { RoomName = roomName; Bounds = bounds; }
        public bool Contains(Vector2 point)
        {
            int i, j; bool result = false; int n = Bounds.Count;
            for (i = 0, j = n - 1; i < n; j = i++)
            {
                if ((Bounds[i].y > point.y) != (Bounds[j].y > point.y) &&
                    (point.x < (Bounds[j].x - Bounds[i].x) * (point.y - Bounds[i].y) / (Bounds[j].y - Bounds[i].y) + Bounds[i].x))
                    result = !result;
            }
            return result;
        }
    }
}

