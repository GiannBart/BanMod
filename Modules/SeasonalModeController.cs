using UnityEngine;

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
public static class DleksEhtController
{
    private static SkeldShipStatus _lastSkeld;
    private static bool _lastEnabled;
    private static bool _initializedForCurrentSkeld;

    public static void Update()
    {
        bool enabled = BanMod.DleksEht != null && BanMod.DleksEht.Value;

        SkeldShipStatus skeld = Object.FindObjectOfType<SkeldShipStatus>();

        if (skeld == null)
        {
            _lastSkeld = null;
            _initializedForCurrentSkeld = false;
            _lastEnabled = enabled;
            return;
        }

        if (_lastSkeld != skeld ||
            !_initializedForCurrentSkeld ||
            _lastEnabled != enabled)
        {
            Apply(skeld, enabled);

            _lastSkeld = skeld;
            _lastEnabled = enabled;
            _initializedForCurrentSkeld = true;
        }
    }

    public static void ApplyNow()
    {
        bool enabled = BanMod.DleksEht != null && BanMod.DleksEht.Value;
        SkeldShipStatus skeld = Object.FindObjectOfType<SkeldShipStatus>();

        if (skeld == null)
            return;

        Apply(skeld, enabled);
        _lastSkeld = skeld;
        _lastEnabled = enabled;
        _initializedForCurrentSkeld = true;
    }

    private static void Apply(SkeldShipStatus skeld, bool enabled)
    {
        Transform t = skeld.transform;
        Vector3 scale = t.localScale;

        float x = Mathf.Abs(scale.x);
        scale.x = enabled ? -x : x;

        t.localScale = scale;

        BanMod.PluginLogger?.LogInfo(
            $"[Dleks eht] {(enabled ? "ON" : "OFF")} - scaleX={scale.x}"
        );
    }
}
