//credits and licenses in the resources folder
using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class CamTaskDetector
    {
        public static readonly Dictionary<byte, DataTask> PlayerDataTask = new();
        private static readonly Dictionary<byte, int> lastTimerUpdateFrame = new();
        private static readonly Dictionary<MapNames, List<RoomZoneTask>> RoomZonesTaskByMap = new()
        {
            { MapNames.Skeld, new List<RoomZoneTask> { new RoomZoneTask("Security", new List<Vector2> { new Vector2(-12.0f,-1.80f), new Vector2(-12.0f,-7.40f), new Vector2(-15.0f,-7.40f), new Vector2(-15.0f,-1.80f) }) } },
            { MapNames.Polus, new List<RoomZoneTask> { new RoomZoneTask("Security", new List<Vector2> { new Vector2(1.8512791f,-11.1489315f), new Vector2(1.7445393f,-12.595622f), new Vector2(4.1805935f,-12.602555f), new Vector2(4.1892643f,-11.220876f) }) } },
            { MapNames.Airship, new List<RoomZoneTask> { new RoomZoneTask("Security", new List<Vector2> { new Vector2(6.8444366f,-9.876846f), new Vector2(6.8444366f,-10.751844f), new Vector2(9.175756f,-10.751841f), new Vector2(9.433342f,-9.944351f) }) } },
            { MapNames.Fungle, new List<RoomZoneTask> { new RoomZoneTask("Security", new List<Vector2> { new Vector2(6.49043f,-0.08541406f), new Vector2(8.414647f,0.19194058f), new Vector2(7.6020155f,1.8918657f), new Vector2(6.029099f,1.9040151f) }) } }
        };

        public static void RecordPosition(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableCamTaskDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;

            if (!PlayerDataTask.ContainsKey(pc.PlayerId))
            {
                PlayerDataTask[pc.PlayerId] = new DataTask
                {
                    LastPosition = pc.Pos(),
                    Timer = Options.DetectionCamDelay.GetFloat(),
                    CurrentPhase = DataTask.Phase.Detection,
                    OriginalName = pc.Data.PlayerName
                };
                PlayerWarningMessenger.ClearForPlayer(pc.PlayerId, "cam_task");
            }
            else
            {
                var data = PlayerDataTask[pc.PlayerId];
                data.LastPosition = pc.Pos();
                data.Timer = Options.DetectionCamDelay.GetFloat();
            }
        }

        public static void OnFixedUpdate(PlayerControl pc, bool force = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableCamTaskDetector.GetBool() || !GameStates.IsInGameplay || pc == null || pc.Data == null) return;
            if (pc.Data.IsDead || pc.inVent) return;

            MapNames currentMap = GetCurrentMap();
            if (!RoomZonesTaskByMap.TryGetValue(currentMap, out var rooms)) rooms = RoomZonesTaskByMap[MapNames.Skeld];
            var targetRoom = rooms.Find(r => r.RoomName == "Security");
            if (targetRoom == null) return;

            int currentFrame = Time.frameCount;
            foreach (var kvp in PlayerDataTask.ToList())
            {
                if (!lastTimerUpdateFrame.TryGetValue(kvp.Key, out int lastFrame) || lastFrame != currentFrame)
                {
                    kvp.Value.Timer -= Time.fixedDeltaTime;
                    lastTimerUpdateFrame[kvp.Key] = currentFrame;
                }
            }

            foreach (var kvp in PlayerDataTask.ToList())
            {
                var trackedPlayer = BanMod.AllAlivePlayerControls.FirstOrDefault(p => p.PlayerId == kvp.Key);
                if (trackedPlayer == null || trackedPlayer.inVent || trackedPlayer.Data.IsDead) continue;
                if (!targetRoom.Contains(trackedPlayer.Pos()))
                {
                    PlayerDataTask.Remove(kvp.Key);
                    PlayerWarningMessenger.ClearForPlayer(kvp.Key, "cam_task");
                }
            }

            List<PlayerControl> playersInRoom = BanMod.AllAlivePlayerControls.Where(p => p != null && p.Data != null && !p.inVent && targetRoom.Contains(p.Pos())).ToList();
            int requiredTasksCrew = Options.MinTasksToUseCamCrew.GetInt();
            int requiredTasksImp = Options.MinTasksToUseCamImp.GetInt();

            foreach (var player in playersInRoom)
            {
                bool isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
                bool isCrewmate = player.Data.Role.TeamType == RoleTeamTypes.Crewmate;

                if (isCrewmate)
                {
                    int completedTasks = CountCompletedTasks(player.Data.Tasks);
                    if (completedTasks < requiredTasksCrew) HandlePlayerWithLowTasks(player, completedTasks, requiredTasksCrew, false, 0);
                    else ResetPlayerStateIfNeeded(player);
                }
                else if (isImpostor)
                {
                    int impostorCondition = Options.ImpostorCamCondition.GetValue();
                    var aliveCrewmates = BanMod.AllAlivePlayerControls.Where(p => p.Data.Role.TeamType == RoleTeamTypes.Crewmate && !p.Data.IsDead && !p.inVent).ToList();
                    bool allCrewmatesCompleted = aliveCrewmates.All(cm => CountCompletedTasks(cm.Data.Tasks) >= requiredTasksImp);
                    int kills = KillTracker.GetKills(player.PlayerId);
                    int requiredKills = Options.MinKillsToUseCamImp.GetInt();
                    bool allowAccess = impostorCondition switch
                    {
                        0 => allCrewmatesCompleted,
                        1 => kills >= requiredKills,
                        2 => allCrewmatesCompleted && kills >= requiredKills,
                        3 => allCrewmatesCompleted || kills >= requiredKills,
                        _ => false
                    };
                    if (!allowAccess) HandlePlayerWithLowTasks(player, kills, requiredKills, true, impostorCondition);
                    else ResetPlayerStateIfNeeded(player);
                }
            }
        }

        private static void HandlePlayerWithLowTasks(PlayerControl player, int completedTasks, int requiredTasks, bool impostor = false, int conditionMode = 0)
        {
            if (!PlayerDataTask.TryGetValue(player.PlayerId, out var data))
            {
                PlayerDataTask[player.PlayerId] = new DataTask
                {
                    LastPosition = player.Pos(),
                    Timer = Options.DetectionCamTaskDelay.GetFloat(),
                    CurrentPhase = DataTask.Phase.Detection,
                    OriginalName = player.Data.PlayerName
                };
                PlayerWarningMessenger.ClearForPlayer(player.PlayerId, "cam_task");
                return;
            }

            if (data.CurrentPhase == DataTask.Phase.Detection && data.Timer <= 0)
            {
                data.CurrentPhase = DataTask.Phase.Warning;
                data.Timer = Options.TimeToCamTaskActivate.GetFloat();
                PlayerWarningMessenger.SendOnce(player, "cam_task", impostor ? "DetectorCamTaskImpostorWarning" : "DetectorCamTaskCrewWarning");
            }
            else if (data.CurrentPhase == DataTask.Phase.Warning && data.Timer <= 0)
            {
                data.CurrentPhase = DataTask.Phase.Consequence;
                bool isVip = Utils.IsVip(player.FriendCode) && BanMod.ExcludeFriends.Value;
                bool isHostPlayer = player.PlayerId == PlayerControl.LocalPlayer.PlayerId;
                if (isVip || isHostPlayer)
                {
                    PlayerDataTask.Remove(player.PlayerId);
                    PlayerWarningMessenger.ClearForPlayer(player.PlayerId, "cam_task");
                    return;
                }
                if (Options.EnableCamTaskKick.GetBool())
                {
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                    ChatCommands.ShowChat($"{player.Data.PlayerName} {GetString("Camkicked2")}");
                    PlayerDataTask.Remove(player.PlayerId);
                    PlayerWarningMessenger.ClearForPlayer(player.PlayerId, "cam_task");
                }
            }
        }

        private static void ResetPlayerStateIfNeeded(PlayerControl player)
        {
            if (PlayerDataTask.ContainsKey(player.PlayerId))
            {
                PlayerDataTask.Remove(player.PlayerId);
                PlayerWarningMessenger.ClearForPlayer(player.PlayerId, "cam_task");
            }
        }

        public static int CountCompletedTasks(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo.TaskInfo> tasks)
        {
            int count = 0;
            foreach (var task in tasks) if (task.Complete) count++;
            return count;
        }

        public class DataTask
        {
            public enum Phase { Detection, Warning, Consequence }
            public Vector2 LastPosition { get; set; }
            public float Timer { get; set; }
            public Phase CurrentPhase { get; set; }
            public string OriginalName { get; init; }
        }
    }

    public class RoomZoneTask
    {
        public string RoomName;
        public List<Vector2> Bounds;
        public RoomZoneTask(string roomName, List<Vector2> bounds) { RoomName = roomName; Bounds = bounds; }
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

