using System.Linq;
using HarmonyLib;
using Hazel;

namespace BanMod;

public static class VentRestriction
{
    public static bool ShouldBlock(
        PlayerControl player)
    {
        if (Options.GameMode.GetValue() != 0)
            return false;

        if (player?.Data?.Role == null)
            return false;

        if (player.Data.IsDead)
            return false;

        return player.Data.Role.IsImpostor;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class DisableImpostorVentCanUsePatch
{
    public static bool Prefix(
        NetworkedPlayerInfo pc,
        ref bool canUse,
        ref bool couldUse,
        ref float __result)
    {
        PlayerControl player = pc?.Object;

        if (!VentRestriction.ShouldBlock(player))
            return true;

        canUse = false;
        couldUse = false;
        __result = float.MaxValue;

        return false;
    }
}

[HarmonyPatch(
    typeof(PlayerPhysics),
    nameof(PlayerPhysics.HandleRpc)
)]
public static class DisableImpostorVentRpcPatch
{
    private const byte EnterVentRpcId = 19;

    public static bool Prefix(
        PlayerPhysics __instance,
        byte callId,
        MessageReader reader)
    {
        if (callId != EnterVentRpcId)
            return true;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        if (__instance == null || reader == null)
            return true;

        PlayerControl player =
            __instance.GetComponent<PlayerControl>();

        if (!VentRestriction.ShouldBlock(player))
            return true;

        int ventId;

        try
        {
            ventId = reader.ReadPackedInt32();
        }
        catch
        {
            return false;
        }

        bool validVent =
            ShipStatus.Instance != null &&
            ShipStatus.Instance.AllVents != null &&
            ShipStatus.Instance.AllVents.Any(
                vent =>
                    vent != null &&
                    vent.Id == ventId
            );

        if (validVent)
        {
            __instance.RpcBootFromVent(ventId);
        }

        return false;
    }
}