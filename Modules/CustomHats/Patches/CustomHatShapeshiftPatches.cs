//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BanMod.Modules.CustomHats.Patches
{
    [HarmonyPatch]
    internal static class CustomHatShapeshiftPatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (MethodInfo method in typeof(PlayerControl).GetMethods(flags))
            {
                if (method == null)
                    continue;

                string name = method.Name ?? string.Empty;

                if (name.IndexOf("Shapeshift", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (name.StartsWith("Cmd", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Check", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Can", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("RpcReject", StringComparison.OrdinalIgnoreCase))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length < 2)
                    continue;

                if (parameters[0].ParameterType != typeof(PlayerControl))
                    continue;

                if (parameters[1].ParameterType != typeof(bool))
                    continue;

                yield return method;
            }
        }

        [HarmonyPostfix]
        private static void ShapeshiftAppliedPostfix(PlayerControl __instance, PlayerControl __0, bool __1)
        {
            try
            {
                if (__instance == null)
                    return;

                PlayerControl target = __0;
                CustomHatSync.SetShapeshiftTarget(__instance, target);
                ClearBodyCustomRender(__instance);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] ShapeshiftAppliedPostfix failed: " + ex);
            }
        }

        private static void ClearBodyCustomRender(PlayerControl player)
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
                    if (parents[i] != null)
                        HatParentPatches.ClearCustomRender(parents[i]);
                }
            }
            catch
            {
            }
        }
    }
}
