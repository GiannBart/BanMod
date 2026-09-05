//credits and licenses in the resources folder/
using System;
using System.Linq;
using System.Reflection;

namespace BanMod;

public static class OptionalPluginAvailability
{
    public static bool Ffa => BanModCore.IsAnyPluginLoaded(
        "ffa",
        "Ffa",
        "FFA",
        "freeforall",
        "free_for_all"
    );
}

public static class FfaExternalBridge
{
    private static Assembly FfaAssembly;

    private static FieldInfo EnabledField;
    private static FieldInfo MaxVentSecondsField;
    private static FieldInfo VentBootModeField;
    private static FieldInfo TeamsEnabledField;
    private static FieldInfo TeamCountField;

    private static FieldInfo HotPotatoEnabledField;
    private static FieldInfo HotPotatoExplosionSecondsField;
    private static FieldInfo HotPotatoFirstDelaySecondsField;

    private static Type VentBootModeEnumType;

    public static bool IsAvailable()
    {
        try
        {
            return OptionalPluginAvailability.Ffa && TryResolve();
        }
        catch
        {
            return false;
        }
    }

    private static bool TryResolve()
    {
        try
        {
            if (!OptionalPluginAvailability.Ffa)
            {
                ResetCache();
                return false;
            }

            if (FfaAssembly != null && EnabledField != null)
                return true;

            ResetCache();

            FfaAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly =>
                    assembly.GetType("FFA.FFAOptions", false) != null
                );

            if (FfaAssembly == null)
                return false;

            Type ffaOptionsType = FfaAssembly.GetType(
                "FFA.FFAOptions",
                false
            );

            Type ventOptionsType =
                FfaAssembly.GetType("FFA.FfaVentOptions", false) ??
                FfaAssembly.GetType(
                    "FFA.FfaVentLimitPatch+FfaVentOptions",
                    false
                );

            Type ventBootOptionsType = FfaAssembly.GetType(
                "FFA.FfaVentBootOptions",
                false
            );

            VentBootModeEnumType = FfaAssembly.GetType(
                "FFA.FfaVentBootMode",
                false
            );

            EnabledField = FindStaticField(
                ffaOptionsType,
                "Enabled"
            );

            TeamsEnabledField = FindStaticField(
                ffaOptionsType,
                "TeamsEnabled"
            );

            TeamCountField = FindStaticField(
                ffaOptionsType,
                "TeamCount"
            );

            HotPotatoEnabledField = FindStaticField(
                ffaOptionsType,
                "HotPotatoEnabled"
            );

            HotPotatoExplosionSecondsField = FindStaticField(
                ffaOptionsType,
                "HotPotatoExplosionSeconds"
            );

            HotPotatoFirstDelaySecondsField = FindStaticField(
                ffaOptionsType,
                "HotPotatoFirstDelaySeconds"
            );

            MaxVentSecondsField = FindStaticField(
                ventOptionsType,
                "MaxVentSeconds"
            );

            VentBootModeField = FindStaticField(
                ventBootOptionsType,
                "Mode"
            );

            if (EnabledField == null)
            {
                ResetCache();
                return false;
            }

            return true;
        }
        catch
        {
            ResetCache();
            return false;
        }
    }

    private static FieldInfo FindStaticField(
        Type type,
        string name)
    {
        return type?.GetField(
            name,
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static
        );
    }

    public static void SyncAll()
    {
        if (!IsAvailable())
            return;

        SyncGameMode();

        SyncVentSeconds();
        SyncVentMode();

        SyncTeamMode();
        SyncTeamCount();

        SyncHotPotatoMode();
        SyncHotPotatoExplosionSeconds();
        SyncHotPotatoFirstDelaySeconds();
    }

    public static void SyncGameMode()
    {
        try
        {
            if (!TryResolve())
                return;

            bool enabled =
                Options.GameMode != null &&
                Options.GameMode.GetValue(GameModeType.FFA);

            SetConvertedValue(
                EnabledField,
                enabled
            );
        }
        catch
        {
        }
    }

    public static void SyncVentSeconds()
    {
        try
        {
            if (!TryResolve() ||
                Options.FfaVentMaxSeconds == null)
            {
                return;
            }

            SetConvertedValue(
                MaxVentSecondsField,
                Options.FfaVentMaxSeconds.GetInt()
            );
        }
        catch
        {
        }
    }

    public static void SyncVentMode()
    {
        try
        {
            if (!TryResolve() ||
                Options.FFAVentTeleportMode == null ||
                VentBootModeField == null ||
                VentBootModeEnumType == null)
            {
                return;
            }

            object enumValue = Enum.ToObject(
                VentBootModeEnumType,
                Options.FFAVentTeleportMode.GetValue()
            );

            VentBootModeField.SetValue(
                null,
                enumValue
            );
        }
        catch
        {
        }
    }

    public static void SyncTeamMode()
    {
        try
        {
            if (!TryResolve())
                return;

            bool ffaEnabled = IsFfaSelected();

            bool hotPotatoEnabled =
                ffaEnabled &&
                Options.FfaHotPotatoMode != null &&
                Options.FfaHotPotatoMode.GetValue() == 1;

            bool teamModeEnabled =
                ffaEnabled &&
                !hotPotatoEnabled &&
                Options.FfaTeamMode != null &&
                Options.FfaTeamMode.GetValue() == 1;

            SetConvertedValue(
                TeamsEnabledField,
                teamModeEnabled
            );
        }
        catch
        {
        }
    }

    public static void SyncTeamCount()
    {
        try
        {
            if (!TryResolve() ||
                Options.FfaTeamCount == null)
            {
                return;
            }

            int teamCount =
                Options.FfaTeamCount.GetValue() + 2;

            teamCount = Math.Max(
                2,
                Math.Min(5, teamCount)
            );

            SetConvertedValue(
                TeamCountField,
                teamCount
            );
        }
        catch
        {
        }
    }

    public static void SyncHotPotatoMode()
    {
        try
        {
            if (!TryResolve())
                return;

            bool enabled =
                IsFfaSelected() &&
                Options.FfaHotPotatoMode != null &&
                Options.FfaHotPotatoMode.GetValue() == 1;

            SetConvertedValue(
                HotPotatoEnabledField,
                enabled
            );
        }
        catch
        {
        }
    }

    public static void SyncHotPotatoExplosionSeconds()
    {
        try
        {
            if (!TryResolve() ||
                Options.FfaHotPotatoExplosionSeconds == null)
            {
                return;
            }

            int seconds = Math.Max(
                5,
                Math.Min(
                    120,
                    Options.FfaHotPotatoExplosionSeconds.GetInt()
                )
            );

            SetConvertedValue(
                HotPotatoExplosionSecondsField,
                seconds
            );
        }
        catch
        {
        }
    }

    public static void SyncHotPotatoFirstDelaySeconds()
    {
        try
        {
            if (!TryResolve() ||
                Options.FfaHotPotatoFirstDelaySeconds == null)
            {
                return;
            }

            int seconds = Math.Max(
                0,
                Math.Min(
                    30,
                    Options.FfaHotPotatoFirstDelaySeconds.GetInt()
                )
            );

            SetConvertedValue(
                HotPotatoFirstDelaySecondsField,
                seconds
            );
        }
        catch
        {
        }
    }

    private static bool IsFfaSelected()
    {
        return Options.GameMode != null &&
               Options.GameMode.GetValue(GameModeType.FFA);
    }

    private static void SetConvertedValue(
        FieldInfo field,
        object value)
    {
        if (field == null || value == null)
            return;

        object convertedValue = Convert.ChangeType(
            value,
            field.FieldType
        );

        field.SetValue(
            null,
            convertedValue
        );
    }

    public static void ResetCache()
    {
        FfaAssembly = null;

        EnabledField = null;

        MaxVentSecondsField = null;
        VentBootModeField = null;

        TeamsEnabledField = null;
        TeamCountField = null;

        HotPotatoEnabledField = null;
        HotPotatoExplosionSecondsField = null;
        HotPotatoFirstDelaySecondsField = null;

        VentBootModeEnumType = null;
    }
}