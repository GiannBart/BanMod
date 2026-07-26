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

[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
public static class SecurityCameraSystemType_UpdateSystem_Patch
{
    private static bool triggeringAutoComms;
    private static bool fixingAutoComms;

    private static readonly HashSet<byte> PlayersAutoComms = new HashSet<byte>();

    private static bool commsCausedByCameras;

    public static bool Prefix(SecurityCameraSystemType __instance, PlayerControl player, MessageReader msgReader)
    {
        bool isJbMode = (GameModeType)Options.GameMode.GetValue() == GameModeType.JBMode;
        bool forceEnabled = Options.CameraCommsSabotage != null && Options.CameraCommsSabotage.GetBool();

        if (!isJbMode && !forceEnabled)
            return true;

        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (msgReader == null || msgReader.BytesRemaining <= 0)
            return false;

        byte op = msgReader.ReadByte();

        if (player == null)
            return false;

        if (op == SecurityCameraSystemType.IncrementOp)
        {
            PlayersAutoComms.Add(player.PlayerId);

            if (!triggeringAutoComms)
            {
                triggeringAutoComms = true;

                try
                {
                    if (!Utils.IsActive(SystemTypes.Comms))
                    {
                        commsCausedByCameras = true;
                        ShipStatus.Instance.UpdateSystem(SystemTypes.Comms, player, 128);
                    }
                    else
                    {
                        commsCausedByCameras = false;
                    }
                }
                finally
                {
                    triggeringAutoComms = false;
                }
            }

            return false;
        }

        if (op == SecurityCameraSystemType.DecrementOp)
        {
            PlayersAutoComms.Remove(player.PlayerId);

            if (PlayersAutoComms.Count <= 0 && commsCausedByCameras && !fixingAutoComms)
            {
                fixingAutoComms = true;

                try
                {
                    if (Utils.IsActive(SystemTypes.Comms))
                    {
                        ShipStatus_FixedUpdate_Patch.FixSabotage(
                            ShipStatus.Instance,
                            SystemTypes.Comms
                        );
                    }

                    commsCausedByCameras = false;
                }
                finally
                {
                    fixingAutoComms = false;
                }
            }

            return false;
        }

        return false;
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
