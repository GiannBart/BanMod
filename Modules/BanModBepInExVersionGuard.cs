using BepInEx;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BanMod
{
    internal static class BanModBepInExVersionGuard
    {
        private const string RequiredVersion = "6.0.0-be.735";
        private static bool _checked;

        internal static void CheckAndWarn()
        {
            if (_checked)
                return;

            _checked = true;

            string detectedVersion = GetLoadedBepInExVersion();
            string detectedBuild = GetShortBepInExVersion(detectedVersion);

            try
            {
                Debug.Log("[BANMOD] Loaded BepInEx version: " + detectedVersion);
            }
            catch { }

            if (IsRequiredVersion(detectedVersion))
                return;

            string message =
                "<size=135%><b><color=#FF3B30>⚠ WARNING ⚠</color></b></size>\n\n" +

                "<size=115%><b><color=#FF9500>Incompatible BepInEx version detected!</color></b></size>\n\n" +

                "<color=#FFFFFF>Required BepInEx version</color>\n" +
                "<color=#34C759><b>" + RequiredVersion + "</b></color>\n\n" +

                "<color=#FFFFFF>Detected BepInEx version</color>\n" +
                "<color=#FF453A><b>" + detectedBuild + "</b></color>\n\n" +

                "<color=#FFD60A><b>BanMod may not work correctly with this BepInEx version.</b></color>\n\n" +

                "<size=115%><b><color=#64D2FF>HOW TO FIX IT</color></b></size>\n\n" +

                "<b>1.</b> Close <b>Among Us</b> completely.\n\n" +

                "<b>2.</b> Open the official BanMod GitHub release page:\n" +
                "<color=#5AC8FA><u>https://github.com/GiannBart/BanMod/releases/latest</u></color>\n\n" +

                "<b>3.</b> Download the <b>ZIP file that matches your game platform</b> " +
                "(for example <b>Steam / Itch.io</b> or the corresponding version for your installation).\n\n" +

                "<b>4.</b> Open your Among Us installation folder.\n\n" +

                "<b>5.</b> Delete the existing:\n" +
                "<color=#FF9F0A><b>BepInEx/core</b></color>\n" +
                "folder before installing the new files.\n\n" +

                "<b>6.</b> It is also recommended to delete:\n" +
                "<color=#FF9F0A><b>BepInEx/interop</b></color>\n" +
                "so that BepInEx can regenerate it correctly on the next launch.\n\n" +

                "<b>7.</b> Extract the contents of the downloaded BanMod ZIP " +
                "<b>directly into the Among Us folder</b> and allow Windows to replace existing files.\n\n" +

                "<color=#FF453A><b>IMPORTANT:</b></color>\n" +
                "Do <b>NOT</b> replace only <b>BepInEx.Core.dll</b> and do not mix files from different " +
                "BepInEx versions. The entire BepInEx runtime must use the same build.\n\n" +

                "<b>8.</b> Start Among Us again.\n" +
                "BepInEx should now report:\n" +
                "<color=#34C759><b>" + RequiredVersion + "</b></color>\n\n" +

                "<size=90%><color=#8E8E93>" +
                "If this warning still appears after reinstalling, verify that you extracted the correct " +
                "ZIP into the actual Among Us installation directory." +
                "</color></size>";

            try
            {
                Debug.LogWarning(
                    "[BANMOD] Unsupported BepInEx version. Required: " +
                    RequiredVersion + " | Detected: " + detectedVersion);
            }
            catch { }

            try
            {
                BanModCommunicationUi.EnsureCreated();

                if (BanModCommunicationUi.Instance != null)
                {
                    BanModCommunicationUi.Instance.ShowMessagePopup(
                        "BANMOD - BepInEx",
                        message
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Debug.LogWarning(
                        "[BANMOD] Could not show BepInEx version popup: " +
                        ex.Message);
                }
                catch { }
            }

        }

        internal static string GetLoadedBepInExVersion()
        {
            try
            {
                Assembly coreAssembly = typeof(Paths).Assembly;

                AssemblyInformationalVersionAttribute informational =
                    coreAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                if (informational != null &&
                    !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                {
                    return informational.InformationalVersion.Trim();
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(coreAssembly.Location))
                    {
                        FileVersionInfo versionInfo =
                            FileVersionInfo.GetVersionInfo(coreAssembly.Location);

                        if (versionInfo != null &&
                            !string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
                        {
                            return versionInfo.ProductVersion.Trim();
                        }
                    }
                }
                catch { }

                Version assemblyVersion = coreAssembly.GetName().Version;
                return assemblyVersion != null
                    ? assemblyVersion.ToString()
                    : "unknown";
            }
            catch (Exception ex)
            {
                try
                {
                    Debug.LogWarning(
                        "[BANMOD] Failed to detect BepInEx version: " +
                        ex.Message);
                }
                catch { }

                return "unknown";
            }
        }
        private static string GetShortBepInExVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "unknown";

            int plusIndex = version.IndexOf('+');

            if (plusIndex > 0)
                return version.Substring(0, plusIndex);

            return version;
        }
        private static bool IsRequiredVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            string value = version.Trim();

            if (!value.StartsWith(
                    RequiredVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (value.Length == RequiredVersion.Length)
                return true;

            return value.Length > RequiredVersion.Length &&
                   value[RequiredVersion.Length] == '+';
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    internal static class BanModBepInExVersionGuardMainMenuPatch
    {
        private static void Postfix()
        {
            BanModBepInExVersionGuard.CheckAndWarn();
        }
    }
}
