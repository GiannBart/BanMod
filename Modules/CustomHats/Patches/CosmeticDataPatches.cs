//credits and licenses in the resources folder
using AmongUs.Data;
using HarmonyLib;
using Innersloth.Assets;
using System;
using UnityEngine;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(CosmeticData))]
    internal static class CosmeticDataPatches
    {
        [HarmonyPatch(nameof(CosmeticData.SetPreview))]
        [HarmonyPrefix]
        private static bool SetPreviewPrefix(CosmeticData __instance, SpriteRenderer renderer, int color)
        {
            try
            {
                HatData hat = __instance as HatData;
                if (hat == null) return true;

                string prodId = null;
                try { prodId = hat.ProdId; } catch { }

                if (string.IsNullOrEmpty(prodId))
                    return true;

                if (!CustomHatManager.ViewDataCache.TryGetValue(prodId, out HatViewData viewData))
                    return true;

                if (renderer == null) return false;

                HatViewData coloredView = CustomHatManager.GetViewDataForColor(prodId, viewData, color);
                coloredView = CustomHatManager.GetAdaptiveViewData(prodId, coloredView, color);

                Sprite previewSprite = CustomHatInventoryPreview350.GetPreviewSprite(prodId + "|setPreview_" + color, coloredView.MainImage);

                renderer.sprite = previewSprite;
                renderer.enabled = previewSprite != null;
                renderer.gameObject.SetActive(previewSprite != null);
                renderer.transform.localScale = Vector3.one;

                if (DestroyableSingleton<HatManager>.Instance != null)
                    renderer.sharedMaterial = DestroyableSingleton<HatManager>.Instance.DefaultShader;
                renderer.color = Color.white;

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CosmeticData.SetPreview patch failed: " + ex);
                return true;
            }
        }

        [HarmonyPatch(nameof(CosmeticData.CoLoadPreview))]
        [HarmonyPrefix]
        private static bool CoLoadPreviewPrefix(CosmeticData __instance, Action<Sprite, AddressableAsset> onLoaded)
        {
            try
            {
                HatData hat = __instance as HatData;
                if (hat == null) return true;

                string prodId = null;
                try { prodId = hat.ProdId; } catch { }

                if (string.IsNullOrEmpty(prodId))
                    return true;

                if (!CustomHatManager.TryGetViewData(prodId, out HatViewData viewData))
                    return true;

                int currentColor = 0;
                try
                {
                    if (DataManager.Player != null && DataManager.Player.Customization != null)
                        currentColor = (int)DataManager.Player.Customization.Color;
                }
                catch { }

                HatViewData coloredView = CustomHatManager.GetViewDataForColor(prodId, viewData, currentColor);
                coloredView = CustomHatManager.GetAdaptiveViewData(prodId, coloredView, currentColor);

                Sprite previewSprite = CustomHatInventoryPreview350.GetPreviewSprite(prodId + "|coLoadPreview_" + currentColor, coloredView.MainImage);
                onLoaded?.Invoke(previewSprite, null);

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CosmeticData.CoLoadPreview patch failed: " + ex);
                return true;
            }
        }
    }
}