//credits and licenses in the resources folder
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(HatManager))]
    internal static class HatManagerPatches
    {
        private static bool created;

        [HarmonyPatch(nameof(HatManager.Initialize))]
        [HarmonyPostfix]
        private static void InitializePostfix(HatManager __instance)
        {
            EnsureCustomHatsCreated();
        }

        [HarmonyPatch(nameof(HatManager.GetUnlockedHats))]
        [HarmonyPostfix]
        private static void GetUnlockedHatsPostfix(ref Il2CppReferenceArray<HatData> __result)
        {
            try
            {
                EnsureCustomHatsCreated();

                bool customSkinEnabled = Options.CustomSkin.GetBool();

                List<HatData> noHat = new List<HatData>();
                List<HatData> originalHats = new List<HatData>();
                List<HatData> customHats = new List<HatData>();

                if (__result != null)
                {
                    for (int i = 0; i < __result.Length; i++)
                    {
                        HatData hat = __result[i];
                        if (hat == null) continue;

                        if (IsNoHat(hat))
                        {
                            if (!ContainsHatByProdId(noHat, hat.ProdId)) noHat.Add(hat);
                        }
                        else if (CustomHatManager.IsCustomHat(hat))
                        {
                            if (customSkinEnabled && !ContainsHatByProdId(customHats, hat.ProdId)) customHats.Add(hat);
                        }
                        else
                        {
                            if (!ContainsHatByProdId(originalHats, hat.ProdId)) originalHats.Add(hat);
                        }
                    }
                }

                for (int i = 0; i < CustomHatManager.RegisteredHats.Count; i++)
                {
                    HatData customHat = CustomHatManager.RegisteredHats[i];
                    if (customSkinEnabled && customHat != null && !ContainsHatByProdId(customHats, customHat.ProdId))
                        customHats.Add(customHat);
                }

                List<HatData> ordered = new List<HatData>();

                for (int i = 0; i < noHat.Count; i++) ordered.Add(noHat[i]);
                for (int i = 0; i < customHats.Count; i++) ordered.Add(customHats[i]);
                for (int i = 0; i < originalHats.Count; i++) ordered.Add(originalHats[i]);

                __result = new Il2CppReferenceArray<HatData>(ordered.ToArray());
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] GetUnlockedHatsPostfix failed: " + ex);
            }
        }

        [HarmonyPatch(nameof(HatManager.GetHatById))]
        [HarmonyPostfix]
        private static void GetHatByIdPostfix(string hatId, ref HatData __result)
        {
            try
            {
                EnsureCustomHatsCreated();

                if (string.IsNullOrEmpty(hatId))
                    return;

                for (int i = 0; i < CustomHatManager.RegisteredHats.Count; i++)
                {
                    HatData customHat = CustomHatManager.RegisteredHats[i];

                    if (customHat != null && customHat.ProdId == hatId)
                    {
                        __result = customHat;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] GetHatByIdPostfix failed: " + ex);
            }
        }

        private static void EnsureCustomHatsCreated()
        {
            if (created) return;
            created = true;
            try
            {
                for (int i = 0; i < CustomHatManager.PendingHats.Count; i++)
                {
                    CustomHat pendingHat = CustomHatManager.PendingHats[i];
                    if (pendingHat == null || string.IsNullOrEmpty(pendingHat.ProductId)) continue;
                    if (ContainsHatByProdId(CustomHatManager.RegisteredHats, pendingHat.ProductId)) continue;

                    HatData hatData = CustomHatManager.CreateHatBehaviour(pendingHat);
                    if (hatData == null) continue;

                    hatData.Free = true;
                    hatData.BundleId = "";
                    hatData.NotInStore = true;
                    hatData.StoreName = "Modded";

                    CustomHatManager.RegisteredHats.Add(hatData);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] EnsureCustomHatsCreated failed: " + ex);
            }
        }

        private static bool IsNoHat(HatData hat)
        {
            try { return hat != null && (hat.IsEmpty || hat.ProdId == "hat_NoHat" || hat.ProductId == "hat_NoHat"); }
            catch { return false; }
        }

        private static bool ContainsHatByProdId(List<HatData> hats, string prodId)
        {
            if (string.IsNullOrEmpty(prodId)) return false;
            for (int i = 0; i < hats.Count; i++) { if (hats[i] != null && hats[i].ProdId == prodId) return true; }
            return false;
        }
    }
}