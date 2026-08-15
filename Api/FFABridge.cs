//credits and licenses in the resources folder/
using System;
using System.Linq;
using System.Reflection;

namespace BanMod;

public static class OptionalPluginAvailability
{
    public static bool Any =>
        Ffa ||
        Translate ||
        Invite ||
        Chat;

    public static bool Ffa => BanModCore.IsAnyPluginLoaded(
        "ffa",
        "Ffa",
        "FFA",
        "freeforall",
        "free_for_all"
    );

    public static bool Translate => BanModCore.IsAnyPluginLoaded(
        "translate",
        "translator",
        "livetranslator",
        "live_translator"
    );

    public static bool Invite => BanModCore.IsAnyPluginLoaded(
        "invite",
        "invites"
    );

    public static bool Chat => BanModCore.IsAnyPluginLoaded(
        "Chat",
        "chat"
    );
}

public static class FfaExternalBridge
{
    private static Assembly FfaAssembly;

    private static FieldInfo EnabledField;
    private static FieldInfo MaxVentSecondsField;
    private static FieldInfo VentBootModeField;

    // NUOVO: modalità a gruppi.
    private static FieldInfo TeamsEnabledField;
    private static FieldInfo TeamCountField;

    private static Type VentBootModeEnumType;

    public static bool IsAvailable()
    {
        try
        {
            return OptionalPluginAvailability.Ffa &&
                   TryResolve();
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

            if (FfaAssembly != null &&
                EnabledField != null)
            {
                return true;
            }

            ResetCache();

            FfaAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly =>
                    assembly.GetType(
                        "FFA.FFAOptions",
                        false
                    ) != null
                );

            if (FfaAssembly == null)
                return false;

            Type ffaOptionsType =
                FfaAssembly.GetType(
                    "FFA.FFAOptions",
                    false
                );

            Type ventOptionsType =
                FfaAssembly.GetType(
                    "FFA.FfaVentOptions",
                    false
                ) ??
                FfaAssembly.GetType(
                    "FFA.FfaVentLimitPatch+FfaVentOptions",
                    false
                );

            Type ventBootOptionsType =
                FfaAssembly.GetType(
                    "FFA.FfaVentBootOptions",
                    false
                );

            VentBootModeEnumType =
                FfaAssembly.GetType(
                    "FFA.FfaVentBootMode",
                    false
                );

            EnabledField = ffaOptionsType?.GetField(
                "Enabled",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

            // NUOVO.
            TeamsEnabledField = ffaOptionsType?.GetField(
                "TeamsEnabled",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

            // NUOVO.
            TeamCountField = ffaOptionsType?.GetField(
                "TeamCount",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static
            );

            MaxVentSecondsField =
                ventOptionsType?.GetField(
                    "MaxVentSeconds",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static
                );

            VentBootModeField =
                ventBootOptionsType?.GetField(
                    "Mode",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static
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

    public static void SyncAll()
    {
        if (!IsAvailable())
            return;

        SyncGameMode();
        SyncVentSeconds();
        SyncVentMode();

        // NUOVO.
        SyncTeamMode();
        SyncTeamCount();
    }

    public static void SyncGameMode()
    {
        try
        {
            if (!TryResolve())
                return;

            bool enabled =
                Options.GameMode != null &&
                Options.GameMode.GetValue() == 6;

            EnabledField?.SetValue(
                null,
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
            if (!TryResolve())
                return;

            if (Options.FfaVentMaxSeconds == null ||
                MaxVentSecondsField == null)
            {
                return;
            }

            int seconds =
                Options.FfaVentMaxSeconds.GetValue();

            object convertedValue =
                Convert.ChangeType(
                    seconds,
                    MaxVentSecondsField.FieldType
                );

            MaxVentSecondsField.SetValue(
                null,
                convertedValue
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
            if (!TryResolve())
                return;

            if (Options.FFAVentTeleportMode == null)
                return;

            if (VentBootModeField == null ||
                VentBootModeEnumType == null)
            {
                return;
            }

            int mode =
                Options.FFAVentTeleportMode.GetValue();

            object enumValue =
                Enum.ToObject(
                    VentBootModeEnumType,
                    mode
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

            if (TeamsEnabledField == null)
                return;

            bool ffaEnabled =
                Options.GameMode != null &&
                Options.GameMode.GetValue() == 6;

            bool teamModeEnabled = false;

            if (ffaEnabled &&
                Options.FfaTeamMode != null)
            {
                teamModeEnabled =
                    Options.FfaTeamMode.GetValue() == 1;
            }

            object convertedValue =
                Convert.ChangeType(
                    teamModeEnabled,
                    TeamsEnabledField.FieldType
                );

            TeamsEnabledField.SetValue(
                null,
                convertedValue
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
            if (!TryResolve())
                return;

            if (Options.FfaTeamCount == null ||
                TeamCountField == null)
            {
                return;
            }

            int selectedIndex =
                Options.FfaTeamCount.GetValue();

            int teamCount =
                selectedIndex + 2;

            if (teamCount < 2)
                teamCount = 2;

            if (teamCount > 3)
                teamCount = 3;

            object convertedValue =
                Convert.ChangeType(
                    teamCount,
                    TeamCountField.FieldType
                );

            TeamCountField.SetValue(
                null,
                convertedValue
            );
        }
        catch
        {
        }
    }

    public static void ResetCache()
    {
        FfaAssembly = null;

        EnabledField = null;
        MaxVentSecondsField = null;
        VentBootModeField = null;

        TeamsEnabledField = null;
        TeamCountField = null;

        VentBootModeEnumType = null;
    }
}
