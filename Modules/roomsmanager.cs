//credits and licenses in the resources folder
using BanMod;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public enum RoomShape
    {
        Circle,
        Rectangle,
        Polygon
    }

    public class RoomZoneManager
    {
        public static List<RoomZone> rooms = new List<RoomZone>();
        public static Dictionary<string, Vector2> playerPositions = new Dictionary<string, Vector2>();
        public static List<PlayerRoomSnapshot> killSnapshots = new List<PlayerRoomSnapshot>();

        public RoomZoneManager()
        {
            LoadRooms();
        }

        public void UpdatePlayerPosition(string playerName, Vector2 position)
        {
            playerPositions[playerName] = position;
        }

        public string GetPlayerRoom(string playerName, MapNames map)
        {
            if (!playerPositions.ContainsKey(playerName)) return "Unknown";

            Vector2 pos = playerPositions[playerName];
            RoomZone room = GetCurrentRoom(pos, map);
            return room != null ? room.RoomName : "Unknown";
        }

        public RoomZone GetCurrentRoom(Vector2 playerPos, MapNames currentMap)
        {
            foreach (var room in rooms)
            {
                if (room.MapID == currentMap && room.Contains(playerPos))
                    return room;
            }
            return null;
        }

        public void RegisterKillSnapshot(MapNames currentMap)
        {
            PlayerRoomSnapshot snapshot = new PlayerRoomSnapshot(Time.time);

            foreach (var kvp in playerPositions)
            {
                string playerName = kvp.Key;
                Vector2 position = kvp.Value;
                RoomZone room = GetCurrentRoom(position, currentMap);
                string roomName = room != null ? room.RoomName : "Unknown";

                snapshot.PlayerRooms[playerName] = roomName;
            }

            killSnapshots.Add(snapshot);

            BMLogger.Info($"[KILL SNAPSHOT @ {snapshot.Time}]");
            foreach (var entry in snapshot.PlayerRooms)
            {
                BMLogger.Info($" - {entry.Key} era in {entry.Value}");
            }
        }

        public List<PlayerRoomSnapshot> GetAllKillSnapshots()
        {
            return new List<PlayerRoomSnapshot>(killSnapshots);
        }

        public List<RoomZone> GetRoomsForMap(MapNames mapID)
        {
            return rooms.FindAll(r => r.MapID == mapID);
        }

        public void ClearAllData()
        {
            killSnapshots.Clear();
        }

        public void LoadRooms()
        {
            rooms.Clear();

            rooms.Add(new RoomZone(MapNames.Skeld, "Cafeteria", new List<Vector2>
            {
                new Vector2(-4.16f, 6.03f),
                new Vector2(1.94f, 6.05f),
                new Vector2(4.74f, 3.24f),
                new Vector2(4.76f, -1.55f),
                new Vector2(2.28f, -4.02f),
                new Vector2(-3.66f, -4.01f),
                new Vector2(-6.05f, -1.64f),
                new Vector2(-5.99f, 4.24f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Weapons", new List<Vector2>
            {
                new Vector2(7.24f, 3.76f),
                new Vector2(9.58f, 3.76f),
                new Vector2(11.67f, 1.81f),
                new Vector2(11.67f, -0.67f),
                new Vector2(8.17f, -0.67f),
                new Vector2(7.11f, 0.39f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "O2", new List<Vector2>
            {
                new Vector2(8.73f, -2.49f),
                new Vector2(5.96f, -2.49f),
                new Vector2(3.83f, -4.91f),
                new Vector2(7.77f, -4.91f),
                new Vector2(7.77f, -3.89f),
                new Vector2(8.79f, -3.89f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Navigation", new List<Vector2>
            {
                new Vector2(12.77f, -4.24f),
                new Vector2(15.54f, -4.24f),
                new Vector2(15.54f, -2.64f),
                new Vector2(17.58f, -2.64f),
                new Vector2(19.04f, -3.80f),
                new Vector2(19.04f, -5.55f),
                new Vector2(17.57f, -6.58f),
                new Vector2(15.68f, -6.58f),
                new Vector2(15.68f, -5.27f),
                new Vector2(12.91f, -5.27f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Shields", new List<Vector2>
            {
                new Vector2(6.69f, -11.61f),
                new Vector2(8.37f, -10.22f),
                new Vector2(11.58f, -10.22f),
                new Vector2(11.58f, -12.85f),
                new Vector2(9.33f, -15.10f),
                new Vector2(7.14f, -14.66f),
                new Vector2(6.70f, -12.62f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Communications", new List<Vector2>
            {
                new Vector2(1.49f, -13.82f),
                new Vector2(6.59f, -13.82f),
                new Vector2(6.59f, -16.45f),
                new Vector2(5.46f, -17.58f),
                new Vector2(2.69f, -17.58f),
                new Vector2(1.41f, -16.30f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Storage", new List<Vector2>
            {
                new Vector2(-3.78f, -8.64f),
                new Vector2(0.89f, -8.64f),
                new Vector2(0.89f, -17.39f),
                new Vector2(-3.05f, -17.39f),
                new Vector2(-5.18f, -15.41f),
                new Vector2(-5.18f, -10.01f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Admin", new List<Vector2>
            {
                new Vector2(0.25f, -6.34f),
                new Vector2(6.96f, -6.34f),
                new Vector2(6.96f, -9.26f),
                new Vector2(5.99f, -10.23f),
                new Vector2(1.98f, -10.23f),
                new Vector2(1.91f, -7.75f),
                new Vector2(0.16f, -7.75f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Electrical", new List<Vector2>
            {
                new Vector2(-10.14f, -7.17f),
                new Vector2(-5.04f, -7.17f),
                new Vector2(-5.04f, -8.34f),
                new Vector2(-6.19f, -9.35f),
                new Vector2(-6.19f, -11.25f),
                new Vector2(-7.33f, -12.24f),
                new Vector2(-9.08f, -12.24f),
                new Vector2(-9.08f, -13.69f),
                new Vector2(-10.25f, -13.69f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Lower Engine", new List<Vector2>
            {
                new Vector2(-19.59f, -9.24f),
                new Vector2(-14.92f, -9.24f),
                new Vector2(-14.92f, -13.62f),
                new Vector2(-18.13f, -14.05f),
                new Vector2(-19.50f, -12.98f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Security", new List<Vector2>
            {
                new Vector2(-14.50f, -2.28f),
                new Vector2(-11.73f, -2.28f),
                new Vector2(-11.73f, -7.39f),
                new Vector2(-14.50f, -7.39f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Upper Engine", new List<Vector2>
            {
                new Vector2(-19.62f, 2.31f),
                new Vector2(-18.38f, 3.25f),
                new Vector2(-14.73f, 3.25f),
                new Vector2(-14.73f, -1.42f),
                new Vector2(-19.69f, -1.42f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "MedBay", new List<Vector2>
            {
                new Vector2(-11.35f, -0.72f),
                new Vector2(-6.83f, -0.72f),
                new Vector2(-6.83f, -3.34f),
                new Vector2(-4.57f, -5.60f),
                new Vector2(-10.40f, -5.60f),
                new Vector2(-11.31f, -4.40f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Reactor", new List<Vector2>
            {
                new Vector2(-23.42f, -2.35f),
                new Vector2(-21.60f, -1.11f),
                new Vector2(-20.73f, -1.11f),
                new Vector2(-20.73f, -3.00f),
                new Vector2(-19.12f, -3.00f),
                new Vector2(-19.12f, -7.38f),
                new Vector2(-20.73f, -7.38f),
                new Vector2(-20.73f, -9.13f),
                new Vector2(-21.89f, -9.13f),
                new Vector2(-23.46f, -7.85f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_O2_Shield", new List<Vector2>
            {
                new Vector2(8.98f, -0.97f),
                new Vector2(8.98f, -3.67f),
                new Vector2(11.31f, -3.66f),
                new Vector2(11.34f, -5.84f),
                new Vector2(8.90f, -5.95f),
                new Vector2(9.01f, -10.11f),
                new Vector2(10.10f, -10.11f),
                new Vector2(10.05f, -6.96f),
                new Vector2(12.46f, -6.71f),
                new Vector2(12.47f, -2.87f),
                new Vector2(10.05f, -3.00f),
                new Vector2(10.03f, -0.97f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_Comms", new List<Vector2>
            {
                new Vector2(6.74f, -11.69f),
                new Vector2(1.05f, -11.69f),
                new Vector2(1.05f, -12.46f),
                new Vector2(4.55f, -12.46f),
                new Vector2(4.58f, -13.46f),
                new Vector2(5.72f, -13.46f),
                new Vector2(5.70f, -12.59f),
                new Vector2(6.73f, -12.46f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_Storage_Lower", new List<Vector2>
            {
                new Vector2(-5.41f, -13.97f),
                new Vector2(-11.67f, -13.98f),
                new Vector2(-11.55f, -11.12f),
                new Vector2(-14.75f, -11.13f),
                new Vector2(-14.75f, -11.93f),
                new Vector2(-12.86f, -11.92f),
                new Vector2(-12.72f, -14.78f),
                new Vector2(-5.44f, -14.76f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_Cam", new List<Vector2>
            {
                new Vector2(-16.34f, -9.16f),
                new Vector2(-17.51f, -9.16f),
                new Vector2(-17.49f, -5.81f),
                new Vector2(-18.94f, -5.63f),
                new Vector2(-18.94f, -4.91f),
                new Vector2(-17.48f, -4.91f),
                new Vector2(-17.39f, -1.78f),
                new Vector2(-16.41f, -1.78f),
                new Vector2(-16.41f, -4.82f),
                new Vector2(-14.95f, -4.84f),
                new Vector2(-14.95f, -5.63f),
                new Vector2(-16.26f, -5.63f),
                new Vector2(-16.36f, -9.22f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_Med", new List<Vector2>
            {
                new Vector2(-14.46f, 1.69f),
                new Vector2(-6.45f, 1.67f),
                new Vector2(-6.45f, 0.86f),
                new Vector2(-8.58f, 0.33f),
                new Vector2(-8.58f, -0.40f),
                new Vector2(-9.74f, -0.40f),
                new Vector2(-9.69f, 0.57f),
                new Vector2(-14.73f, 0.87f)
            }));

            rooms.Add(new RoomZone(MapNames.Skeld, "Flor_Admin", new List<Vector2>
            {
                new Vector2(-1.27f, -4.76f),
                new Vector2(-1.27f, -8.70f),
                new Vector2(-0.13f, -8.70f),
                new Vector2(-0.13f, -4.77f)
            }));
            rooms.Add(new RoomZone(MapNames.MiraHQ, "Cafeteria", new List<Vector2>
{
    new Vector2(21.36f, 5.56f),
    new Vector2(28.88f, 5.53f),
    new Vector2(28.91f, -0.16f),
    new Vector2(18.12f, -0.15f),
    new Vector2(18.13f, 0.89f),
    new Vector2(21.33f, 0.87f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Balcony", new List<Vector2>
{
    new Vector2(18.13f, -1.30f),
    new Vector2(28.34f, -1.28f),
    new Vector2(28.34f, -2.33f),
    new Vector2(20.18f, -3.35f),
    new Vector2(18.10f, -3.34f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Storage", new List<Vector2>
{
    new Vector2(18.13f, 5.21f),
    new Vector2(20.89f, 5.64f),
    new Vector2(20.59f, 2.02f),
    new Vector2(18.14f, 2.02f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Communications", new List<Vector2>
{
    new Vector2(16.82f, 5.73f),
    new Vector2(13.90f, 5.73f),
    new Vector2(13.90f, 2.81f),
    new Vector2(16.82f, 2.81f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Launchpad", new List<Vector2>
{
    new Vector2(-6.51f, 4.15f),
    new Vector2(-2.67f, 4.10f),
    new Vector2(-2.67f, 0.28f),
    new Vector2(-6.61f, 0.28f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "MedBay", new List<Vector2>
{
    new Vector2(13.87f, 0.97f),
    new Vector2(16.92f, 0.94f),
    new Vector2(16.92f, -2.12f),
    new Vector2(13.86f, -2.12f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Locker", new List<Vector2>
{
    new Vector2(10.96f, 5.33f),
    new Vector2(8.62f, 5.32f),
    new Vector2(8.62f, 2.26f),
    new Vector2(3.98f, 2.26f),
    new Vector2(3.98f, 0.55f),
    new Vector2(10.87f, 0.55f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Decontamination", new List<Vector2>
{
    new Vector2(7.02f, 3.34f),
    new Vector2(5.11f, 3.34f),
    new Vector2(5.13f, 8.82f),
    new Vector2(7.05f, 8.94f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Reactor", new List<Vector2>
{
    new Vector2(4.65f, 10.08f),
    new Vector2(0.41f, 10.08f),
    new Vector2(0.41f, 15.77f),
    new Vector2(4.64f, 15.77f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Laboratory", new List<Vector2>
{
    new Vector2(11.92f, 10.09f),
    new Vector2(7.51f, 10.06f),
    new Vector2(7.52f, 14.66f),
    new Vector2(11.95f, 14.97f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Office", new List<Vector2>
{
    new Vector2(13.04f, 17.18f),
    new Vector2(13.04f, 21.26f),
    new Vector2(16.39f, 21.26f),
    new Vector2(16.39f, 17.03f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Admin", new List<Vector2>
{
    new Vector2(19.24f, 20.96f),
    new Vector2(22.72f, 21.39f),
    new Vector2(22.72f, 16.73f),
    new Vector2(19.22f, 16.73f)
}));

            rooms.Add(new RoomZone(MapNames.MiraHQ, "Greenhouse", new List<Vector2>
{
    new Vector2(12.91f, 22.29f),
    new Vector2(22.77f, 22.27f),
    new Vector2(22.77f, 26.06f),
    new Vector2(13.00f, 26.06f)
}));
            rooms.Add(new RoomZone(MapNames.MiraHQ, "Floor", new List<Vector2>
{
    new Vector2(-6.02f, -0.28f),
    new Vector2(-6.02f, -2.21f),
    new Vector2(3.54f, -2.18f),
    new Vector2(13.41f, -1.82f),
    new Vector2(13.38f, 6.62f),
    new Vector2(17.73f, 10.92f),
    new Vector2(22.29f, 5.81f),
    new Vector2(24.24f, 5.81f),
    new Vector2(24.22f, 7.57f),
    new Vector2(22.96f, 7.74f),
    new Vector2(18.77f, 11.84f),
    new Vector2(18.79f, 21.16f),
    new Vector2(16.86f, 21.12f),
    new Vector2(16.89f, 11.68f),
    new Vector2(12.75f, 7.72f),
    new Vector2(11.41f, 7.73f),
    new Vector2(11.43f, -0.58f),
    new Vector2(-2.81f, -0.66f),
    new Vector2(-2.96f, -0.08f)
}));

        }

        public class RoomZone
        {
            public MapNames MapID;
            public string RoomName;
            public RoomShape Shape;

            public Vector2 Center;
            public float Radius;

            public Vector2 TopLeft;
            public Vector2 BottomRight;

            public List<Vector2> PolygonPoints;

            public RoomZone(MapNames mapID, string roomName, Vector2 center, float radius)
            {
                MapID = mapID;
                RoomName = roomName;
                Shape = RoomShape.Circle;
                Center = center;
                Radius = radius;
            }

            public RoomZone(MapNames mapID, string roomName, Vector2 topLeft, Vector2 bottomRight)
            {
                MapID = mapID;
                RoomName = roomName;
                Shape = RoomShape.Rectangle;

                float minX = Mathf.Min(topLeft.x, bottomRight.x);
                float maxX = Mathf.Max(topLeft.x, bottomRight.x);
                float minY = Mathf.Min(topLeft.y, bottomRight.y);
                float maxY = Mathf.Max(topLeft.y, bottomRight.y);

                TopLeft = new Vector2(minX, maxY);
                BottomRight = new Vector2(maxX, minY);
            }

            public RoomZone(MapNames mapID, string roomName, List<Vector2> polygonPoints)
            {
                MapID = mapID;
                RoomName = roomName;
                Shape = RoomShape.Polygon;
                PolygonPoints = new List<Vector2>(polygonPoints);
            }

            public bool Contains(Vector2 pos)
            {
                switch (Shape)
                {
                    case RoomShape.Circle:
                        return Vector2.Distance(pos, Center) <= Radius;

                    case RoomShape.Rectangle:
                        return pos.x >= TopLeft.x && pos.x <= BottomRight.x &&
                               pos.y <= TopLeft.y && pos.y >= BottomRight.y;

                    case RoomShape.Polygon:
                        return IsPointInPolygon(pos, PolygonPoints);

                    default:
                        return false;
                }
            }

            private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
            {
                int j = polygon.Count - 1;
                bool inside = false;

                for (int i = 0; i < polygon.Count; j = i++)
                {
                    Vector2 pi = polygon[i];
                    Vector2 pj = polygon[j];

                    if (((pi.y > point.y) != (pj.y > point.y)) &&
                        (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
                    {
                        inside = !inside;
                    }
                }

                return inside;
            }
        }
    }

    public class PlayerRoomSnapshot
    {
        public float Time;
        public Dictionary<string, string> PlayerRooms;

        public PlayerRoomSnapshot(float time)
        {
            Time = time;
            PlayerRooms = new Dictionary<string, string>();
        }
    }
}
public class PlayerPositionUpdater : MonoBehaviour
{
    void Update()
    {
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost || !GameStates.isOnlineGame || GameStates.isLobby) return;
        var roomManager = BanMod.BanMod.RoomZoneManagerInstance;
        if (roomManager == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null) continue;
            Vector2 position = player.transform.position;
            roomManager.UpdatePlayerPosition(player.Data.PlayerName, position);
        }
    }
}
