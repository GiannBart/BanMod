//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch(typeof(EndGameManager), "SetEverythingUp")]
    internal static class EndGameManagerPatches
    {
        [HarmonyPostfix]
        private static void SetEverythingUpPostfix(EndGameManager __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                PoolablePlayer[] poolables = __instance.GetComponentsInChildren<PoolablePlayer>(true);
                if (poolables == null || poolables.Length == 0)
                    return;

                if (EndGameResult.CachedWinners == null)
                    return;

                List<object> winners = new List<object>();
                foreach (object winner in EndGameResult.CachedWinners)
                    winners.Add(winner);

                winners = winners
                    .OrderBy(delegate (object winner)
                    {
                        try
                        {
                            Type type = winner.GetType();
                            object value = null;
                            System.Reflection.PropertyInfo prop = type.GetProperty("IsYou");
                            if (prop != null)
                                value = prop.GetValue(winner, null);
                            else
                            {
                                System.Reflection.FieldInfo field = type.GetField("IsYou");
                                if (field != null)
                                    value = field.GetValue(winner);
                            }
                            return value is bool b && b ? -1 : 0;
                        }
                        catch
                        {
                            return 0;
                        }
                    })
                    .ToList();

                int count = Math.Min(poolables.Length, winners.Count);
                for (int i = 0; i < count; i++)
                {
                    CustomHatSceneRenderer.ApplyToEndGamePoolablePlayerIfNeeded(poolables[i], winners[i]);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] EndGameManager.SetEverythingUp postfix failed: " + ex);
            }
        }
    }
}
