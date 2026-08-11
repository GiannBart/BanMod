//credits and licenses in the resources folder
using System;
using HarmonyLib;
using Hazel;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    internal static class CustomHatSyncRpcPatches
    {
        [HarmonyPrefix]
        private static bool HandleRpcPrefix(PlayerControl __instance, byte callId, MessageReader reader)
        {
            if (callId != CustomHatSync.RpcId)
                return true;

            try
            {
                CustomHatSync.ReceiveRpc(reader, __instance);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSyncRpcPatches.HandleRpcPrefix failed: " + ex);
            }

            return false;
        }
    }
}
