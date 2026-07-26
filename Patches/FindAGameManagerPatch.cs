// Adapted by GianniBart / BanMod.
// Active lobby browser for the new BanMod API:
// - reads /api/lobbies/active from the server
// - merges BanMod active lobbies above vanilla lobbies
// - resolves each BanMod lobby through the official game code lookup
// - keeps vanilla rows below

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;

namespace BanMod
{
    [HarmonyPatch(typeof(FindAGameManager))]
    internal static class BanModFindAGameActiveLobbyPatch
    {
        private const int EXTRA_CONTAINERS = 20;
        private const string BanModLabelName = "BanModMergedLabel";
        private static Scroller scroller;
        private static int requestId;

        private sealed class BanModRow
        {
            public GameListing Game;
            public string Code;
            public string Host;
        }

        [HarmonyPatch(nameof(FindAGameManager.Start))]
        [HarmonyPrefix]
        private static void Start_Prefix(FindAGameManager __instance)
        {
            try
            {
                if (__instance == null || __instance.gameContainers == null || __instance.gameContainers.Length == 0)
                    return;

                GameContainer prefab = __instance.gameContainers[Math.Min(4, __instance.gameContainers.Length - 1)];
                if (prefab == null)
                    return;

                Transform parent = prefab.transform.parent;
                if (parent == null)
                    return;

                GameObject list = new GameObject("BanModGameListScroller");
                list.transform.SetParent(parent, false);

                scroller = list.AddComponent<Scroller>();
                scroller.Inner = list.transform;
                scroller.allowY = true;
                scroller.ScrollWheelSpeed = 0.3f;
                scroller.MouseMustBeOverToScroll = true;
                scroller.SetYBoundsMin(0f);
                scroller.SetYBoundsMax(4.5f);

                BoxCollider2D box = parent.gameObject.GetComponent<BoxCollider2D>();
                if (box == null)
                    box = parent.gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(100f, 100f);
                scroller.ClickMask = box;

                foreach (GameContainer con in __instance.gameContainers)
                {
                    if (con == null)
                        continue;

                    con.transform.SetParent(list.transform, false);
                    Vector3 pos = con.transform.localPosition;
                    con.transform.localPosition = new Vector3(pos.x, pos.y, 25f);
                }

                List<GameContainer> containers = __instance.gameContainers.ToList();

                for (int i = 0; i < EXTRA_CONTAINERS; i++)
                {
                    GameContainer clone = UnityEngine.Object.Instantiate(prefab, list.transform);
                    Vector3 pos = clone.transform.localPosition;
                    clone.transform.localPosition = new Vector3(pos.x, pos.y - 0.75f * (i + 1), 25f);
                    clone.gameObject.SetActive(false);
                    containers.Add(clone);
                }

                __instance.gameContainers = containers.ToArray();

                SpriteRenderer cutoff = CreateCutoff();
                cutoff.transform.SetParent(parent, false);
                cutoff.transform.localPosition = new Vector3(0f, 3f, 1f);
                cutoff.transform.localScale = new Vector3(1500f, 200f, 100f);
            }
            catch (Exception ex)
            {
                Debug.Log("[BanMod] lobby browser Start patch error: " + ex.Message);
            }
        }

        [HarmonyPatch(nameof(FindAGameManager.RefreshList))]
        [HarmonyPostfix]
        private static void Refresh_Postfix()
        {
            try { scroller?.ScrollRelative(new Vector2(0f, -100f)); } catch { }
        }

        [HarmonyPatch(nameof(FindAGameManager.HandleList))]
        [HarmonyPrefix]
        private static bool HandleList_Prefix(
            FindAGameManager __instance,
            InnerNetClient.TotalGameData totalGames,
            HttpMatchmakerManager.FindGamesListFilteredResponse response)
        {
            if (__instance == null || response == null || response.Games == null)
                return true;

            int id = ++requestId;
            __instance.StartCoroutine(CoRenderMergedList(__instance, response, id).WrapToIl2Cpp());
            return false;
        }

        private static IEnumerator CoRenderMergedList(
            FindAGameManager manager,
            HttpMatchmakerManager.FindGamesListFilteredResponse vanillaResponse,
            int id)
        {
            List<GameListing> vanillaGames = CopyVanillaGames(vanillaResponse);

            List<BanModActiveLobbyInfo> apiLobbies = null;
            string apiError = null;

            string regionKey = GetCurrentRegionKey();
            uint langFilter = GetCurrentLanguageFilter(manager);

            try
            {
                manager.ResetContainers();
                SoundManager.Instance.PlaySound(manager.findGameSFX, false, 1f, null);
            }
            catch { }

            yield return BanModActiveLobbyApi.GetActiveLobbiesCoroutine(
                result => apiLobbies = result,
                err => apiError = err
            ).WrapToIl2Cpp();

            if (id != requestId)
                yield break;

            List<BanModActiveLobbyInfo> candidates = apiLobbies == null
                ? new List<BanModActiveLobbyInfo>()
                : apiLobbies
                    .Where(l => l != null)
                    .Where(l => IsLobbyVisible(l))
                    .Where(l => HasRealCode(l))
                    .Where(l => MatchesCurrentRegion(l, regionKey))
                    .Where(l => MatchesCurrentLanguageFilter(l, langFilter))
                    .OrderByDescending(l => SafePlayers(l))
                    .ThenBy(l => SafeCode(l))
                    .ToList();

            List<BanModRow> banModRows = new List<BanModRow>();
            HashSet<string> usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> usedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int matchedFromVanilla = 0;
            int resolvedFromCode = 0;

            // 1) If the BanMod lobby already exists in the vanilla response, move it above.
            foreach (BanModActiveLobbyInfo lobby in candidates)
            {
                string code = NormalizeCode(SafeCode(lobby));
                string host = NormalizeName(SafeHost(lobby));

                if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(host))
                    continue;

                GameListing matched = FindAndRemoveMatchingVanilla(vanillaGames, code, host);
                if (matched != null && matched.Options != null)
                {
                    banModRows.Add(new BanModRow { Game = matched, Code = code, Host = host });

                    if (!string.IsNullOrWhiteSpace(code)) usedCodes.Add(code);
                    if (!string.IsNullOrWhiteSpace(host)) usedHosts.Add(host);
                    matchedFromVanilla++;
                }
            }

            // 2) If it is not in vanilla, resolve through official code lookup.
            foreach (BanModActiveLobbyInfo lobby in candidates)
            {
                if (id != requestId)
                    yield break;

                string code = NormalizeCode(SafeCode(lobby));
                string host = NormalizeName(SafeHost(lobby));

                if ((!string.IsNullOrWhiteSpace(code) && usedCodes.Contains(code)) ||
                    (!string.IsNullOrWhiteSpace(host) && usedHosts.Contains(host)))
                    continue;

                int gameId;
                try { gameId = GameCode.GameNameToInt(code); }
                catch { continue; }

                HttpMatchmakerManager.FindGameByCodeResponse found = null;
                Action<HttpMatchmakerManager.FindGameByCodeResponse, string> value = (resp, token) => { found = resp; };

                yield return DestroyableSingleton<HttpMatchmakerManager>.Instance.CoFindGameInfo(gameId, value);

                TryRestoreRegion(regionKey);

                if (id != requestId)
                    yield break;

                if (found != null && found.Game != null && found.Game.Options != null)
                {
                    banModRows.Add(new BanModRow { Game = found.Game, Code = code, Host = host });

                    if (!string.IsNullOrWhiteSpace(code)) usedCodes.Add(code);
                    if (!string.IsNullOrWhiteSpace(host)) usedHosts.Add(host);

                    FindAndRemoveMatchingVanilla(vanillaGames, code, host);
                    resolvedFromCode++;
                }
            }

            TryRestoreRegion(regionKey);

            if (id != requestId)
                yield break;

            int rendered = RenderFinalList(manager, banModRows, vanillaGames);
            SetFoundTexts(manager, rendered);

            try
            {
                SoundManager.Instance.StopSound(manager.findGameSFX);
                SoundManager.Instance.StopSound(manager.foundGameSFX);
                SoundManager.Instance.PlaySound(manager.foundGameSFX, false, 1f, null);
            }
            catch { }

            try { DestroyableSingleton<MatchMaker>.Instance.NotConnecting(); } catch { }

            try
            {
                FieldInfo timerField = typeof(FindAGameManager).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (timerField != null)
                    timerField.SetValue(manager, 0f);
            }
            catch { }

            Debug.Log("[BanMod] ACTIVE LOBBY MERGE api_total=" + (apiLobbies == null ? 0 : apiLobbies.Count) +
                      " candidates=" + candidates.Count +
                      " matched_vanilla=" + matchedFromVanilla +
                      " resolved_code=" + resolvedFromCode +
                      " banmod_rows=" + banModRows.Count +
                      " vanilla_rows=" + vanillaGames.Count +
                      " rendered=" + rendered +
                      " region=" + regionKey +
                      " lang=" + langFilter +
                      (string.IsNullOrWhiteSpace(apiError) ? "" : " error=" + apiError));
        }

        private static int RenderFinalList(FindAGameManager manager, List<BanModRow> banModRows, List<GameListing> vanillaGames)
        {
            if (manager == null)
                return 0;

            manager.ResetContainers();

            for (int i = 0; i < manager.gameContainers.Length; i++)
                RemoveBanModLabel(manager.gameContainers[i]);

            int index = 0;

            if (banModRows != null)
            {
                for (int i = 0; i < banModRows.Count; i++)
                {
                    if (index >= manager.gameContainers.Length)
                        break;

                    BanModRow row = banModRows[i];
                    if (row == null || row.Game == null || row.Game.Options == null)
                        continue;

                    GameContainer container = manager.gameContainers[index++];
                    container.gameObject.SetActive(true);
                    container.SetGameListing(row.Game);
                    container.SetupGameInfo();
                    AddBanModLabel(container);
                }
            }

            if (vanillaGames != null)
            {
                for (int i = 0; i < vanillaGames.Count; i++)
                {
                    if (index >= manager.gameContainers.Length)
                        break;

                    GameListing game = vanillaGames[i];
                    if (game == null || game.Options == null)
                        continue;

                    GameContainer container = manager.gameContainers[index++];
                    container.gameObject.SetActive(true);
                    container.SetGameListing(game);
                    container.SetupGameInfo();
                    RemoveBanModLabel(container);
                }
            }

            try { scroller?.ScrollRelative(new Vector2(0f, -100f)); } catch { }
            return index;
        }

        private static List<GameListing> CopyVanillaGames(HttpMatchmakerManager.FindGamesListFilteredResponse response)
        {
            List<GameListing> result = new List<GameListing>();
            try
            {
                if (response == null || response.Games == null)
                    return result;

                for (int i = 0; i < response.Games.Count; i++)
                {
                    GameListing game = response.Games[i];
                    if (game != null && game.Options != null)
                        result.Add(game);
                }
            }
            catch { }
            return result;
        }

        private static GameListing FindAndRemoveMatchingVanilla(List<GameListing> vanillaGames, string code, string host)
        {
            if (vanillaGames == null || vanillaGames.Count == 0)
                return null;

            for (int i = 0; i < vanillaGames.Count; i++)
            {
                GameListing game = vanillaGames[i];
                if (game == null)
                    continue;

                string gameCode = NormalizeCode(GetGameCodeFromListing(game));
                string gameHost = NormalizeName(GetHostNameFromListing(game));

                bool codeMatch = !string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(gameCode) && string.Equals(code, gameCode, StringComparison.OrdinalIgnoreCase);
                bool hostMatch = !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(gameHost) && string.Equals(host, gameHost, StringComparison.OrdinalIgnoreCase);

                if (codeMatch || hostMatch)
                {
                    vanillaGames.RemoveAt(i);
                    return game;
                }
            }

            return null;
        }

        private static void AddBanModLabel(GameContainer container)
        {
            ForceRightButtonText(container, true);
            try
            {
                if (container != null)
                    container.StartCoroutine(CoForceRightButtonText(container, true).WrapToIl2Cpp());
            }
            catch { }
        }

        private static void RemoveBanModLabel(GameContainer container)
        {
            try
            {
                if (container == null)
                    return;

                RemoveFloatingBanModLabels(container);
                ForceRightButtonText(container, false);
            }
            catch { }
        }

        private static IEnumerator CoForceRightButtonText(GameContainer container, bool isBanMod)
        {
            for (int i = 0; i < 10; i++)
            {
                ForceRightButtonText(container, isBanMod);
                yield return null;
            }
        }

        private static void RemoveFloatingBanModLabels(GameContainer container)
        {
            try
            {
                if (container == null)
                    return;

                Transform[] children = container.GetComponentsInChildren<Transform>(true);
                if (children == null)
                    return;

                for (int i = children.Length - 1; i >= 0; i--)
                {
                    Transform tr = children[i];
                    if (tr == null || tr.gameObject == null)
                        continue;

                    string n = tr.gameObject.name ?? "";
                    if (n == BanModLabelName || n == "BanModRowLabel_FORCE" || n == "BanModPrependLabel_FORCE" || n == "BanModPrependLabel" || n == "BanModMergedLabel")
                        UnityEngine.Object.Destroy(tr.gameObject);
                }
            }
            catch { }
        }

        private static void ForceRightButtonText(GameContainer container, bool isBanMod)
        {
            try
            {
                if (container == null)
                    return;

                TextMeshPro tmp = FindRightButtonText(container);
                if (tmp == null)
                    return;

                DisableTranslatorComponents(tmp);

                string value = isBanMod ? "BANMOD" : "ALTRO...";
                tmp.gameObject.SetActive(true);
                tmp.text = value;
                try { tmp.SetText(value); } catch { }
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.fontSize = 1.05f;
                tmp.color = isBanMod ? new Color(1f, 0.45f, 0.10f, 1f) : Color.white;
                tmp.ForceMeshUpdate(false, false);
            }
            catch (Exception ex)
            {
                Debug.Log("[BanMod] ForceRightButtonText error: " + ex.Message);
            }
        }

        private static void DisableTranslatorComponents(TextMeshPro tmp)
        {
            try
            {
                if (tmp == null || tmp.gameObject == null)
                    return;

                Component[] comps = tmp.gameObject.GetComponents<Component>();
                if (comps == null)
                    return;

                for (int i = 0; i < comps.Length; i++)
                {
                    Component c = comps[i];
                    if (c == null)
                        continue;

                    string n = c.GetType().Name ?? "";
                    if (n.IndexOf("TextTranslator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Translator", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) >= 0)
                        UnityEngine.Object.Destroy(c);
                }
            }
            catch { }
        }

        private static TextMeshPro FindRightButtonText(GameContainer container)
        {
            try
            {
                if (container == null)
                    return null;

                TextMeshPro[] texts = container.GetComponentsInChildren<TextMeshPro>(true);
                if (texts == null || texts.Length == 0)
                    return null;

                TextMeshPro byText = texts
                    .Where(t => t != null && !string.IsNullOrWhiteSpace(t.text))
                    .Where(t =>
                    {
                        string s = t.text.Trim().ToUpperInvariant();
                        return s.Contains("ALTRO") || s.Contains("MORE") || s.Contains("BANMOD");
                    })
                    .OrderByDescending(t => t.transform.position.x)
                    .FirstOrDefault();

                if (byText != null)
                    return byText;

                return texts.Where(t => t != null).OrderByDescending(t => t.transform.position.x).FirstOrDefault();
            }
            catch { return null; }
        }

        private static string GetGameCodeFromListing(GameListing game)
        {
            try
            {
                object directCode = GetMemberValue(game, "Code", "GameCode", "gameCode", "RoomCode", "roomCode");
                if (directCode is string)
                    return ((string)directCode).Trim().ToUpperInvariant();

                object idObj = GetMemberValue(game, "GameId", "GameID", "gameId", "gameID", "Id", "id");
                if (idObj == null)
                    return null;

                int gameId = Convert.ToInt32(idObj);
                MethodInfo m = AccessTools.Method(typeof(GameCode), "IntToGameName", new[] { typeof(int) }) ?? AccessTools.Method(typeof(GameCode), "IntToGameName");
                if (m == null)
                    return null;

                object result = m.Invoke(null, new object[] { gameId });
                return result == null ? null : result.ToString().Trim().ToUpperInvariant();
            }
            catch { return null; }
        }

        private static string GetHostNameFromListing(GameListing game)
        {
            try
            {
                object host = GetMemberValue(game, "HostName", "hostName", "Host", "host", "Name", "name");
                return host == null ? null : host.ToString();
            }
            catch { return null; }
        }

        private static object GetMemberValue(object obj, params string[] names)
        {
            if (obj == null || names == null)
                return null;

            Type t = obj.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    PropertyInfo p = t.GetProperty(names[i], flags);
                    if (p != null)
                        return p.GetValue(obj, null);
                }
                catch { }

                try
                {
                    FieldInfo f = t.GetField(names[i], flags);
                    if (f != null)
                        return f.GetValue(obj);
                }
                catch { }
            }

            return null;
        }

        private static bool IsLobbyVisible(BanModActiveLobbyInfo lobby)
        {
            if (lobby == null)
                return false;

            if (!lobby.is_public || lobby.is_private)
                return false;

            // ShareLobby non filtra la mod: tutte le lobby pubbliche sono visibili.
            if (!string.IsNullOrWhiteSpace(lobby.status))
                return IsLobbyStatus(lobby.status);

            // /api/lobbies/active already returns active lobby rows.
            return true;
        }

        private static bool MatchesCurrentRegion(BanModActiveLobbyInfo lobby, string currentRegionKey)
        {
            if (lobby == null)
                return false;

            if (string.IsNullOrWhiteSpace(currentRegionKey) || string.IsNullOrWhiteSpace(lobby.region))
                return true;

            return string.Equals(NormalizeRegionKey(lobby.region), currentRegionKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentRegionKey()
        {
            try
            {
                IRegionInfo region = DestroyableSingleton<ServerManager>.Instance.CurrentRegion;
                if (region == null)
                    return "";

                return NormalizeRegionKey(region.Name);
            }
            catch { return ""; }
        }

        private static void TryRestoreRegion(string regionKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(regionKey))
                    return;

                foreach (IRegionInfo region in DestroyableSingleton<ServerManager>.Instance.AvailableRegions)
                {
                    if (region == null)
                        continue;

                    if (NormalizeRegionKey(region.Name) == regionKey)
                    {
                        DestroyableSingleton<ServerManager>.Instance.SetRegion(region);
                        return;
                    }
                }
            }
            catch { }
        }

        private static string NormalizeRegionKey(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return "";

            string r = region.Trim().ToUpperInvariant();
            if (r == "EU" || r == "EUROPE" || r == "EUROPA") return "EU";
            if (r == "NA" || r == "NORTH AMERICA" || r == "NORD AMERICA") return "NA";
            if (r == "AS" || r == "ASIA") return "AS";
            return r;
        }

        private static uint GetCurrentLanguageFilter(FindAGameManager manager)
        {
            try
            {
                if (manager == null)
                    return 0U;

                return manager.GetLangFilter();
            }
            catch { return 0U; }
        }

        private static bool MatchesCurrentLanguageFilter(BanModActiveLobbyInfo lobby, uint langFilter)
        {
            if (lobby == null)
                return false;

            if (langFilter == 0U || string.IsNullOrWhiteSpace(lobby.language))
                return true;

            uint lobbyFlag = LanguageNameToFilterFlag(lobby.language);
            if (lobbyFlag == 0U)
                return true;

            return (langFilter & lobbyFlag) != 0U;
        }

        private static uint LanguageNameToFilterFlag(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return 0U;
            string l = language.Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");
            if (l == "english" || l == "en") return 32768U;
            if (l == "spanish" || l == "español" || l == "espanol" || l == "spanish european") return 1U;
            if (l == "portuguese" || l == "português" || l == "portugues") return 2U;
            if (l == "korean") return 4U;
            if (l == "russian") return 8U;
            if (l == "dutch") return 16U;
            if (l == "filipino" || l == "tagalog") return 32U;
            if (l == "french" || l == "français" || l == "francais") return 64U;
            if (l == "german" || l == "deutsch") return 128U;
            if (l == "italian" || l == "italiano") return 256U;
            if (l == "japanese") return 512U;
            if (l == "spanish latam" || l == "latin american spanish" || l == "latam") return 1024U;
            if (l == "portuguese brazil" || l == "brazilian portuguese" || l == "pt br") return 2048U;
            if (l == "irish") return 4096U;
            if (l == "schinese" || l == "simplified chinese" || l == "chinese simplified") return 8192U;
            if (l == "tchinese" || l == "traditional chinese" || l == "chinese traditional") return 16384U;
            return 0U;
        }

        private static bool IsLobbyStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            string s = status.Trim().Replace("-", "_").Replace(" ", "_");
            return s.Equals("In_Lobby", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("Lobby", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("InLobby", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasRealCode(BanModActiveLobbyInfo lobby)
        {
            return !string.IsNullOrWhiteSpace(SafeCode(lobby));
        }

        private static string SafeCode(BanModActiveLobbyInfo lobby)
        {
            if (lobby == null)
                return null;

            string code = !string.IsNullOrWhiteSpace(lobby.lobby_code) ? lobby.lobby_code : lobby.game_code;
            if (string.IsNullOrWhiteSpace(code))
                return null;

            code = code.Trim().ToUpperInvariant();
            if (code.Equals("HIDDEN", StringComparison.OrdinalIgnoreCase))
                return null;

            return code;
        }

        private static string SafeHost(BanModActiveLobbyInfo lobby)
        {
            if (lobby == null)
                return "";

            if (!string.IsNullOrWhiteSpace(lobby.host_name)) return lobby.host_name;
            if (!string.IsNullOrWhiteSpace(lobby.player_name)) return lobby.player_name;
            return "";
        }

        private static int SafePlayers(BanModActiveLobbyInfo lobby)
        {
            if (lobby == null) return 0;
            if (lobby.players > 0) return lobby.players;
            if (lobby.players_count > 0) return lobby.players_count;
            return 0;
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            return code.Trim().ToUpperInvariant();
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            return name.Trim().ToUpperInvariant().Replace(" ", "");
        }

        private static void SetFoundTexts(FindAGameManager manager, int count)
        {
            try
            {
                manager.matchesFoundText.text = count.ToString();
                manager.TotalText.text = count.ToString();
            }
            catch { }
        }

        private static SpriteRenderer CreateCutoff()
        {
            GameObject go = new GameObject("BanModCutOffTop");
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
            tex.Apply();
            r.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            return r;
        }
    }
}
