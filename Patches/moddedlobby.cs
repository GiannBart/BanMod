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
                if (__instance == null ||
                    __instance.gameListing == null ||
                    __instance.capacity == null)
                {
                    return;
                }

                GameListing listing = __instance.gameListing;
                string rawName =
                    listing.TrueHostName ??
                    listing.HostName ??
                    "Lobby";

                string currentLobbyCode = "";
                try
                {
                    currentLobbyCode =
                        GameCode.IntToGameName(listing.GameId);
                }
                catch { }

                var moddedResult = ModdedLobby.DetectIfModded(listing);
                bool isModded = moddedResult.IsModded;

                StringBuilder sb = new StringBuilder();

                sb.Append("<size=45%>")
                  .Append("<color=")
                  .Append(COLOR_WHITE)
                  .Append(">")
                  .Append(rawName)
                  .Append("</color>")
                  .Append("\n<size=40%>")
                  .Append("<color=")
                  .Append(COLOR_CYAN)
                  .Append(">")
                  .Append(ModdedLobby.FormatPlatform(listing.Platform))
                  .Append("</color>");

                if (isModded)
                    sb.Append(" <color=")
                      .Append(COLOR_RED)
                      .Append(">[MODDED]</color>");

                sb.Append("\n<size=37%>");

                string playerCountColor =
                    listing.PlayerCount < 4
                        ? COLOR_RED
                        : listing.PlayerCount < 10
                            ? COLOR_YELLOW
                            : COLOR_GREEN;

                sb.Append("<color=")
                  .Append(playerCountColor)
                  .Append(">")
                  .Append(listing.PlayerCount)
                  .Append("/")
                  .Append(listing.MaxPlayers)
                  .Append("</color>   ")
                  .Append("<color=")
                  .Append(COLOR_ORANGE)
                  .Append(">")
                  .Append(currentLobbyCode)
                  .Append("</color>\n");

                if (listing.Options != null)
                {
                    sb.Append(Translator.GetString("Impostors1"))
                      .Append(": <color=")
                      .Append(COLOR_WHITE)
                      .Append(">")
                      .Append(listing.Options.NumImpostors)
                      .Append("</color> - ")
                      .Append(Translator.GetString("KillCD"))
                      .Append(": <color=")
                      .Append(COLOR_WHITE)
                      .Append(">")
                      .Append(listing.Options.GetFloat(
                          FloatOptionNames.KillCooldown))
                      .Append("s</color>");
                }

                sb.Append("</size>");

                BanModActiveLobbyInfo banLobby = null;

                try
                {
                    if (!string.IsNullOrWhiteSpace(currentLobbyCode))
                    {
                        banLobby =
                            BanModActiveLobbyApi.FindCachedLobby(
                                currentLobbyCode);
                    }
                }
                catch { }

                if (banLobby != null)
                {
                    string serverHost =
                        !string.IsNullOrWhiteSpace(banLobby.host_name)
                            ? banLobby.host_name
                            : banLobby.player_name;

                    string serverMode =
                        !string.IsNullOrWhiteSpace(banLobby.game_mode)
                            ? banLobby.game_mode
                            : banLobby.mode;

                    string serverPlayers =
                        !string.IsNullOrWhiteSpace(banLobby.players_text)
                            ? banLobby.players_text
                            : ((banLobby.players > 0
                                    ? banLobby.players
                                    : banLobby.players_count)
                               + "/"
                               + (banLobby.max_players > 0
                                    ? banLobby.max_players
                                    : listing.MaxPlayers));

                    int serverKc =
                        banLobby.kill_cooldown > 0
                            ? banLobby.kill_cooldown
                            : banLobby.kc;

                    sb.Append("\n<size=31%>")
                      .Append("<color=")
                      .Append(COLOR_RED)
                      .Append(">[BANMOD]</color>");

                    if (!string.IsNullOrWhiteSpace(serverMode))
                        sb.Append(" <color=")
                          .Append(COLOR_PURPLE)
                          .Append(">")
                          .Append(serverMode)
                          .Append("</color>");

                    if (!string.IsNullOrWhiteSpace(serverPlayers))
                        sb.Append(" <color=")
                          .Append(COLOR_GREEN)
                          .Append(">")
                          .Append(serverPlayers)
                          .Append("</color>");

                    if (serverKc > 0)
                        sb.Append(" <color=")
                          .Append(COLOR_ORANGE)
                          .Append(">KC:")
                          .Append(serverKc)
                          .Append("s</color>");

                    if (!string.IsNullOrWhiteSpace(banLobby.region))
                        sb.Append(" <color=")
                          .Append(COLOR_CYAN)
                          .Append(">")
                          .Append(banLobby.region)
                          .Append("</color>");

                    if (!string.IsNullOrWhiteSpace(banLobby.language))
                        sb.Append(" <color=")
                          .Append(COLOR_WHITE)
                          .Append(">")
                          .Append(banLobby.language)
                          .Append("</color>");

                    if (!string.IsNullOrWhiteSpace(serverHost))
                        sb.Append(" <color=")
                          .Append(COLOR_YELLOW)
                          .Append(">")
                          .Append(serverHost)
                          .Append("</color>");

                    sb.Append("</size>");
                }

                __instance.capacity.richText = true;
                __instance.capacity.text = sb.ToString();
            }
        }
    }


    public static class BanModSearchInput
    {
        private const int MAX_SEARCH_LENGTH = 25;

        private sealed class SearchRow
        {
            public GameContainer Container;
            public GameListing Listing;
            public Vector3 Slot;
            public int OriginalIndex;
        }

        private static readonly List<SearchRow> CurrentRows =
            new List<SearchRow>();

        private static FindAGameManager currentManager;
        private static int currentRenderedCount;

        public static void SetSearch(string value)
        {
            if (SceneManager.GetActiveScene().name != "FindAGame")
                return;

            value = value ?? "";
            value = value.Replace("\n", "").Replace("\r", "");

            if (value.Length > MAX_SEARCH_LENGTH)
                value = value.Substring(0, MAX_SEARCH_LENGTH);

            if (string.Equals(
                    ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch,
                    value,
                    StringComparison.Ordinal))
            {
                return;
            }

            ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch = value;
            ApplyFilterAndUpdateCount();
        }

        internal static int ApplyToCurrentList(
            FindAGameManager manager,
            int renderedCount)
        {
            if (SceneManager.GetActiveScene().name != "FindAGame")
                return Math.Max(0, renderedCount);

            currentManager = manager;
            currentRenderedCount = Math.Max(0, renderedCount);
            CurrentRows.Clear();

            if (manager == null || manager.gameContainers == null)
                return 0;

            int count = Math.Min(
                currentRenderedCount,
                manager.gameContainers.Length);

            for (int i = 0; i < count; i++)
            {
                GameContainer container = manager.gameContainers[i];

                if (container == null ||
                    container.transform == null ||
                    container.gameListing == null)
                {
                    continue;
                }

                CurrentRows.Add(new SearchRow
                {
                    Container = container,
                    Listing = container.gameListing,
                    Slot = container.transform.localPosition,
                    OriginalIndex = i
                });
            }

            string search =
                ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                SetFoundTexts(CurrentRows.Count);
                return CurrentRows.Count;
            }

            return ApplyFilterAndUpdateCount();
        }

        private static int ApplyFilterAndUpdateCount()
        {
            if (SceneManager.GetActiveScene().name != "FindAGame")
                return 0;

            if (currentManager == null || CurrentRows.Count == 0)
            {
                SetFoundTexts(0);
                return 0;
            }

            string search =
                ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                RestoreAllRows();
                SetFoundTexts(CurrentRows.Count);
                return CurrentRows.Count;
            }

            List<SearchRow> ordered = new List<SearchRow>();

            for (int i = 0; i < CurrentRows.Count; i++)
            {
                SearchRow row = CurrentRows[i];

                if (row != null &&
                    row.Container != null &&
                    row.Listing != null &&
                    MatchesSearch(row.Listing, search))
                {
                    ordered.Add(row);
                }
            }

            ordered.Sort((a, b) =>
                a.OriginalIndex.CompareTo(b.OriginalIndex));

            for (int i = 0; i < CurrentRows.Count; i++)
            {
                SearchRow row = CurrentRows[i];

                try
                {
                    if (row == null ||
                        row.Container == null ||
                        row.Container.transform == null ||
                        row.Container.gameObject == null)
                    {
                        continue;
                    }

                    row.Container.transform.localPosition = row.Slot;
                    row.Container.transform.localScale = Vector3.one;
                    row.Container.gameObject.SetActive(false);
                }
                catch { }
            }

            int visibleCount = 0;

            for (int i = 0; i < ordered.Count; i++)
            {
                SearchRow row = ordered[i];

                try
                {
                    if (row == null ||
                        row.Container == null ||
                        row.Container.transform == null ||
                        row.Container.gameObject == null)
                    {
                        continue;
                    }

                    if (i < CurrentRows.Count)
                    {
                        row.Container.transform.localPosition =
                            CurrentRows[i].Slot;
                    }

                    row.Container.transform.localScale = Vector3.one;
                    row.Container.gameObject.SetActive(true);
                    visibleCount++;
                }
                catch { }
            }

            SetFoundTexts(visibleCount);
            return visibleCount;
        }

        private static void RestoreAllRows()
        {
            for (int i = 0; i < CurrentRows.Count; i++)
            {
                SearchRow row = CurrentRows[i];

                try
                {
                    if (row == null ||
                        row.Container == null ||
                        row.Container.transform == null ||
                        row.Container.gameObject == null)
                    {
                        continue;
                    }

                    row.Container.transform.localPosition = row.Slot;
                    row.Container.transform.localScale = Vector3.one;
                    row.Container.gameObject.SetActive(true);
                }
                catch { }
            }
        }

        private static bool MatchesSearch(
            GameListing listing,
            string search)
        {
            if (listing == null)
                return false;

            string normalizedSearch = NormalizeSearchText(search);

            if (string.IsNullOrEmpty(normalizedSearch))
                return true;

            bool isModded = false;

            try
            {
                isModded = ModdedLobby.DetectIfModded(listing).IsModded;
            }
            catch { }

            if (IsModdedSearchTerm(normalizedSearch))
                return isModded;

            if (IsVanillaSearchTerm(normalizedSearch))
                return !isModded;

            StringBuilder searchable = new StringBuilder();

            string listingHost =
                listing.TrueHostName ??
                listing.HostName ??
                "";

            searchable.Append(listingHost).Append(' ');
            searchable.Append(GetPlatformSearchText(listing)).Append(' ');
            searchable.Append(GetMapSearchText(listing)).Append(' ');

            try
            {
                BanModActiveLobbyInfo cached = GetCachedLobby(listing);

                if (cached != null)
                {
                    string apiHost =
                        !string.IsNullOrWhiteSpace(cached.host_name)
                            ? cached.host_name
                            : cached.player_name;

                    searchable.Append(apiHost).Append(' ');
                    searchable.Append(GetApiMapSearchText(cached)).Append(' ');
                }
            }
            catch { }

            return NormalizeSearchText(searchable.ToString())
                .Contains(normalizedSearch);
        }

        private static bool IsModdedSearchTerm(string value)
        {
            return value == "MOD" ||
                   value == "MODDED" ||
                   value == "MODDATO" ||
                   value == "MODDATA";
        }

        private static bool IsVanillaSearchTerm(string value)
        {
            return value == "VANILLA" ||
                   value == "NOTMODDED" ||
                   value == "NONMODDED" ||
                   value == "UNMODDED" ||
                   value == "NOMOD" ||
                   value == "NONMODDATO" ||
                   value == "NONMODDATA";
        }

        private static string GetPlatformSearchText(GameListing listing)
        {
            if (listing == null)
                return "";

            string formatted = ModdedLobby.FormatPlatform(listing.Platform);

            switch (listing.Platform)
            {
                case Platforms.StandaloneSteamPC:
                    return formatted + " SteamPC PC";

                case Platforms.StandaloneEpicPC:
                    return formatted + " EpicGames EpicPC PC";

                case Platforms.StandaloneWin10:
                    return formatted + " MicrosoftStore Windows Win10 PC";

                case Platforms.Switch:
                    return formatted + " Nintendo NintendoSwitch";

                case Platforms.Xbox:
                    return formatted + " Microsoft XboxConsole";

                case Platforms.Playstation:
                    return formatted + " PS PlayStationConsole";

                case Platforms.StandaloneMac:
                    return formatted + " Apple MacOS PC";

                case Platforms.StandaloneItch:
                    return formatted + " Itchio PC";

                case Platforms.Android:
                    return formatted + " Mobile Phone Tablet";

                case Platforms.IPhone:
                    return formatted + " iOS Apple Mobile Phone";

                default:
                    return formatted;
            }
        }

        private static string GetMapSearchText(GameListing listing)
        {
            if (listing == null)
                return "";

            int mapId = -1;

            try
            {
                object value = GetMemberValue(
                    listing.Options,
                    "MapId",
                    "MapID",
                    "mapId",
                    "mapID",
                    "Map");

                if (value == null)
                {
                    value = GetMemberValue(
                        listing,
                        "MapId",
                        "MapID",
                        "mapId",
                        "mapID",
                        "Map");
                }

                if (value != null)
                    mapId = Convert.ToInt32(value);
            }
            catch { }

            return GetMapNames(mapId);
        }

        private static string GetApiMapSearchText(
            BanModActiveLobbyInfo lobby)
        {
            if (lobby == null)
                return "";

            StringBuilder result = new StringBuilder();

            try
            {
                object mapName = GetMemberValue(
                    lobby,
                    "map_name",
                    "mapName",
                    "MapName",
                    "map",
                    "Map");

                if (mapName != null)
                    result.Append(mapName.ToString()).Append(' ');
            }
            catch { }

            try
            {
                object mapIdValue = GetMemberValue(
                    lobby,
                    "map_id",
                    "mapId",
                    "MapId",
                    "MapID");

                if (mapIdValue != null)
                {
                    int mapId = Convert.ToInt32(mapIdValue);
                    result.Append(GetMapNames(mapId));
                }
            }
            catch { }

            return result.ToString();
        }

        private static string GetMapNames(int mapId)
        {
            switch (mapId)
            {
                case 0:
                    return "The Skeld Skeld";

                case 1:
                    return "MIRA HQ Mira MiraHQ";

                case 2:
                    return "Polus";

                case 3:
                    return "The Skeld Skeld Dleks";

                case 4:
                    return "The Airship Airship";

                case 5:
                    return "The Fungle Fungle";

                default:
                    return mapId >= 0 ? "Map " + mapId : "";
            }
        }

        private static BanModActiveLobbyInfo GetCachedLobby(
            GameListing listing)
        {
            if (listing == null)
                return null;

            try
            {
                string code = GameCode.IntToGameName(listing.GameId);
                return BanModActiveLobbyApi.FindCachedLobby(code);
            }
            catch
            {
                return null;
            }
        }

        private static object GetMemberValue(
            object instance,
            params string[] names)
        {
            if (instance == null || names == null)
                return null;

            Type type = instance.GetType();

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.IgnoreCase;

            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    System.Reflection.PropertyInfo property =
                        type.GetProperty(names[i], flags);

                    if (property != null)
                        return property.GetValue(instance, null);
                }
                catch { }

                try
                {
                    System.Reflection.FieldInfo field =
                        type.GetField(names[i], flags);

                    if (field != null)
                        return field.GetValue(instance);
                }
                catch { }
            }

            return null;
        }

        private static string NormalizeSearchText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string withoutTags =
                System.Text.RegularExpressions.Regex.Replace(
                    value,
                    "<.*?>",
                    "");

            StringBuilder result =
                new StringBuilder(withoutTags.Length);

            for (int i = 0; i < withoutTags.Length; i++)
            {
                char c = withoutTags[i];

                if (char.IsWhiteSpace(c) || char.IsControl(c))
                    continue;

                System.Globalization.UnicodeCategory category =
                    System.Globalization.CharUnicodeInfo
                        .GetUnicodeCategory(c);

                if (category ==
                    System.Globalization.UnicodeCategory.Format)
                {
                    continue;
                }

                result.Append(char.ToUpperInvariant(c));
            }

            return result.ToString();
        }

        private static int CompareSlots(Vector3 a, Vector3 b)
        {
            int y = b.y.CompareTo(a.y);

            if (y != 0)
                return y;

            int x = a.x.CompareTo(b.x);

            if (x != 0)
                return x;

            return a.z.CompareTo(b.z);
        }

        private static void SetFoundTexts(int count)
        {
            try
            {
                if (currentManager == null)
                    return;

                string value = Math.Max(0, count).ToString();

                if (currentManager.matchesFoundText != null)
                    currentManager.matchesFoundText.text = value;

                if (currentManager.TotalText != null)
                    currentManager.TotalText.text = value;
            }
            catch { }
        }

        public static void Reset()
        {
            ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch = "";
            CurrentRows.Clear();
            currentManager = null;
            currentRenderedCount = 0;
        }
    }

    public class BanModGUI : MonoBehaviour
    {
        public static BanModGUI Instance;

        private const int MAX_SEARCH_LENGTH = 25;

        private GUIStyle titleStyle;
        private GUIStyle fieldStyle;
        private GUIStyle placeholderStyle;
        private GUIStyle clearStyle;

        private bool isFocused;
        private bool wasInFindAGame;

        public static void Create()
        {
            if (Instance != null)
                return;

            GameObject existing = GameObject.Find("BanModGUI");

            if (existing != null)
            {
                BanModGUI existingGui =
                    existing.GetComponent<BanModGUI>();

                if (existingGui != null)
                {
                    Instance = existingGui;
                    return;
                }
            }

            GameObject obj = new GameObject("BanModGUI");
            obj.AddComponent<BanModGUI>();
            Object.DontDestroyOnLoad(obj);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            Object.DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            bool inFindAGame =
                SceneManager.GetActiveScene().name == "FindAGame";

            if (!inFindAGame)
            {
                if (wasInFindAGame)
                {
                    isFocused = false;
                    BanModSearchInput.Reset();
                }

                wasInFindAGame = false;
                return;
            }

            wasInFindAGame = true;
            HandleMouse();
            HandleKeyboard();
        }

        private void HandleMouse()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            Vector3 rawMouse = Input.mousePosition;
            Vector2 guiMouse = new Vector2(
                rawMouse.x,
                Screen.height - rawMouse.y);

            Rect fieldRect = GetFieldRect();
            Rect clearRect = GetClearRect();

            if (clearRect.Contains(guiMouse))
            {
                BanModSearchInput.SetSearch("");
                isFocused = true;
                return;
            }

            isFocused = fieldRect.Contains(guiMouse);
        }

        private void HandleKeyboard()
        {
            if (!isFocused ||
                SceneManager.GetActiveScene().name != "FindAGame")
            {
                return;
            }

            string value =
                ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch ?? "";

            bool changed = false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BanModSearchInput.SetSearch("");
                isFocused = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                isFocused = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace) &&
                value.Length > 0)
            {
                value = value.Substring(0, value.Length - 1);
                changed = true;
            }

            string input = Input.inputString;

            if (!string.IsNullOrEmpty(input))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];

                    if (c == '\b' ||
                        c == '\n' ||
                        c == '\r' ||
                        char.IsControl(c))
                    {
                        continue;
                    }

                    if (value.Length >= MAX_SEARCH_LENGTH)
                        break;

                    value += c;
                    changed = true;
                }
            }

            if (changed)
                BanModSearchInput.SetSearch(value);
        }

        private void InitializeStyles()
        {
            if (fieldStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;

            fieldStyle = new GUIStyle(GUI.skin.label);
            fieldStyle.fontSize = 23;
            fieldStyle.alignment = TextAnchor.MiddleLeft;
            fieldStyle.normal.textColor = Color.white;

            placeholderStyle = new GUIStyle(GUI.skin.label);
            placeholderStyle.fontSize = 20;
            placeholderStyle.fontStyle = FontStyle.Italic;
            placeholderStyle.alignment = TextAnchor.MiddleLeft;
            placeholderStyle.normal.textColor =
                new Color(1f, 1f, 1f, 0.45f);

            clearStyle = new GUIStyle(GUI.skin.label);
            clearStyle.fontSize = 22;
            clearStyle.fontStyle = FontStyle.Bold;
            clearStyle.alignment = TextAnchor.MiddleCenter;
            clearStyle.normal.textColor = Color.white;
        }

        private static float GetPanelWidth()
        {
            return Mathf.Max(
                280f,
                Mathf.Min(520f, Screen.width - 60f));
        }

        private static float GetLeft()
        {
            return Mathf.Max(
                10f,
                (Screen.width - GetPanelWidth()) * 0.5f);
        }

        private static Rect GetPanelRect()
        {
            return new Rect(
                GetLeft(),
                25f,
                GetPanelWidth(),
                82f);
        }

        private static Rect GetFieldRect()
        {
            float left = GetLeft();
            float width = GetPanelWidth();

            return new Rect(
                left + 12f,
                56f,
                width - 68f,
                40f);
        }

        private static Rect GetClearRect()
        {
            float left = GetLeft();
            float width = GetPanelWidth();

            return new Rect(
                left + width - 48f,
                56f,
                36f,
                40f);
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "FindAGame")
                return;

            InitializeStyles();

            Rect panelRect = GetPanelRect();
            Rect fieldRect = GetFieldRect();
            Rect clearRect = GetClearRect();

            Rect titleRect = new Rect(
                panelRect.x + 12f,
                panelRect.y + 1f,
                panelRect.width - 24f,
                28f);

            Color oldColor = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.86f);
            GUI.Box(panelRect, "");

            GUI.color = isFocused
                ? new Color(1f, 0.85f, 0.20f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);

            GUI.Box(fieldRect, "");
            GUI.Box(clearRect, "");

            GUI.color = oldColor;

            GUI.Label(
                titleRect,
                "SEARCH LOBBY",
                titleStyle);

            string search =
                ModdedLobby.SetupGameInfoPatchNoTooltip.CurrentSearch ?? "";

            Rect textRect = new Rect(
                fieldRect.x + 10f,
                fieldRect.y,
                fieldRect.width - 20f,
                fieldRect.height);

            if (string.IsNullOrEmpty(search))
            {
                GUI.Label(
                    textRect,
                    isFocused ? "_" : "Name, platform, map or modded...",
                    placeholderStyle);
            }
            else
            {
                bool cursorVisible =
                    isFocused &&
                    ((int)(Time.unscaledTime * 2f) % 2 == 0);

                GUI.Label(
                    textRect,
                    search + (cursorVisible ? "_" : ""),
                    fieldStyle);
            }

            GUI.Label(clearRect, "X", clearStyle);
        }
    }

    [HarmonyPatch(
        typeof(SceneManager),
        nameof(SceneManager.Internal_SceneLoaded))]
    public static class SceneLoadPatch
    {
        public static void Postfix(Scene scene)
        {
            if (scene.name == "FindAGame")
            {
                BanModGUI.Create();
            }
            else
            {
                BanModSearchInput.Reset();
            }
        }
    }
}
