//credits and licenses in the resources folder
using System;
using HarmonyLib;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(HatData))]
    internal static class HatDataPatches
    {
        [HarmonyPatch(nameof(HatData.PreviewOnPlayer))]
        [HarmonyPrefix]
        private static bool PreviewOnPlayerPrefix(HatData __instance, PoolablePlayer p, int colorId, string resetIgnoreType)
        {
            try
            {
                if (__instance == null || p == null)
                    return true;

                if (!CustomHatManager.TryGetViewData(__instance, out HatViewData viewData))
                    return true;

                try
                {
                    p.ResetCosmetics(resetIgnoreType);
                }
                catch
                {
                    try { p.ResetCosmetics(""); } catch { }
                }

                PoolablePlayerPatches.ApplyToAllHatParents(p, __instance, viewData, colorId);
                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] HatData.PreviewOnPlayer prefix failed: " + ex);
                return true;
            }
        }
    }
}
