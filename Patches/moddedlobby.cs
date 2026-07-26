//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BanMod
{
    public class ModdedLobby
    {
        private static readonly string[] MOD_SIGNATURES = { "(M)", "[MOD]", "REACTOR", "EHR", "TOHE" };
        private const int VANILLA_MAX_PLAYERS = 15;
        private const int VANILLA_MAX_IMPOSTORS = 3;
        private const float VANILLA_MAX_SPEED = 3.0f;
        private const float VANILLA_MIN_KILL_CD = 10.0f;

        private const string COLOR_GREEN = "#80FF80", COLOR_YELLOW = "#FFFF80", COLOR_RED = "#FF8080";
        private const string COLOR_CYAN = "#80FFFF", COLOR_ORANGE = "#FFBF80", COLOR_PURPLE = "#D980FF";
        private const string COLOR_WHITE = "#FFFFFF";

        public static string FormatPlatform(Platforms platform) => platform switch
        {
            Platforms.StandaloneSteamPC => "Steam",
            Platforms.StandaloneEpicPC => "Epic",
            Platforms.StandaloneWin10 => "MS Store",
            Platforms.Switch => "Switch",
            Platforms.Xbox => "Xbox",
            Platforms.Playstation => "PlayStation",
            Platforms.StandaloneMac => "Mac OS",
            Platforms.StandaloneItch => "Itch.io",
            Platforms.Android => "Android",
            Platforms.IPhone => "IPhone",
            _ => "PC"
        };

        public static (bool IsModded, string Reason) DetectIfModded(InnerNet.GameListing listing)
        {
            string hostName = listing.HostName ?? "";
            foreach (var signature in MOD_SIGNATURES)
            {
                if (hostName.IndexOf(signature, StringComparison.OrdinalIgnoreCase) >= 0)
                    return (true, "Signature");
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(hostName, @"#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})"))
                return (true, "ColoredName");

            var options = listing.Options;
            if (options != null)
            {
                if (options.NumImpostors > VANILLA_MAX_IMPOSTORS ||
                    listing.MaxPlayers > VANILLA_MAX_PLAYERS ||
                    options.GetFloat(FloatOptionNames.PlayerSpeedMod) > VANILLA_MAX_SPEED ||
                    options.GetFloat(FloatOptionNames.KillCooldown) < VANILLA_MIN_KILL_CD)
                    return (true, "InvalidRules");
            }
            return (false, "");
        }

        [HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
        public class SetupGameInfoPatchNoTooltip
        {
            public static string CurrentSearch = "";

            [HarmonyPostfix]
            public static void OnSetupGameInfo(GameContainer __instance)
            {
                if (__instance == null || __instance.gameListing == null || __instance.capacity == null)
                    return;

                var listing = __instance.gameListing;
                string rawName = listing.TrueHostName ?? listing.HostName ?? "Lobby";
                string cleanHostName = System.Text.RegularExpressions.Regex.Replace(rawName, "<.*?>", string.Empty);

                bool isMatch = string.IsNullOrEmpty(CurrentSearch) ||
                               cleanHostName.IndexOf(CurrentSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;

                if (isMatch)
                {
                    __instance.gameObject.SetActive(true);
                    __instance.transform.localScale = Vector3.one;
                    if (!string.IsNullOrEmpty(CurrentSearch)) __instance.transform.SetAsFirstSibling();
                }
                else
                {
                    __instance.gameObject.SetActive(false);
                    __instance.transform.localScale = Vector3.zero;
                    return;
                }

                if (!BanMod.InfoLobby) return;

                string currentLobbyCode = GameCode.IntToGameName(listing.GameId);
                var (isModded, _) = ModdedLobby.DetectIfModded(listing);

                var sb = new StringBuilder();
                sb.Append("<size=45%>").Append($"<color={COLOR_WHITE}>{rawName}</color>").Append("\n<size=40%>")
                  .Append($"<color={COLOR_CYAN}>{ModdedLobby.FormatPlatform(listing.Platform)}</color>");

                if (isModded) sb.Append($" <color={COLOR_RED}>[MODDED]</color>");

                sb.Append("\n<size=37%>");
                string playerCountColor = listing.PlayerCount < 4 ? COLOR_RED : listing.PlayerCount < 10 ? COLOR_YELLOW : COLOR_GREEN;
                sb.Append($"<color={playerCountColor}>{listing.PlayerCount}/{listing.MaxPlayers}</color>   ")
                  .Append($"<color={COLOR_ORANGE}>{currentLobbyCode}</color>\n");

                if (listing.Options != null)
                {
                    sb.Append($"{Translator.GetString("Impostors1")}: <color={COLOR_WHITE}>{listing.Options.NumImpostors}</color> - ")
                      .Append($"{Translator.GetString("KillCD")}: <color={COLOR_WHITE}>{listing.Options.GetFloat(FloatOptionNames.KillCooldown)}s</color></size>");
                }

                __instance.capacity.text = sb.ToString();
                __instance.capacity.richText = true;

                // La cache contiene tutte le lobby pubbliche e non private,
                // indipendentemente da ShareLobby.
                BanModActiveLobbyInfo banLobby = BanModActiveLobbyApi.FindCachedLobby(currentLobbyCode);
                if (banLobby != null)
                {
                    string serverHost = !string.IsNullOrWhiteSpace(banLobby.host_name) ? banLobby.host_name : banLobby.player_name;
                    string serverMode = !string.IsNullOrWhiteSpace(banLobby.game_mode) ? banLobby.game_mode : banLobby.mode;
                    string serverPlayers = !string.IsNullOrWhiteSpace(banLobby.players_text)
                        ? banLobby.players_text
                        : ((banLobby.players > 0 ? banLobby.players : banLobby.players_count) + "/" + (banLobby.max_players > 0 ? banLobby.max_players : listing.MaxPlayers));
                    int serverKc = banLobby.kill_cooldown > 0 ? banLobby.kill_cooldown : banLobby.kc;

                    string extra = $" <color={COLOR_RED}>[BANMOD]</color>";
                    if (!string.IsNullOrWhiteSpace(serverMode)) extra += $" <color={COLOR_PURPLE}>{serverMode}</color>";
                    if (!string.IsNullOrWhiteSpace(serverPlayers)) extra += $" <color={COLOR_GREEN}>{serverPlayers}</color>";
                    if (serverKc > 0) extra += $" <color={COLOR_ORANGE}>KC:{serverKc}s</color>";
                    if (!string.IsNullOrWhiteSpace(banLobby.region)) extra += $" <color={COLOR_CYAN}>{banLobby.region}</color>";
                    if (!string.IsNullOrWhiteSpace(banLobby.language)) extra += $" <color={COLOR_WHITE}>{banLobby.language}</color>";
                    if (!string.IsNullOrWhiteSpace(serverHost)) extra += $" <color={COLOR_YELLOW}>{serverHost}</color>";

                    __instance.capacity.text = __instance.capacity.text.Replace("</size></size>", extra + "</size></size>");
                }
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    public static class BanModSearchInput
    {
        private static float lastRequestTime = 0f;

        public static void Postfix()
        {
            if (SceneManager.GetActiveScene().name != "FindAGame") return;

            string oldSearch = ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch;
            bool changed = false;

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch.Length > 0)
                {
                    ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch = ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch.Substring(0, ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch.Length - 1);
                    changed = true;
                }
            }

            foreach (char c in Input.inputString)
            {
                if (c == '\b') continue;
                if (c == '\n' || c == '\r') { ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch = ""; changed = true; }
                else if (c >= 32 && c <= 126 && ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch.Length < 25)
                {
                    ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch += c;
                    changed = true;
                }
            }

            if (changed || oldSearch != ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch)
            {
                if (Time.time > lastRequestTime + 0.5f && FindAGameManager.Instance != null)
                {
                    FindAGameManager.Instance.ResetTimer();
                    FindAGameManager.Instance.RefreshList();
                    lastRequestTime = Time.time;
                }

                ForceImmediateSort();
            }
        }

        public static void ForceImmediateSort()
        {
            var containers = Object.FindObjectsOfType<GameContainer>();
            if (containers == null) return;

            foreach (var container in containers)
            {
                if (container == null) continue;
                ModdedLobby.SetupGameInfoPatchNoTooltip.OnSetupGameInfo(container);
            }

            if (containers.Length > 0 && containers[0].transform.parent != null)
            {
                var layout = containers[0].transform.parent.GetComponent<VerticalLayoutGroup>();
                if (layout != null) { layout.enabled = false; layout.enabled = true; }
            }
        }
    }

    [HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.HandleList))]
    public static class HandleListPatch
    {
        public static void Postfix()
        {
            BanModSearchInput.ForceImmediateSort();
        }
    }

    public class BanModGUI : MonoBehaviour
    {
        public static void Create()
        {
            if (GameObject.Find("BanModGUI")) return;
            var obj = new GameObject("BanModGUI");
            obj.AddComponent<BanModGUI>();
            Object.DontDestroyOnLoad(obj);
        }

        void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "FindAGame") return;
            string search = ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch;
            if (string.IsNullOrEmpty(search)) return;

            GUI.color = Color.yellow;
            GUI.Label(new Rect(30, 30, 1000, 100), $"<size=40> 🔍 CERCA: {search.ToUpper()}_</size>");
        }
    }

    [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.Internal_SceneLoaded))]
    public static class SceneLoadPatch
    {
        public static void Postfix(Scene scene)
        {
            if (scene.name == "FindAGame") BanModGUI.Create();
            else ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch = "";
        }
    }
}