//credits and licenses in the resources folder
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using UnityEngine;
using static BanMod.Translator;

namespace BanMod
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    public static class IntroCutsceneCoBeginPatch
    {
        public static void Prefix(IntroCutscene __instance)
        {
            if (__instance == null)
                return;

            __instance.StartCoroutine(WatchIntroAndReplaceRoleText(__instance));
        }

        private static IEnumerator WatchIntroAndReplaceRoleText(IntroCutscene instance)
        {
            float timer = 0f;

            while (timer < 15f)
            {
                ApplyCustomIntroRoleText(instance);

                timer += Time.deltaTime;
                yield return null;
            }
        }

        private static void ApplyCustomIntroRoleText(IntroCutscene instance)
        {
            if (instance == null)
                return;

            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                return;

            if (instance.RoleText == null || instance.RoleBlurbText == null || instance.YouAreText == null)
                return;

            string customRoleName = GetLocalCustomRoleName();
            string customRoleBlurb = GetLocalCustomRoleBlurb();

            if (string.IsNullOrEmpty(customRoleName))
                return;

            Color roleColor = GetLocalCustomRoleColor(Color.white);
            Color teamColor = GetLocalCustomTeamColor(roleColor);
            string teamName = GetLocalCustomTeamName();

            instance.RoleText.text = customRoleName;

            if (!string.IsNullOrEmpty(customRoleBlurb))
                instance.RoleBlurbText.text = FormatBlurb(customRoleBlurb);

            instance.RoleText.color = roleColor;
            instance.YouAreText.color = roleColor;
            instance.RoleBlurbText.color = roleColor;

            if (instance.TeamTitle != null && !string.IsNullOrEmpty(teamName))
            {
                instance.TeamTitle.text = teamName;
                instance.TeamTitle.color = teamColor;
            }

            if (instance.BackgroundBar != null && instance.BackgroundBar.material != null)
            {
                instance.BackgroundBar.material.SetColor("_Color", teamColor);
            }
        }

        private static string FormatBlurb(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string formatted = text
                .Replace("\\n", "\n")
                .Replace(". ", ".\n")
                .Replace("! ", "!\n")
                .Replace("? ", "?\n")
                .Replace("; ", ";\n");

            return "<size=85%>" + formatted + "</size>";
        }

        private static string GetLocalCustomRoleName()
        {
            if (PlayerControl.LocalPlayer == null)
                return string.Empty;

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (Jester.JesterId != 255 && Jester.JesterId == playerId)
                return GetString("Jester");

            if (Exiler.ExilerId != 255 && Exiler.ExilerId == playerId)
                return GetString("Exiler");

            if (Guesser.SpecialKillerId != 255 && Guesser.SpecialKillerId == playerId)
                return GetString("Guesser");

            return string.Empty;
        }

        private static string GetLocalCustomRoleBlurb()
        {
            if (PlayerControl.LocalPlayer == null)
                return string.Empty;

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (Jester.JesterId != 255 && Jester.JesterId == playerId)
                return GetString("JesterInfo");

            if (Exiler.ExilerId != 255 && Exiler.ExilerId == playerId)
                return GetString("ExilerInfo");

            if (Guesser.SpecialKillerId != 255 && Guesser.SpecialKillerId == playerId)
                return GetString("GuesserInfo");

            return string.Empty;
        }

        private static Color GetLocalCustomRoleColor(Color fallback)
        {
            if (PlayerControl.LocalPlayer == null)
                return fallback;

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (Jester.JesterId != 255 && Jester.JesterId == playerId)
                return new Color32(255, 60, 60, 255);   

            if (Exiler.ExilerId != 255 && Exiler.ExilerId == playerId)
                return new Color32(80, 180, 255, 255);

            if (Guesser.SpecialKillerId != 255 && Guesser.SpecialKillerId == playerId)
                return new Color32(255, 150, 40, 255);   

            return fallback;
        }

        private static string GetLocalCustomTeamName()
        {
            if (PlayerControl.LocalPlayer == null)
                return string.Empty;

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (Jester.JesterId != 255 && Jester.JesterId == playerId)
                return "Neutral";

            if (Exiler.ExilerId != 255 && Exiler.ExilerId == playerId)
                return "Custom Crew";

            if (Guesser.SpecialKillerId != 255 && Guesser.SpecialKillerId == playerId)
                return "Custom Crew";

            return string.Empty;
        }

        private static Color GetLocalCustomTeamColor(Color fallback)
        {
            if (PlayerControl.LocalPlayer == null)
                return fallback;

            byte playerId = PlayerControl.LocalPlayer.PlayerId;

            if (Jester.JesterId != 255 && Jester.JesterId == playerId)
                return new Color32(80, 180, 255, 255);

            return fallback;
        }
    }
}