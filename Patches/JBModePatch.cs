//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;

namespace BanMod;

[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
public static class SabotageSystemType_UpdateSystem_Patch
{
    private static readonly HashSet<byte> ForcedCrewmateGhosts = new HashSet<byte>();

    public static bool Prefix(PlayerControl player, MessageReader msgReader)
    {
        if ((GameModeType)Options.GameMode.GetValue() != GameModeType.JBMode)
            return true;

        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (player == null || player.Data == null || player.Data.Role == null)
            return false;

        if (player.Data.IsDead && player.Data.Role.IsImpostor)
        {
            if (msgReader != null && msgReader.BytesRemaining > 0)
                msgReader.ReadByte();

            ForcedCrewmateGhosts.Add(player.PlayerId);

            player.RpcSetRole(RoleTypes.CrewmateGhost);

            return false;
        }

        return true;
    }

    public static void RestoreImpostorGhosts()
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null)
                continue;

            if (!ForcedCrewmateGhosts.Contains(pc.PlayerId))
                continue;

            pc.RpcSetRole(RoleTypes.ImpostorGhost);
        }
    }

    public static void Clear()
    {
        ForcedCrewmateGhosts.Clear();
    }
}

[HarmonyPatch(
    typeof(SecurityCameraSystemType),
    nameof(SecurityCameraSystemType.UpdateSystem))]
public static class SecurityCameraSystemType_UpdateSystem_Patch
{
    public static bool Prefix(
        [HarmonyArgument(1)] MessageReader msgReader)
    {
        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (!VanillaDeviceBlocker.ShouldBlockCameras())
            return true;

        if (msgReader == null ||
            msgReader.BytesRemaining <= 0)
        {
            return true;
        }

        MessageReader readerCopy =
            MessageReader.Get(msgReader);

        byte operation = readerCopy.ReadByte();

        readerCopy.Recycle();

        if (operation ==
            SecurityCameraSystemType.IncrementOp)
        {
            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public static class LogicGameFlowNormal_CheckEndCriteria_Patch
{
    public static void Prefix()
    {
        if ((GameModeType)Options.GameMode.GetValue() != GameModeType.JBMode)
            return;

        if (!AmongUsClient.Instance.AmHost)
            return;

        SabotageSystemType_UpdateSystem_Patch.RestoreImpostorGhosts();
    }
}
