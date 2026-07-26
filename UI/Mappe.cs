//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BanMod
{
    public static class FakeMapLobbyUtility
    {
        private static LobbyBehaviour cachedLobby;
        private static ShipStatus spawnedShip;
        public static bool active;
        private static bool loading;

        public static bool Active => active;
        public static bool Loading => loading;
        public static ShipStatus CurrentShip => spawnedShip;

        public static void Enable(int mapId)
        {
            if (active || loading) return;
            if (AmongUsClient.Instance == null) return;
            if (!AmongUsClient.Instance.AmHost) return;

            var client = DestroyableSingleton<AmongUsClient>.Instance;
            if (client == null) return;
            if (client.ShipPrefabs == null) return;
            if (mapId < 0 || mapId >= client.ShipPrefabs.Count) return;

            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud == null) return;

            hud.StartCoroutine(CoEnable(mapId));
        }

        public static void Disable()
        {
            if (loading) return;
            if (!active) return;
            if (AmongUsClient.Instance == null) return;
            if (!AmongUsClient.Instance.AmHost) return;

            if (spawnedShip != null)
            {
                (spawnedShip as InnerNetObject)?.Despawn();
                Object.Destroy(spawnedShip.gameObject);
                spawnedShip = null;
                ShipStatus.Instance = null;
            }

            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.NetTransform != null)
            {
                PlayerControl.LocalPlayer.NetTransform.SnapTo(new Vector2(0f, 0f));
            }

            cachedLobby = null;
            active = false;
            loading = false;
        }

        public static void Toggle(int mapId)
        {
            if (active) Disable();
            else Enable(mapId);
        }

        private static IEnumerator CoEnable(int mapId)
        {
            loading = true;

            var client = DestroyableSingleton<AmongUsClient>.Instance;
            if (client == null || client.ShipPrefabs == null || mapId < 0 || mapId >= client.ShipPrefabs.Count)
            {
                loading = false;
                yield break;
            }

            var assetRef = client.ShipPrefabs[mapId];
            if (assetRef == null)
            {
                loading = false;
                yield break;
            }

            cachedLobby = LobbyBehaviour.Instance;

            if (cachedLobby != null)
            {
                (cachedLobby as InnerNetObject)?.Despawn();
                Object.Destroy(cachedLobby.gameObject);
                LobbyBehaviour.Instance = null;
                yield return null;
            }

            GameObject prefab = null;

            if (assetRef.Asset != null)
            {
                prefab = assetRef.Asset.TryCast<GameObject>();
            }
            else
            {
                AsyncOperationHandle<GameObject> handle = assetRef.LoadAssetAsync<GameObject>();

                while (!handle.IsDone)
                    yield return null;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    loading = false;
                    yield break;
                }

                prefab = handle.Result;
            }

            if (prefab == null)
            {
                loading = false;
                yield break;
            }

            var shipPrefab = prefab.GetComponent<ShipStatus>();
            if (shipPrefab == null)
            {
                loading = false;
                yield break;
            }

            spawnedShip = Object.Instantiate(shipPrefab);
            if (spawnedShip == null)
            {
                loading = false;
                yield break;
            }

            DisableFakeMapInteractions(spawnedShip);

            ShipStatus.Instance = spawnedShip;
            AmongUsClient.Instance.Spawn(spawnedShip, -2, SpawnFlags.None);

            if (GameData.Instance != null)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player != null)
                    {
                        ShipStatus.Instance.SpawnPlayer(player, 5, false);
                    }
                }
            }

            active = true;
            loading = false;
        }

        private static void DisableFakeMapInteractions(ShipStatus ship)
        {
            if (ship == null) return;

            if (ship.EmergencyButton != null)
            {
                try
                {
                    ship.BreakEmergencyButton();
                }
                catch { }

                ship.EmergencyButton.enabled = false;
                ship.EmergencyButton.gameObject.SetActive(false);
            }

            var airship = ship.TryCast<AirshipStatus>();
            if (airship != null)
            {
                if (airship.GapPlatform != null)
                {
                    airship.GapPlatform.enabled = false;
                    airship.GapPlatform.gameObject.SetActive(false);
                }

                var platformConsoles = ship.GetComponentsInChildren<PlatformConsole>(true);
                foreach (var console in platformConsoles)
                {
                    if (console == null) continue;
                    console.enabled = false;
                    console.gameObject.SetActive(false);
                }

                var movingPlatforms = ship.GetComponentsInChildren<MovingPlatformBehaviour>(true);
                foreach (var platform in movingPlatforms)
                {
                    if (platform == null) continue;
                    platform.enabled = false;
                    platform.gameObject.SetActive(false);
                }
            }

            var fungle = ship.TryCast<FungleShipStatus>();
            if (fungle != null)
            {
                if (fungle.Zipline != null)
                {
                    fungle.Zipline.enabled = false;
                    fungle.Zipline.gameObject.SetActive(false);
                }

                var ziplineConsoles = ship.GetComponentsInChildren<ZiplineConsole>(true);
                foreach (var console in ziplineConsoles)
                {
                    if (console == null) continue;
                    console.enabled = false;
                    console.gameObject.SetActive(false);
                }

                var ziplines = ship.GetComponentsInChildren<ZiplineBehaviour>(true);
                foreach (var zipline in ziplines)
                {
                    if (zipline == null) continue;
                    zipline.enabled = false;
                    zipline.gameObject.SetActive(false);
                }

                var mushrooms = ship.GetComponentsInChildren<Mushroom>(true);
                foreach (var mushroom in mushrooms)
                {
                    if (mushroom == null) continue;
                    mushroom.enabled = false;
                    mushroom.gameObject.SetActive(false);
                }

                var mushroomSabotages = ship.GetComponentsInChildren<MushroomMixupSabotageSystem>(true);
                foreach (var sabotage in mushroomSabotages)
                {
                    if (sabotage == null) continue;
                    sabotage.enabled = false;
                    sabotage.gameObject.SetActive(false);
                }
            }
        }

    }
}