//credits and licenses in the resources folder
using System;
using HarmonyLib;
using AmongUs.Data;
using UnityEngine;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch]
    internal static class ProgressionScreenPatches
    {
        [HarmonyPatch(typeof(ProgressionScreen), "Activate")]
        [HarmonyPostfix]
        private static void ActivatePostfix(ProgressionScreen __instance)
        {
            try
            {
                ApplyToProgressionScreen(__instance);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ProgressionScreen.Activate postfix failed: " + ex);
            }
        }

        [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.UpdateFromDataManager), typeof(PlayerMaterial.MaskType))]
        [HarmonyPostfix]
        private static void UpdateFromDataManagerPostfix(PoolablePlayer __instance)
        {
            ApplyLocalCustomHatIfProgressionChild(__instance);
        }

        [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.UpdateFromDataManager), typeof(PlayerMaterial.MaskType), typeof(int))]
        [HarmonyPostfix]
        private static void UpdateFromDataManagerColorPostfix(PoolablePlayer __instance)
        {
            ApplyLocalCustomHatIfProgressionChild(__instance);
        }

        [HarmonyPatch(typeof(PoolablePlayer), nameof(PoolablePlayer.UpdateFromPlayerOutfit))]
        [HarmonyPostfix]
        private static void UpdateFromPlayerOutfitPostfix(PoolablePlayer __instance, NetworkedPlayerInfo.PlayerOutfit outfit)
        {
            try
            {
                if (__instance == null || outfit == null)
                    return;

                string hatId = outfit.HatId;

                if (string.IsNullOrEmpty(hatId) || !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(__instance);

                    __instance.cosmetics.SetHat(hatId, outfit.ColorId);
                    return;
                }

                HatData hat = FindCustomHat(hatId);
                if (hat == null)
                    return;

                PoolablePlayerPatches.ApplyToAllHatParents(__instance, hat, viewData, outfit.ColorId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] PoolablePlayer.UpdateFromPlayerOutfit postfix failed: " + ex);
            }
        }

        private static void ApplyToProgressionScreen(ProgressionScreen screen)
        {
            try
            {
                if (screen == null)
                    return;

                PoolablePlayer[] poolables = screen.GetComponentsInChildren<PoolablePlayer>(true);
                if (poolables == null)
                    return;

                for (int i = 0; i < poolables.Length; i++)
                    ApplyLocalCustomHatToPoolable(poolables[i]);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ApplyToProgressionScreen failed: " + ex);
            }
        }

        private static void ApplyLocalCustomHatIfProgressionChild(PoolablePlayer poolable)
        {
            try
            {
                if (poolable == null)
                    return;

                if (!IsChildOfProgressionScreen(poolable))
                    return;

                ApplyLocalCustomHatToPoolable(poolable);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] Progression child custom hat patch failed: " + ex);
            }
        }

        private static bool IsChildOfProgressionScreen(PoolablePlayer poolable)
        {
            try
            {
                if (poolable == null)
                    return false;

                return poolable.GetComponentsInParent<ProgressionScreen>(true) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyLocalCustomHatToPoolable(PoolablePlayer poolable)
        {
            try
            {
                if (poolable == null)
                    return;

                string hatId = GetLocalCustomizationHatId();
                int colorId = GetLocalCustomizationColorId(poolable.ColorId);

                if (string.IsNullOrEmpty(hatId) ||
                    hatId == "hat_NoHat" ||
                    hatId == "missing" ||
                    !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(poolable);
                    return;
                }

                HatData hat = FindCustomHat(hatId);
                if (hat == null)
                {
                    PoolablePlayerPatches.ClearAllHatParents(poolable);
                    return;
                }

                PoolablePlayerPatches.ApplyToAllHatParents(poolable, hat, viewData, colorId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ApplyLocalCustomHatToPoolable failed: " + ex);
            }
        }

        private static string GetLocalCustomizationHatId()
        {
            try
            {
                if (DataManager.Player != null &&
                    DataManager.Player.Customization != null &&
                    !string.IsNullOrEmpty(DataManager.Player.Customization.Hat))
                {
                    return DataManager.Player.Customization.Hat;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static int GetLocalCustomizationColorId(int fallback)
        {
            try
            {
                if (DataManager.Player != null && DataManager.Player.Customization != null)
                    return (int)DataManager.Player.Customization.Color;
            }
            catch
            {
            }

            return fallback;
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
