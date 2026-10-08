using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BanMod;

public static class SeasonalModeController
{
    private static bool _changing;

    public static void EnableHalloween()
    {
        if (_changing) return;

        _changing = true;
        try
        {
            if (BanMod.HalloweenDecorations.Value)
                BanMod.AnniversaryDecorations.Value = false;
        }
        finally
        {
            _changing = false;
        }
    }

    public static void EnableAnniversary()
    {
        if (_changing) return;

        _changing = true;
        try
        {
            if (BanMod.AnniversaryDecorations.Value)
                BanMod.HalloweenDecorations.Value = false;
        }
        finally
        {
            _changing = false;
        }
    }

    public static void Normalize()
    {
        if (_changing) return;

        _changing = true;
        try
        {
            if (BanMod.AnniversaryDecorations.Value &&
                BanMod.HalloweenDecorations.Value)
            {
                BanMod.HalloweenDecorations.Value = false;
            }
        }
        finally
        {
            _changing = false;
        }
    }
}
