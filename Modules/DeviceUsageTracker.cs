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
    private static readonly Dictionary<string, List<byte>> devicesNearPlayers = new();
    private static int updateCounter = 0;

    public static void AddDeviceUsage(byte playerId, Device device)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!playersNearDevices.TryGetValue(playerId, out var devices))
        {
            devices = new HashSet<Device>();
            playersNearDevices[playerId] = devices;
        }

        devices.Add(device);

        string deviceKey = device.ToString();
        if (!devicesNearPlayers.ContainsKey(deviceKey))
            devicesNearPlayers[deviceKey] = new List<byte>();

        if (!devicesNearPlayers[deviceKey].Contains(playerId))
            devicesNearPlayers[deviceKey].Add(playerId);
    }

    public static void ClearDeviceUsage(byte playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        playersNearDevices.Remove(playerId);

        foreach (var deviceKey in new List<string>(devicesNearPlayers.Keys))
            devicesNearPlayers[deviceKey].Remove(playerId);
    }

    public static void ResetAll()
    {
        playersNearDevices.Clear();
        devicesNearPlayers.Clear();
    }

    public static bool IsUsingDevice(byte playerId, Device device) =>
        playersNearDevices.TryGetValue(playerId, out var devices) && devices.Contains(device);

    public static bool IsUsingAnyDevice(byte playerId) => playersNearDevices.ContainsKey(playerId);

    public static HashSet<Device> GetUsedDevices(byte playerId) =>
        playersNearDevices.TryGetValue(playerId, out var devices) ? new HashSet<Device>(devices) : new HashSet<Device>();

    public static void UpdateUsage()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        updateCounter--;
        if (updateCounter > 0) return;
        updateCounter = 5;

        ResetAll();

        float usableDistance = 1.0f;

        foreach (var pc in BanMod.AllAlivePlayerControls)
        {
            if (pc == null || pc.Data == null) continue;
            if (pc.inVent) continue;

            Vector2 pos = pc.Pos();
            byte id = pc.PlayerId;

            TryAdd(id, pos, "SkeldAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "SkeldCamera", Device.Camera, usableDistance);
            TryAdd(id, pos, "MiraHQAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "MiraHQDoorLog", Device.DoorLog, usableDistance);
            TryAdd(id, pos, "PolusLeftAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "PolusRightAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "PolusCamera", Device.Camera, usableDistance);
            TryAdd(id, pos, "PolusVital", Device.Vitals, usableDistance);
            TryAdd(id, pos, "AirshipCockpitAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "AirshipRecordsAdmin", Device.Admin, usableDistance);
            TryAdd(id, pos, "AirshipCamera", Device.Camera, usableDistance);
            TryAdd(id, pos, "AirshipVital", Device.Vitals, usableDistance);
            TryAdd(id, pos, "FungleCamera", Device.Camera, usableDistance);
            TryAdd(id, pos, "FungleVital", Device.Vitals, usableDistance);
        }
    }

    private static void TryAdd(byte id, Vector2 playerPos, string deviceKey, Device device, float range)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Utils.DevicePos.TryGetValue(deviceKey, out var devicePos)) return;
        if (Vector2.Distance(playerPos, devicePos) <= range) AddDeviceUsage(id, device);
    }
}


