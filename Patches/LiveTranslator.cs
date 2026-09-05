using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace BanMod
{
    public static class LiveTranslator
    {
        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<bool> TranslateIncoming;
        private static ConfigEntry<bool> TranslateOutgoing;
        private static ConfigEntry<bool> ShowOriginalIncoming;
        private static ConfigEntry<bool> ShowStatusInChat;
        private static ConfigEntry<bool> OutgoingRequiresPrefix;
        private static ConfigEntry<bool> GameAwareTranslations;
        private static ConfigEntry<bool> TranslateWhenChatClosed;
        private static ConfigEntry<int> ConfigVersion;

        private static ConfigEntry<string> ReadFromLang;
        private static ConfigEntry<string> ReadToLang;
        private static ConfigEntry<string> SendFromLang;
        private static ConfigEntry<string> SendToLang;
        private static ConfigEntry<string> OutgoingTranslatePrefix;

        private static ConfigEntry<string> Provider;
        private static ConfigEntry<string> GoogleTranslateApiKey;
        private static ConfigEntry<string> GeminiApiKey;
        private static ConfigEntry<string> GeminiModel;
        private static ConfigEntry<string> OpenAIApiKey;
        private static ConfigEntry<string> OpenAIModel;

        private const string TranslateListPath = "./TRANSLATE_DATA/Translate/translate.txt";

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(9) };
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        private static readonly object LocalTranslationLock = new object();
        private static readonly Dictionary<string, Dictionary<string, LocalTranslationRecord>> LocalTranslations =
            new Dictionary<string, Dictionary<string, LocalTranslationRecord>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoadedLocalTranslationPairs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool initialized;
        private static string LastIncomingDetectedLang = string.Empty;
        private static string LastResponseDetectedSourceLang = string.Empty;

        private sealed class LocalTranslationRecord
        {
            public string Original;
            public string Translation;
            public bool Manual;
        }

        public sealed class LocalTranslationEntry
        {
            public string SourceLang;
            public string TargetLang;
            public string Original;
            public string Translation;
            public bool Manual;
        }

        public static readonly LanguageDef[] TranslationLanguages = new LanguageDef[]
        {
            new LanguageDef("auto", "Auto detect", "Auto detect"),
            new LanguageDef("among", "Among Us language", "Among Us"),
            new LanguageDef("en", "English", "English"),
            new LanguageDef("it", "Italian", "Italiano"),
            new LanguageDef("fr", "French", "Français"),
            new LanguageDef("de", "German", "Deutsch"),
            new LanguageDef("es", "Spanish", "Español"),
            new LanguageDef("pt", "Portuguese", "Português"),
            new LanguageDef("nl", "Dutch", "Nederlands"),
            new LanguageDef("pl", "Polish", "Polski"),
            new LanguageDef("tr", "Turkish", "Türkçe"),
            new LanguageDef("ru", "Russian", "Русский"),
            new LanguageDef("uk", "Ukrainian", "Українська"),
            new LanguageDef("ja", "Japanese", "日本語"),
            new LanguageDef("ko", "Korean", "한국어"),
            new LanguageDef("zh", "Chinese", "中文"),
            new LanguageDef("ar", "Arabic", "العربية"),
            new LanguageDef("cs", "Czech", "Čeština"),
            new LanguageDef("sv", "Swedish", "Svenska"),
            new LanguageDef("fi", "Finnish", "Suomi"),
            new LanguageDef("da", "Danish", "Dansk"),
            new LanguageDef("el", "Greek", "Ελληνικά"),
            new LanguageDef("he", "Hebrew", "עברית"),
            new LanguageDef("hi", "Hindi", "हिन्दी"),
            new LanguageDef("id", "Indonesian", "Bahasa Indonesia"),
            new LanguageDef("vi", "Vietnamese", "Tiếng Việt"),
            new LanguageDef("th", "Thai", "ไทย"),
            new LanguageDef("ro", "Romanian", "Română"),
            new LanguageDef("hu", "Hungarian", "Magyar")
        };

        public static void Initialize(ConfigFile config)
        {
            if (initialized) return;
            initialized = true;

            ConfigVersion = config.Bind("LiveTranslator", "ConfigVersion", 0, "Internal LiveTranslator config version. Do not edit manually.");

            Enabled = config.Bind("LiveTranslator", "Enabled", false, "Enable LiveTranslator. Default OFF: nothing is translated until you enable it from the menu.");

            TranslateIncoming = config.Bind("LiveTranslator", "TranslateIncoming", true, "When LiveTranslator is ON, translate received chat messages.");
            TranslateOutgoing = config.Bind("LiveTranslator", "TranslateOutgoing", true, "When LiveTranslator is ON, translate sent messages only when they start with the required prefix.");
            ShowOriginalIncoming = config.Bind("LiveTranslator", "ShowOriginalIncoming", true, "Show original + translation for received messages. If false, show only translation.");
            ShowStatusInChat = config.Bind("LiveTranslator", "ShowStatusInChat", true, "Show LiveTranslator status messages in chat.");
            OutgoingRequiresPrefix = config.Bind("LiveTranslator", "OutgoingRequiresPrefix", true, "If true, sent messages are translated only when they start with OutgoingTranslatePrefix. The prefix is removed before sending.");
            GameAwareTranslations = config.Bind("LiveTranslator", "GameAwareTranslations", true, "Use Among Us/game-aware slang corrections for better chat translations.");
            TranslateWhenChatClosed = config.Bind("LiveTranslator", "TranslateWhenChatClosed", true, "If true, incoming messages are translated even when the chat is closed. If false, only while chat is open.");

            // Default richiesto: ricevuti AUTO -> lingua impostata su Among Us, invio lingua Among Us -> lingua rilevata dall'ultimo messaggio ricevuto.
            ReadFromLang = config.Bind("LiveTranslator", "ReadFromLang", "auto", "Source language for received messages. Default: auto detect.");
            ReadToLang = config.Bind("LiveTranslator", "ReadToLang", "among", "Target language for received messages. Default: Among Us UI language.");
            SendFromLang = config.Bind("LiveTranslator", "SendFromLang", "among", "Source language for messages you write. Default: Among Us UI language.");
            SendToLang = config.Bind("LiveTranslator", "SendToLang", "auto", "Target language for messages you send. Default: last detected incoming language.");
            OutgoingTranslatePrefix = config.Bind("LiveTranslator", "OutgoingTranslatePrefix", "-", "Prefix required to translate sent messages when OutgoingRequiresPrefix is true. Example: -ciao");

            Provider = config.Bind("LiveTranslator", "Provider", "GoogleTranslate", "Translation provider: GoogleTranslate, Gemini or OpenAI.");
            GoogleTranslateApiKey = config.Bind("LiveTranslator", "GoogleTranslateApiKey", string.Empty, "Google Cloud Translation Basic v2 API key.");
            GeminiApiKey = config.Bind("LiveTranslator", "GeminiApiKey", string.Empty, "Google Gemini API key.");
            GeminiModel = config.Bind("LiveTranslator", "GeminiModel", "gemini-3.5-flash", "Gemini model used for translation.");
            OpenAIApiKey = config.Bind("LiveTranslator", "OpenAIApiKey", string.Empty, "OpenAI API key used only when Provider is OpenAI.");
            OpenAIModel = config.Bind("LiveTranslator", "OpenAIModel", "gpt-4.1", "OpenAI model used for translation.");

            string configuredProvider = Provider == null ? string.Empty : Provider.Value;
            if (!IsNormalProvider(configuredProvider))
            {
                configuredProvider = "GoogleTranslate";
                if (Provider != null) Provider.Value = configuredProvider;
            }

            EnsureLocalTranslationDirectory();
            AutoMigrateOldDefaultLanguages();
            AutoMigrateMinusPrefixAndDefaultOff();
        }

        public static bool GetEnabled() { return Enabled != null && Enabled.Value; }
        public static bool GetTranslateIncoming() { return true; }
        public static bool GetTranslateOutgoing() { return true; }
        public static bool GetShowOriginalIncoming() { return true; }
        public static bool GetShowStatusInChat() { return ShowStatusInChat == null || ShowStatusInChat.Value; }
        public static bool GetOutgoingRequiresPrefix() { return false; }
        public static bool GetGameAwareTranslations() { return true; }
        public static bool GetTranslateWhenChatClosed() { return TranslateWhenChatClosed == null || TranslateWhenChatClosed.Value; }

        public static string GetReadFromLang() { return SafeValue(ReadFromLang, "auto"); }
        public static string GetReadToLang() { return SafeValue(ReadToLang, "among"); }
        public static string GetSendFromLang() { return SafeValue(SendFromLang, "among"); }
        public static string GetSendToLang() { return SafeValue(SendToLang, "auto"); }
        private static string GetSystemMenuLanguage()
        {
            try
            {
                string name = string.Empty;
                try { name = CultureInfo.CurrentUICulture.Name; } catch { }
                if (string.IsNullOrWhiteSpace(name))
                {
                    try { name = CultureInfo.InstalledUICulture.Name; } catch { }
                }

                name = NormalizeLang(name, "en").ToLowerInvariant();

                if (name.StartsWith("it", StringComparison.OrdinalIgnoreCase)) return "it";
                if (name.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return "es";
                if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt";
                if (name.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return "fr";
                if (name.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return "de";
                if (name.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en";

                // Menu text currently has complete translations only for the languages above.
                return "en";
            }
            catch
            {
                return "en";
            }
        }
        public static string GetOutgoingTranslatePrefix()
        {
            string p = SafeValue(OutgoingTranslatePrefix, "-");
            if (string.IsNullOrWhiteSpace(p)) return "-";
            p = p.Trim();
            if (p.StartsWith("/", StringComparison.OrdinalIgnoreCase)) return "-";
            if (p.Length > 3) p = p.Substring(0, 3);
            return p;
        }
        private static bool IsNormalProvider(string provider)
        {
            return string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetProvider()
        {
            string p = SafeValue(Provider, "GoogleTranslate");
            return IsNormalProvider(p) ? p : "GoogleTranslate";
        }

        public static string GetProviderDisplay()
        {
            string provider = GetProvider();
            if (string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
                return "Google Translate";
            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                return "Gemini";
            return "OpenAI";
        }

        public static string GetGoogleTranslateApiKey()
        {
            return SafeValue(GoogleTranslateApiKey, string.Empty);
        }

        public static void SetGoogleTranslateApiKey(string value)
        {
            if (GoogleTranslateApiKey != null)
                GoogleTranslateApiKey.Value = value ?? string.Empty;
        }

        public static string GetGeminiApiKey()
        {
            return SafeValue(GeminiApiKey, string.Empty);
        }

        public static void SetGeminiApiKey(string value)
        {
            if (GeminiApiKey != null)
                GeminiApiKey.Value = value ?? string.Empty;
        }

        public static string GetGeminiModel()
        {
            return SafeValue(GeminiModel, "gemini-3.5-flash");
        }

        public static string GetOpenAIApiKey()
        {
            return SafeValue(OpenAIApiKey, string.Empty);
        }

        public static void SetOpenAIApiKey(string value)
        {
            if (OpenAIApiKey != null)
                OpenAIApiKey.Value = value ?? string.Empty;
        }

        public static string GetOpenAIModel()
        {
            return SafeValue(OpenAIModel, "gpt-4.1");
        }

        private static string SafeValue(ConfigEntry<string> entry, string fallback)
        {
            try
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Value)) return fallback;
                return entry.Value.Trim();
            }
            catch { return fallback; }
        }

        public static void ToggleEnabled()
        {
            if (Enabled != null) Enabled.Value = !Enabled.Value;
            ShowStatusPublic("LiveTranslator " + (GetEnabled() ? "ON" : "OFF"));
        }

        public static void ToggleTranslateWhenChatClosed()
        {
            if (TranslateWhenChatClosed != null) TranslateWhenChatClosed.Value = !TranslateWhenChatClosed.Value;
            ShowStatusPublic("Translate mode: " + GetTranslateModeDisplay());
        }

        public static string GetTranslateModeDisplay()
        {
            return "Lobby/Meeting + chat-open tasks";
        }

        public static void ToggleTranslateIncoming()
        {
            if (TranslateIncoming != null) TranslateIncoming.Value = true;
        }

        public static void ToggleTranslateOutgoing()
        {
            if (TranslateOutgoing != null) TranslateOutgoing.Value = true;
        }

        public static void ToggleShowOriginalIncoming()
        {
            if (ShowOriginalIncoming != null) ShowOriginalIncoming.Value = true;
        }

        public static void ToggleShowStatusInChat()
        {
            if (ShowStatusInChat != null) ShowStatusInChat.Value = !ShowStatusInChat.Value;
        }

        public static void ToggleOutgoingRequiresPrefix()
        {
            if (OutgoingRequiresPrefix != null) OutgoingRequiresPrefix.Value = false;
        }

        public static void ToggleGameAwareTranslations()
        {
            if (GameAwareTranslations != null) GameAwareTranslations.Value = true;
        }

        public static void SetReadFromLang(string lang) { if (ReadFromLang != null) ReadFromLang.Value = NormalizeLang(lang, "auto"); ClearCache(); }
        public static void SetReadToLang(string lang) { if (ReadToLang != null) ReadToLang.Value = NormalizeLang(lang, "among"); ClearCache(); }
        public static void SetSendFromLang(string lang) { if (SendFromLang != null) SendFromLang.Value = NormalizeLang(lang, "among"); ClearCache(); }
        public static void SetSendToLang(string lang) { if (SendToLang != null) SendToLang.Value = NormalizeLang(lang, "auto"); ClearCache(); }

        private static void AutoMigrateOldDefaultLanguages()
        {
            try
            {
                if (ReadFromLang != null && ReadToLang != null && SendFromLang != null && SendToLang != null &&
                    string.Equals(ReadFromLang.Value, "en", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(ReadToLang.Value, "it", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(SendFromLang.Value, "it", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(SendToLang.Value, "en", StringComparison.OrdinalIgnoreCase))
                {
                    ReadFromLang.Value = "auto";
                    ReadToLang.Value = "among";
                    SendFromLang.Value = "among";
                    SendToLang.Value = "auto";
                }
            }
            catch { }
        }

        private static void AutoMigrateMinusPrefixAndDefaultOff()
        {
            try
            {
                const int targetVersion = 2026071401;
                int current = ConfigVersion == null ? 0 : ConfigVersion.Value;
                if (current >= targetVersion) return;

                if (Enabled != null) Enabled.Value = false;
                if (TranslateIncoming != null) TranslateIncoming.Value = true;
                if (TranslateOutgoing != null) TranslateOutgoing.Value = true;
                if (OutgoingRequiresPrefix != null) OutgoingRequiresPrefix.Value = false;
                if (OutgoingTranslatePrefix != null) OutgoingTranslatePrefix.Value = "-";
                if (ShowOriginalIncoming != null) ShowOriginalIncoming.Value = true;
                if (GameAwareTranslations != null) GameAwareTranslations.Value = true;
                if (TranslateWhenChatClosed != null) TranslateWhenChatClosed.Value = true;
                if (Provider != null) Provider.Value = "GoogleTranslate";
                if (ReadFromLang != null) ReadFromLang.Value = "auto";
                if (ReadToLang != null) ReadToLang.Value = "among";
                if (SendFromLang != null) SendFromLang.Value = "among";
                if (SendToLang != null) SendToLang.Value = "auto";
                if (ConfigVersion != null) ConfigVersion.Value = targetVersion;
            }
            catch { }
        }

        public static string GetAmongUserLangCode()
        {
            try { return MapSupportedLangToLibreCode(GetUserTrueLang()); }
            catch { return "en"; }
        }

        public static string GetLastIncomingDetectedLang()
        {
            return NormalizeLang(LastIncomingDetectedLang, string.Empty);
        }

        private static void UpdateLastDetectedFromText(string text)
        {
            try
            {
                string guessed = InferLikelySourceLang(text);
                if (!string.IsNullOrWhiteSpace(guessed))
                    LastIncomingDetectedLang = guessed;
            }
            catch { }
        }

        public static string GetAutoReplyTargetLang()
        {
            string last = GetLastIncomingDetectedLang();
            string among = GetAmongUserLangCode();
            if (!string.IsNullOrWhiteSpace(last) && !SameLang(last, among)) return last;
            return among;
        }

        private static string ResolveSourceLangForRequest(string lang, string fallback)
        {
            string value = NormalizeLang(lang, fallback);
            if (value.Equals("among", StringComparison.OrdinalIgnoreCase)) return GetAmongUserLangCode();
            return value;
        }

        private static string ResolveTargetLangForRequest(string lang, string fallback)
        {
            string value = NormalizeLang(lang, fallback);
            if (value.Equals("among", StringComparison.OrdinalIgnoreCase)) return GetAmongUserLangCode();
            if (value.Equals("auto", StringComparison.OrdinalIgnoreCase)) return GetAutoReplyTargetLang();
            return value;
        }

        public static SupportedLangs GetUserTrueLang()
        {
            try
            {
                var name = CultureInfo.CurrentUICulture.Name;
                if (name.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return SupportedLangs.English;
                if (name.StartsWith("zh_CHT", StringComparison.OrdinalIgnoreCase) || name.StartsWith("zh-CHT", StringComparison.OrdinalIgnoreCase) || name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase)) return SupportedLangs.TChinese;
                if (name.StartsWith("it_IT", StringComparison.OrdinalIgnoreCase) || name.StartsWith("it-IT", StringComparison.OrdinalIgnoreCase) || name.StartsWith("it", StringComparison.OrdinalIgnoreCase)) return SupportedLangs.Italian;
                if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return SupportedLangs.SChinese;
                if (name.StartsWith("ru", StringComparison.OrdinalIgnoreCase)) return SupportedLangs.Russian;
                return TranslationController.Instance.currentLanguage.languageID;
            }
            catch
            {
                return SupportedLangs.English;
            }
        }

        private static string MapSupportedLangToLibreCode(SupportedLangs lang)
        {
            string s = lang.ToString().ToLowerInvariant();
            if (s.Contains("ital")) return "it";
            if (s.Contains("english") || s == "en") return "en";
            if (s.Contains("russian")) return "ru";
            if (s.Contains("french")) return "fr";
            if (s.Contains("german")) return "de";
            if (s.Contains("spanish")) return "es";
            if (s.Contains("portuguese")) return "pt";
            if (s.Contains("dutch")) return "nl";
            if (s.Contains("polish")) return "pl";
            if (s.Contains("turkish")) return "tr";
            if (s.Contains("japanese")) return "ja";
            if (s.Contains("korean")) return "ko";
            if (s.Contains("chinese") || s.Contains("schinese") || s.Contains("tchinese")) return "zh";
            if (s.Contains("arabic")) return "ar";
            if (s.Contains("gaeilge") || s.Contains("irish")) return "ga";
            if (s.Contains("bisaya") || s.Contains("cebuano")) return "ceb";
            return "en";
        }

        public static void SetProvider(string provider)
        {
            if (!IsNormalProvider(provider))
                provider = "GoogleTranslate";

            if (Provider != null)
                Provider.Value = provider;

            ClearCache();
            ShowStatusPublic("Provider: " + GetProviderDisplay());
        }

        public static void CycleProvider()
        {
            string current = GetProvider();
            if (string.Equals(current, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
                SetProvider("Gemini");
            else if (string.Equals(current, "Gemini", StringComparison.OrdinalIgnoreCase))
                SetProvider("OpenAI");
            else
                SetProvider("GoogleTranslate");
        }

        public static bool CanUseTranslateNow()
        {
            return GetEnabled();
        }

        private static bool CanUseCurrentRemoteProvider()
        {
            string provider = GetProvider();
            if (string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(GetGoogleTranslateApiKey());
            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(GetGeminiApiKey());
            return !string.IsNullOrWhiteSpace(GetOpenAIApiKey());
        }

        public static void ApplyDefaultPreset()
        {
            SetReadFromLang("auto");
            SetReadToLang("among");
            SetSendFromLang("among");
            SetSendToLang("auto");
            if (TranslateWhenChatClosed != null) TranslateWhenChatClosed.Value = true;
            if (ShowOriginalIncoming != null) ShowOriginalIncoming.Value = true;
            if (TranslateIncoming != null) TranslateIncoming.Value = true;
            if (TranslateOutgoing != null) TranslateOutgoing.Value = true;
            if (OutgoingRequiresPrefix != null) OutgoingRequiresPrefix.Value = false;
            if (GameAwareTranslations != null) GameAwareTranslations.Value = true;
            SetProvider("GoogleTranslate");
            ClearCache();
            ShowStatusPublic("Default preset restored.");
        }

        public static void ClearCache()
        {
            lock (CacheLock) { Cache.Clear(); }
        }

        private static void EnsureLocalTranslationDirectory()
        {
            try
            {
                string directory = Path.GetDirectoryName(TranslateListPath);
                if (string.IsNullOrWhiteSpace(directory))
                    directory = "./TRANSLATE_DATA/Translate";
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[LiveTranslator] Cannot create local translation directory: " + ex.Message); } catch { }
            }
        }

        private static void UpdateLocalTranslationIndex()
        {
            try
            {
                EnsureLocalTranslationDirectory();
                string directory = Path.GetDirectoryName(TranslateListPath);
                if (string.IsNullOrWhiteSpace(directory))
                    return;

                string[] files = Directory.GetFiles(directory, "*_to_*.txt");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# BanMod LiveTranslator language-pair files");
                for (int i = 0; i < files.Length; i++)
                    sb.AppendLine(Path.GetFileName(files[i]));

                File.WriteAllText(
                    TranslateListPath,
                    sb.ToString(),
                    new UTF8Encoding(false));
            }
            catch { }
        }

        private static string NormalizeLocalLang(string lang, bool target)
        {
            string normalized = NormalizeLang(lang, target ? GetAmongUserLangCode() : "auto");
            if (string.Equals(normalized, "among", StringComparison.OrdinalIgnoreCase))
                normalized = GetAmongUserLangCode();
            if (target && string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase))
                normalized = GetAutoReplyTargetLang();
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = target ? GetAmongUserLangCode() : "auto";
            return normalized.ToLowerInvariant();
        }

        public static string ResolveEditorLanguage(string lang, bool target)
        {
            string value = NormalizeLocalLang(lang, target);
            if (!target && string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
            {
                string detected = GetLastIncomingDetectedLang();
                if (!string.IsNullOrWhiteSpace(detected))
                    value = detected;
                else
                    value = "en";
            }
            return value;
        }

        private static string MakeLocalPairKey(string sourceLang, string targetLang)
        {
            return NormalizeLocalLang(sourceLang, false) + "_to_" + NormalizeLocalLang(targetLang, true);
        }

        private static string GetLocalTranslationFilePath(string sourceLang, string targetLang)
        {
            EnsureLocalTranslationDirectory();
            string directory = Path.GetDirectoryName(TranslateListPath);
            if (string.IsNullOrWhiteSpace(directory))
                directory = "./TRANSLATE_DATA/Translate";

            string pair = MakeLocalPairKey(sourceLang, targetLang);
            pair = Regex.Replace(pair, @"[^a-z0-9_\-]", "_", RegexOptions.IgnoreCase);
            return Path.Combine(directory, pair + ".txt");
        }

        private static string NormalizeLocalTextKey(string text)
        {
            string cleaned = CleanChatText(text);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned.ToLowerInvariant();
        }

        private static string EscapeLocalField(string value)
        {
            if (value == null) return string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("\t", "\\t")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string UnescapeLocalField(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            StringBuilder sb = new StringBuilder(value.Length);
            bool escape = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!escape)
                {
                    if (c == '\\') escape = true;
                    else sb.Append(c);
                    continue;
                }

                escape = false;
                switch (c)
                {
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'n': sb.Append('\n'); break;
                    case '\\': sb.Append('\\'); break;
                    default: sb.Append(c); break;
                }
            }
            if (escape) sb.Append('\\');
            return sb.ToString();
        }

        private static Dictionary<string, LocalTranslationRecord> LoadLocalPair(string sourceLang, string targetLang)
        {
            string pair = MakeLocalPairKey(sourceLang, targetLang);
            Dictionary<string, LocalTranslationRecord> records;

            lock (LocalTranslationLock)
            {
                if (LocalTranslations.TryGetValue(pair, out records) &&
                    LoadedLocalTranslationPairs.Contains(pair))
                    return records;

                records = new Dictionary<string, LocalTranslationRecord>(StringComparer.OrdinalIgnoreCase);
                LocalTranslations[pair] = records;
                LoadedLocalTranslationPairs.Add(pair);
            }

            try
            {
                string path = GetLocalTranslationFilePath(sourceLang, targetLang);
                if (!File.Exists(path)) return records;

                string[] fileLines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < fileLines.Length; i++)
                {
                    string line = fileLines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    string[] parts = line.Split(new char[] { '\t' }, 3);
                    if (parts.Length < 2) continue;

                    string original = CleanChatText(UnescapeLocalField(parts[0]));
                    string translated = CleanTranslatedText(UnescapeLocalField(parts[1]));
                    bool manual = parts.Length >= 3 &&
                        string.Equals(parts[2].Trim(), "manual", StringComparison.OrdinalIgnoreCase);

                    string key = NormalizeLocalTextKey(original);
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(translated))
                        continue;

                    records[key] = new LocalTranslationRecord
                    {
                        Original = original,
                        Translation = translated,
                        Manual = manual
                    };
                }
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[LiveTranslator] Cannot load local translations: " + ex.Message); } catch { }
            }

            return records;
        }

        private static void SaveLocalPair(string sourceLang, string targetLang)
        {
            string pair = MakeLocalPairKey(sourceLang, targetLang);
            Dictionary<string, LocalTranslationRecord> records;

            lock (LocalTranslationLock)
            {
                if (!LocalTranslations.TryGetValue(pair, out records))
                    return;

                List<LocalTranslationRecord> ordered = new List<LocalTranslationRecord>(records.Values);
                ordered.Sort(delegate (LocalTranslationRecord a, LocalTranslationRecord b)
                {
                    return string.Compare(a.Original, b.Original, StringComparison.OrdinalIgnoreCase);
                });

                try
                {
                    string path = GetLocalTranslationFilePath(sourceLang, targetLang);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# BanMod LiveTranslator local translations");
                    sb.AppendLine("# Format: original<TAB>translation<TAB>manual|auto");
                    sb.AppendLine("# Pair: " + NormalizeLocalLang(sourceLang, false) + " -> " + NormalizeLocalLang(targetLang, true));

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        LocalTranslationRecord record = ordered[i];
                        sb.Append(EscapeLocalField(record.Original));
                        sb.Append('\t');
                        sb.Append(EscapeLocalField(record.Translation));
                        sb.Append('\t');
                        sb.Append(record.Manual ? "manual" : "auto");
                        sb.AppendLine();
                    }

                    string tempPath = path + ".tmp";
                    File.WriteAllText(tempPath, sb.ToString(), new UTF8Encoding(false));
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tempPath, path);
                    UpdateLocalTranslationIndex();
                }
                catch (Exception ex)
                {
                    try { Debug.LogWarning("[LiveTranslator] Cannot save local translations: " + ex.Message); } catch { }
                }
            }
        }

        private static void DiscoverLocalPairsForTarget(string targetLang)
        {
            try
            {
                EnsureLocalTranslationDirectory();
                string directory = Path.GetDirectoryName(TranslateListPath);
                if (string.IsNullOrWhiteSpace(directory)) return;

                string target = NormalizeLocalLang(targetLang, true);
                string[] files = Directory.GetFiles(directory, "*_to_" + target + ".txt");
                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    string suffix = "_to_" + target;
                    if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                    string source = name.Substring(0, name.Length - suffix.Length);
                    LoadLocalPair(source, target);
                }
            }
            catch { }
        }

        private static bool TryGetLocalTranslation(
            string original,
            string sourceLang,
            string targetLang,
            out string translated,
            out string detectedSource)
        {
            translated = string.Empty;
            detectedSource = string.Empty;

            string target = NormalizeLocalLang(targetLang, true);
            string source = NormalizeLocalLang(sourceLang, false);
            string key = NormalizeLocalTextKey(original);
            if (string.IsNullOrWhiteSpace(key)) return false;

            List<string> sources = new List<string>();
            if (!string.Equals(source, "auto", StringComparison.OrdinalIgnoreCase))
                sources.Add(source);
            else
            {
                string inferred = InferLikelySourceLang(original);
                if (!string.IsNullOrWhiteSpace(inferred)) sources.Add(inferred);
                sources.Add("auto");
                DiscoverLocalPairsForTarget(target);

                lock (LocalTranslationLock)
                {
                    string suffix = "_to_" + target;
                    foreach (string pair in LocalTranslations.Keys)
                    {
                        if (!pair.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                        string candidate = pair.Substring(0, pair.Length - suffix.Length);
                        if (!sources.Contains(candidate)) sources.Add(candidate);
                    }
                }
            }

            for (int i = 0; i < sources.Count; i++)
            {
                Dictionary<string, LocalTranslationRecord> records = LoadLocalPair(sources[i], target);
                LocalTranslationRecord record;
                if (records.TryGetValue(key, out record) &&
                    record != null &&
                    record.Manual)
                {
                    translated = record.Translation;
                    detectedSource = sources[i];
                    return !string.IsNullOrWhiteSpace(translated);
                }
            }

            return false;
        }

        public static bool UpsertLocalTranslation(
            string sourceLang,
            string targetLang,
            string original,
            string translation,
            bool manual)
        {
            // Local persistence is reserved for translations explicitly saved
            // by the user from the editor. Automatic/provider translations
            // must live only in the in-memory cache for the current session.
            if (!manual)
                return false;

            string cleanOriginal = CleanChatText(original);
            string cleanTranslation = CleanTranslatedText(translation);
            if (string.IsNullOrWhiteSpace(cleanOriginal) || string.IsNullOrWhiteSpace(cleanTranslation))
                return false;

            string source = NormalizeLocalLang(sourceLang, false);
            string target = NormalizeLocalLang(targetLang, true);
            string key = NormalizeLocalTextKey(cleanOriginal);
            Dictionary<string, LocalTranslationRecord> records = LoadLocalPair(source, target);

            lock (LocalTranslationLock)
            {
                LocalTranslationRecord existing;
                if (records.TryGetValue(key, out existing) && existing != null && existing.Manual && !manual)
                    return true;

                records[key] = new LocalTranslationRecord
                {
                    Original = cleanOriginal,
                    Translation = cleanTranslation,
                    Manual = manual || (existing != null && existing.Manual)
                };
            }

            SaveLocalPair(source, target);
            ClearCache();
            return true;
        }

        public static bool DeleteLocalTranslation(
            string sourceLang,
            string targetLang,
            string original)
        {
            string source = NormalizeLocalLang(sourceLang, false);
            string target = NormalizeLocalLang(targetLang, true);
            string key = NormalizeLocalTextKey(original);
            Dictionary<string, LocalTranslationRecord> records = LoadLocalPair(source, target);
            bool removed;

            lock (LocalTranslationLock)
            {
                removed = records.Remove(key);
            }

            if (removed)
            {
                SaveLocalPair(source, target);
                ClearCache();
            }
            return removed;
        }

        public static LocalTranslationEntry[] SearchLocalTranslations(
            string sourceLang,
            string targetLang,
            string query,
            int maxResults)
        {
            if (maxResults <= 0) maxResults = 20;
            string source = NormalizeLocalLang(sourceLang, false);
            string target = NormalizeLocalLang(targetLang, true);
            string search = (query ?? string.Empty).Trim();
            List<LocalTranslationEntry> results = new List<LocalTranslationEntry>();

            List<string> sources = new List<string>();
            if (!string.Equals(source, "auto", StringComparison.OrdinalIgnoreCase))
                sources.Add(source);
            else
            {
                DiscoverLocalPairsForTarget(target);
                lock (LocalTranslationLock)
                {
                    string suffix = "_to_" + target;
                    foreach (string pair in LocalTranslations.Keys)
                    {
                        if (!pair.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
                        sources.Add(pair.Substring(0, pair.Length - suffix.Length));
                    }
                }
            }

            for (int s = 0; s < sources.Count && results.Count < maxResults; s++)
            {
                Dictionary<string, LocalTranslationRecord> records = LoadLocalPair(sources[s], target);
                foreach (LocalTranslationRecord record in records.Values)
                {
                    if (!string.IsNullOrWhiteSpace(search) &&
                        record.Original.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                        record.Translation.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    results.Add(new LocalTranslationEntry
                    {
                        SourceLang = sources[s],
                        TargetLang = target,
                        Original = record.Original,
                        Translation = record.Translation,
                        Manual = record.Manual
                    });

                    if (results.Count >= maxResults) break;
                }
            }

            results.Sort(delegate (LocalTranslationEntry a, LocalTranslationEntry b)
            {
                return string.Compare(a.Original, b.Original, StringComparison.OrdinalIgnoreCase);
            });
            return results.ToArray();
        }

        public static string GetLocalTranslationDirectory()
        {
            string directory = Path.GetDirectoryName(TranslateListPath);
            return string.IsNullOrWhiteSpace(directory)
                ? "./TRANSLATE_DATA/Translate"
                : directory;
        }

        public static string GetSettingsSummary()
        {
            return (GetEnabled() ? "ON" : "OFF") + "  |  " + GetProviderDisplay();
        }

        public static void ShowStatusPublic(string message)
        {
            if (!GetShowStatusInChat()) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                if (PlayerControl.LocalPlayer == null) return;
                if (!DestroyableSingleton<HudManager>.InstanceExists) return;
                HudManager hud = DestroyableSingleton<HudManager>.Instance;
                if (hud == null || hud.Chat == null) return;
                hud.Chat.AddChat(PlayerControl.LocalPlayer, "[LT] " + message, false);
            }
            catch
            {
                try { Debug.LogWarning("[LiveTranslator] " + message); } catch { }
            }
        }

        private static bool IsGameChatOpenOrOpening()
        {
            try
            {
                if (!DestroyableSingleton<HudManager>.InstanceExists) return false;
                HudManager hud = DestroyableSingleton<HudManager>.Instance;
                return hud != null && hud.Chat != null && hud.Chat.IsOpenOrOpening;
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldTranslateIncomingByGameState()
        {
            try
            {
                if (GetTranslateWhenChatClosed())
                    return true;

                if (LobbyBehaviour.Instance != null)
                    return true;

                if (MeetingHud.Instance != null)
                    return true;

                return IsGameChatOpenOrOpening();
            }
            catch
            {
                return GetTranslateWhenChatClosed() || IsGameChatOpenOrOpening();
            }
        }

        private static bool ShouldTranslateOutgoingByGameState()
        {
            try
            {
                if (LobbyBehaviour.Instance != null)
                    return true;

                if (MeetingHud.Instance != null)
                    return true;

                if (ShipStatus.Instance != null)
                    return true;

                return IsGameChatOpenOrOpening();
            }
            catch
            {
                return true;
            }
        }

        public static void TryTranslateIncomingForDisplay(PlayerControl sourcePlayer, ref string chatText)
        {
            if (!CanUseTranslateNow()) return;
            if (string.IsNullOrWhiteSpace(chatText)) return;
            if (IsLiveTranslatorText(chatText)) return;
            if (LooksLikeCommand(chatText)) return;

            try
            {
                if (sourcePlayer == null) return;
                if (PlayerControl.LocalPlayer != null && sourcePlayer == PlayerControl.LocalPlayer) return;

                string original = CleanChatText(chatText);
                if (string.IsNullOrWhiteSpace(original)) return;

                // Aggiorna sempre l'ultima lingua rilevata, anche se poi non traduci perché la chat è chiusa.
                string localDetected = InferLikelySourceLang(original);
                if (!string.IsNullOrWhiteSpace(localDetected)) LastIncomingDetectedLang = localDetected;

                if (!ShouldTranslateIncomingByGameState()) return;

                string from = ResolveSourceLangForRequest(GetReadFromLang(), "auto");
                string to = ResolveTargetLangForRequest(GetReadToLang(), GetAmongUserLangCode());
                if (SameLang(from, to)) return;
                if (string.Equals(from, "auto", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(localDetected) && SameLang(localDetected, to)) return;

                LastResponseDetectedSourceLang = string.Empty;
                string translated = TranslateBlocking(original, from, to);
                string detected = NormalizeLang(LastResponseDetectedSourceLang, string.Empty);
                if (!string.IsNullOrWhiteSpace(detected))
                {
                    LastIncomingDetectedLang = detected;
                    if (SameLang(detected, to)) return;
                }
                translated = CleanTranslatedText(translated);

                if (string.IsNullOrWhiteSpace(translated)) return;
                if (string.Equals(translated, original, StringComparison.OrdinalIgnoreCase)) return;

                string label = GetLanguageShortLabel(to);
                chatText = SanitizeForAmongUsDisplay(original) + "\n___________\n[" + label + "] " + translated;
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[LiveTranslator] incoming translation failed: " + ex.Message); } catch { }
            }
        }

        public static void TryTranslateOutgoingForSend(ref string chatText)
        {
            if (!CanUseTranslateNow()) return;
            if (string.IsNullOrWhiteSpace(chatText)) return;
            if (IsLiveTranslatorText(chatText)) return;
            if (LooksLikeCommand(chatText)) return;
            if (!ShouldTranslateOutgoingByGameState()) return;

            try
            {
                string original = CleanChatText(chatText);
                if (string.IsNullOrWhiteSpace(original)) return;

                // Nessun prefisso richiesto: se il generale è ON, l'invio viene tradotto sempre.
                // Se la traduzione non parte, almeno il testo resta pulito e senza simboli pericolosi.
                chatText = CleanOutgoingChatText(original, original);

                string from = ResolveSourceLangForRequest(GetSendFromLang(), GetAmongUserLangCode());
                string to = ResolveTargetLangForRequest(GetSendToLang(), GetAutoReplyTargetLang());
                if (SameLang(from, to)) return;

                string localDetected = InferLikelySourceLang(original);
                if (!string.IsNullOrWhiteSpace(localDetected) && SameLang(localDetected, to)) return;

                LastResponseDetectedSourceLang = string.Empty;
                string translated = TranslateBlocking(original, from, to);
                string detected = NormalizeLang(LastResponseDetectedSourceLang, string.Empty);
                if (!string.IsNullOrWhiteSpace(detected) && SameLang(detected, to)) return;
                translated = CleanTranslatedText(translated);
                translated = CleanOutgoingChatText(translated, original);
                if (string.IsNullOrWhiteSpace(translated)) return;

                // IMPORTANT: outgoing RPC must contain ONLY the safe translated message.
                chatText = translated;
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[LiveTranslator] outgoing translation failed: " + ex.Message); } catch { }
            }
        }

        public static string TranslateBlocking(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            string originalText = CleanChatText(text);
            sourceLang = ResolveSourceLangForRequest(sourceLang, "auto");
            targetLang = ResolveTargetLangForRequest(targetLang, GetAmongUserLangCode());

            string localTranslation;
            string localDetectedSource;
            if (TryGetLocalTranslation(
                originalText,
                sourceLang,
                targetLang,
                out localTranslation,
                out localDetectedSource))
            {
                if (string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(localDetectedSource) &&
                    !string.Equals(localDetectedSource, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    LastResponseDetectedSourceLang = localDetectedSource;
                }

                return CleanTranslatedText(localTranslation);
            }

            if (GetGameAwareTranslations())
            {
                string direct = TryGameAwareDirectTranslation(originalText, sourceLang, targetLang);
                if (!string.IsNullOrWhiteSpace(direct))
                {
                    string detected = InferLikelySourceLang(originalText);
                    if (string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(detected))
                    {
                        LastResponseDetectedSourceLang = detected;
                    }

                    direct = CleanTranslatedText(direct);
                    return direct;
                }
            }

            string preparedText = GetGameAwareTranslations()
                ? NormalizeSlangBeforeTranslate(originalText, sourceLang)
                : originalText;

            string key = MakeCacheKey(originalText, sourceLang, targetLang);
            lock (CacheLock)
            {
                string cached;
                if (Cache.TryGetValue(key, out cached)) return cached;
            }

            if (!CanUseCurrentRemoteProvider())
                return string.Empty;

            LastResponseDetectedSourceLang = string.Empty;
            string result = TranslateRawAsync(preparedText, sourceLang, targetLang)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            result = CleanTranslatedText(result);

            if (GetGameAwareTranslations())
                result = PostFixGameTranslation(originalText, result, sourceLang, targetLang);

            if (!string.IsNullOrWhiteSpace(result))
            {
                // Automatic translations are cached only in memory.
                // They are intentionally not persisted to local translation files.
                lock (CacheLock)
                {
                    if (Cache.Count > 500) Cache.Clear();
                    Cache[key] = result;
                }
            }

            return result;
        }

        private static string InferLikelySourceLang(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string t = text.Trim();
            if (Regex.IsMatch(t, "[А-Яа-яЁё]")) return "ru";
            if (Regex.IsMatch(t, "[\u3040-\u30ff]")) return "ja";
            if (Regex.IsMatch(t, "[\uac00-\ud7af]")) return "ko";
            if (Regex.IsMatch(t, "[\u4e00-\u9fff]")) return "zh";
            if (Regex.IsMatch(t, "[\u0600-\u06ff]")) return "ar";

            string lower = t.ToLowerInvariant();
            if (Regex.IsMatch(lower, @"\b(red|blue|green|pink|orange|yellow|black|white|purple|brown|cyan|lime|sus|vent|vented|kill|killed|where|who|body|skip|ss|omg|wtf|idk|btw|dtw|rn|brb|afk|hello|hi|the|and|you|are|is|was|were|saw|vote|voted)\b")) return "en";
            if (Regex.IsMatch(lower, @"\b(rosso|blu|verde|rosa|giallo|nero|bianco|viola|marrone|ciano|lime|dove|chi|corpo|cadavere|ucciso|uccisa|killato|ventato|skippa|ciao|sono|sei|era|ero|ho|hai|ha|visto|votiamo|vota|non|si|sì|che|per|con|in|il|lo|la|gli|le|un|una)\b")) return "it";
            return string.Empty;
        }

        private static string TryGameAwareDirectTranslation(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string src = NormalizeLang(sourceLang, "auto").ToLowerInvariant();
            string dst = NormalizeLang(targetLang, "it").ToLowerInvariant();
            string trimmed = CleanChatText(text);

            bool fromEnglish = src == "en" || src == "auto";
            bool toItalian = dst == "it";
            bool fromItalian = src == "it" || src == "auto";
            bool toEnglish = dst == "en";

            if (fromEnglish && toItalian)
            {
                string lower = trimmed.ToLowerInvariant();
                if (lower == "skip" || lower == "skipp") return "skip";
                if (lower == "who" || lower == "who?") return "chi?";
                if (lower == "where" || lower == "whr" || lower == "where?") return "dove è il corpo?";
                if (lower == "lights") return "luci";
                if (lower == "fix lights") return "sistemate le luci";
                if (lower == "body" || lower == "dead body") return "corpo";
                if (lower == "report" || lower == "reported") return "report";
                if (lower == "self" || lower == "self report" || lower == "self-report") return "self report";
                if (lower == "omg") return "oh mio dio";
                if (lower == "wtf" || lower == "tf") return "ma che cazzo?";
                if (lower == "btw" || lower == "dtw") return "a proposito";
                if (lower == "idk") return "non lo so";
                if (lower == "idc") return "non mi interessa";
                if (lower == "imo") return "secondo me";
                if (lower == "ngl") return "sinceramente";
                if (lower == "rn") return "adesso";
                if (lower == "afk") return "assente";
                if (lower == "brb") return "torno subito";
                if (lower == "gg") return "bella partita";
                if (lower == "wp") return "ben giocato";
                if (lower == "np") return "nessun problema";
                if (lower == "lol" || lower == "lmao") return "ahaha";
                if (lower == "thx" || lower == "ty" || lower == "tysm") return "grazie";
                if (lower == "imp" || lower == "impo") return "impostore";
                if (lower == "crew") return "crew";
                if (lower == "clear") return "clear";
                if (lower == "safe") return "safe";

                Match m;
                m = Regex.Match(trimmed, @"^\s*(?:wtf|tf|what\s+the\s+fuck)[,!?\s]+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:has\s+)?(?:vented|used\s+the\s+vent)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return "ma che cazzo, " + CleanSubject(m.Groups["who"].Value) + " ha ventato";

                m = Regex.Match(trimmed, @"^\s*(?:omg|oh\s+my\s+god)[,!?\s]+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:has\s+)?(?:vented|used\s+the\s+vent)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return "oh mio dio, " + CleanSubject(m.Groups["who"].Value) + " ha ventato";

                m = Regex.Match(trimmed, @"^\s*(?:wtf|tf|what\s+the\s+fuck)[,!?\s]+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:is\s+)?(?:sus|suspicious)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return "ma che cazzo, " + CleanSubject(m.Groups["who"].Value) + " è sus";

                m = Regex.Match(trimmed, @"^\s*(?:wtf|tf|what\s+the\s+fuck)[,!?\s]+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:did\s+)?(?:kill|killed)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return "ma che cazzo, " + CleanSubject(m.Groups["who"].Value) + " ha killato";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:has\s+)?(?:vented|used\s+the\s+vent)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " ha ventato";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:did\s+)?(?:kill|killed)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " ha killato";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:is\s+)?sus\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " è sus";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:is\s+)?(?:clear|safe)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " è clear";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:did\s+)?fake\s+task\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " ha finto una task";

                m = Regex.Match(trimmed, @"^\s*(?:vote|v)\s+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return "votate " + CleanSubject(m.Groups["who"].Value);

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:is\s+)?(?:ss|shapeshifter)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " è shapeshifter";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:was\s+|is\s+)?in\s+(?<loc>[A-Za-z0-9_\- ]{2,32})\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " era in " + NormalizeGameLocation(m.Groups["loc"].Value, "it");

                m = Regex.Match(trimmed, @"^\s*(?:i\s+)?saw\s+(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?<action>vent|vented|kill|killed)\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string action = m.Groups["action"].Value.ToLowerInvariant().StartsWith("vent") ? "ventare" : "killare";
                    return "ho visto " + CleanSubject(m.Groups["who"].Value) + " " + action;
                }
            }

            if (fromItalian && toEnglish)
            {
                string lower = trimmed.ToLowerInvariant();
                if (lower == "salta" || lower == "skippa" || lower == "skip") return "skip";
                if (lower == "chi") return "who?";
                if (lower == "dove") return "where is the dead body?";
                if (lower == "luci") return "lights";
                if (lower == "corpo" || lower == "cadavere") return "dead body";
                if (lower == "omg") return "oh my god";
                if (lower == "wtf") return "what the fuck?";
                if (lower == "boh") return "I do not know";
                if (lower == "non so") return "I do not know";
                if (lower == "aspe" || lower == "asp") return "wait";

                Match m;
                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:ha\s+)?ventato\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " vented";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:ha\s+)?killato\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " killed";

                m = Regex.Match(trimmed, @"^\s*(?<who>[A-Za-z0-9_\-\[\]# ]{1,32})\s+(?:è|e)\s+sus\s*[.!?]*\s*$", RegexOptions.IgnoreCase);
                if (m.Success) return CleanSubject(m.Groups["who"].Value) + " is sus";
            }

            return string.Empty;
        }

        private static string NormalizeSlangBeforeTranslate(string text, string sourceLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string src = NormalizeLang(sourceLang, "auto").ToLowerInvariant();
            string t = text;

            if (src == "en" || src == "auto")
            {
                t = Regex.Replace(t, @"\bu\b", "you", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bur\b", "your", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\br\b", "are", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bim\b", "I'm", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bdont\b", "don't", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bbc\b|\bbcs\b|\bcuz\b", "because", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bppl\b", "people", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\brn\b", "right now", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bomg\b", "oh my god", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bwtf\b|\btf\b", "what the fuck", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bbtw\b|\bdtw\b", "by the way", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bidk\b", "I do not know", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bidc\b", "I do not care", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bimo\b", "in my opinion", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bngl\b", "not going to lie", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\btbh\b", "to be honest", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bafk\b", "away from keyboard", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bbrb\b", "be right back", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bgg\b", "good game", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bwp\b", "well played", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bnp\b", "no problem", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bthx\b|\bty\b|\btysm\b", "thank you", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bpls\b|\bplz\b", "please", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bimp\b|\bimpo\b", "impostor", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bcrew\b", "crewmate", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bss\b", "shapeshifter", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\belec\b", "electrical", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bcams\b|\bcam\b", "security cameras", RegexOptions.IgnoreCase);
            }

            return t;
        }

        private static string PostFixGameTranslation(string original, string translated, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(translated)) return string.Empty;

            string dst = NormalizeLang(targetLang, "it").ToLowerInvariant();
            string src = NormalizeLang(sourceLang, "auto").ToLowerInvariant();
            string t = translated;
            string o = original ?? string.Empty;

            if (dst == "it" && (src == "en" || src == "auto"))
            {
                if (Regex.IsMatch(o, @"\bvented\b", RegexOptions.IgnoreCase))
                {
                    t = Regex.Replace(t, @"\b(mi sono sfogato|si è sfogato|si e sfogato|sfogato|ventilato)\b", "ha ventato", RegexOptions.IgnoreCase);
                }

                t = Regex.Replace(t, @"\belettric[ao]\b", "elec", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\btelecamere\b", "cams", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bmutatore di forma\b", "shapeshifter", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bsegnala\b", "report", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bimpostore\b", "impostore", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bcompagno di squadra\b", "crewmate", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\bcorpo morto\b", "corpo", RegexOptions.IgnoreCase);
            }
            else if (dst == "en" && (src == "it" || src == "auto"))
            {
                t = Regex.Replace(t, @"\bventilated\b", "vented", RegexOptions.IgnoreCase);
                t = Regex.Replace(t, @"\belectrical\b", "elec", RegexOptions.IgnoreCase);
            }

            return CleanTranslatedText(t);
        }

        private static string CleanSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return string.Empty;
            return Regex.Replace(subject.Trim(), @"\s+", " ");
        }

        private static string NormalizeGameLocation(string loc, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(loc)) return string.Empty;
            string l = loc.Trim().ToLowerInvariant();

            if (l == "elec" || l == "electrical") return "elec";
            if (l == "cams" || l == "camera" || l == "cameras" || l == "security") return "cams";
            if (l == "med" || l == "medbay" || l == "med bay") return "medbay";
            if (l == "admin") return "admin";
            if (l == "o2" || l == "oxygen") return "O2";
            if (l == "nav" || l == "navigation") return "nav";
            if (l == "comms" || l == "communications") return "comms";
            if (l == "weap" || l == "weapons") return "weapons";
            if (l == "storage") return "storage";
            if (l == "caf" || l == "cafeteria") return "cafeteria";
            if (l == "reactor") return "reactor";
            if (l == "shields") return "shields";
            if (l == "specimen") return "specimen";
            if (l == "lab" || l == "laboratory") return "lab";
            if (l == "office") return "office";
            if (l == "vitals") return "vitals";
            return loc.Trim();
        }

        private static async Task<string> TranslateRawAsync(string text, string sourceLang, string targetLang)
        {
            string provider = GetProvider();

            if (string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
                return await TranslateWithGoogleTranslate(text, sourceLang, targetLang).ConfigureAwait(false);

            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                return await TranslateWithGemini(text, sourceLang, targetLang).ConfigureAwait(false);

            return await TranslateWithOpenAI(text, sourceLang, targetLang).ConfigureAwait(false);
        }

        private static async Task<string> TranslateWithGoogleTranslate(
            string text,
            string sourceLang,
            string targetLang)
        {
            string apiKey = GetGoogleTranslateApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return string.Empty;

            string source = ResolveSourceLangForRequest(sourceLang, "auto");
            string target = ResolveTargetLangForRequest(targetLang, GetAmongUserLangCode());

            StringBuilder body = new StringBuilder();
            body.Append("{");
            body.Append("\"q\":\"").Append(JsonEscape(text)).Append("\",");
            body.Append("\"target\":\"").Append(JsonEscape(target)).Append("\",");
            body.Append("\"format\":\"text\"");
            if (!string.Equals(source, "auto", StringComparison.OrdinalIgnoreCase))
                body.Append(",\"source\":\"").Append(JsonEscape(source)).Append("\"");
            body.Append("}");

            string url =
                "https://translation.googleapis.com/language/translate/v2?key="
                + Uri.EscapeDataString(apiKey);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(
                    body.ToString(),
                    Encoding.UTF8,
                    "application/json");

                using (HttpResponseMessage response =
                    await Http.SendAsync(request).ConfigureAwait(false))
                {
                    string json =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    string detected = ExtractJsonStringProperty(json, "detectedSourceLanguage");
                    if (string.Equals(source, "auto", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(detected))
                    {
                        LastResponseDetectedSourceLang = NormalizeLang(detected, string.Empty);
                    }

                    string translated = ExtractJsonStringProperty(json, "translatedText");
                    return WebUtility.HtmlDecode(translated ?? string.Empty);
                }
            }
        }

        private static async Task<string> TranslateWithGemini(
            string text,
            string sourceLang,
            string targetLang)
        {
            string apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return string.Empty;

            string source = ResolveSourceLangForRequest(sourceLang, "auto");
            string target = ResolveTargetLangForRequest(targetLang, GetAmongUserLangCode());
            string instructions = BuildAmongUsTranslationInstructions(source, target);
            string input = "Translate this Among Us chat message: " + text;

            string body =
                "{"
                + "\"systemInstruction\":{\"parts\":[{\"text\":\"" + JsonEscape(instructions) + "\"}]},"
                + "\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + JsonEscape(input) + "\"}]}],"
                + "\"generationConfig\":{\"temperature\":0,\"maxOutputTokens\":256}"
                + "}";

            string model = GetGeminiModel();
            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/"
                + Uri.EscapeDataString(model)
                + ":generateContent";

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response =
                    await Http.SendAsync(request).ConfigureAwait(false))
                {
                    string json =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();
                    return ExtractGeminiOutputText(json);
                }
            }
        }

        private static async Task<string> TranslateWithOpenAI(
            string text,
            string sourceLang,
            string targetLang)
        {
            string apiKey = GetOpenAIApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return string.Empty;

            string source = ResolveSourceLangForRequest(sourceLang, "auto");
            string target = ResolveTargetLangForRequest(targetLang, GetAmongUserLangCode());
            string instructions = BuildAmongUsTranslationInstructions(source, target);
            string input = "Translate this Among Us chat message: " + text;

            string body =
                "{"
                + "\"model\":\"" + JsonEscape(GetOpenAIModel()) + "\","
                + "\"instructions\":\"" + JsonEscape(instructions) + "\","
                + "\"input\":\"" + JsonEscape(input) + "\","
                + "\"store\":false"
                + "}";

            using (HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/responses"))
            {
                request.Headers.TryAddWithoutValidation(
                    "Authorization",
                    "Bearer " + apiKey);

                request.Content = new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json");

                using (HttpResponseMessage response =
                    await Http.SendAsync(request).ConfigureAwait(false))
                {
                    string json =
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();
                    return ExtractOutputText(json);
                }
            }
        }

        private static string BuildAmongUsTranslationInstructions(
            string sourceLang,
            string targetLang)
        {
            string source =
                string.Equals(
                    sourceLang,
                    "auto",
                    StringComparison.OrdinalIgnoreCase)
                    ? "automatically detected source language"
                    : sourceLang;

            return
                "You are a translation engine specialized in Among Us in-game chat. "
                + "Translate short, informal, typo-filled multiplayer chat messages. "
                + "Understand Among Us slang, abbreviations, misspellings, phonetic spellings and community jargon from context. "
                + "Source language: " + source + ". "
                + "Target language: " + targetLang + ". "

                + "OUTPUT RULES: "
                + "Return ONLY the translated chat message. "
                + "Do not add explanations, labels, notes, quotation marks or extra text. "
                + "Preserve player names exactly. "
                + "Preserve the intended Among Us meaning instead of translating literally. "
                + "Mentally correct obvious typos before translating. "
                + "Keep the result short and natural, like real in-game chat. "

                + "AMONG US GLOSSARY AND CONTEXT: "
                + "vent, vented, venting, ventato, ventata, botolato, botolata, botola, ha botolato, si è botolato "
                + "and similar slang usually mean using or entering a vent. "
                + "sus means suspicious. "
                + "imp or impo means impostor. "
                + "crew means crewmate. "
                + "ss means shapeshifter when the context is Among Us. "
                + "self or self report means self-report. "
                + "elec means Electrical. "
                + "cams means Security cameras. "
                + "med or medbay means MedBay. "
                + "comms means Communications. "
                + "nav means Navigation. "
                + "report means reporting a dead body. "
                + "kill, killed, killato and killata refer to an in-game kill. "
                + "clear and safe usually mean a player is considered innocent. "
                + "scan usually refers to MedBay scan. "
                + "task or tasks refer to in-game tasks. "
                + "fake task means pretending to do a task. "
                + "skip or skippa means skipping the vote. "
                + "where in a meeting usually asks where the dead body was found. "

                + "PLAYER COLORS: "
                + "Color words often identify players. "
                + "Treat them as player colors, not as objects, metaphors or generic adjectives. "
                + "Common colors include red, blue, green, pink, orange, yellow, black, white, purple, brown, cyan and lime, "
                + "plus their equivalents in the source language. "
                + "Examples: rosso=red player, blu=blue player, verde=green player, rosa=pink player, arancione=orange player, "
                + "giallo=yellow player, nero=black player, bianco=white player, viola=purple player, marrone=brown player, "
                + "ciano=cyan player, lime=lime player. "

                + "EXAMPLES OF INTENDED MEANING: "
                + "'red vented' means the red player used a vent. "
                + "'rosso ha botolato' means the red player used a vent. "
                + "'blu si è botolato' means the blue player used a vent. "
                + "'red sus' means the red player is suspicious. "
                + "'blue self' means the blue player may have self-reported. "
                + "'pink ss' means the pink player is a shapeshifter. "
                + "'lime clear' means the lime player is considered innocent. "
                + "'where' means asking where the body was found. "
                + "'botolato' must be interpreted as Among Us vent slang and never translated literally. ";
        }

        private static string ExtractOutputText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            // Responses API REST output: output[].content[] where type = output_text and text contains the answer.
            Match match = Regex.Match(
                json,
                @"""type""\s*:\s*""output_text""[\s\S]*?""text""\s*:\s*""((?:\\.|[^""\\])*)""",
                RegexOptions.IgnoreCase
            );

            if (match.Success && match.Groups.Count > 1)
                return UnescapeJsonString(match.Groups[1].Value);

            // Compatibility fallback for APIs that expose a direct output_text string.
            match = Regex.Match(
                json,
                @"""output_text""\s*:\s*""((?:\\.|[^""\\])*)""",
                RegexOptions.IgnoreCase
            );

            if (match.Success && match.Groups.Count > 1)
                return UnescapeJsonString(match.Groups[1].Value);

            return string.Empty;
        }

        private static string ExtractJsonStringProperty(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            Match match = Regex.Match(
                json,
                "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.IgnoreCase);

            if (!match.Success || match.Groups.Count < 2)
                return string.Empty;

            return UnescapeJsonString(match.Groups[1].Value);
        }

        private static string ExtractGeminiOutputText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            Match match = Regex.Match(
                json,
                "\"candidates\"\\s*:\\s*\\[[\\s\\S]*?\"content\"\\s*:\\s*\\{[\\s\\S]*?\"parts\"\\s*:\\s*\\[[\\s\\S]*?\"text\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
                return UnescapeJsonString(match.Groups[1].Value);

            return ExtractJsonStringProperty(json, "text");
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder sb = new StringBuilder(value.Length + 16);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private static string ExtractLibreTranslation(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            Match m = Regex.Match(json, "\\\"translatedText\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"\\\\])*)\\\"");
            if (!m.Success || m.Groups.Count < 2) return string.Empty;
            return UnescapeJsonString(m.Groups[1].Value);
        }


        private static string ExtractLibreDetectedSource(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            Match m = Regex.Match(json, "\\\"detectedSource\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"\\\\])*)\\\"");
            if (!m.Success) m = Regex.Match(json, "\\\"detectedLanguage\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"\\\\])*)\\\"");
            if (!m.Success) m = Regex.Match(json, "\\\"language\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"\\\\])*)\\\"");
            if (!m.Success || m.Groups.Count < 2) return string.Empty;
            string lang = NormalizeLang(UnescapeJsonString(m.Groups[1].Value), string.Empty);
            if (string.Equals(lang, "auto", StringComparison.OrdinalIgnoreCase) || string.Equals(lang, "among", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            return lang;
        }

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    sb.Append(c);
                    continue;
                }

                char n = value[++i];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < value.Length)
                        {
                            string hex = value.Substring(i + 1, 4);
                            int code;
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                            else sb.Append("\\u");
                        }
                        else sb.Append("\\u");
                        break;
                    default:
                        sb.Append(n);
                        break;
                }
            }
            return sb.ToString();
        }

        private static bool SameLang(string a, string b)
        {
            return string.Equals(NormalizeLang(a, ""), NormalizeLang(b, ""), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLang(string lang, string fallback)
        {
            if (string.IsNullOrWhiteSpace(lang)) return fallback;
            string value = lang.Trim();
            if (value.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || value.Equals("zh_CN", StringComparison.OrdinalIgnoreCase)) return "zh";
            if (value.Equals("among", StringComparison.OrdinalIgnoreCase)) return "among";
            if (value.Equals("auto", StringComparison.OrdinalIgnoreCase)) return "auto";
            return value.ToLowerInvariant();
        }

        private static string MakeCacheKey(string text, string sourceLang, string targetLang)
        {
            return sourceLang.ToLowerInvariant() + "|" + targetLang.ToLowerInvariant() + "|" + NormalizeLocalTextKey(text);
        }

        private static bool LooksLikeCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.TrimStart();
            return t.StartsWith("/") || t.StartsWith(".") || t.StartsWith("!");
        }

        private static bool IsLiveTranslatorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.TrimStart();
            return t.StartsWith("[LT]", StringComparison.OrdinalIgnoreCase);
        }

        public static string CleanChatText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text;
            try { t = Regex.Replace(t, "<[^>]*>", string.Empty); } catch { }
            t = t.Replace("\u200B", string.Empty).Replace("\u200C", string.Empty).Replace("\u200D", string.Empty).Replace("\uFEFF", string.Empty);
            t = RemoveTrailingHexHashes(t);
            t = Regex.Replace(t, "[ \\t]+", " ");
            t = Regex.Replace(t, "\\s+\\n", "\n");
            t = Regex.Replace(t, "\\n\\s+", "\n");
            return t.Trim();
        }

        private static string CleanTranslatedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = CleanChatText(text);
            t = RemoveTrailingHexHashes(t);
            t = StripAiTranslationArtifacts(t, string.Empty);
            t = SanitizeForAmongUsDisplay(t);
            return t.Trim();
        }

        private static string CleanOutgoingChatText(string translated, string original)
        {
            if (string.IsNullOrEmpty(translated)) return string.Empty;
            string t = StripAiTranslationArtifacts(translated, original);
            t = SanitizeForAmongUsDisplay(t);
            t = NormalizeSafePunctuation(t);
            t = RemoveUnsafeOutgoingCharacters(t);
            t = Regex.Replace(t, "[ \t\r\n]+", " ").Trim();
            if (t.Length > 180) t = t.Substring(0, 180).Trim();
            return t;
        }

        private static string StripAiTranslationArtifacts(string text, string original)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text.Trim();

            try
            {
                // AI responses should not return "source > translation", but if they do, keep only the last translated part.
                string[] separators = new string[] { "→", "⇒", "➜", "➔", "➡", "=>", "->", " > " };
                foreach (string sep in separators)
                {
                    int idx = t.LastIndexOf(sep, StringComparison.Ordinal);
                    if (idx >= 0 && idx + sep.Length < t.Length)
                    {
                        string right = t.Substring(idx + sep.Length).Trim();
                        if (!string.IsNullOrWhiteSpace(right))
                        {
                            t = right;
                            break;
                        }
                    }
                }

                // Remove common labels accidentally returned by AI.
                t = Regex.Replace(t, "^(translation|translated|traduzione|tradotto|output|result|risultato|italian|italiano|english|inglese|chinese|cinese|russian|russo|japanese|giapponese|korean|coreano|arabic|arabo)\\s*[:：-]\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

                // If multiple lines are returned, prefer the last non-label line.
                string[] lines = t.Replace("\r", "").Split('\n');
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (Regex.IsMatch(line, "^(source|target|original|traduzione|translation)\\b", RegexOptions.IgnoreCase)) continue;
                    t = line;
                    break;
                }
            }
            catch { }

            return t.Trim().Trim('"', '\'', '`');
        }

        private static string NormalizeSafePunctuation(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text;
            t = t.Replace('“', '"').Replace('”', '"').Replace('„', '"');
            t = t.Replace('‘', '\'').Replace('’', '\'').Replace('`', '\'');
            t = t.Replace('–', '-').Replace('—', '-').Replace('―', '-');
            t = t.Replace('…', '.');
            t = t.Replace('。', '.').Replace('，', ',').Replace('！', '!').Replace('？', '?').Replace('：', ':').Replace('；', ';');
            t = t.Replace('（', '(').Replace('）', ')').Replace('【', '[').Replace('】', ']');
            return t;
        }

        private static string RemoveUnsafeOutgoingCharacters(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                if (char.IsSurrogate(ch)) continue;
                if (ch == '<' || ch == '>') continue;
                UnicodeCategory cat = char.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.Control || cat == UnicodeCategory.Format || cat == UnicodeCategory.PrivateUse || cat == UnicodeCategory.Surrogate) continue;

                // Remove arrows, emoji-like symbols and math/technical symbols that Among Us chat can reject.
                if ((ch >= '\u2190' && ch <= '\u21FF') || (ch >= '\u2700' && ch <= '\u27BF') || (ch >= '\u2900' && ch <= '\u297F') || (ch >= '\u2B00' && ch <= '\u2BFF')) continue;
                if (cat == UnicodeCategory.OtherSymbol || cat == UnicodeCategory.MathSymbol || cat == UnicodeCategory.ModifierSymbol) continue;

                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                {
                    sb.Append(ch);
                    continue;
                }

                const string allowed = ".,!?;:'\"()[]{}+-_/@#&%*=$";
                if (allowed.IndexOf(ch) >= 0) sb.Append(ch);
            }
            return sb.ToString();
        }


        public static string SanitizeForAmongUsDisplay(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text;
            t = t.Replace("\u200E", string.Empty).Replace("\u200F", string.Empty);
            t = t.Replace("\u202A", string.Empty).Replace("\u202B", string.Empty).Replace("\u202C", string.Empty).Replace("\u202D", string.Empty).Replace("\u202E", string.Empty);
            t = t.Replace("\u2066", string.Empty).Replace("\u2067", string.Empty).Replace("\u2068", string.Empty).Replace("\u2069", string.Empty);
            t = NormalizeSafePunctuation(t);
            try
            {
                if (Regex.IsMatch(t, "[\u0600-\u06FF]"))
                    t = TransliterateArabicToLatin(t);
                t = Regex.Replace(t, "[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
            }
            catch { }
            return t.Trim();
        }

        private static string TransliterateArabicToLatin(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (char ch in text)
            {
                if (ch >= '\u064B' && ch <= '\u065F') continue;
                switch (ch)
                {
                    case 'ء': sb.Append("a"); break;
                    case 'آ': sb.Append("aa"); break;
                    case 'أ': sb.Append("a"); break;
                    case 'ؤ': sb.Append("w"); break;
                    case 'إ': sb.Append("i"); break;
                    case 'ئ': sb.Append("y"); break;
                    case 'ا': sb.Append("a"); break;
                    case 'ب': sb.Append("b"); break;
                    case 'ة': sb.Append("h"); break;
                    case 'ت': sb.Append("t"); break;
                    case 'ث': sb.Append("th"); break;
                    case 'ج': sb.Append("j"); break;
                    case 'ح': sb.Append("h"); break;
                    case 'خ': sb.Append("kh"); break;
                    case 'د': sb.Append("d"); break;
                    case 'ذ': sb.Append("dh"); break;
                    case 'ر': sb.Append("r"); break;
                    case 'ز': sb.Append("z"); break;
                    case 'س': sb.Append("s"); break;
                    case 'ش': sb.Append("sh"); break;
                    case 'ص': sb.Append("s"); break;
                    case 'ض': sb.Append("d"); break;
                    case 'ط': sb.Append("t"); break;
                    case 'ظ': sb.Append("z"); break;
                    case 'ع': sb.Append("a"); break;
                    case 'غ': sb.Append("gh"); break;
                    case 'ف': sb.Append("f"); break;
                    case 'ق': sb.Append("q"); break;
                    case 'ك': sb.Append("k"); break;
                    case 'ل': sb.Append("l"); break;
                    case 'م': sb.Append("m"); break;
                    case 'ن': sb.Append("n"); break;
                    case 'ه': sb.Append("h"); break;
                    case 'و': sb.Append("w"); break;
                    case 'ى': sb.Append("a"); break;
                    case 'ي': sb.Append("y"); break;
                    case '٠': sb.Append("0"); break;
                    case '١': sb.Append("1"); break;
                    case '٢': sb.Append("2"); break;
                    case '٣': sb.Append("3"); break;
                    case '٤': sb.Append("4"); break;
                    case '٥': sb.Append("5"); break;
                    case '٦': sb.Append("6"); break;
                    case '٧': sb.Append("7"); break;
                    case '٨': sb.Append("8"); break;
                    case '٩': sb.Append("9"); break;
                    case '؟': sb.Append("?"); break;
                    case '،': sb.Append(","); break;
                    case '؛': sb.Append(";"); break;
                    default:
                        if (ch >= '\u0600' && ch <= '\u06FF') { }
                        else sb.Append(ch);
                        break;
                }
            }
            return Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
        }

        private static string RemoveTrailingHexHashes(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string t = text.Trim();
            // Rimuove suffissi/hash esadecimali da 32 caratteri, anche se attaccati alla parola precedente.
            // Esempio: "italiano0aea2f5ef36731b02422e218d5add801" -> "italiano".
            try
            {
                string old;
                do
                {
                    old = t;
                    t = Regex.Replace(t, "[0-9a-fA-F]{32}$", string.Empty).TrimEnd();
                }
                while (!string.Equals(old, t, StringComparison.Ordinal));
            }
            catch { }
            return t;
        }

        public static string GetLanguageDisplay(string code)
        {
            LanguageDef def = FindLanguage(TranslationLanguages, code);
            if (def == null) return code;
            if (def.Code.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                string last = GetLastIncomingDetectedLang();
                return T("autoDetect") + " (auto" + (string.IsNullOrWhiteSpace(last) ? "" : " -> " + last.ToUpperInvariant()) + ")";
            }
            if (def.Code.Equals("among", StringComparison.OrdinalIgnoreCase)) return "Among Us: " + GetLanguageDisplay(GetAmongUserLangCode());
            return def.EnglishName + " / " + def.NativeName + " (" + def.Code + ")";
        }

        private static string GetLanguageShortLabel(string code)
        {
            code = NormalizeLang(code, string.Empty);
            if (string.Equals(code, "among", StringComparison.OrdinalIgnoreCase)) return GetLanguageShortLabel(GetAmongUserLangCode());
            if (string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase)) return "AUTO";
            LanguageDef def = FindLanguage(TranslationLanguages, code);
            if (def == null) return code.ToUpperInvariant();
            return def.Code.ToUpperInvariant();
        }

        public static LanguageDef FindLanguage(LanguageDef[] source, string code)
        {
            if (source == null || string.IsNullOrWhiteSpace(code)) return null;
            for (int i = 0; i < source.Length; i++)
            {
                if (string.Equals(source[i].Code, code, StringComparison.OrdinalIgnoreCase)) return source[i];
            }
            return null;
        }

        public static string T(string key)
        {
            string lang = GetSystemMenuLanguage().ToLowerInvariant();
            if (lang == "es-419") lang = "es";
            if (lang == "pt-br") lang = "pt";
            if (lang == "zh-tw" || lang == "other" || lang == "ga" || lang == "ceb" || lang == "ru" || lang == "ja" || lang == "ko" || lang == "nl" || lang == "pl" || lang == "ar") lang = "en";

            if (lang == "it")
            {
                switch (key)
                {
                    case "title": return "LiveTranslator";
                    case "menuLanguage": return "Lingua menu";
                    case "general": return "Generale";
                    case "enabled": return "LiveTranslator";
                    case "incomingEnabled": return "Traduci messaggi ricevuti";
                    case "outgoingEnabled": return "Traduci messaggi che invio";
                    case "outgoingPrefixOnly": return "Invio tradotto solo con - davanti";
                    case "showOriginal": return "Mostra originale + traduzione";
                    case "showStatus": return "Mostra messaggi [LT] in chat";
                    case "gameAware": return "Correzioni slang Among Us";
                    case "incomingSection": return "MESSAGGI RICEVUTI";
                    case "incomingFrom": return "Leggi messaggi da";
                    case "incomingTo": return "Traduci messaggi in";
                    case "outgoingSection": return "MESSAGGI INVIATI";
                    case "outgoingFrom": return "Scrivo messaggi in";
                    case "outgoingTo": return "Invia messaggi in";
                    case "provider": return "Provider";
                    case "googleOnlyNote": return "Provider disponibile: OpenAI";
                    case "preset": return "Preset Inglese→Italiano / Italiano→Inglese";
                    case "clearCache": return "Svuota cache";
                    case "close": return "Chiudi";
                    case "back": return "Indietro";
                    case "selectLanguage": return "Seleziona lingua";
                    case "summaryRead": return "Lettura";
                    case "summarySend": return "Invio";
                    case "autoDetect": return "Rilevamento automatico";
                    case "note": return "Modalità: puoi tradurre sempre oppure solo quando la chat è aperta.";
                    case "paste": return "Incolla";
                    case "show": return "Mostra";
                    case "hide": return "Nascondi";
                    case "noApiKey": return "Nessuna chiave API";
                    case "apiKeyEditHint": return "Scrivi qui • Invio per terminare • Ctrl+V per incollare";
                    case "apiKeySaved": return "Chiave API salvata.";
                    case "clipboardEmpty": return "Gli appunti sono vuoti.";
                    case "localEditor": return "Archivio traduzioni locali";
                    case "clear": return "Pulisci";
                    case "localFiles": return "Cartella file locali";
                    case "languagePair": return "Coppia lingue";
                    case "searchTranslation": return "Cerca nell'archivio";
                    case "searchHint": return "Testo originale o tradotto da cercare";
                    case "search": return "Cerca";
                    case "newTranslation": return "Nuova";
                    case "originalText": return "Testo originale";
                    case "originalHint": return "Scrivi il testo originale";
                    case "translatedText": return "Traduzione corretta";
                    case "translationHint": return "Scrivi la traduzione corretta";
                    case "saveTranslation": return "Salva / modifica";
                    case "deleteTranslation": return "Elimina";
                    case "translationSaved": return "Traduzione salvata come correzione manuale.";
                    case "translationInvalid": return "Testo originale o traduzione mancanti.";
                    case "translationDeleted": return "Traduzione eliminata.";
                    case "translationNotFound": return "Traduzione non trovata.";
                    case "searchResults": return "Risultati";
                    case "noResults": return "Nessuna traduzione trovata.";
                    case "manualTranslation": return "Correzione manuale selezionata.";
                    case "automaticTranslation": return "Traduzione automatica selezionata.";
                    case "resultsFound": return "risultati trovati";
                }
            }
            else if (lang == "es")
            {
                switch (key)
                {
                    case "menuLanguage": return "Idioma del menú";
                    case "general": return "General";
                    case "enabled": return "LiveTranslator";
                    case "incomingEnabled": return "Traducir mensajes recibidos";
                    case "outgoingEnabled": return "Traducir mensajes enviados";
                    case "outgoingPrefixOnly": return "Traducir envío solo con símbolo";
                    case "showOriginal": return "Mostrar original + traducción";
                    case "showStatus": return "Mostrar mensajes [LT] en el chat";
                    case "incomingSection": return "MENSAJES RECIBIDOS";
                    case "incomingFrom": return "Leer mensajes de";
                    case "incomingTo": return "Traducir mensajes a";
                    case "outgoingSection": return "MENSAJES ENVIADOS";
                    case "outgoingFrom": return "Escribo mensajes en";
                    case "outgoingTo": return "Enviar mensajes en";
                    case "provider": return "Proveedor";
                    case "googleOnlyNote": return "Proveedor disponible: OpenAI";
                    case "preset": return "Preset Inglés→Italiano / Italiano→Inglés";
                    case "clearCache": return "Vaciar caché";
                    case "close": return "Cerrar";
                    case "back": return "Volver";
                    case "selectLanguage": return "Seleccionar idioma";
                    case "summaryRead": return "Lectura";
                    case "summarySend": return "Envío";
                    case "autoDetect": return "Detección automática";
                    case "note": return "Los mensajes recibidos se traducen solo cuando el chat está abierto, para reducir lag.";
                    case "paste": return "Pegar";
                    case "show": return "Mostrar";
                    case "hide": return "Ocultar";
                    case "noApiKey": return "Sin clave API";
                    case "apiKeyEditHint": return "Escribe aquí • Enter para terminar • Ctrl+V para pegar";
                    case "apiKeySaved": return "Clave API guardada.";
                    case "clipboardEmpty": return "El portapapeles está vacío.";
                }
            }
            else if (lang == "fr")
            {
                switch (key)
                {
                    case "menuLanguage": return "Langue du menu";
                    case "general": return "Général";
                    case "enabled": return "LiveTranslator";
                    case "incomingEnabled": return "Traduire les messages reçus";
                    case "outgoingEnabled": return "Traduire les messages envoyés";
                    case "outgoingPrefixOnly": return "Traduire l'envoi seulement avec symbole";
                    case "showOriginal": return "Afficher original + traduction";
                    case "showStatus": return "Afficher les messages [LT] dans le chat";
                    case "incomingSection": return "MESSAGES REÇUS";
                    case "incomingFrom": return "Lire les messages depuis";
                    case "incomingTo": return "Traduire les messages vers";
                    case "outgoingSection": return "MESSAGES ENVOYÉS";
                    case "outgoingFrom": return "J'écris les messages en";
                    case "outgoingTo": return "Envoyer les messages en";
                    case "provider": return "Fournisseur";
                    case "googleOnlyNote": return "Fournisseur disponible : OpenAI";
                    case "preset": return "Preset Anglais→Italien / Italien→Anglais";
                    case "clearCache": return "Vider le cache";
                    case "close": return "Fermer";
                    case "back": return "Retour";
                    case "selectLanguage": return "Choisir la langue";
                    case "summaryRead": return "Lecture";
                    case "summarySend": return "Envoi";
                    case "autoDetect": return "Détection automatique";
                    case "note": return "Les messages reçus sont traduits seulement quand le chat est ouvert, pour réduire le lag.";
                    case "paste": return "Coller";
                    case "show": return "Afficher";
                    case "hide": return "Masquer";
                    case "noApiKey": return "Aucune clé API";
                    case "apiKeyEditHint": return "Écrivez ici • Entrée pour terminer • Ctrl+V pour coller";
                    case "apiKeySaved": return "Clé API enregistrée.";
                    case "clipboardEmpty": return "Le presse-papiers est vide.";
                }
            }
            else if (lang == "de")
            {
                switch (key)
                {
                    case "menuLanguage": return "Menüsprache";
                    case "general": return "Allgemein";
                    case "enabled": return "LiveTranslator";
                    case "incomingEnabled": return "Empfangene Nachrichten übersetzen";
                    case "outgoingEnabled": return "Gesendete Nachrichten übersetzen";
                    case "outgoingPrefixOnly": return "Senden nur mit Symbol übersetzen";
                    case "showOriginal": return "Original + Übersetzung anzeigen";
                    case "showStatus": return "[LT]-Meldungen im Chat anzeigen";
                    case "incomingSection": return "EMPFANGENE NACHRICHTEN";
                    case "incomingFrom": return "Nachrichten lesen aus";
                    case "incomingTo": return "Nachrichten übersetzen nach";
                    case "outgoingSection": return "GESENDETE NACHRICHTEN";
                    case "outgoingFrom": return "Ich schreibe Nachrichten auf";
                    case "outgoingTo": return "Nachrichten senden auf";
                    case "provider": return "Anbieter";
                    case "googleOnlyNote": return "Verfügbarer Anbieter: OpenAI";
                    case "preset": return "Preset Englisch→Italienisch / Italienisch→Englisch";
                    case "clearCache": return "Cache leeren";
                    case "close": return "Schließen";
                    case "back": return "Zurück";
                    case "selectLanguage": return "Sprache auswählen";
                    case "summaryRead": return "Lesen";
                    case "summarySend": return "Senden";
                    case "autoDetect": return "Automatisch erkennen";
                    case "note": return "Empfangene Nachrichten werden nur übersetzt, wenn der Chat geöffnet ist, um Lag zu reduzieren.";
                    case "paste": return "Einfügen";
                    case "show": return "Anzeigen";
                    case "hide": return "Ausblenden";
                    case "noApiKey": return "Kein API-Schlüssel";
                    case "apiKeyEditHint": return "Hier eingeben • Enter zum Beenden • Ctrl+V zum Einfügen";
                    case "apiKeySaved": return "API-Schlüssel gespeichert.";
                    case "clipboardEmpty": return "Die Zwischenablage ist leer.";
                }
            }
            else if (lang == "pt")
            {
                switch (key)
                {
                    case "menuLanguage": return "Idioma do menu";
                    case "general": return "Geral";
                    case "enabled": return "LiveTranslator";
                    case "incomingEnabled": return "Traduzir mensagens recebidas";
                    case "outgoingEnabled": return "Traduzir mensagens enviadas";
                    case "outgoingPrefixOnly": return "Traduzir envio só com símbolo";
                    case "showOriginal": return "Mostrar original + tradução";
                    case "showStatus": return "Mostrar mensagens [LT] no chat";
                    case "incomingSection": return "MENSAGENS RECEBIDAS";
                    case "incomingFrom": return "Ler mensagens de";
                    case "incomingTo": return "Traduzir mensagens para";
                    case "outgoingSection": return "MENSAGENS ENVIADAS";
                    case "outgoingFrom": return "Escrevo mensagens em";
                    case "outgoingTo": return "Enviar mensagens em";
                    case "provider": return "Provedor";
                    case "googleOnlyNote": return "Provedor disponível: OpenAI";
                    case "preset": return "Preset Inglês→Italiano / Italiano→Inglês";
                    case "clearCache": return "Limpar cache";
                    case "close": return "Fechar";
                    case "back": return "Voltar";
                    case "selectLanguage": return "Selecionar idioma";
                    case "summaryRead": return "Leitura";
                    case "summarySend": return "Envio";
                    case "autoDetect": return "Detectar automaticamente";
                    case "note": return "As mensagens recebidas são traduzidas só quando o chat está aberto, para reduzir lag.";
                    case "paste": return "Colar";
                    case "show": return "Mostrar";
                    case "hide": return "Ocultar";
                    case "noApiKey": return "Sem chave API";
                    case "apiKeyEditHint": return "Digite aqui • Enter para terminar • Ctrl+V para colar";
                    case "apiKeySaved": return "Chave API salva.";
                    case "clipboardEmpty": return "A área de transferência está vazia.";
                }
            }

            // Default English.
            switch (key)
            {
                case "title": return "LiveTranslator";
                case "menuLanguage": return "Menu language";
                case "general": return "General";
                case "enabled": return "LiveTranslator";
                case "incomingEnabled": return "Translate received messages";
                case "outgoingEnabled": return "Translate messages I send";
                case "outgoingPrefixOnly": return "Translate sent only with - prefix";
                case "showOriginal": return "Show original + translation";
                case "showStatus": return "Show [LT] messages in chat";
                case "gameAware": return "Among Us slang corrections";
                case "incomingSection": return "RECEIVED MESSAGES";
                case "incomingFrom": return "Read messages from";
                case "incomingTo": return "Translate messages to";
                case "outgoingSection": return "SENT MESSAGES";
                case "outgoingFrom": return "I write messages in";
                case "outgoingTo": return "Send messages in";
                case "provider": return "Provider";
                case "googleOnlyNote": return "Available provider: OpenAI";
                case "preset": return "Preset English→Italian / Italian→English";
                case "clearCache": return "Clear cache";
                case "close": return "Close";
                case "back": return "Back";
                case "selectLanguage": return "Select language";
                case "summaryRead": return "Reading";
                case "summarySend": return "Sending";
                case "autoDetect": return "Auto detect";
                case "note": return "Lobby/Meeting: always. Task: only while chat is open.";
                case "paste": return "Paste";
                case "show": return "Show";
                case "hide": return "Hide";
                case "noApiKey": return "No API key";
                case "apiKeyEditHint": return "Type here • Enter to finish • Ctrl+V to paste";
                case "apiKeySaved": return "API key saved.";
                case "clipboardEmpty": return "Clipboard is empty.";
                case "localEditor": return "Local translation archive";
                case "clear": return "Clear";
                case "localFiles": return "Local files folder";
                case "languagePair": return "Language pair";
                case "searchTranslation": return "Search local archive";
                case "searchHint": return "Search original or translated text";
                case "search": return "Search";
                case "newTranslation": return "New";
                case "originalText": return "Original text";
                case "originalHint": return "Type the original text";
                case "translatedText": return "Correct translation";
                case "translationHint": return "Type the correct translation";
                case "saveTranslation": return "Save / update";
                case "deleteTranslation": return "Delete";
                case "translationSaved": return "Translation saved as a manual correction.";
                case "translationInvalid": return "Original text or translation is missing.";
                case "translationDeleted": return "Translation deleted.";
                case "translationNotFound": return "Translation not found.";
                case "searchResults": return "Results";
                case "noResults": return "No translations found.";
                case "manualTranslation": return "Manual correction selected.";
                case "automaticTranslation": return "Automatic translation selected.";
                case "resultsFound": return "results found";
            }
            return key;
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        private static class ChatControllerAddChatPatch
        {
            public static void Prefix(PlayerControl sourcePlayer, ref string chatText, bool censor)
            {
                LiveTranslator.TryTranslateIncomingForDisplay(sourcePlayer, ref chatText);
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
        private static class PlayerControlRpcSendChatPatch
        {
            public static void Prefix(PlayerControl __instance, ref string chatText)
            {
                LiveTranslator.TryTranslateOutgoingForSend(ref chatText);
            }
        }
    }

    public class LanguageDef
    {
        public readonly string Code;
        public readonly string EnglishName;
        public readonly string NativeName;

        public LanguageDef(string code, string englishName, string nativeName)
        {
            Code = code;
            EnglishName = englishName;
            NativeName = nativeName;
        }
    }

    public class LiveTranslatorMenu : MonoBehaviour
    {
        public static LiveTranslatorMenu Instance;

        public LiveTranslatorMenu(IntPtr ptr) : base(ptr) { }

        public bool showMenu;

        private Rect panelRect = new Rect(80f, 80f, 820f, 650f);
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 editorScrollPosition = Vector2.zero;
        private SelectorMode selectorMode = SelectorMode.None;

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle sectionStyle;
        private GUIStyle inputStyle;

        private bool showApiKey;
        private bool translationEditorOpen;

        private string editorFromLang = "en";
        private string editorToLang = "it";
        private string editorSearch = string.Empty;
        private string editorOriginal = string.Empty;
        private string editorTranslation = string.Empty;
        private string editorStatus = string.Empty;
        private LiveTranslator.LocalTranslationEntry[] editorResults =
            new LiveTranslator.LocalTranslationEntry[0];

        // Manual input avoids Unity IMGUI TextField/TextEditor, which can fail
        // in IL2CPP when an unstripped TextEditor method is unavailable.
        // Types 0..2 are provider keys; 10..12 are editor fields.
        private bool apiKeyInputFocused;
        private int apiKeyInputType = -1;
        private string apiKeyInputBuffer = string.Empty;
        private const int ApiKeyMaxLength = 512;
        private const int EditorTextMaxLength = 1000;

        private enum SelectorMode
        {
            None,
            ReadFrom,
            ReadTo,
            SendFrom,
            SendTo,
            EditorFrom,
            EditorTo
        }

        private void Awake()
        {
            Instance = this;
            CenterPanel();
        }

        public void Update()
        {
            try
            {
                // Handle the custom API-key field before resetting input axes.
                HandleApiKeyInput();

                bool ctrl =
                    Input.GetKey(KeyCode.LeftControl) ||
                    Input.GetKey(KeyCode.RightControl);

                if (!apiKeyInputFocused &&
                    ctrl &&
                    Input.GetKeyDown(KeyCode.T) &&
                    !Input.GetKey(KeyCode.G))
                {
                    ToggleMenu();
                }
            }
            catch
            {
            }

            if (showMenu)
            {
                try
                {
                    Input.ResetInputAxes();
                }
                catch
                {
                }
            }
        }

        private void HandleApiKeyInput()
        {
            if (!apiKeyInputFocused) return;

            if (!showMenu || selectorMode != SelectorMode.None)
            {
                StopApiKeyInput();
                return;
            }

            if (apiKeyInputType < 10)
            {
                int providerType = GetCurrentProviderKeyType();
                if (providerType != apiKeyInputType)
                {
                    StopApiKeyInput();
                    return;
                }
            }
            else if (!translationEditorOpen)
            {
                StopApiKeyInput();
                return;
            }

            bool ctrl =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            if (ctrl && Input.GetKeyDown(KeyCode.V))
            {
                PasteApiKeyFromClipboard(apiKeyInputType);
                return;
            }

            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed)) return;

            bool changed = false;
            int maxLength = apiKeyInputType >= 10 ? EditorTextMaxLength : ApiKeyMaxLength;

            foreach (char c in typed)
            {
                if (c == '\b')
                {
                    if (apiKeyInputBuffer.Length > 0)
                    {
                        apiKeyInputBuffer =
                            apiKeyInputBuffer.Substring(0, apiKeyInputBuffer.Length - 1);
                        changed = true;
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    SaveApiKeyBuffer();
                    StopApiKeyInput();
                    return;
                }
                else if (!char.IsControl(c) && apiKeyInputBuffer.Length < maxLength)
                {
                    apiKeyInputBuffer += c;
                    changed = true;
                }
            }

            if (changed)
                SaveApiKeyBuffer();
        }

        private int GetCurrentProviderKeyType()
        {
            string provider = LiveTranslator.GetProvider();
            if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                return 2;
            return -1;
        }

        private int GetInputMaxLength(int keyType)
        {
            return keyType >= 10 ? EditorTextMaxLength : ApiKeyMaxLength;
        }

        private void FocusApiKeyInput(int keyType, string currentValue)
        {
            apiKeyInputType = keyType;
            apiKeyInputBuffer = currentValue ?? string.Empty;
            int maxLength = GetInputMaxLength(keyType);
            if (apiKeyInputBuffer.Length > maxLength)
                apiKeyInputBuffer = apiKeyInputBuffer.Substring(0, maxLength);
            apiKeyInputFocused = true;
        }

        private void StopApiKeyInput()
        {
            apiKeyInputFocused = false;
            apiKeyInputType = -1;
            apiKeyInputBuffer = string.Empty;
        }

        private void SaveApiKeyBuffer()
        {
            switch (apiKeyInputType)
            {
                case 0:
                    LiveTranslator.SetOpenAIApiKey(apiKeyInputBuffer);
                    break;
                case 1:
                    LiveTranslator.SetGoogleTranslateApiKey(apiKeyInputBuffer);
                    break;
                case 2:
                    LiveTranslator.SetGeminiApiKey(apiKeyInputBuffer);
                    break;
                case 10:
                    editorSearch = apiKeyInputBuffer;
                    break;
                case 11:
                    editorOriginal = apiKeyInputBuffer;
                    break;
                case 12:
                    editorTranslation = apiKeyInputBuffer;
                    break;
            }
        }

        private void PasteApiKeyFromClipboard(int keyType)
        {
            try
            {
                string clipboard = GUIUtility.systemCopyBuffer ?? string.Empty;
                clipboard = clipboard.Trim();

                if (string.IsNullOrWhiteSpace(clipboard))
                {
                    LiveTranslator.ShowStatusPublic(LiveTranslator.T("clipboardEmpty"));
                    return;
                }

                int maxLength = GetInputMaxLength(keyType);
                if (clipboard.Length > maxLength)
                    clipboard = clipboard.Substring(0, maxLength);

                apiKeyInputType = keyType;
                apiKeyInputBuffer = clipboard;
                apiKeyInputFocused = true;
                SaveApiKeyBuffer();

                if (keyType < 10)
                    LiveTranslator.ShowStatusPublic(LiveTranslator.T("apiKeySaved"));
            }
            catch (Exception ex)
            {
                try
                {
                    Debug.LogWarning("[LiveTranslator] Paste API key failed: " + ex.Message);
                }
                catch
                {
                }
            }
        }

        public void OpenMenu()
        {
            showMenu = true;
            selectorMode = SelectorMode.None;
            StopApiKeyInput();
            CenterPanel();
        }

        public void CloseMenu()
        {
            showMenu = false;
            selectorMode = SelectorMode.None;
            translationEditorOpen = false;
            StopApiKeyInput();
        }

        public void ToggleMenu()
        {
            if (showMenu) CloseMenu();
            else OpenMenu();
        }

        private void CenterPanel()
        {
            float w = Mathf.Min(900f, Screen.width - 40f);
            float targetH;
            if (selectorMode != SelectorMode.None)
                targetH = 620f;
            else if (translationEditorOpen)
                targetH = 760f;
            else
                targetH = 690f;

            float h = Mathf.Min(targetH, Screen.height - 40f);
            panelRect = new Rect(
                Screen.width / 2f - w / 2f,
                Screen.height / 2f - h / 2f,
                w,
                h);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 26;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.wordWrap = true;
            labelStyle.normal.textColor = Color.white;

            sectionStyle = new GUIStyle(GUI.skin.label);
            sectionStyle.fontSize = 18;
            sectionStyle.fontStyle = FontStyle.Bold;
            sectionStyle.alignment = TextAnchor.MiddleLeft;
            sectionStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.wordWrap = true;
            buttonStyle.normal.textColor = Color.white;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.textColor = Color.white;

            // Visual style for the custom manual-input field. This is drawn as a button/box
            // and never invokes GUI.TextField/GUILayout.TextField or UnityEngine.TextEditor.
            inputStyle = new GUIStyle(GUI.skin.box);
            inputStyle.fontSize = 15;
            inputStyle.alignment = TextAnchor.MiddleLeft;
            inputStyle.wordWrap = false;
            RectOffset inputPadding = new RectOffset();
            inputPadding.left = 12;
            inputPadding.right = 12;
            inputPadding.top = 0;
            inputPadding.bottom = 0;
            inputStyle.padding = inputPadding;
            inputStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (!showMenu) return;
            EnsureStyles();
            CenterPanel();

            Color oldBg = GUI.backgroundColor;
            Color oldColor = GUI.color;
            GUI.backgroundColor = Color.black;
            GUI.color = Color.black;
            for (int i = 0; i < 8; i++) GUI.Box(panelRect, string.Empty);
            GUI.backgroundColor = oldBg;
            GUI.color = oldColor;

            GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, panelRect.height - 28f));
            if (selectorMode != SelectorMode.None)
                DrawLanguageSelector();
            else if (translationEditorOpen)
                DrawTranslationEditor();
            else
                DrawMainPanel();
            GUILayout.EndArea();
        }

        private void DrawMainPanel()
        {
            GUILayout.Label(LiveTranslator.T("title"), titleStyle, GUILayout.Height(36f));
            GUILayout.Space(8f);

            // Main panel is short: no ScrollView here, otherwise GUILayout stretches it
            // and leaves a huge empty gap between Default and Close.
            GUILayout.BeginVertical();

            GUILayout.BeginVertical(boxStyle);
            DrawToggle(LiveTranslator.T("enabled"), LiveTranslator.GetEnabled(), 1);
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.BeginVertical(boxStyle);
            DrawLanguagePairRow("Read", LiveTranslator.GetReadFromLang(), SelectorMode.ReadFrom, LiveTranslator.GetReadToLang(), SelectorMode.ReadTo);
            DrawLanguagePairRow("Send", LiveTranslator.GetSendFromLang(), SelectorMode.SendFrom, LiveTranslator.GetSendToLang(), SelectorMode.SendTo);
            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                LiveTranslator.T("provider"),
                labelStyle,
                GUILayout.Width(220f),
                GUILayout.Height(42f)
            );

            if (GUILayout.Button(
                LiveTranslator.GetProviderDisplay(),
                buttonStyle,
                GUILayout.Height(42f)))
            {
                StopApiKeyInput();
                LiveTranslator.CycleProvider();
            }

            GUILayout.EndHorizontal();

            string provider = LiveTranslator.GetProvider();

            if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                DrawApiKeyField(
                    "OpenAI API Key",
                    LiveTranslator.GetOpenAIApiKey(),
                    0);
            }
            else if (string.Equals(provider, "GoogleTranslate", StringComparison.OrdinalIgnoreCase))
            {
                DrawApiKeyField(
                    "Google Cloud Translation API Key",
                    LiveTranslator.GetGoogleTranslateApiKey(),
                    1);
            }
            else if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
            {
                DrawApiKeyField(
                    "Gemini API Key",
                    LiveTranslator.GetGeminiApiKey(),
                    2);

                GUILayout.Label(
                    "Model: " + LiveTranslator.GetGeminiModel(),
                    labelStyle,
                    GUILayout.Height(24f));
            }
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            if (GUILayout.Button(
                LiveTranslator.T("localEditor"),
                buttonStyle,
                GUILayout.Height(44f)))
            {
                OpenTranslationEditor();
            }

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Default", buttonStyle, GUILayout.Height(44f)))
                LiveTranslator.ApplyDefaultPreset();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUI.backgroundColor = new Color(0.8f, 0f, 0f, 1f);
            if (GUILayout.Button(LiveTranslator.T("close"), buttonStyle, GUILayout.Height(48f))) CloseMenu();
            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
        }

        private void DrawApiKeyField(
            string label,
            string currentValue,
            int keyType)
        {
            GUILayout.Space(8f);
            GUILayout.Label(label, labelStyle, GUILayout.Height(28f));

            bool focused = apiKeyInputFocused && apiKeyInputType == keyType;
            string value = focused ? apiKeyInputBuffer : (currentValue ?? string.Empty);

            string displayValue;
            if (string.IsNullOrEmpty(value))
            {
                displayValue = focused
                    ? LiveTranslator.T("apiKeyEditHint")
                    : LiveTranslator.T("noApiKey");
            }
            else if (showApiKey)
            {
                displayValue = value;
            }
            else
            {
                displayValue = new string('*', Mathf.Min(value.Length, 48));
            }

            if (focused)
                displayValue += "  |";

            GUILayout.BeginHorizontal();

            // This looks and behaves like an editable field, but input is captured manually
            // in Update() via Input.inputString. No Unity TextEditor is used.
            if (GUILayout.Button(
                displayValue,
                inputStyle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)))
            {
                if (focused)
                    StopApiKeyInput();
                else
                    FocusApiKeyInput(keyType, currentValue);
            }

            if (GUILayout.Button(
                LiveTranslator.T("paste"),
                buttonStyle,
                GUILayout.Width(100f),
                GUILayout.Height(40f)))
            {
                PasteApiKeyFromClipboard(keyType);
            }

            if (GUILayout.Button(
                showApiKey ? LiveTranslator.T("hide") : LiveTranslator.T("show"),
                buttonStyle,
                GUILayout.Width(100f),
                GUILayout.Height(40f)))
            {
                showApiKey = !showApiKey;
            }

            GUILayout.EndHorizontal();

            if (focused)
            {
                GUILayout.Label(
                    LiveTranslator.T("apiKeyEditHint"),
                    labelStyle,
                    GUILayout.Height(24f)
                );
            }
        }

        private void OpenTranslationEditor()
        {
            StopApiKeyInput();
            translationEditorOpen = true;
            selectorMode = SelectorMode.None;
            editorFromLang = LiveTranslator.ResolveEditorLanguage(
                LiveTranslator.GetReadFromLang(),
                false);
            editorToLang = LiveTranslator.ResolveEditorLanguage(
                LiveTranslator.GetReadToLang(),
                true);
            editorStatus = string.Empty;
            RefreshEditorResults();
        }

        private void RefreshEditorResults()
        {
            editorResults = LiveTranslator.SearchLocalTranslations(
                editorFromLang,
                editorToLang,
                editorSearch,
                30);
        }

        private void DrawPlainInputField(
            string label,
            string currentValue,
            int inputType,
            string emptyHint)
        {
            GUILayout.Label(label, labelStyle, GUILayout.Height(25f));

            bool focused = apiKeyInputFocused && apiKeyInputType == inputType;
            string value = focused ? apiKeyInputBuffer : (currentValue ?? string.Empty);
            string display = string.IsNullOrEmpty(value) ? emptyHint : value;
            if (focused) display += "  |";

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                display,
                inputStyle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)))
            {
                if (focused)
                    StopApiKeyInput();
                else
                    FocusApiKeyInput(inputType, currentValue);
            }

            if (GUILayout.Button(
                LiveTranslator.T("paste"),
                buttonStyle,
                GUILayout.Width(95f),
                GUILayout.Height(40f)))
            {
                PasteApiKeyFromClipboard(inputType);
            }

            if (GUILayout.Button(
                LiveTranslator.T("clear"),
                buttonStyle,
                GUILayout.Width(95f),
                GUILayout.Height(40f)))
            {
                StopApiKeyInput();
                if (inputType == 10) editorSearch = string.Empty;
                else if (inputType == 11) editorOriginal = string.Empty;
                else if (inputType == 12) editorTranslation = string.Empty;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }

        private void DrawTranslationEditor()
        {
            GUILayout.Label(
                LiveTranslator.T("localEditor"),
                titleStyle,
                GUILayout.Height(36f));

            GUILayout.Space(5f);

            if (GUILayout.Button(
                LiveTranslator.T("back"),
                buttonStyle,
                GUILayout.Height(40f)))
            {
                StopApiKeyInput();
                translationEditorOpen = false;
                return;
            }

            GUILayout.Space(7f);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label(
                LiveTranslator.T("localFiles") + ": "
                + LiveTranslator.GetLocalTranslationDirectory(),
                labelStyle,
                GUILayout.Height(28f));

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                LiveTranslator.T("languagePair"),
                labelStyle,
                GUILayout.Width(150f),
                GUILayout.Height(42f));

            if (GUILayout.Button(
                LiveTranslator.GetLanguageDisplay(editorFromLang),
                buttonStyle,
                GUILayout.Height(42f)))
            {
                StopApiKeyInput();
                selectorMode = SelectorMode.EditorFrom;
            }

            GUILayout.Label(
                "  >  ",
                sectionStyle,
                GUILayout.Width(50f),
                GUILayout.Height(42f));

            if (GUILayout.Button(
                LiveTranslator.GetLanguageDisplay(editorToLang),
                buttonStyle,
                GUILayout.Height(42f)))
            {
                StopApiKeyInput();
                selectorMode = SelectorMode.EditorTo;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(7f);
            GUILayout.BeginVertical(boxStyle);

            DrawPlainInputField(
                LiveTranslator.T("searchTranslation"),
                editorSearch,
                10,
                LiveTranslator.T("searchHint"));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                LiveTranslator.T("search"),
                buttonStyle,
                GUILayout.Height(40f)))
            {
                StopApiKeyInput();
                RefreshEditorResults();
                editorStatus =
                    editorResults.Length
                    + " "
                    + LiveTranslator.T("resultsFound");
            }

            if (GUILayout.Button(
                LiveTranslator.T("newTranslation"),
                buttonStyle,
                GUILayout.Height(40f)))
            {
                StopApiKeyInput();
                editorOriginal = string.Empty;
                editorTranslation = string.Empty;
                editorStatus = string.Empty;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(7f);

            DrawPlainInputField(
                LiveTranslator.T("originalText"),
                editorOriginal,
                11,
                LiveTranslator.T("originalHint"));

            DrawPlainInputField(
                LiveTranslator.T("translatedText"),
                editorTranslation,
                12,
                LiveTranslator.T("translationHint"));

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0f, 0.45f, 1f, 1f);
            if (GUILayout.Button(
                LiveTranslator.T("saveTranslation"),
                buttonStyle,
                GUILayout.Height(42f)))
            {
                StopApiKeyInput();
                bool saved = LiveTranslator.UpsertLocalTranslation(
                    editorFromLang,
                    editorToLang,
                    editorOriginal,
                    editorTranslation,
                    true);

                RefreshEditorResults();
                editorStatus = saved
                    ? LiveTranslator.T("translationSaved")
                    : LiveTranslator.T("translationInvalid");
            }

            GUI.backgroundColor = new Color(0.75f, 0.1f, 0.1f, 1f);
            if (GUILayout.Button(
                LiveTranslator.T("deleteTranslation"),
                buttonStyle,
                GUILayout.Height(42f)))
            {
                StopApiKeyInput();
                bool removed = LiveTranslator.DeleteLocalTranslation(
                    editorFromLang,
                    editorToLang,
                    editorOriginal);

                RefreshEditorResults();
                editorStatus = removed
                    ? LiveTranslator.T("translationDeleted")
                    : LiveTranslator.T("translationNotFound");
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(editorStatus))
            {
                GUILayout.Label(
                    editorStatus,
                    labelStyle,
                    GUILayout.Height(25f));
            }

            GUILayout.EndVertical();

            GUILayout.Space(7f);
            GUILayout.Label(
                LiveTranslator.T("searchResults"),
                sectionStyle,
                GUILayout.Height(28f));

            editorScrollPosition = GUILayout.BeginScrollView(
                editorScrollPosition,
                GUILayout.ExpandHeight(true));

            if (editorResults == null || editorResults.Length == 0)
            {
                GUILayout.Label(
                    LiveTranslator.T("noResults"),
                    labelStyle,
                    GUILayout.Height(32f));
            }
            else
            {
                for (int i = 0; i < editorResults.Length; i++)
                {
                    LiveTranslator.LocalTranslationEntry entry = editorResults[i];
                    string marker = entry.Manual ? "[M] " : "[A] ";
                    string label =
                        marker
                        + entry.Original
                        + "\n→ "
                        + entry.Translation;

                    if (GUILayout.Button(
                        label,
                        buttonStyle,
                        GUILayout.Height(58f)))
                    {
                        StopApiKeyInput();
                        editorFromLang = entry.SourceLang;
                        editorToLang = entry.TargetLang;
                        editorOriginal = entry.Original;
                        editorTranslation = entry.Translation;
                        editorStatus = entry.Manual
                            ? LiveTranslator.T("manualTranslation")
                            : LiveTranslator.T("automaticTranslation");
                    }

                    GUILayout.Space(4f);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawModeRow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode", labelStyle, GUILayout.Width(260f), GUILayout.Height(42f));
            if (GUILayout.Button(LiveTranslator.GetTranslateModeDisplay(), buttonStyle, GUILayout.Height(42f)))
                LiveTranslator.ToggleTranslateWhenChatClosed();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawToggle(string label, bool value, int actionId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(520f), GUILayout.Height(40f));
            GUI.backgroundColor = value ? new Color(0f, 0.45f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            if (GUILayout.Button(value ? "ON" : "OFF", buttonStyle, GUILayout.Width(180f), GUILayout.Height(40f)))
            {
                InvokeToggle(actionId);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }

        private void InvokeToggle(int actionId)
        {
            switch (actionId)
            {
                case 1: LiveTranslator.ToggleEnabled(); break;
                case 5: LiveTranslator.ToggleShowStatusInChat(); break;
                case 8: LiveTranslator.ToggleTranslateWhenChatClosed(); break;
            }
        }

        private void DrawLanguagePairRow(string label, string leftCode, SelectorMode leftMode, string rightCode, SelectorMode rightMode)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(90f), GUILayout.Height(48f));
            if (GUILayout.Button(LiveTranslator.GetLanguageDisplay(leftCode), buttonStyle, GUILayout.Height(48f)))
            {
                StopApiKeyInput();
                selectorMode = leftMode;
            }
            GUILayout.Label("  >  ", sectionStyle, GUILayout.Width(50f), GUILayout.Height(48f));
            if (GUILayout.Button(LiveTranslator.GetLanguageDisplay(rightCode), buttonStyle, GUILayout.Height(48f)))
            {
                StopApiKeyInput();
                selectorMode = rightMode;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawLanguageSelector()
        {
            string title = LiveTranslator.T("selectLanguage");
            GUILayout.Label(title, titleStyle, GUILayout.Height(34f));
            GUILayout.Space(8f);

            if (GUILayout.Button(LiveTranslator.T("back"), buttonStyle, GUILayout.Height(42f)))
            {
                selectorMode = SelectorMode.None;
                return;
            }

            GUILayout.Space(10f);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            LanguageDef[] list = LiveTranslator.TranslationLanguages;
            int cols = 2;
            for (int i = 0; i < list.Length; i += cols)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < cols; c++)
                {
                    int index = i + c;
                    if (index >= list.Length)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    LanguageDef def = list[index];
                    string display = def.EnglishName + " / " + def.NativeName + " (" + def.Code + ")";

                    if (GUILayout.Button(display, buttonStyle, GUILayout.Height(48f)))
                    {
                        ApplySelectedLanguage(def.Code);
                        selectorMode = SelectorMode.None;
                    }
                    if (c < cols - 1) GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
        }

        private void ApplySelectedLanguage(string code)
        {
            switch (selectorMode)
            {
                case SelectorMode.ReadFrom:
                    LiveTranslator.SetReadFromLang(code);
                    break;
                case SelectorMode.ReadTo:
                    if (!string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase))
                        LiveTranslator.SetReadToLang(code);
                    break;
                case SelectorMode.SendFrom:
                    if (!string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase))
                        LiveTranslator.SetSendFromLang(code);
                    break;
                case SelectorMode.SendTo:
                    if (!string.Equals(code, "among", StringComparison.OrdinalIgnoreCase))
                        LiveTranslator.SetSendToLang(code);
                    break;
                case SelectorMode.EditorFrom:
                    if (!string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(code, "among", StringComparison.OrdinalIgnoreCase))
                    {
                        editorFromLang = code;
                        RefreshEditorResults();
                    }
                    break;
                case SelectorMode.EditorTo:
                    if (!string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(code, "among", StringComparison.OrdinalIgnoreCase))
                    {
                        editorToLang = code;
                        RefreshEditorResults();
                    }
                    break;
            }
        }
    }

}
