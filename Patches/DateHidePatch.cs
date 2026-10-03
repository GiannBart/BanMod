using HarmonyLib;

namespace BanMod;


[HarmonyPatch(typeof(DateHide), nameof(DateHide.ShouldHide))]
public static class DateHidePatch
{
    public static bool Prefix(DateHide __instance, ref bool __result)
    {
        if (ContainsDate(__instance, 6, 15))
        {
            __result = !BanMod.AnniversaryDecorations.Value;
            return false;
        }

        if (ContainsDate(__instance, 10, 31))
        {
            __result = !BanMod.HalloweenDecorations.Value;
            return false;
        }

        return true;
    }

    private static bool ContainsDate(DateHide dateHide, int month, int day)
    {
        int target = month * 100 + day;
        int start = dateHide.MonthStart * 100 + dateHide.DayStart;
        int end = dateHide.MonthEnd * 100 + dateHide.DayEnd;

        if (start <= end)
            return target >= start && target <= end;

        return target >= start || target <= end;
    }
}
