// Symbols sourced from: https://ultradragon005.github.io/AmongUs-Utilities/symbols.html

using BanMod;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BanMod
{
    internal static class BanModSymbolPicker
    {
        private sealed class SymbolCategory
        {
            public readonly string Name;
            public readonly string Symbols;

            public SymbolCategory(string name, string symbols)
            {
                Name = name;
                Symbols = symbols;
            }
        }

        private sealed class FavoriteHitTarget
        {
            public RectTransform Rect;
            public string Symbol;
        }

        private static readonly SymbolCategory[] Categories =
        {
            new SymbolCategory("Stars", "★|☆|⁂|⁑|✽"),
            new SymbolCategory("Arrows", "☝|☞|☟|☜|↑|↓|→|←|↔|↕|⬆|↗|➡|↘|⬇|↙|⬅|↖|⤴|⤵|￩|￪|￫|￬|⇦|⇧|⇨|⇩|⇵|⇄|⇅|⇆|↹|↸|⌅|⌆|⏎|▶|➔"),
            new SymbolCategory("Shapes", "•|○|◦|⦿|▲|▼|♠|♥|♣|♦|♤|♡|♧|♢|■|□|▢|▣|▤|▥|▦|▧|▨|▩|▪|▫|◌|●|◐|◑|◒|◓|◯|◆|◇|◈|❖|▱|▶|◀|◉|◍|◎|〇|〶|〄"),
            new SymbolCategory("Symbols", "✓|╳|∞|†|✚|♫|♪|♲|♳|♴|♵|♶|♷|♸|♹|♺|♻|♼|♽|☎|☏|✂|♀|♂|⚠"),
            new SymbolCategory("Emoji", "😂|☹️|😆|☺️|😎|😉|😅|😊|😋|😀|😁|😃|😄|😍|☁️|☂️|☀️"),
            new SymbolCategory("Random", "‰|§|¶|©|™|¥|$|¢|€|ƒ|£|Æ"),
            new SymbolCategory("Copyright", "©|®|™|℡|№|℀|℅|☎|☏|‰|§|¶|☝|☜"),
            new SymbolCategory("Currency", "¢|$|€|£|¥|₩|₫|￥|¤|ƒ"),
            new SymbolCategory("Brackets", "〈|〉|《|》|「|」|『|』|〖|〗|〔|〕|︵|︶|︷|︸|︹|︺|︻|︼|︽|︾|︿|﹀|﹁|﹂|﹃|﹄|﹙|﹚|﹛|﹜|﹝|﹞|﹤|﹥|（|）|＜|＞|｛|｝|〘|〙|〚|〛|«|»|‹|›|〈|〉|〱"),
            new SymbolCategory("Cards", "♤|♠|♧|♣|♡|♥|♢|♦"),
            new SymbolCategory("Musical", "♩|♪|♫|♬|♭|♮|♯|°|ø|≠"),
            new SymbolCategory("Degree", "°|℃|℉|☀|☁|☂|☃|☉|♁|♨|㎎|㎏|㎜|㎝|㎞|㎡|㏄|㏎|㏑|㏒|㏕"),
            new SymbolCategory("Astrological", "☯|✚|†|‡|♁|❖|卍|卐|〷"),
            new SymbolCategory("Heart", "♥|♡"),
            new SymbolCategory("Check", "✓|∨|√|〤|〥"),
            new SymbolCategory("Gender / Faces", "♀|♂|☹|☺|〠|ヅ|ツ|㋡|웃|유|ü|Ü|シ|ッ|㋛|☃|〲|〴"),
            new SymbolCategory("Punctuation", "·|‑|‒|–|—|―|‘|’|‚|“|”|„|•|‥|…|‧|′|″|‵|ʻ|ˇ|ˉ|ˊ|ˋ|˙|～|¿|﹐|﹒|﹔|﹕|！|＃|＄|％|＆|＊|，|．|：|；|？|＠|、|。|〃|〝|〞|︰"),
            new SymbolCategory("Math / Numbers", "π|∞|Σ|√|∫|∬|∭|∀|∂|∃|∅|∆|∇|∈|∉|∊|∋|∏|∑|−|∓|∕|∝|∟|∠|∣|∥|∦|∧|∨|∩|∪|∴|∵|∶|∷|∽|≃|≅|≈|≌|≒|≠|≡|≢|≤|≥|≦|≧|≪|≫|≮|≯|≲|≳|≶|≷|⊂|⊃|⊄|⊅|⊆|⊇|⊊|⊋|⊕|⊖|⊗|⊘|⊙|⊠|⊥|⊿|⋚|⋛|⋯|﹢|﹣|＋|－|／|＝|÷|±|Ⅰ|Ⅱ|Ⅲ|Ⅳ|Ⅴ|Ⅵ|Ⅶ|Ⅷ|Ⅸ|Ⅹ|Ⅺ|Ⅻ|ⅰ|ⅱ|ⅲ|ⅳ|ⅴ|ⅵ|ⅶ|ⅷ|ⅸ|ⅹ|ⅺ|ⅻ|➀|➁|➂|➃|➄|➅|➆|➇|➈|➉|➊|➋|➌|➍|➎|➏|➐|➑|➒|➓|⓵|⓶|⓷|⓸|⓹|⓺|⓻|⓼|⓽|⓾|⓿|❶|❷|❸|❹|❺|❻|❼|❽|❾|❿|¹|²|³|⁴|⓪|①|②|③|④|⑤|⑥|⑦|⑧|⑨|⑩|⑪|⑫|⑬|⑭|⑮|⑯|⑰|⑱|⑲|⑳|⑴|⑵|⑶|⑷|⑸|⑹|⑺|⑻|⑼|⑽|⑾|⑿|⒀|⒁|⒂|⒃|⒄|⒅|⒆|⒇|⒈|⒉|⒊|⒋|⒌|⒍|⒎|⒏|⒐|⒑|⒒|⒓|⒔|⒕|⒖|⒗|⒘|⒙|⒚|⒛|㈠|㈡|㈢|㈣|㈤|㈥|㈦|㈧|㈨|㈩|㊀|㊁|㊂|㊃|㊄|㊅|㊆|㊇|㊈|㊉|０|１|２|３|４|５|６|７|８|９|◉|○|◌|◍|◎|●|◐|◑|◒|◓|⊗|⊙|◯|〇|〶|◦|∅|⊕|⊖|⊘|⦿|⚽|⚾|〄|θ|ð|ĩ|Ň|Ⓐ|Ⓑ|Ⓒ|Ⓓ|Ⓔ|Ⓕ|Ⓖ|Ⓗ|Ⓘ|Ⓙ|Ⓚ|Ⓛ|Ⓜ|Ⓝ|Ⓞ|Ⓟ|Ⓠ|Ⓡ|Ⓢ|Ⓣ|Ⓤ|Ⓥ|Ⓦ|Ⓧ|Ⓨ|Ⓩ|ⓐ|ⓑ|ⓒ|ⓓ|ⓔ|ⓕ|ⓖ|ⓗ|ⓘ|ⓙ|ⓚ|ⓛ|ⓜ|ⓝ|ⓞ|ⓟ|ⓠ|ⓡ|ⓢ|ⓣ|ⓤ|ⓥ|ⓦ|ⓧ|ⓨ|ⓩ|⓫|⓬|⓭|⓮|⓯|⓰|⓱|⓲|⓳|⓴|㊏|㊐|㊑|㊒|㊓|㊔|㊕|㊖|㊗|㊘|㊙|㊚|㊛|㊜|㊝|㊞|㊟|㊠|㊡|㊢|㊣|㉈|㉉|㉊|㉋|㉌|㉍|㉎|㉏|㉐|㉑|㉒|㉓|㉔|㉕|㉖|㉗|㉘|㉙|㉚|㉛|㉜|㉝|㉞|㉟|㊱|㊲|㊳|㊴|㊵|㊶|㊷|㊸|㊹|㊺|㊻|㊼|㊽|㊾|㊿"),
            new SymbolCategory("Latin", "ĩ|Ň|Ⓐ|Ⓑ|Ⓒ|Ⓓ|Ⓔ|Ⓕ|Ⓖ|Ⓗ|Ⓘ|Ⓙ|Ⓚ|Ⓛ|Ⓜ|Ⓝ|Ⓞ|Ⓟ|Ⓠ|Ⓡ|Ⓢ|Ⓣ|Ⓤ|Ⓥ|Ⓦ|Ⓧ|Ⓨ|Ⓩ|ⓐ|ⓑ|ⓒ|ⓓ|ⓔ|ⓕ|ⓖ|ⓗ|ⓘ|ⓙ|ⓚ|ⓛ|ⓜ|ⓝ|ⓞ|ⓟ|ⓠ|ⓡ|ⓢ|ⓣ|ⓤ|ⓥ|ⓦ|ⓧ|ⓨ|ⓩ"),
            new SymbolCategory("Symbol Emoji", "™|〰|🆗|🆕|🆙|🆒|🆓|🆖|🅿|Ⓜ|🆑|🆘|🆚|⚠|🅰|🅱|🆎|🅾|♻|🆔")
        };

        private static readonly HashSet<char> GamePickerSymbolChars = BuildGamePickerSymbolChars();
        private static int _gameTextBoxInstanceId;
        private static bool _gameNativeSymbolWhitelistInstalled;

        private const int FavoritesCategoryIndex = 0;
        private const string FavoritesCategoryName = "★ Favorites";
        private static readonly string FavoritesFolder = "./DATA/Symbols";
        private static readonly string FavoritesFilePath = Path.Combine(FavoritesFolder, "Favorites.txt");
        private static readonly List<string> FavoriteSymbols = new List<string>();
        private static readonly HashSet<string> FavoriteSymbolSet = new HashSet<string>();
        private static readonly List<FavoriteHitTarget> FavoriteHitTargets = new List<FavoriteHitTarget>();
        private static bool _favoritesLoaded;

        private static readonly string[] PaletteHex =
        {
            "FFFFFF", "D8D8D8", "9B9B9B", "4A4A4A",
            "FF4D4D", "FF8A3D", "FFD93D", "B8E85C",
            "56E07B", "42D7D7", "4FC3F7", "5D7CFA",
            "8A7DFF", "B66DFF", "E66DFF", "FF6FAE",
            "E74C3C", "E67E22", "F1C40F", "2ECC71",
            "1ABC9C", "3498DB", "9B59B6", "E84393"
        };

        private static readonly Regex AllowedColorSpanRegex = new Regex(
            @"<color=#(?<hex>[0-9A-Fa-f]{6})(?:[0-9A-Fa-f]{2})?>(?<body>.*?)</color>",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex AnyRichTextTagRegex = new Regex(
            @"<.*?>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex NoParseCloseRegex = new Regex(
            @"</\s*noparse\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static GameObject _canvasRoot;
        private static GameObject _blocker;
        private static GameObject _window;
        private static RectTransform _categoryContent;
        private static RectTransform _symbolContent;
        private static TextMeshProUGUI _contextText;
        private static TextMeshProUGUI _colorText;
        private static GameObject _gameLauncher;
        private static Button _gameLauncherButton;

        private const bool GameLauncherCalibrationDragEnabled = true;
        private const float GameLauncherDragThresholdPixels = 5f;
        private static bool _gameLauncherDragging;
        private static bool _gameLauncherManualPosition;
        private static Vector2 _gameLauncherDragStartScreen;
        private static Vector2 _gameLauncherDragOffsetCanvas;
        private static float _gameLauncherSuppressClickUntil;
        private static float _gameLauncherNextDragLogTime;

        private static Action<string> _insertAction;
        private static Func<bool> _colorAllowed;
        private static string _contextName = "SYMBOLS";
        private static string _selectedHex = "FFFFFF";
        private static int _selectedCategoryIndex;

        private static float _nextGameScanTime;
        private const float GameChatScanInterval = 0.35f;
        private static ChatController _gameChatController;
        private static GameChatTextAdapter _gameAdapter;

        private static string _pendingOutgoingColoredGameChat;
        private static uint _pendingOutgoingColoredGameChatNetId;

        private static Func<bool> _gameModdedResolver = delegate { return false; };
        private static bool _hasExplicitGameModdedResolver;
        private static Func<bool> _autoGameModdedResolver;
        private static bool _autoResolverDiscoveryAttempted;

        internal static bool IsOpen
        {
            get
            {
                try
                {
                    return _blocker != null &&
                           _window != null &&
                           _blocker.activeInHierarchy &&
                           _window.activeInHierarchy;
                }
                catch { return false; }
            }
        }

        internal static void SetGameModdedResolver(Func<bool> resolver)
        {
            _gameModdedResolver = resolver ?? delegate { return false; };
            _hasExplicitGameModdedResolver = resolver != null;
        }

        internal static Button AttachToTmpInput(
            Transform parent,
            string name,
            TMP_InputField input,
            Vector2 position,
            Vector2 size,
            bool alwaysColor)
        {
            if (parent == null || input == null)
                return null;

            EnableRichText(input);

            Button button = CreateUiButton(parent, name, "Ω", position, size, new Color(0.18f, 0.22f, 0.34f, 0.98f));
            if (button != null)
            {
                button.onClick.AddListener((UnityAction)delegate
                {
                    OpenForTmpInput(input, alwaysColor, alwaysColor ? "COMMUNITY / PRIVATE" : "TEXT");
                });
            }
            return button;
        }

        internal static void OpenForTmpInput(TMP_InputField input, bool alwaysColor, string contextName)
        {
            if (input == null)
                return;

            EnableRichText(input);
            EnsurePickerUi();

            _contextName = string.IsNullOrWhiteSpace(contextName) ? "SYMBOLS" : contextName;
            _colorAllowed = alwaysColor ? (Func<bool>)(delegate { return true; }) : (delegate { return false; });
            _insertAction = delegate (string symbol)
            {
                string token = FormatSymbolForCurrentContext(symbol);
                InsertIntoTmpInput(input, token);
            };

            RefreshHeader();
            OpenWindow();
            try { input.ActivateInputField(); } catch { }
        }

        internal static void RuntimeTick()
        {
            try
            {
                if (IsOpen)
                    HandleFavoriteRightClickHitTest();

                if (PlayerControl.LocalPlayer == null)
                {
                    if (_gameLauncher != null)
                        _gameLauncher.SetActive(false);
                    _gameChatController = null;
                    _gameAdapter = null;
                    return;
                }

                if (Time.unscaledTime >= _nextGameScanTime)
                {
                    _nextGameScanTime = Time.unscaledTime + GameChatScanInterval;
                    RefreshGameChatAdapter();
                }

                bool showLauncher = _gameAdapter != null && _gameAdapter.IsVisible;
                if (showLauncher)
                {
                    EnsureGameLauncherUi();

                    if (_gameLauncher != null)
                        _gameLauncher.SetActive(!IsOpen);

                    try
                    {
                        RectTransform launcherRect = _gameLauncher != null ? _gameLauncher.GetComponent<RectTransform>() : null;
                        RectTransform canvasRect = _canvasRoot != null ? _canvasRoot.GetComponent<RectTransform>() : null;

                        if (!_gameLauncherManualPosition && !_gameLauncherDragging)
                            _gameAdapter.UpdateLauncherPosition(launcherRect, canvasRect);

                        if (!IsOpen)
                            HandleGameLauncherCalibrationDrag(launcherRect, canvasRect);
                    }
                    catch (Exception ex)
                    {
                        try { Debug.LogWarning("[BANMOD Symbols] Launcher drag handler failed: " + ex.Message); } catch { }
                    }
                }
                else
                {
                    _gameLauncherDragging = false;
                }

                if (_gameLauncher != null && (!showLauncher || IsOpen))
                    _gameLauncher.SetActive(false);
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[BANMOD Symbols] Runtime tick failed: " + ex.Message); } catch { }
            }
        }

        internal static string ToSafeRichText(string raw)
        {
            raw = raw ?? "";
            if (raw.Length == 0)
                return "";

            StringBuilder sb = new StringBuilder(raw.Length + 32);
            int index = 0;
            MatchCollection matches = AllowedColorSpanRegex.Matches(raw);
            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                if (m.Index > index)
                    AppendNoParse(sb, raw.Substring(index, m.Index - index));

                string hex = m.Groups["hex"].Value.ToUpperInvariant();
                string body = m.Groups["body"].Value;
                sb.Append("<color=#").Append(hex).Append(">");
                AppendNoParse(sb, body);
                sb.Append("</color>");
                index = m.Index + m.Length;
            }

            if (index < raw.Length)
                AppendNoParse(sb, raw.Substring(index));

            return sb.ToString();
        }

        internal static string StripPickerColorTags(string raw)
        {
            raw = raw ?? "";
            if (raw.Length == 0)
                return "";
            return AllowedColorSpanRegex.Replace(raw, delegate (Match m) { return m.Groups["body"].Value; });
        }

        internal static string TranslatePreservingPickerColors(string raw, Func<string, string> translate)
        {
            raw = raw ?? "";
            if (raw.Length == 0 || translate == null)
                return raw;

            MatchCollection matches = AllowedColorSpanRegex.Matches(raw);
            if (matches.Count == 0)
            {
                string translated = translate(raw);
                return string.IsNullOrWhiteSpace(translated) ? raw : translated;
            }

            StringBuilder sb = new StringBuilder(raw.Length + 32);
            int index = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                if (m.Index > index)
                {
                    string plain = raw.Substring(index, m.Index - index);
                    string translated = translate(plain);
                    sb.Append(string.IsNullOrWhiteSpace(translated) ? plain : translated);
                }

                sb.Append(m.Value);
                index = m.Index + m.Length;
            }

            if (index < raw.Length)
            {
                string plain = raw.Substring(index);
                string translated = translate(plain);
                sb.Append(string.IsNullOrWhiteSpace(translated) ? plain : translated);
            }

            return sb.ToString();
        }

        private static void AppendNoParse(StringBuilder sb, string literal)
        {
            if (string.IsNullOrEmpty(literal))
                return;

            literal = NoParseCloseRegex.Replace(literal, "‹/noparse›");
            sb.Append("<noparse>").Append(literal).Append("</noparse>");
        }

        private static void RefreshGameChatAdapter()
        {
            try
            {
                if (_gameChatController == null)
                {
                    try
                    {
                        if (HudManager.Instance != null)
                            _gameChatController = HudManager.Instance.GetComponentInChildren<ChatController>(true);
                    }
                    catch { }

                    if (_gameChatController == null)
                        _gameChatController = UnityEngine.Object.FindObjectOfType<ChatController>();

                    _gameAdapter = null;
                }

                if (_gameChatController == null)
                {
                    _gameAdapter = null;
                    return;
                }

                if (_gameAdapter == null || !_gameAdapter.IsAlive)
                    _gameAdapter = GameChatTextAdapter.TryCreate(_gameChatController);
            }
            catch
            {
                _gameChatController = null;
                _gameAdapter = null;
            }
        }

        private static void OpenForGameChat()
        {
            RefreshGameChatAdapter();
            if (_gameAdapter == null)
                return;

            EnsurePickerUi();
            _contextName = SafeGameColorAllowed() ? "GAME CHAT · MODDED COLOR" : "GAME CHAT · VANILLA SYMBOL ONLY";
            _colorAllowed = SafeGameColorAllowed;
            _insertAction = delegate (string symbol)
            {
                string token = FormatSymbolForCurrentContext(symbol);
                _gameAdapter.Insert(token);
            };
            RefreshHeader();
            OpenWindow();
            _gameAdapter.Focus();
        }

        private static bool SafeGameColorAllowed()
        {
            try
            {
                return BanModServerSelection.Mode == BanModServerMode.Modded25;
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsGamePickerSymbol(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return false;

            try
            {
                if (AllowedColorSpanRegex.IsMatch(raw))
                    return true;

                for (int i = 0; i < raw.Length; i++)
                {
                    if (GamePickerSymbolChars.Contains(raw[i]))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static string BuildDirectGameChatPayload(string raw)
        {
            raw = raw ?? "";

            if (SafeGameColorAllowed())
            {
                return KeepOnlyPickerColorTags(raw);
            }

            string plain = StripPickerColorTags(raw);
            return AnyRichTextTagRegex.Replace(plain, "");
        }

        internal static bool TrySendPickerGameChatDirect(PlayerControl player, string rawText)
        {
            if (player == null || string.IsNullOrEmpty(rawText))
                return false;

            if (!ContainsGamePickerSymbol(rawText))
                return false;

            try
            {
                string payload = BuildDirectGameChatPayload(rawText);
                if (string.IsNullOrEmpty(payload))
                {
                    return false;
                }


                RefreshGameChatAdapter();

                ChatController chat = _gameChatController;
                if (chat == null)
                {
                    try
                    {
                        if (HudManager.Instance != null)
                            chat = HudManager.Instance.GetComponentInChildren<ChatController>(true);
                    }
                    catch { }
                }

                var message =
                    new AmongUs.InnerNet.GameDataMessages.RpcSendChatMessage(
                        player.NetId,
                        payload);

                if (AmongUsClient.Instance == null)
                {
                    return false;
                }

                var gameDataMessage =
                    message.Cast<AmongUs.InnerNet.GameDataMessages.IGameDataMessage>();

                AmongUsClient.Instance.LateBroadcastReliableMessage(gameDataMessage);


                if (chat != null)
                {
                    try
                    {
                        chat.AddChat(player, payload);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void BeginOutgoingGameChat(PlayerControl player, string rawText)
        {
            _pendingOutgoingColoredGameChat = null;
            _pendingOutgoingColoredGameChatNetId = 0;

            bool colorAllowed = false;
            try { colorAllowed = SafeGameColorAllowed(); } catch { }


            try
            {
                if (!colorAllowed)
                {
                    return;
                }

                if (player == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(rawText))
                {
                    return;
                }

                bool hasColorSpan = AllowedColorSpanRegex.IsMatch(rawText);

                if (!hasColorSpan)
                {
                    return;
                }

                string safe = KeepOnlyPickerColorTags(rawText);

                if (string.IsNullOrEmpty(safe))
                {
                    return;
                }

                _pendingOutgoingColoredGameChat = safe;
                _pendingOutgoingColoredGameChatNetId = player.NetId;

            }
            catch (Exception)
            {
                _pendingOutgoingColoredGameChat = null;
                _pendingOutgoingColoredGameChatNetId = 0;
            }
        }

        internal static void EndOutgoingGameChat()
        {

            _pendingOutgoingColoredGameChat = null;
            _pendingOutgoingColoredGameChatNetId = 0;
        }

        internal static void RestorePendingColorForLocalBubble(PlayerControl sourcePlayer, ref string text)
        {

            try
            {
                if (string.IsNullOrEmpty(_pendingOutgoingColoredGameChat))
                {
                    return;
                }

                if (!SafeGameColorAllowed())
                {
                    return;
                }

                if (sourcePlayer == null || PlayerControl.LocalPlayer == null || sourcePlayer != PlayerControl.LocalPlayer)
                {
                    return;
                }

                text = _pendingOutgoingColoredGameChat;
            }
            catch (Exception)
            {
            }
        }

        internal static void RestorePendingColorForRpcMessage(uint netId, ref string text)
        {

            try
            {
                if (string.IsNullOrEmpty(_pendingOutgoingColoredGameChat))
                {
                    return;
                }

                if (!SafeGameColorAllowed())
                {
                    return;
                }

                if (_pendingOutgoingColoredGameChatNetId != 0 &&
                    netId != _pendingOutgoingColoredGameChatNetId)
                {
                    return;
                }

                text = _pendingOutgoingColoredGameChat;
            }
            catch (Exception)
            {
            }
        }

        private static string KeepOnlyPickerColorTags(string raw)
        {
            raw = raw ?? "";
            if (raw.Length == 0)
                return "";

            MatchCollection matches = AllowedColorSpanRegex.Matches(raw);
            if (matches.Count == 0)
                return AnyRichTextTagRegex.Replace(raw, "");

            StringBuilder sb = new StringBuilder(raw.Length);
            int index = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                if (m.Index > index)
                {
                    string plain = raw.Substring(index, m.Index - index);
                    sb.Append(AnyRichTextTagRegex.Replace(plain, ""));
                }

                string hex = m.Groups["hex"].Value.ToUpperInvariant();
                string body = AnyRichTextTagRegex.Replace(m.Groups["body"].Value ?? "", "");
                sb.Append("<color=#").Append(hex).Append(">").Append(body).Append("</color>");
                index = m.Index + m.Length;
            }

            if (index < raw.Length)
                sb.Append(AnyRichTextTagRegex.Replace(raw.Substring(index), ""));

            return sb.ToString();
        }

        private static Func<bool> DiscoverBanModModdedResolver()
        {
            try
            {
                Assembly assembly = typeof(BanModSymbolPicker).Assembly;
                Type[] types = assembly.GetTypes();
                string[] exactBoolNames =
                {
                    "IsFullyModdedLobby", "AllPlayersModded", "AllClientsModded",
                    "IsAllModded", "CanUseColoredGameChat"
                };

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.Namespace == null || !type.Namespace.StartsWith("BanMod", StringComparison.Ordinal))
                        continue;

                    for (int i = 0; i < exactBoolNames.Length; i++)
                    {
                        string name = exactBoolNames[i];
                        PropertyInfo prop = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
                        {
                            return delegate
                            {
                                try { return (bool)prop.GetValue(null, null); } catch { return false; }
                            };
                        }

                        FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null && field.FieldType == typeof(bool))
                        {
                            return delegate
                            {
                                try { return (bool)field.GetValue(null); } catch { return false; }
                            };
                        }
                    }
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.Namespace == null || !type.Namespace.StartsWith("BanMod", StringComparison.Ordinal))
                        continue;

                    FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        string n = field.Name ?? "";
                        bool looksLikeVersionMap =
                            n.IndexOf("PlayerVersion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("ClientVersion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("ModdedPlayers", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!looksLikeVersionMap)
                            continue;

                        return delegate
                        {
                            try
                            {
                                object value = field.GetValue(null);
                                System.Collections.ICollection collection = value as System.Collections.ICollection;
                                int playerCount = GameData.Instance == null ? 0 : GameData.Instance.PlayerCount;
                                return collection != null && playerCount > 0 && collection.Count >= playerCount;
                            }
                            catch { return false; }
                        };
                    }
                }
            }
            catch { }

            return delegate { return false; };
        }

        private static HashSet<char> BuildGamePickerSymbolChars()
        {
            HashSet<char> chars = new HashSet<char>();
            try
            {
                for (int i = 0; i < Categories.Length; i++)
                {
                    string symbols = Categories[i] == null ? "" : (Categories[i].Symbols ?? "");
                    for (int j = 0; j < symbols.Length; j++)
                    {
                        char c = symbols[j];
                        if (c != '|')
                            chars.Add(c);
                    }
                }
            }
            catch { }
            return chars;
        }

        private static void EnsureGameNativeSymbolWhitelist()
        {
            if (_gameNativeSymbolWhitelistInstalled)
                return;

            try
            {
                var native = TextBoxTMP.SymbolChars;
                if (native == null)
                    return;

                foreach (char c in GamePickerSymbolChars)
                {
                    if (!native.Contains(c))
                        native.Add(c);
                }

                const string RichTextSyntaxChars = "<>/=#";
                for (int i = 0; i < RichTextSyntaxChars.Length; i++)
                {
                    char c = RichTextSyntaxChars[i];
                    if (!native.Contains(c))
                        native.Add(c);
                }

                _gameNativeSymbolWhitelistInstalled = true;
            }
            catch
            {
                _gameNativeSymbolWhitelistInstalled = false;
            }
        }

        internal static void RegisterGameTextBox(TextBoxTMP textBox)
        {
            try
            {
                EnsureGameNativeSymbolWhitelist();
                _gameTextBoxInstanceId = textBox == null ? 0 : textBox.GetInstanceID();
                if (textBox != null)
                    textBox.AllowSymbols = true;
            }
            catch { _gameTextBoxInstanceId = 0; }
        }

        internal static bool IsRegisteredGameTextBox(TextBoxTMP textBox)
        {
            if (textBox == null || _gameTextBoxInstanceId == 0)
                return false;
            try { return textBox.GetInstanceID() == _gameTextBoxInstanceId; }
            catch { return false; }
        }

        internal static bool ShouldForceAllowGameCharacter(TextBoxTMP textBox, object[] args)
        {
            if (!IsRegisteredGameTextBox(textBox) || args == null)
                return false;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    object arg = args[i];
                    if (arg is char c)
                        return GamePickerSymbolChars.Contains(c);

                    if (arg is string str && str.Length == 1)
                        return GamePickerSymbolChars.Contains(str[0]);

                    if (arg is int code && code >= char.MinValue && code <= char.MaxValue)
                        return GamePickerSymbolChars.Contains((char)code);
                }
            }
            catch { }

            return false;
        }

        private static string FormatSymbolForCurrentContext(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return "";

            bool useColor = false;
            try { useColor = _colorAllowed != null && _colorAllowed(); } catch { }
            if (!useColor)
                return symbol;

            string hex = string.IsNullOrWhiteSpace(_selectedHex) ? "FFFFFF" : _selectedHex.Trim().ToUpperInvariant();
            return "<color=#" + hex + ">" + symbol + "</color>";
        }

        private static void InsertIntoTmpInput(TMP_InputField input, string token)
        {
            if (input == null || string.IsNullOrEmpty(token))
                return;

            try
            {
                string current = input.text ?? "";

                int fallbackCaret = 0;
                try { fallbackCaret = input.caretPosition; } catch { }

                int anchorVisual = ReadTmpInputPosition(
                    input,
                    fallbackCaret,
                    "selectionAnchorPosition");

                int focusVisual = ReadTmpInputPosition(
                    input,
                    anchorVisual,
                    "selectionFocusPosition");

                int visibleLength = GetPickerVisibleLength(current);
                anchorVisual = Mathf.Clamp(anchorVisual, 0, visibleLength);
                focusVisual = Mathf.Clamp(focusVisual, 0, visibleLength);

                int startVisual = Math.Min(anchorVisual, focusVisual);
                int endVisual = Math.Max(anchorVisual, focusVisual);
                int startRaw = PickerVisibleIndexToRawIndex(current, startVisual);
                int endRaw = PickerVisibleIndexToRawIndex(current, endVisual);

                startRaw = Mathf.Clamp(startRaw, 0, current.Length);
                endRaw = Mathf.Clamp(endRaw, startRaw, current.Length);

                string next = current.Remove(startRaw, endRaw - startRaw).Insert(startRaw, token);

                int nextVisibleLength = GetPickerVisibleLength(next);
                if (input.characterLimit > 0 && nextVisibleLength > input.characterLimit)
                    return;

                input.text = next;

                int insertedVisibleLength = GetPickerVisibleLength(token);
                int nextVisualCaret = startVisual + insertedVisibleLength;
                int nextRawCaret = startRaw + token.Length;

                WriteTmpInputPosition(input, nextRawCaret, "stringPosition");
                WriteTmpInputPosition(input, nextRawCaret, "stringSelectPosition");
                WriteTmpInputPosition(input, nextVisualCaret, "selectionAnchorPosition");
                WriteTmpInputPosition(input, nextVisualCaret, "selectionFocusPosition");
                try { input.caretPosition = nextVisualCaret; } catch { }

                try { input.ActivateInputField(); } catch { }
                try { input.ForceLabelUpdate(); } catch { }
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[BANMOD Symbols] TMP insertion failed: " + ex.Message); } catch { }
            }
        }

        private static int GetPickerVisibleLength(string raw)
        {
            raw = raw ?? "";
            if (raw.Length == 0)
                return 0;

            try { return StripPickerColorTags(raw).Length; }
            catch { return raw.Length; }
        }
        private static int PickerVisibleIndexToRawIndex(string raw, int visibleIndex)
        {
            raw = raw ?? "";
            if (raw.Length == 0 || visibleIndex <= 0)
                return 0;

            int maxVisible = GetPickerVisibleLength(raw);
            visibleIndex = Mathf.Clamp(visibleIndex, 0, maxVisible);

            int rawIndex = 0;
            int visible = 0;
            MatchCollection matches = AllowedColorSpanRegex.Matches(raw);

            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                if (m.Index < rawIndex)
                    continue;

                int plainLength = m.Index - rawIndex;
                if (visibleIndex <= visible + plainLength)
                    return rawIndex + (visibleIndex - visible);

                visible += plainLength;

                string body = m.Groups["body"].Value ?? "";
                int bodyLength = body.Length;
                if (visibleIndex < visible + bodyLength)
                {
                    int bodyOffset = visibleIndex - visible;
                    return m.Groups["body"].Index + bodyOffset;
                }

                visible += bodyLength;
                rawIndex = m.Index + m.Length;

                if (visibleIndex == visible)
                    return rawIndex;
            }

            int remaining = raw.Length - rawIndex;
            if (visibleIndex <= visible + remaining)
                return rawIndex + (visibleIndex - visible);

            return raw.Length;
        }

        private static int ReadTmpInputPosition(TMP_InputField input, int fallback, params string[] propertyNames)
        {
            if (input == null || propertyNames == null)
                return fallback;

            Type type = input.GetType();
            for (int i = 0; i < propertyNames.Length; i++)
            {
                string name = propertyNames[i];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                try
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property == null || !property.CanRead)
                        continue;

                    object value = property.GetValue(input, null);
                    if (value is int)
                        return (int)value;
                }
                catch { }
            }

            return fallback;
        }

        private static void WriteTmpInputPosition(TMP_InputField input, int value, params string[] propertyNames)
        {
            if (input == null || propertyNames == null)
                return;

            Type type = input.GetType();
            for (int i = 0; i < propertyNames.Length; i++)
            {
                string name = propertyNames[i];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                try
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property == null || !property.CanWrite || property.PropertyType != typeof(int))
                        continue;

                    property.SetValue(input, value, null);
                    return;
                }
                catch { }
            }
        }

        private static void EnableRichText(TMP_InputField input)
        {
            if (input == null)
                return;
            try { input.richText = true; } catch { }
            try { if (input.textComponent != null) input.textComponent.richText = true; } catch { }
        }

        private static void EnsureCanvasRoot()
        {
            if (_canvasRoot != null)
                return;

            _canvasRoot = new GameObject("BanMod_SymbolPickerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasRoot);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;

            canvas.sortingOrder = 32000;

            CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRoot.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureGameLauncherUi()
        {
            EnsureCanvasRoot();
            if (_gameLauncher == null)
                BuildGameLauncher();
        }

        private static void EnsurePickerUi()
        {
            EnsureCanvasRoot();

            if (_gameLauncher == null)
                BuildGameLauncher();

            if (_blocker == null || _window == null)
                BuildPickerWindow();
        }


        private static void BuildGameLauncher()
        {
            _gameLauncher = new GameObject("GameChatSymbolLauncher");
            _gameLauncher.transform.SetParent(_canvasRoot.transform, false);

            RectTransform rt = _gameLauncher.AddComponent<RectTransform>();

            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-520f, 155f);

            rt.sizeDelta = new Vector2(50f, 50f);

            Image image = _gameLauncher.AddComponent<Image>();
            image.sprite = Utils.LoadSprite("BanMod.Resources.image.icon.png", 100f);
            image.color = Color.white;
            image.raycastTarget = true;
            image.preserveAspect = true;

            _gameLauncherButton = _gameLauncher.AddComponent<Button>();
            _gameLauncherButton.targetGraphic = image;
            _gameLauncherButton.transition = Selectable.Transition.None;

            _gameLauncherButton.onClick.AddListener((UnityAction)delegate
            {
                if (Time.unscaledTime < _gameLauncherSuppressClickUntil)
                    return;

                OpenForGameChat();
            });

            _gameLauncher.SetActive(false);
        }

        private static void HandleGameLauncherCalibrationDrag(RectTransform launcherRect, RectTransform canvasRect)
        {
            if (!GameLauncherCalibrationDragEnabled || launcherRect == null || canvasRect == null)
                return;

            Vector2 mouseScreen = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                bool overLauncher = false;
                try { overLauncher = RectTransformUtility.RectangleContainsScreenPoint(launcherRect, mouseScreen, null); }
                catch { }

                if (!overLauncher)
                    return;

                Vector2 launcherScreen = RectTransformUtility.WorldToScreenPoint(null, launcherRect.position);
                Vector2 launcherLocal;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, launcherScreen, null, out launcherLocal))
                {
                    launcherRect.anchorMin = launcherRect.anchorMax = new Vector2(0.5f, 0.5f);
                    launcherRect.pivot = new Vector2(0.5f, 0.5f);
                    launcherRect.anchoredPosition = launcherLocal;
                }

                Vector2 pointerLocal;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, null, out pointerLocal))
                    return;

                _gameLauncherDragging = true;
                _gameLauncherManualPosition = true;
                _gameLauncherDragStartScreen = mouseScreen;
                _gameLauncherDragOffsetCanvas = launcherRect.anchoredPosition - pointerLocal;
                _gameLauncherNextDragLogTime = 0f;

            }

            if (!_gameLauncherDragging)
                return;

            if (Input.GetMouseButton(0))
            {
                Vector2 pointerLocal;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, null, out pointerLocal))
                {
                    launcherRect.anchoredPosition = pointerLocal + _gameLauncherDragOffsetCanvas;

                    float moved = Vector2.Distance(mouseScreen, _gameLauncherDragStartScreen);
                    if (moved >= GameLauncherDragThresholdPixels)
                        _gameLauncherSuppressClickUntil = Time.unscaledTime + 0.35f;

                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                _gameLauncherDragging = false;
                _gameLauncherSuppressClickUntil = Time.unscaledTime + 0.35f;

            }
        }

        private static string BuildLauncherPositionLog(RectTransform launcherRect)
        {
            if (launcherRect == null)
                return "launcher=null";

            Vector2 anchored = launcherRect.anchoredPosition;
            Vector2 screen = Vector2.zero;
            try { screen = RectTransformUtility.WorldToScreenPoint(null, launcherRect.position); } catch { }

            float nx = Screen.width > 0 ? screen.x / Screen.width : 0f;
            float ny = Screen.height > 0 ? screen.y / Screen.height : 0f;

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "anchored=({0:0.00},{1:0.00}) anchor=(0.5,0.5) screen=({2:0.0},{3:0.0}) normalized=({4:0.0000},{5:0.0000}) resolution={6}x{7}",
                anchored.x,
                anchored.y,
                screen.x,
                screen.y,
                nx,
                ny,
                Screen.width,
                Screen.height);
        }

        private static void BuildPickerWindow()
        {
            _blocker = new GameObject("PickerBlocker");
            _blocker.transform.SetParent(_canvasRoot.transform, false);
            RectTransform blockerRt = _blocker.AddComponent<RectTransform>();
            Stretch(blockerRt);
            Image blockerImage = _blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.64f);
            blockerImage.raycastTarget = true;
            Button blockerButton = _blocker.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;
            blockerButton.targetGraphic = blockerImage;
            blockerButton.onClick.AddListener((UnityAction)CloseWindow);

            _window = new GameObject("PickerWindow");
            _window.transform.SetParent(_blocker.transform, false);
            RectTransform wrt = _window.AddComponent<RectTransform>();
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(1080f, 720f);
            wrt.anchoredPosition = Vector2.zero;
            Image wbg = _window.AddComponent<Image>();
            wbg.color = new Color(0.055f, 0.065f, 0.09f, 0.995f);
            wbg.raycastTarget = true;

            TextMeshProUGUI title = CreateText(_window.transform, "AMONG US SYMBOLS", 28f, TextAlignmentOptions.Left, Color.white, new Vector2(-330f, 326f), new Vector2(360f, 44f), false);
            title.fontStyle = FontStyles.Bold;
            _contextText = CreateText(_window.transform, "", 15f, TextAlignmentOptions.Left, new Color(0.65f, 0.72f, 0.86f, 1f), new Vector2(45f, 327f), new Vector2(390f, 38f), false);
            CreateUiButton(_window.transform, "Close", "×", new Vector2(505f, 328f), new Vector2(52f, 48f), new Color(0.50f, 0.13f, 0.16f, 1f))
                .onClick.AddListener((UnityAction)CloseWindow);

            CreateText(_window.transform, "COLOR", 14f, TextAlignmentOptions.Left, new Color(0.78f, 0.82f, 0.90f, 1f), new Vector2(-463f, 272f), new Vector2(120f, 24f), false);
            _colorText = CreateText(_window.transform, "#FFFFFF", 14f, TextAlignmentOptions.Left, Color.white, new Vector2(-330f, 272f), new Vector2(120f, 24f), false);
            BuildPalette();

            TextMeshProUGUI favoritesHint = CreateText(
                _window.transform,
                "Tip: Right-click any symbol to add or remove it from Favorites.",
                13f,
                TextAlignmentOptions.Left,
                new Color(0.68f, 0.74f, 0.84f, 1f),
                new Vector2(112f, 232f),
                new Vector2(840f, 28f),
                false);
            favoritesHint.fontStyle = FontStyles.Italic;

            EnsureFavoritesLoaded();
            BuildCategoryScroll();
            BuildSymbolScroll();
            SelectCategory(0);

            _window.SetActive(false);
            _blocker.SetActive(false);
        }

        private static void BuildPalette()
        {
            float startX = -250f;
            float y = 271f;
            float step = 31f;
            for (int i = 0; i < PaletteHex.Length; i++)
            {
                int captured = i;
                Color color;
                if (!ColorUtility.TryParseHtmlString("#" + PaletteHex[i], out color))
                    color = Color.white;

                Button b = CreateUiButton(_window.transform, "Color_" + PaletteHex[i], "", new Vector2(startX + i * step, y), new Vector2(26f, 26f), color);
                b.onClick.AddListener((UnityAction)delegate
                {
                    _selectedHex = PaletteHex[captured];
                    RefreshHeader();
                });
            }
        }

        private static void BuildCategoryScroll()
        {
            GameObject root = new GameObject("CategoryScroll");
            root.transform.SetParent(_window.transform, false);
            RectTransform rr = root.AddComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = new Vector2(-430f, -55f);
            rr.sizeDelta = new Vector2(190f, 530f);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.13f, 1f);
            bg.raycastTarget = true;
            ScrollRect scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(root.transform, false);
            RectTransform vrt = viewport.AddComponent<RectTransform>();
            Stretch(vrt, new Vector4(6f, 6f, 6f, 6f));
            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0.001f);
            vpImage.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _categoryContent = content.AddComponent<RectTransform>();
            _categoryContent.anchorMin = new Vector2(0f, 1f);
            _categoryContent.anchorMax = new Vector2(1f, 1f);
            _categoryContent.pivot = new Vector2(0.5f, 1f);
            _categoryContent.anchoredPosition = Vector2.zero;

            int categoryCount = GetCategoryCount();
            _categoryContent.sizeDelta = new Vector2(0f, categoryCount * 44f + 12f);

            scroll.viewport = vrt;
            scroll.content = _categoryContent;

            for (int i = 0; i < categoryCount; i++)
            {
                int captured = i;
                Button b = CreateUiButton(_categoryContent, "Category_" + i, GetCategoryName(i),
                    new Vector2(0f, -12f - i * 44f), new Vector2(164f, 38f), new Color(0.14f, 0.16f, 0.22f, 1f));
                RectTransform br = b.GetComponent<RectTransform>();
                if (br != null)
                {
                    br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
                    br.pivot = new Vector2(0.5f, 1f);
                    br.anchoredPosition = new Vector2(0f, -6f - i * 44f);
                }
                b.onClick.AddListener((UnityAction)delegate { SelectCategory(captured); });
                TextMeshProUGUI text = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    text.fontSize = 13f;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.margin = new Vector4(8f, 0f, 3f, 0f);
                }
            }
        }

        private static void BuildSymbolScroll()
        {
            GameObject root = new GameObject("SymbolScroll");
            root.transform.SetParent(_window.transform, false);
            RectTransform rr = root.AddComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = new Vector2(120f, -55f);
            rr.sizeDelta = new Vector2(850f, 530f);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.075f, 0.085f, 0.12f, 1f);
            bg.raycastTarget = true;
            ScrollRect scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(root.transform, false);
            RectTransform vrt = viewport.AddComponent<RectTransform>();
            Stretch(vrt, new Vector4(10f, 10f, 10f, 10f));
            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0.001f);
            vpImage.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _symbolContent = content.AddComponent<RectTransform>();
            _symbolContent.anchorMin = new Vector2(0f, 1f);
            _symbolContent.anchorMax = new Vector2(1f, 1f);
            _symbolContent.pivot = new Vector2(0.5f, 1f);
            _symbolContent.anchoredPosition = Vector2.zero;
            _symbolContent.sizeDelta = new Vector2(0f, 540f);

            scroll.viewport = vrt;
            scroll.content = _symbolContent;
        }

        private static void SelectCategory(int index)
        {
            if (_symbolContent == null)
                return;

            EnsureFavoritesLoaded();

            int categoryCount = GetCategoryCount();
            if (categoryCount <= 0)
                return;

            _selectedCategoryIndex = Mathf.Clamp(index, 0, categoryCount - 1);
            FavoriteHitTargets.Clear();
            ClearChildren(_symbolContent);

            List<string> symbols = GetCategorySymbols(_selectedCategoryIndex);
            const int columns = 10;
            const float cellW = 78f;
            const float cellH = 58f;
            const float buttonW = 66f;
            const float buttonH = 48f;
            float startX = -352f;
            float startY = -32f;

            if (_selectedCategoryIndex == FavoritesCategoryIndex && symbols.Count == 0)
            {
                TextMeshProUGUI empty = CreateText(
                    _symbolContent,
                    "No favorites yet.\nRight-click any symbol to add it here.",
                    19f,
                    TextAlignmentOptions.Center,
                    new Color(0.72f, 0.76f, 0.86f, 1f),
                    new Vector2(0f, -115f),
                    new Vector2(720f, 80f),
                    false);
                empty.enableWordWrapping = true;
            }

            for (int i = 0; i < symbols.Count; i++)
            {
                string captured = symbols[i];
                int row = i / columns;
                int col = i % columns;
                Vector2 pos = new Vector2(startX + col * cellW, startY - row * cellH);
                Button b = CreateUiButton(_symbolContent, "Symbol_" + i, captured, pos, new Vector2(buttonW, buttonH), new Color(0.14f, 0.16f, 0.22f, 1f));
                RectTransform br = b.GetComponent<RectTransform>();
                if (br != null)
                {
                    br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
                    br.pivot = new Vector2(0.5f, 0.5f);
                    br.anchoredPosition = pos;
                }

                b.onClick.AddListener((UnityAction)delegate
                {
                    try { _insertAction?.Invoke(captured); } catch { }
                });

                FavoriteHitTargets.Add(new FavoriteHitTarget
                {
                    Rect = br,
                    Symbol = captured
                });

                TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (t != null)
                {
                    t.fontSize = 24f;
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 13f;
                    t.fontSizeMax = 26f;
                }
            }

            int rows = (symbols.Count + columns - 1) / columns;
            _symbolContent.sizeDelta = new Vector2(0f, Mathf.Max(540f, 24f + rows * cellH));
            RefreshHeader();
        }

        private static int GetCategoryCount()
        {
            return Categories.Length + 1;
        }

        private static string GetCategoryName(int index)
        {
            if (index == FavoritesCategoryIndex)
                return FavoritesCategoryName;

            int realIndex = index - 1;
            if (realIndex < 0 || realIndex >= Categories.Length)
                return "Symbols";

            return Categories[realIndex].Name;
        }

        private static List<string> GetCategorySymbols(int index)
        {
            if (index == FavoritesCategoryIndex)
                return new List<string>(FavoriteSymbols);

            int realIndex = index - 1;
            if (realIndex < 0 || realIndex >= Categories.Length)
                return new List<string>();

            return SplitUnique(Categories[realIndex].Symbols);
        }

        private static void EnsureFavoritesLoaded()
        {
            if (_favoritesLoaded)
                return;

            _favoritesLoaded = true;
            FavoriteSymbols.Clear();
            FavoriteSymbolSet.Clear();

            try
            {
                Directory.CreateDirectory(FavoritesFolder);
                if (!File.Exists(FavoritesFilePath))
                    return;

                string[] lines = File.ReadAllLines(FavoritesFilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string symbol = (lines[i] ?? "").Trim();
                    if (string.IsNullOrEmpty(symbol) || !FavoriteSymbolSet.Add(symbol))
                        continue;

                    FavoriteSymbols.Add(symbol);
                }
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[BANMOD Symbols] Failed to load favorites: " + ex.Message); } catch { }
            }
        }

        private static void SaveFavorites()
        {
            try
            {
                Directory.CreateDirectory(FavoritesFolder);
                File.WriteAllText(
                    FavoritesFilePath,
                    string.Join("\n", FavoriteSymbols.ToArray()),
                    new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[BANMOD Symbols] Failed to save favorites: " + ex.Message); } catch { }
            }
        }

        private static void ToggleFavorite(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return;

            EnsureFavoritesLoaded();

            if (FavoriteSymbolSet.Remove(symbol))
            {
                FavoriteSymbols.Remove(symbol);
            }
            else
            {
                FavoriteSymbolSet.Add(symbol);
                FavoriteSymbols.Add(symbol);
            }

            SaveFavorites();

            if (_selectedCategoryIndex == FavoritesCategoryIndex)
                SelectCategory(FavoritesCategoryIndex);
            else
                RefreshHeader();
        }

        private static void HandleFavoriteRightClickHitTest()
        {
            if (!Input.GetMouseButtonDown(1) || FavoriteHitTargets.Count == 0)
                return;

            Vector2 mouse = Input.mousePosition;
            for (int i = FavoriteHitTargets.Count - 1; i >= 0; i--)
            {
                FavoriteHitTarget hit = FavoriteHitTargets[i];
                if (hit == null || hit.Rect == null || string.IsNullOrEmpty(hit.Symbol))
                    continue;

                try
                {
                    if (!hit.Rect.gameObject.activeInHierarchy)
                        continue;

                    if (!RectTransformUtility.RectangleContainsScreenPoint(hit.Rect, mouse, null))
                        continue;

                    ToggleFavorite(hit.Symbol);
                    return;
                }
                catch { }
            }
        }

        private static List<string> SplitUnique(string packed)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            string[] parts = (packed ?? "").Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i];
                if (string.IsNullOrEmpty(s) || !seen.Add(s))
                    continue;
                result.Add(s);
            }
            return result;
        }

        private static void RefreshHeader()
        {
            try
            {
                bool colored = _colorAllowed != null && _colorAllowed();
                if (_contextText != null)
                    _contextText.text = _contextName + "  ·  " + GetCategoryName(Mathf.Clamp(_selectedCategoryIndex, 0, GetCategoryCount() - 1));
                if (_colorText != null)
                {
                    _colorText.text = colored ? "#" + _selectedHex : "NO COLOR";
                    Color parsed;
                    _colorText.color = colored && ColorUtility.TryParseHtmlString("#" + _selectedHex, out parsed) ? parsed : Color.white;
                }
            }
            catch { }
        }

        private static void OpenWindow()
        {
            if (_blocker != null) _blocker.SetActive(true);
            if (_window != null) _window.SetActive(true);
            if (_gameLauncher != null) _gameLauncher.SetActive(false);
        }

        private static void CloseWindow()
        {
            if (_window != null) _window.SetActive(false);
            if (_blocker != null) _blocker.SetActive(false);
            _insertAction = null;
            _colorAllowed = null;
        }

        private static Button CreateUiButton(Transform parent, string name, string text, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            cb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = cb;
            if (!string.IsNullOrEmpty(text))
                CreateText(go.transform, text, 20f, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.zero, true);
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color, Vector2 position, Vector2 size, bool stretch)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            if (stretch)
            {
                Stretch(rt, new Vector4(3f, 3f, 3f, 3f));
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = position;
                rt.sizeDelta = size;
            }

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text ?? "";
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            tmp.richText = true;
            try { if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset; } catch { }
            return tmp;
        }

        private static void Stretch(RectTransform rt)
        {
            Stretch(rt, Vector4.zero);
        }

        private static void Stretch(RectTransform rt, Vector4 pad)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad.x, pad.y);
            rt.offsetMax = new Vector2(-pad.z, -pad.w);
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private sealed class GameChatTextAdapter
        {
            private readonly ChatController _chat;
            private readonly FreeChatInputField _freeChat;
            private readonly TextBoxTMP _textBox;
            private readonly ChatInputFieldButton _submitButton;

            private GameChatTextAdapter(
                ChatController chat,
                FreeChatInputField freeChat,
                TextBoxTMP textBox,
                ChatInputFieldButton submitButton)
            {
                _chat = chat;
                _freeChat = freeChat;
                _textBox = textBox;
                _submitButton = submitButton;
            }

            internal bool IsAlive
            {
                get
                {
                    try { return _chat != null && _freeChat != null && _textBox != null; }
                    catch { return false; }
                }
            }

            internal bool IsVisible
            {
                get
                {
                    try
                    {
                        if (!IsAlive || !_chat.gameObject.activeInHierarchy || !_freeChat.gameObject.activeInHierarchy)
                            return false;

                        try
                        {
                            if (_chat.IsOpenOrOpening)
                                return true;
                        }
                        catch { }

                        if (IsSubmitButtonActuallyVisible())
                            return true;

                        SpriteRenderer bg = null;
                        try { bg = _freeChat.Background; } catch { }
                        if (bg != null && bg.gameObject.activeInHierarchy && IsBoundsInsideScreen(bg.bounds, 140f))
                            return true;

                        try
                        {
                            if (_textBox.outputText != null &&
                                _textBox.outputText.gameObject.activeInHierarchy &&
                                _textBox.outputText.enabled)
                            {
                                Renderer renderer = _textBox.outputText.GetComponent<Renderer>();
                                if (renderer != null && renderer.enabled && IsBoundsInsideScreen(renderer.bounds, 140f))
                                    return true;
                            }
                        }
                        catch { }

                        return false;
                    }
                    catch { return false; }
                }
            }

            private bool IsSubmitButtonActuallyVisible()
            {
                if (_submitButton == null || !_submitButton.gameObject.activeInHierarchy)
                    return false;

                try
                {
                    Renderer[] renderers = _submitButton.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                            continue;
                        if (IsBoundsInsideScreen(renderer.bounds, 140f))
                            return true;
                    }
                }
                catch { }

                return false;
            }

            private static bool IsBoundsInsideScreen(Bounds bounds, float margin)
            {
                try
                {
                    Camera camera = GetGameUiCamera();
                    if (camera == null)
                        return true;

                    Vector3 screen = RectTransformUtility.WorldToScreenPoint(camera, bounds.center);
                    return screen.x >= -margin && screen.x <= Screen.width + margin &&
                           screen.y >= -margin && screen.y <= Screen.height + margin;
                }
                catch { return false; }
            }

            internal static GameChatTextAdapter TryCreate(ChatController chat)
            {
                if (chat == null)
                    return null;

                try
                {
                    FreeChatInputField freeChat = null;
                    TextBoxTMP textBox = null;
                    ChatInputFieldButton submitButton = null;

                    try { freeChat = chat.GetComponentInChildren<FreeChatInputField>(true); } catch { }

                    if (freeChat == null)
                    {
                        Component[] components = chat.GetComponentsInChildren<Component>(true);
                        for (int i = 0; i < components.Length; i++)
                        {
                            Component c = components[i];
                            if (c == null)
                                continue;
                            if (freeChat == null)
                                freeChat = c as FreeChatInputField;
                        }
                    }

                    if (freeChat == null)
                        return null;

                    try { textBox = freeChat.GetComponentInChildren<TextBoxTMP>(true); } catch { }
                    try { submitButton = freeChat.GetComponentInChildren<ChatInputFieldButton>(true); } catch { }

                    Component[] freeComponents = freeChat.GetComponentsInChildren<Component>(true);
                    for (int i = 0; i < freeComponents.Length; i++)
                    {
                        Component c = freeComponents[i];
                        if (c == null)
                            continue;

                        if (textBox == null)
                            textBox = c as TextBoxTMP;

                        if (submitButton == null)
                            submitButton = c as ChatInputFieldButton;
                    }

                    if (submitButton == null)
                    {
                        try
                        {
                            ChatInputFieldButton[] allSubmitButtons = chat.GetComponentsInChildren<ChatInputFieldButton>(true);
                            if (allSubmitButtons != null && allSubmitButtons.Length > 0)
                                submitButton = allSubmitButtons[0];
                        }
                        catch { }
                    }

                    if (textBox == null)
                        return null;

                    try { textBox.AllowSymbols = true; } catch { }
                    RegisterGameTextBox(textBox);

                    return new GameChatTextAdapter(chat, freeChat, textBox, submitButton);
                }
                catch (Exception ex)
                {
                    try { Debug.LogWarning("[BANMOD Symbols] FreeChat adapter creation failed: " + ex.Message); } catch { }
                    return null;
                }
            }

            internal void Insert(string token)
            {

                if (string.IsNullOrEmpty(token) || !IsAlive)
                {
                    return;
                }

                try
                {
                    string current = _freeChat.Text ?? _textBox.text ?? "";
                    string next = current + token;


                    int limit = _textBox.characterLimit;
                    if (limit > 0 && next.Length > limit)
                        return;

                    EnsureGameNativeSymbolWhitelist();
                    _textBox.AllowSymbols = true;


                    _textBox.SetText(next, "");

                    if (!string.Equals(_textBox.text ?? "", next, StringComparison.Ordinal))
                    {

                        _textBox.text = next;

                        if (_textBox.outputText != null)
                        {
                            _textBox.outputText.richText = true;
                            _textBox.outputText.text = next;
                            _textBox.outputText.ForceMeshUpdate(true, true);
                        }

                        try
                        {
                            if (_textBox.OnChange != null)
                                _textBox.OnChange.Invoke();
                        }
                        catch { }
                    }

                    try
                    {
                        if (_textBox.outputText != null)
                        {
                            _textBox.outputText.richText = true;
                            _textBox.outputText.text = _textBox.text ?? next;
                            _textBox.outputText.ForceMeshUpdate(true, true);
                        }
                    }
                    catch { }
                    try
                    {
                        if (_textBox.OnChange != null)
                            _textBox.OnChange.Invoke();
                    }
                    catch { }

                    try { _freeChat.UpdateCharCount(); } catch { }
                    try { _freeChat.Focus(); } catch { try { _textBox.GiveFocus(); } catch { } }

                }
                catch (Exception ex)
                {
                    try { Debug.LogWarning("[BANMOD Symbols] FreeChat insertion failed: " + ex.Message); } catch { }
                }
            }

            internal void UpdateLauncherPosition(RectTransform launcherRect, RectTransform canvasRect)
            {
                if (launcherRect == null)
                    return;

                launcherRect.anchorMin = launcherRect.anchorMax = new Vector2(0.5f, 0.5f);
                launcherRect.pivot = new Vector2(0.5f, 0.5f);
                launcherRect.anchoredPosition = new Vector2(-577.50f, -221.08f);
            }

            private static Camera GetGameUiCamera()
            {
                Camera camera = null;
                try
                {
                    if (HudManager.Instance != null)
                        camera = HudManager.Instance.UICamera;
                }
                catch { }
                if (camera == null)
                    camera = Camera.main;
                return camera;
            }

            private bool TryGetSubmitBounds(out Bounds bounds)
            {
                bounds = new Bounds();
                if (_submitButton == null)
                    return false;

                bool found = false;
                try
                {
                    Component[] components = _submitButton.GetComponentsInChildren<Component>(true);
                    for (int i = 0; i < components.Length; i++)
                    {
                        SpriteRenderer renderer = components[i] as SpriteRenderer;
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                            continue;

                        if (!found)
                        {
                            bounds = renderer.bounds;
                            found = true;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }
                }
                catch { }

                return found;
            }

            internal void Focus()
            {
                try
                {
                    if (_freeChat != null)
                        _freeChat.Focus();
                    else if (_textBox != null)
                        _textBox.GiveFocus();
                }
                catch { }
            }
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;
            Type type = obj.GetType();
            try
            {
                FieldInfo field = AccessTools.Field(type, name);
                if (field != null) return field.GetValue(obj);
            }
            catch { }
            try
            {
                PropertyInfo prop = AccessTools.Property(type, name);
                if (prop != null && prop.CanRead) return prop.GetValue(obj, null);
            }
            catch { }
            return null;
        }

        private static bool SetMember(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            Type type = obj.GetType();
            try
            {
                FieldInfo field = AccessTools.Field(type, name);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
            try
            {
                PropertyInfo prop = AccessTools.Property(type, name);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(obj, value, null);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static int ReadInt(object obj, int fallback, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                object value = GetMember(obj, names[i]);
                if (value is int n) return n;
            }
            return fallback;
        }

        private static void SetIntMember(object obj, string name, int value)
        {
            try { SetMember(obj, name, value); } catch { }
        }

        private static void InvokeIntMethod(object obj, string name, int value)
        {
            if (obj == null) return;
            try
            {
                MethodInfo method = AccessTools.Method(obj.GetType(), name, new Type[] { typeof(int) });
                if (method != null) method.Invoke(obj, new object[] { value });
            }
            catch { }
        }

        private static void TryInvokeNoArgs(object obj, string name)
        {
            if (obj == null) return;
            try
            {
                MethodInfo method = AccessTools.Method(obj.GetType(), name, Type.EmptyTypes);
                if (method != null) method.Invoke(obj, null);
            }
            catch { }
        }
    }

    [HarmonyPatch]
    internal static class BanModTextBoxTMPSetTextSymbolsPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(TextBoxTMP),
                "SetText",
                new Type[] { typeof(string), typeof(string) });
        }

        private static void Prefix(TextBoxTMP __instance, string __0, string __1)
        {
            if (!BanModSymbolPicker.IsRegisteredGameTextBox(__instance))
                return;


            try { __instance.AllowSymbols = true; } catch { }

        }

    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    internal static class BanModPickerGameChatDirectSendPatch
    {
        private static bool Prefix(
            PlayerControl __instance,
            string __0,
            ref bool __result)
        {
            try
            {
                if (!BanModSymbolPicker.TrySendPickerGameChatDirect(__instance, __0))
                    return true;

                __result = true;


                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    internal static class BanModSymbolPickerRuntimePatch
    {
        private static void Postfix()
        {
            BanModSymbolPicker.RuntimeTick();
        }
    }
}
