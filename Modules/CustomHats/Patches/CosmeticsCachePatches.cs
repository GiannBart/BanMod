//credits and licenses in the resources folder
using System;
using HarmonyLib;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(CosmeticsCache))]
    internal static class CosmeticsCachePatches
    {
        [HarmonyPatch(nameof(CosmeticsCache.GetHat))]
        [HarmonyPrefix]
        private static bool GetHatPrefix(string id, ref HatViewData __result)
        {
            try
            {
                if (!string.IsNullOrEmpty(id) && CustomHatManager.ViewDataCache.TryGetValue(id, out HatViewData viewData))
                {
                    __result = viewData;
                    return false;
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CosmeticsCache.GetHat patch failed: " + ex);
            }

            return true;
        }
    }
}
