//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch]
    internal static class PoolablePlayerOutfitPatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            MethodBase updateEither = typeof(PoolablePlayer)
                .GetMethods(flags)
                .FirstOrDefault(m =>
                    m.Name == "UpdateFromEitherPlayerDataOrCache" &&
                    m.GetParameters().Length >= 1 &&
                    m.GetParameters()[0].ParameterType == typeof(NetworkedPlayerInfo));

            if (updateEither != null)
                yield return updateEither;

            MethodBase updateData = typeof(PoolablePlayer)
                .GetMethods(flags)
                .FirstOrDefault(m =>
                    m.Name == "UpdateFromPlayerData" &&
                    m.GetParameters().Length >= 1 &&
                    m.GetParameters()[0].ParameterType == typeof(NetworkedPlayerInfo));

            if (updateData != null)
                yield return updateData;
        }

        [HarmonyPostfix]
        private static void UpdateFromPlayerInfoPostfix(PoolablePlayer __instance, object __0)
        {
            try
            {
                if (__instance == null || __0 == null)
                    return;

                NetworkedPlayerInfo playerInfo = __0 as NetworkedPlayerInfo;
                if (playerInfo == null)
                    return;

                if (!CustomHatSync.TryResolveRealHatId(playerInfo, out string hatId) ||
                    !CustomHatManager.TryGetViewData(hatId, out HatViewData viewData))
                {
                    PoolablePlayerPatches.ClearAllHatParents(__instance);

                    if (playerInfo.DefaultOutfit != null)
                    {
                        __instance.cosmetics.SetHat(playerInfo.DefaultOutfit.HatId, playerInfo.DefaultOutfit.ColorId);
                    }
                    return;
                }

                HatData hat = FindCustomHat(hatId);
                if (hat == null)
                    return;

                int colorId = 0;
                try
                {
                    if (playerInfo.DefaultOutfit != null)
                        colorId = playerInfo.DefaultOutfit.ColorId;
                }
                catch
                {
                    colorId = __instance.ColorId;
                }

                PoolablePlayerPatches.ApplyToAllHatParents(__instance, hat, viewData, colorId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] PoolablePlayer player-info postfix failed: " + ex);
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
