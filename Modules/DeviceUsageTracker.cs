//credits and licenses in the resources folder
using System.Collections.Generic;
using UnityEngine;

namespace BanMod;

public static class DeviceUsageTracker
{
    public enum Device
    {
        Admin,
        Vitals,
        DoorLog,
        Camera
    }

    private static readonly Dictionary<byte, HashSet<Device>> playersNearDevices = new();
    private static int updateCounter = 0;
    private static float lastUpdateFixedTime = float.MinValue;
    private static bool isRunning;

    public static bool IsNeeded =>
        !BanMod.IsBanModDisabled &&
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.AmHost &&
        GameStates.IsInGameplay &&
        Options.EnableDetector != null &&
        Options.EnableDetector.GetBool();

    public static void AddDeviceUsage(byte playerId, Device device)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!playersNearDevices.TryGetValue(playerId, out var devices))
        {
            devices = new HashSet<Device>();
            playersNearDevices[playerId] = devices;
        }

        devices.Add(device);
    }

    public static void ClearDeviceUsage(byte playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        playersNearDevices.Remove(playerId);
    }

    public static void ResetAll()
    {
        playersNearDevices.Clear();
        updateCounter = 0;
        lastUpdateFixedTime = float.MinValue;
        isRunning = false;
    }

    public static void StopIfUnused()
    {
        if (isRunning || playersNearDevices.Count > 0)
            ResetAll();
    }

    public static bool IsUsingDevice(byte playerId, Device device) =>
        playersNearDevices.TryGetValue(playerId, out var devices) && devices.Contains(device);

    public static bool IsUsingAnyDevice(byte playerId) => playersNearDevices.ContainsKey(playerId);

    public static HashSet<Device> GetUsedDevices(byte playerId) =>
        playersNearDevices.TryGetValue(playerId, out var devices) ? new HashSet<Device>(devices) : new HashSet<Device>();

    public static void UpdateUsage()
    {
        if (!IsNeeded)
        {
            StopIfUnused();
            return;
        }

        // PlayerControl.FixedUpdate is executed once for every player. This
        // guard makes the device scan run only once per physics step.
        if (Mathf.Approximately(lastUpdateFixedTime, Time.fixedTime))
            return;

        lastUpdateFixedTime = Time.fixedTime;
        isRunning = true;

        updateCounter--;
        if (updateCounter > 0) return;
        updateCounter = 5;

        playersNearDevices.Clear();

        float usableDistance = 1.0f;
        MapNames currentMap = Utils.GetCurrentMap();

        foreach (var pc in BanMod.AllAlivePlayerControls)
        {
            if (pc == null || pc.Data == null) continue;
            if (pc.inVent) continue;
            if (DetectorPlayerExclusions.ShouldIgnore(pc)) continue;

            Vector2 pos = pc.Pos();
            byte id = pc.PlayerId;

            switch (currentMap)
            {
                case MapNames.Skeld:
                    TryAdd(id, pos, "SkeldAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "SkeldCamera", Device.Camera, usableDistance);
                    break;

                case MapNames.MiraHQ:
                    TryAdd(id, pos, "MiraHQAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "MiraHQDoorLog", Device.DoorLog, usableDistance);
                    break;

                case MapNames.Polus:
                    TryAdd(id, pos, "PolusLeftAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "PolusRightAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "PolusCamera", Device.Camera, usableDistance);
                    TryAdd(id, pos, "PolusVital", Device.Vitals, usableDistance);
                    break;

                case MapNames.Airship:
                    TryAdd(id, pos, "AirshipCockpitAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "AirshipRecordsAdmin", Device.Admin, usableDistance);
                    TryAdd(id, pos, "AirshipCamera", Device.Camera, usableDistance);
                    TryAdd(id, pos, "AirshipVital", Device.Vitals, usableDistance);
                    break;

                case MapNames.Fungle:
                    TryAdd(id, pos, "FungleCamera", Device.Camera, usableDistance);
                    TryAdd(id, pos, "FungleVital", Device.Vitals, usableDistance);
                    break;
            }
        }
    }

    private static void TryAdd(byte id, Vector2 playerPos, string deviceKey, Device device, float range)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Utils.DevicePos.TryGetValue(deviceKey, out var devicePos)) return;
        if ((playerPos - devicePos).sqrMagnitude <= range * range) AddDeviceUsage(id, device);
    }
}


