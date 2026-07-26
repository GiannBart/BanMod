//credits and licenses in the resources folder
using System.Collections.Generic;
using UnityEngine;

namespace BanMod
{
    public class SpawnZoneManager
    {
        private List<SpawnZone> zones = new List<SpawnZone>();

        public SpawnZoneManager()
        {
            LoadZones();
        }

        public void LoadZones()
        {
            zones.Clear();

            zones.Add(new SpawnZone(
                MapNames.Skeld,
                new Vector2(-0.94f, 1.29f),
                Distance(new Vector2(-0.94f, 1.29f), new Vector2(-2.47f, 2.52f))
            ));

            zones.Add(new SpawnZone(
                MapNames.MiraHQ,
                new Vector2(22.35f, 0.37f), 
                new Vector2(28.09f, 4.60f)
            ));

            zones.Add(new SpawnZone(
                MapNames.Polus,
                new Vector2(16.34f, -18.22f),
                new Vector2(23.17f, -15.91f)
            ));

            zones.Add(new SpawnZone(
                MapNames.Airship,
                new Vector2(-0.66f, -0.50f),
                Distance(new Vector2(-0.66f, -0.50f), new Vector2(1.09f, -0.50f)),
                "Motor"
            ));

            zones.Add(new SpawnZone(
                MapNames.Airship,
                new Vector2(-7.00f, -11.50f),
                Distance(new Vector2(-7.00f, -11.50f), new Vector2(-5.54f, -11.50f)),
                "Cucina"
            ));

            zones.Add(new SpawnZone(
                MapNames.Airship,
                new Vector2(33.50f, -1.50f),
                Distance(new Vector2(33.50f, -1.50f), new Vector2(34.67f, -1.50f)),
                "Storage"
            ));

            zones.Add(new SpawnZone(
                MapNames.Airship,
                new Vector2(20.00f, 9.48f),
                Distance(new Vector2(20.00f, 9.48f), new Vector2(21.80f, 11.27f)),
                "Archivio"
            ));

            zones.Add(new SpawnZone(
                MapNames.Airship,
                new Vector2(-0.70f, 8.50f),
                Distance(new Vector2(-0.70f, 8.50f), new Vector2(1.34f, 8.50f)),
                "Celle"
            ));

            zones.Add(new SpawnZone(
                MapNames.Fungle,
                new Vector2(-0.94f, 1.29f),
                Distance(new Vector2(-0.94f, 1.29f), new Vector2(-2.47f, 2.52f)),
                "Meeting"
            ));

            zones.Add(new SpawnZone(
                MapNames.Fungle,
                new Vector2(-9.85f, 1.81f),
                Distance(new Vector2(-9.85f, 1.81f), new Vector2(-7.76f, 3.02f)),
                "Bonfire"
            ));
        }

        private float Distance(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        public List<SpawnZone> GetZonesForMap(MapNames mapID)
        {
            return zones.FindAll(z => z.MapID == mapID);
        }

        public bool IsPlayerInAnyZone(Vector2 playerPosition, MapNames currentMap)
        {
            foreach (var zone in GetZonesForMap(currentMap))
            {
                if (zone.Contains(playerPosition))
                    return true;
            }
            return false;
        }

        public SpawnZone GetCurrentZone(Vector2 playerPosition, MapNames currentMap)
        {
            foreach (var zone in GetZonesForMap(currentMap))
            {
                if (zone.Contains(playerPosition))
                    return zone;
            }
            return null;
        }
    }
}
