//credits and licenses in the resources folder
using UnityEngine;

namespace BanMod
{
    public enum ZoneShape
    {
        Circle,
        Rectangle
    }

    public class SpawnZone
    {
        public MapNames MapID;
        public string ZoneName;
        public ZoneShape Shape;

        public Vector2 Center;
        public float Radius;

        public Vector2 RectMin;
        public Vector2 RectMax;

        public SpawnZone(MapNames mapID, Vector2 center, float radius, string zoneName = "")
        {
            MapID = mapID;
            Shape = ZoneShape.Circle;
            Center = center;
            Radius = radius;
            ZoneName = zoneName;
        }

        public SpawnZone(MapNames mapID, Vector2 rectMin, Vector2 rectMax, string zoneName = "")
        {
            MapID = mapID;
            Shape = ZoneShape.Rectangle;
            RectMin = rectMin;
            RectMax = rectMax;
            ZoneName = zoneName;
        }

        public bool Contains(Vector2 position)
        {
            if (Shape == ZoneShape.Circle)
            {
                return Vector2.Distance(position, Center) <= Radius;
            }
            else if (Shape == ZoneShape.Rectangle)
            {
                return position.x >= RectMin.x && position.x <= RectMax.x &&
                       position.y >= RectMin.y && position.y <= RectMax.y;
            }
            return false;
        }
    }
}
