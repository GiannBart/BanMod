//credits and licenses in the resources folder
using System;
using HarmonyLib;
using UnityEngine;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch]
    internal static class PoolablePlayerPatches
    {
        [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.SetHat), typeof(string), typeof(int))]
        [HarmonyPrefix]
        private static bool SetHatStringPrefix(PoolablePlayer __instance, string __0, int __1)
        {
            try
            {
                string hatId = __0;
                int color = __1;

                if (__instance == null) return true;

                if (string.IsNullOrEmpty(hatId) || !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(__instance);
                    return true;
                }

                HatData hat = FindCustomHat(hatId);
                if (hat == null) return true;

                ApplyToAllHatParents(__instance, hat, viewData, color);

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] PoolablePlayer.SetHat(string,int) prefix failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.SetHat), typeof(HatData), typeof(int))]
        [HarmonyPrefix]
        private static bool SetHatDataPrefix(PoolablePlayer __instance, HatData __0, int __1)
        {
            try
            {
                HatData hat = __0;
                int color = __1;

                if (__instance == null) return true;

                if (hat == null || !CustomHatManager.TryGetViewData(hat, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(__instance);
                    return true;
                }

                ApplyToAllHatParents(__instance, hat, viewData, color);

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] PoolablePlayer.SetHat(HatData,int) prefix failed: " + ex);
                return true;
            }
        }
        public static void ApplyToAllHatParents(PoolablePlayer player, HatData hat, HatViewData viewData, int color)
        {
            if (player == null || hat == null || viewData == null)
                return;

            HatParent[] parents = player.GetComponentsInChildren<HatParent>(true);

            for (int i = 0; i < parents.Length; i++)
            {
                HatParentPatches.ForceSticky(parents[i], hat, viewData, color);
            }
        }

        public static void ClearAllHatParents(PoolablePlayer player)
        {
            try
            {
                if (player == null)
                    return;

                HatParent[] parents = player.GetComponentsInChildren<HatParent>(true);

                if (parents == null)
                    return;

                for (int i = 0; i < parents.Length; i++)
                {
                    HatParentPatches.ClearSticky(parents[i]);
                }
            }
            catch
            {
            }
        }

        private static HatData FindCustomHat(string hatId)
        {
            for (int i = 0; i < CustomHatManager.RegisteredHats.Count; i++)
            {
                HatData candidate = CustomHatManager.RegisteredHats[i];
                if (candidate != null && candidate.ProdId == hatId)
                    return candidate;
            }

            return null;
        }
    }
}
