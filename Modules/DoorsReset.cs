//credits and licenses in the resources folder
namespace BanMod;

public static class DoorsReset
{
    private static bool isEnabled = Options.ResetDoorsEveryTurns.GetBool();
    private static DoorsSystemType DoorsSystem => ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Doors, out var system) ? system.TryCast<DoorsSystemType>() : null;

    public static void ResetDoors()
    {
        var action = Options.DoorsResetMode.GetValue();
        byte mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;

        if (mapId == 0 || mapId == 1 || mapId == 3)
        {
            return;
        }
        if (!isEnabled || DoorsSystem == null)
        {
            return;
        }
        BMLogger.Info("Reset", "Reset Doors");

        if (action == 0)
        {
            OpenAllDoors();
            BMLogger.Info("OpenAllDoors");
        }
        if (action == 1)
        {
            CloseAllDoors();
            BMLogger.Info("CloseAllDoors");
        }
    }
    public static void OpenAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, true);
        }
        DoorsSystem.IsDirty = true;
    }
    public static void CloseAllDoors()
    {
        foreach (var door in ShipStatus.Instance.AllDoors)
        {
            SetDoorOpenState(door, false);
        }
        DoorsSystem.IsDirty = true;
    }

    private static void SetDoorOpenState(OpenableDoor door, bool isOpen)
    {
        if (IsValidDoor(door))
        {
            door.SetDoorway(isOpen);
        }
    }
    private static bool IsValidDoor(OpenableDoor door)
    {
        return door.Room is not (SystemTypes.Lounge or SystemTypes.Decontamination);
    }
}
