//credits and licenses in the resources folder
using BanMod;
using GameCore;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class AFKDetector
    {
        private static bool wasInMeeting = false;
        public static readonly Dictionary<byte, DataAFK> PlayerDataAFK = new();
        public static bool IsPlayerAfk;

        private static readonly Dictionary<MapNames, List<RoomZone1>> RoomZonesByMap = new()
        {
            { MapNames.Skeld, new List<RoomZone1> { new RoomZone1("Security", new List<Vector2> { new Vector2(-12.0f,-1.80f), new Vector2(-12.0f,-7.40f), new Vector2(-15.0f,-7.40f), new Vector2(-15.0f,-1.80f) }) } },
            { MapNames.Polus, new List<RoomZone1> { new RoomZone1("Security", new List<Vector2> { new Vector2(1.8512791f,-11.1489315f), new Vector2(1.7445393f,-12.595622f), new Vector2(4.1805935f,-12.602555f), new Vector2(4.1892643f,-11.220876f) }) } },
            { MapNames.Airship, new List<RoomZone1> { new RoomZone1("Security", new List<Vector2> { new Vector2(6.8444366f,-9.876846f), new Vector2(6.8444366f,-10.751844f), new Vector2(9.175756f,-10.751841f), new Vector2(9.433342f,-9.944351f) }) } },
            { MapNames.Fungle, new List<RoomZone1> { new RoomZone1("Security", new List<Vector2> { new Vector2(6.49043f,-0.08541406f), new Vector2(8.414647f,0.19194058f), new Vector2(7.6020155f,1.8918657f), new Vector2(6.029099f,1.9040151f) }) } }
        };

        public static void RecordPosition(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;

            PlayerDataAFK[pc.PlayerId] = new DataAFK
            {
                LastPosition = pc.Pos(),
                Timer = Options.DetectionDelay.GetInt(),
                CurrentPhase = DataAFK.Phase.Detection,
                OriginalName = pc.Data.PlayerName
            };

            PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
        }

        public static void OnFixedUpdate(PlayerControl pc, bool force = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;

            DeviceUsageTracker.UpdateUsage();
            if (DeviceUsageTracker.IsUsingAnyDevice(pc.PlayerId)) return;
            if (pc.Data.IsDead) return;

            if (IsInSecurityZone(pc))
            {
                PlayerDataAFK.Remove(pc.PlayerId);
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
                RecordPosition(pc);
                return;
            }

            if (!PlayerDataAFK.TryGetValue(pc.PlayerId, out var data)) return;

            if (GameStates.IsMeeting)
            {
                if (!wasInMeeting)
                {
                    wasInMeeting = true;

                    if (data.CurrentPhase == DataAFK.Phase.Warning)
                    {
                        if (!ImmortalManager.IsImmortal(pc.PlayerId) && !Watcher.IsWatcher(pc.PlayerId))
                        {
                            BanMod.ShieldedPlayers.Remove(pc.PlayerId);
                            pc.RemoveProtection();
                            pc.protectedByGuardianId = -1;
                            pc.Data.MarkDirty();
                        }
                    }

                    PlayerDataAFK.Remove(pc.PlayerId);
                    PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
                    RecordPosition(pc);
                }
                return;
            }
            else
            {
                wasInMeeting = false;
            }

            if (Vector2.Distance(pc.Pos(), data.LastPosition) > 0.1f)
            {
                if (!ImmortalManager.IsImmortal(pc.PlayerId) && !Watcher.IsWatcher(pc.PlayerId))
                {
                    BanMod.ShieldedPlayers.Remove(pc.PlayerId);
                    pc.RemoveProtection();
                    pc.protectedByGuardianId = -1;
                    pc.Data.MarkDirty();
                }

                PlayerDataAFK.Remove(pc.PlayerId);
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
                RecordPosition(pc);
                return;
            }

            data.Timer -= Time.fixedDeltaTime;

            if (data.Timer <= 0f)
            {
                switch (data.CurrentPhase)
                {
                    case DataAFK.Phase.Detection:
                        data.CurrentPhase = DataAFK.Phase.Warning;
                        data.Timer = Options.TimeToActivate.GetInt() * 60;

                        if (Options.EnableShield.GetBool() && !BanMod.ShieldedPlayers.Contains(pc.PlayerId))
                            BanMod.ShieldedPlayers.Add(pc.PlayerId);

                        PlayerWarningMessenger.SendOnce(pc, "AFK", Options.EnableShield.GetBool() ? "DetectorAFKShieldWarning" : "DetectorAFKWarning");
                        break;

                    case DataAFK.Phase.Consequence:
                        if (!ImmortalManager.IsImmortal(pc.PlayerId) && !Watcher.IsWatcher(pc.PlayerId))
                        {
                            BanMod.ShieldedPlayers.Remove(pc.PlayerId);
                            pc.RemoveProtection();
                            pc.protectedByGuardianId = -1;
                            pc.Data.MarkDirty();
                        }

                        bool isVip = Utils.IsVip(pc.FriendCode);
                        bool isHostPlayer = pc.PlayerId == PlayerControl.LocalPlayer.PlayerId;

                        if (isVip || isHostPlayer)
                        {
                            PlayerDataAFK.Remove(pc.PlayerId);
                            PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
                            return;
                        }

                        if (Options.EnableAfkKick.GetBool())
                        {
                            AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                            string text = $"{pc.Data.PlayerName} {GetString("AFKKicked")}";

                            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                            {
                                Utils.RequestProxyMessage(text, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                Utils.SendMessage(text, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }

                            PlayerDataAFK.Remove(pc.PlayerId);
                            PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "AFK");
                        }
                        return;
                }
            }
        }

        public static bool IsAfk(PlayerControl pc)
        {
            if (pc == null || !PlayerDataAFK.TryGetValue(pc.PlayerId, out var data)) return false;
            return data.CurrentPhase == DataAFK.Phase.Warning || data.CurrentPhase == DataAFK.Phase.Consequence;
        }

        public static void EnsureTrackedPlayers()
        {
            foreach (var player in BanMod.AllAlivePlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (!PlayerDataAFK.ContainsKey(player.PlayerId)) RecordPosition(player);
            }
        }

        public class DataAFK
        {
            public enum Phase { Detection, Warning, Consequence }
            public Vector2 LastPosition { get; init; }
            public float Timer { get; set; }
            public Phase CurrentPhase { get; set; }
            public string OriginalName { get; init; }
        }

        private static bool IsInSecurityZone(PlayerControl pc)
        {
            MapNames currentMap = GetCurrentMap();
            if (!RoomZonesByMap.TryGetValue(currentMap, out var zones)) return false;
            foreach (var zone in zones)
                if (zone.RoomName == "Security" && zone.Contains(pc.transform.position)) return true;
            return false;
        }
    }
}

public class RoomZone1
{
    public string RoomName;
    public List<Vector2> Bounds;
    public RoomZone1(string roomName, List<Vector2> bounds) { RoomName = roomName; Bounds = bounds; }
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

[HarmonyPatch(typeof(HudManager), nameof(HudManager.OnGameStart))]
public static class HudManagerOnGameStartPatch
{
    public static void Postfix()
    {
        if (BanMod.BanMod.IsBanModDisabled) return;
        if (!AmongUsClient.Instance.AmHost) return;
        PlayerWarningMessenger.ResetAll();
        AFKDetector.EnsureTrackedPlayers();
    }
}
