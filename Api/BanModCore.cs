using HarmonyLib;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BanMod
{
    public static class BanModCore
    {
        private const string ApiBaseUrl = "https://server.banmod.online";
        public const string PublicApiBaseUrl = ApiBaseUrl;
        private const string ActivationChallengeUrl = ApiBaseUrl + "/api/activation/challenge";
        private const string ActivationVerifyUrl = ApiBaseUrl + "/api/activation/verify";
        private const string ExtraModsReportUrl = ApiBaseUrl + "/api/mod/extra-mods/report";
        private const string AccessUrl = ApiBaseUrl + "/api/access";
        private const string LobbyStatusUrl = ApiBaseUrl + "/api/lobby/status";
        private const string ActiveLobbiesUrl = ApiBaseUrl + "/api/lobbies/active";
        private const string LoginManifestUrl = ApiBaseUrl + "/api/login/manifest";

        private const int RequestTimeoutSeconds = 15;
        private const int IdentityMaxAttempts = 2147483647;
        private const float IdentityRetrySeconds = 1f;
        private const float LobbyStatusIntervalSeconds = 20f;
        private const float LobbyListIntervalSeconds = 30f;
        private const float PremiumRefreshIntervalSeconds = 30f;

        private static bool _startupRequested;
        private static bool _startupStarted;
        private static bool _loopStarted;
        private static bool _statusRunning;
        private static bool _lobbyListRunning;
        private static bool _premiumRefreshLoopStarted;
        private static bool _premiumRefreshRunning;
        private static bool _premiumRefreshRequested;

        private static string _friendCode = "";
        private static string _playerName = "";
        private static string _activationToken = "";
        private static string _capturedFriendCode = "";
        private static string _capturedPlayerName = "";
        private static int _capturedClientId = -1;

        public static List<ModdedLobbyInfo> LastModdedLobbies { get; private set; } = new List<ModdedLobbyInfo>();

        private static readonly Dictionary<string, LoadedPremiumModule> LoadedPremiumModules =
            new Dictionary<string, LoadedPremiumModule>(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static void CaptureClientData(ClientData client)
        {
            if (client == null)
                return;

            try
            {
                string friendCode = Norm(client.FriendCode);
                string playerName = Norm(client.PlayerName);

                if (!string.IsNullOrWhiteSpace(friendCode))
                {
                    _capturedFriendCode = friendCode;
                    _capturedPlayerName = string.IsNullOrWhiteSpace(playerName) ? _capturedPlayerName : playerName;
                    _capturedClientId = client.Id;

                }
            }
            catch { }
        }

        public static void CaptureClientName(ClientData client)
        {
            if (client == null)
                return;

            try
            {
                string playerName = Norm(client.PlayerName);

                if (!string.IsNullOrWhiteSpace(playerName) && client.Id == _capturedClientId)
                    _capturedPlayerName = playerName;
            }
            catch { }
        }

        private static string ExtractNameFromFriendCode(string friendCode)
        {
            if (string.IsNullOrWhiteSpace(friendCode))
                return "";

            int idx = friendCode.IndexOf('#');

            if (idx > 0)
                return friendCode.Substring(0, idx).Trim();

            return friendCode.Trim();
        }

        public static void TryCaptureFriendCodeFromOriginalAccountObject(object source)
        {
            if (!string.IsNullOrWhiteSpace(_friendCode))
                return;

            try
            {
                string found = ScanOriginalDataObjectForFriendCode(source, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

                if (!string.IsNullOrWhiteSpace(found))
                {
                    _capturedFriendCode = found.Trim();

                    if (string.IsNullOrWhiteSpace(_capturedPlayerName))
                        _capturedPlayerName = ExtractNameFromFriendCode(_capturedFriendCode);

                }
            }
            catch { }
        }

        private static string ScanOriginalDataObjectForFriendCode(object obj, int depth, HashSet<object> visited)
        {
            if (obj == null || depth > 3)
                return "";

            Type t;
            try
            {
                t = obj.GetType();

                if (t.IsPrimitive || t.IsEnum)
                    return "";

                if (obj is string s)
                    return LooksLikeFriendCode(s) ? s.Trim() : "";
            }
            catch
            {
                return "";
            }

            try
            {
                if (!visited.Add(obj))
                    return "";
            }
            catch { }

            try
            {
                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    string fname = f.Name ?? "";

                    // Niente UI scan: accettiamo solo campi dati con nome FriendCode/Friend.
                    if (fname.IndexOf("friend", StringComparison.OrdinalIgnoreCase) < 0 &&
                        fname.IndexOf("code", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    object value = null;
                    try { value = f.GetValue(obj); } catch { }

                    string found = ExtractFriendCodeFromDataValue(value, depth, visited);

                    if (!string.IsNullOrWhiteSpace(found))
                        return found;
                }

                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!p.CanRead)
                        continue;

                    string pname = p.Name ?? "";

                    // Niente UI scan: accettiamo solo proprietà dati con nome FriendCode/Friend.
                    if (pname.IndexOf("friend", StringComparison.OrdinalIgnoreCase) < 0 &&
                        pname.IndexOf("code", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    object value = null;
                    try { value = p.GetValue(obj, null); } catch { }

                    string found = ExtractFriendCodeFromDataValue(value, depth, visited);

                    if (!string.IsNullOrWhiteSpace(found))
                        return found;
                }
            }
            catch { }

            return "";
        }

        private static string ExtractFriendCodeFromDataValue(object value, int depth, HashSet<object> visited)
        {
            if (value == null)
                return "";

            if (value is string s)
                return LooksLikeFriendCode(s) ? s.Trim() : "";

            if (depth >= 3)
                return "";

            return ScanOriginalDataObjectForFriendCode(value, depth + 1, visited);
        }

        private static bool LooksLikeFriendCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            int hash = value.IndexOf('#');
            if (hash <= 0 || hash >= value.Length - 1)
                return false;

            string left = value.Substring(0, hash);
            string right = value.Substring(hash + 1);

            if (left.Length < 2 || left.Length > 32)
                return false;

            if (right.Length < 4 || right.Length > 8)
                return false;

            for (int i = 0; i < right.Length; i++)
            {
                if (!char.IsDigit(right[i]))
                    return false;
            }

            return true;
        }

        public static bool TryCaptureEosFriendCode(string source)
        {
            try
            {
                if (!DestroyableSingleton<EOSManager>.InstanceExists)
                    return false;

                EOSManager eos = DestroyableSingleton<EOSManager>.Instance;

                if (eos == null)
                    return false;

                string friendCode = Norm(eos.FriendCode);

                if (string.IsNullOrWhiteSpace(friendCode))
                    return false;

                string playerName = ExtractNameFromFriendCode(friendCode);

                CaptureIdentity(friendCode, playerName, source);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CaptureIdentity(string friendCode, string playerName, string source)
        {
            friendCode = Norm(friendCode);
            playerName = Norm(playerName);

            if (string.IsNullOrWhiteSpace(friendCode))
                return;

            if (string.IsNullOrWhiteSpace(playerName))
                playerName = ExtractNameFromFriendCode(friendCode);


            _friendCode = friendCode;
            _playerName = string.IsNullOrWhiteSpace(playerName) ? "Unknown" : playerName;

        }


        public static string GetCurrentFriendCode()
        {
            try { return _friendCode ?? ""; } catch { return ""; }
        }

        public static string GetCurrentPlayerName()
        {
            try { return _playerName ?? ""; } catch { return ""; }
        }

        public static string GetCurrentActivationToken()
        {
            try { return _activationToken ?? ""; } catch { return ""; }
        }

        public static string GetCurrentBanModSha256()
        {
            try { return SafeGetOwnBanModSha256(); } catch { return ""; }
        }

        public static string GetCurrentBuildId()
        {
            try { return BanModBuildSecret.BuildId ?? ""; } catch { return ""; }
        }

        public static void RequestPremiumRefresh()
        {
            _premiumRefreshRequested = true;
        }

        public static bool IsPluginLoaded(string serviceKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serviceKey))
                    return false;

                return LoadedPremiumModules.ContainsKey(serviceKey.Trim());
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAnyPluginLoaded(params string[] serviceKeys)
        {
            try
            {
                if (serviceKeys == null || serviceKeys.Length == 0)
                    return false;

                for (int i = 0; i < serviceKeys.Length; i++)
                {
                    if (IsPluginLoaded(serviceKeys[i]))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static string[] GetLoadedPluginKeys()
        {
            try
            {
                string[] keys = new string[LoadedPremiumModules.Keys.Count];
                LoadedPremiumModules.Keys.CopyTo(keys, 0);
                return keys;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static void ClearCurrentActivationToken()
        {
            try { _activationToken = ""; } catch { }
        }

        public static IEnumerator EnsureActivationTokenForApi(Action<bool, string> callback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_activationToken))
                {
                    callback?.Invoke(true, _activationToken);
                    yield break;
                }

                TryCaptureEosFriendCode("EOSManager.FriendCode/api");

                if (string.IsNullOrWhiteSpace(_friendCode))
                {
                    callback?.Invoke(false, "");
                    yield break;
                }
            }
            catch
            {
                callback?.Invoke(false, "");
                yield break;
            }

            bool ok = false;
            yield return ActivationFlow(x => ok = x);
            callback?.Invoke(ok && !string.IsNullOrWhiteSpace(_activationToken), _activationToken);
        }

        public static void Init(ManualLogSource log)
        {
            _ = log;
        }

        public static void RequestStartup()
        {
            _startupRequested = true;
        }

        internal static void TryStart()
        {

            if (!_startupRequested || _startupStarted)
                return;

            if (AmongUsClient.Instance == null)
                return;

            _startupStarted = true;
            AmongUsClient.Instance.StartCoroutine(StartupFlow().WrapToIl2Cpp());
        }

        private static IEnumerator EnsureLoginBinLoaded(Action<bool> callback)
        {
            if (BanModLoginRuntime.IsLoaded)
            {
                callback?.Invoke(true);
                yield break;
            }

            LoginManifest manifest = null;
            byte[] bytes = null;
            string expectedSha = "";
            string version = "";

            UnityWebRequest request = UnityWebRequest.Get(LoginManifestUrl);
            request.timeout = RequestTimeoutSeconds;
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            string text = request.downloadHandler != null ? request.downloadHandler.text : "";
            long manifestResponseCode = request.responseCode;
            UnityWebRequest.Result manifestResult = request.result;
            bool manifestOk = manifestResult == UnityWebRequest.Result.Success &&
                              manifestResponseCode >= 200 && manifestResponseCode < 300;
            try { request.Dispose(); } catch { }

            if (manifestOk)
            {
                try { manifest = JsonSerializer.Deserialize<LoginManifest>(text, JsonOptions); }
                catch { }

                if (manifest != null && manifest.success && manifest.can_download &&
                    !string.IsNullOrWhiteSpace(manifest.download_url))
                {
                    expectedSha = Norm(manifest.sha256).ToLowerInvariant();
                    version = Norm(manifest.version);
                    yield return DownloadBytes(manifest.download_url, value => bytes = value);

                    if (bytes != null && bytes.Length > 0)
                    {
                        string actualSha = Sha256Hex(bytes);
                        if (!string.IsNullOrWhiteSpace(expectedSha) &&
                            !string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                        {
                            bytes = null;
                        }
                        else if (manifest.size_bytes > 0 && bytes.Length != manifest.size_bytes)
                        {
                            bytes = null;
                        }
                        else
                        {
                            expectedSha = actualSha;
                        }
                    }
                }
            }
            else
            {
            }

            if (bytes == null || bytes.Length == 0)
            {
                // login.bin is deliberately never trusted from an offline/local cache.
                // Every process start must receive the current server manifest and payload.
                callback?.Invoke(false);
                yield break;
            }

            bool loaded = BanModLoginRuntime.LoadAndStart(bytes, expectedSha, version, out _);
            try { CryptographicOperations.ZeroMemory(bytes); } catch { }


            callback?.Invoke(loaded);
        }

        private static IEnumerator StartupFlow()
        {
            bool loginLoaded = false;
            yield return EnsureLoginBinLoaded(ok => loginLoaded = ok);

            if (!loginLoaded)
            {
                BanMod.ForceDisableMod("Required login.bin could not be downloaded or verified.");
                yield break;
            }


            // L'updater deve partire prima di attivazione, token ed extra-mod gate.
            // In questo modo anche un client marcato come modificato o con extra mod
            // non consentite può sempre recuperare una release ufficiale obbligatoria.
            ModUpdater.StartupUpdateResult updateResult = null;
            yield return ModUpdater.CheckAtStartupCoroutine(result => updateResult = result);

            if (updateResult != null && updateResult.BlockStartup)
            {

                // Non continuare con attivazione/premium usando una DLL che il server
                // ha dichiarato da aggiornare obbligatoriamente.
                yield break;
            }

            GameIdentity identity = null;

            for (int i = 1; i <= IdentityMaxAttempts; i++)
            {
                identity = ReadIdentity();

                if (identity != null && !string.IsNullOrWhiteSpace(identity.FriendCode))
                    break;

                yield return new WaitForSeconds(IdentityRetrySeconds);
            }

            if (identity == null || string.IsNullOrWhiteSpace(identity.FriendCode))
            {
                yield break;
            }

            string newFriendCode = identity.FriendCode.Trim();
            string newPlayerName = string.IsNullOrWhiteSpace(identity.PlayerName) ? "Unknown" : identity.PlayerName.Trim();

            if (!string.Equals(_friendCode, newFriendCode, StringComparison.OrdinalIgnoreCase))
            {
                _friendCode = newFriendCode;
                _playerName = newPlayerName;

                StopAllPremiumModules();

            }
            else
            {
                _playerName = newPlayerName;
            }

            bool activationOk = false;
            yield return ActivationFlow(ok => activationOk = ok);

            if (!activationOk)
            {
                StartLobbyLoops();
                yield break;
            }

            yield return SendExtraModsReport("startup", _ => { });

            // Una sola richiesta per sessione, dopo attivazione e controllo extra-mod.
            // I manager uniscono i record del server esclusivamente ai file reali:
            // ./BAN_DATA/DENIED/Cheater.txt e ./BAN_DATA/DENIED/Teamer.txt.
            yield return BanModDeniedLists.RefreshAllCoroutine();

            // The base mod and lobby loops are never blocked by the login menu.
            // Only optional services wait until login.bin confirms username and preferences.
            StartLobbyLoops();

            while (!BanModLoginRuntime.IsReady && !BanMod.IsBanModDisabled)
                yield return null;

            if (BanMod.IsBanModDisabled)
                yield break;

            // login.bin può segnare il runtime come pronto mentre il menu è ancora aperto.
            // Non caricare alcun modulo opzionale finché l'utente non ha salvato e il menu
            // non è stato chiuso, altrimenti vengono caricati i valori precedenti/default.
            while (BanModLoginUi.IsOpen && !BanMod.IsBanModDisabled)
                yield return null;

            if (BanMod.IsBanModDisabled)
                yield break;

            yield return RequestPremiumAndLoadBins();
            StartPremiumRefreshLoop();
        }

        private static void StartLobbyLoops()
        {
            if (_loopStarted || AmongUsClient.Instance == null)
                return;

            _loopStarted = true;
            AmongUsClient.Instance.StartCoroutine(LobbyStatusLoop().WrapToIl2Cpp());
            AmongUsClient.Instance.StartCoroutine(LobbyListLoop().WrapToIl2Cpp());
        }

        private static void StartPremiumRefreshLoop()
        {
            if (_premiumRefreshLoopStarted || AmongUsClient.Instance == null)
                return;

            _premiumRefreshLoopStarted = true;
            _premiumRefreshRequested = false;
            AmongUsClient.Instance.StartCoroutine(PremiumRefreshLoop().WrapToIl2Cpp());
        }

        private static IEnumerator PremiumRefreshLoop()
        {
            while (!BanMod.IsBanModDisabled)
            {
                float elapsed = 0f;
                while (elapsed < PremiumRefreshIntervalSeconds && !_premiumRefreshRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                _premiumRefreshRequested = false;

                if (_premiumRefreshRunning || !BanModLoginRuntime.IsReady ||
                    string.IsNullOrWhiteSpace(_activationToken))
                    continue;

                _premiumRefreshRunning = true;
                yield return RequestPremiumAndLoadBins();
                _premiumRefreshRunning = false;
            }
        }

        private static IEnumerator LobbyStatusLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(LobbyStatusIntervalSeconds);

                if (!IsInLobbyOrGame())
                    continue;

                if (_statusRunning)
                    continue;

                yield return SendLobbyStatus();
            }
        }

        private static IEnumerator LobbyListLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(LobbyListIntervalSeconds);

                if (_lobbyListRunning)
                    continue;

                yield return FetchModdedLobbies();
            }
        }

        private static IEnumerator ActivationFlow(Action<bool> callback)
        {
            string challengeUrl = ActivationChallengeUrl + "?friend_code=" + UnityWebRequest.EscapeURL(_friendCode);

            UnityWebRequest c = UnityWebRequest.Get(challengeUrl);
            c.timeout = RequestTimeoutSeconds;
            c.downloadHandler = new DownloadHandlerBuffer();

            yield return c.SendWebRequest();

            string challengeText = c.downloadHandler != null ? c.downloadHandler.text : "";

            if (c.result != UnityWebRequest.Result.Success || c.responseCode < 200 || c.responseCode >= 300)
            {
                callback(false);
                yield break;
            }

            ChallengeResponse challenge = null;
            try { challenge = JsonSerializer.Deserialize<ChallengeResponse>(challengeText, JsonOptions); } catch { }

            if (challenge == null || !challenge.success || string.IsNullOrWhiteSpace(challenge.nonce))
            {
                callback(false);
                yield break;
            }

            string proof = Sha256Hex(Encoding.UTF8.GetBytes(challenge.nonce + ":" + _friendCode + ":" + BanModBuildSecret.GetActivationCode()));

            string body = "{"
                + "\"FriendCode\":" + JsonString(_friendCode) + ","
                + "\"PlayerName\":" + JsonString(_playerName) + ","
                + "\"Nonce\":" + JsonString(challenge.nonce) + ","
                + "\"Proof\":" + JsonString(proof) + ","
                + "\"BuildId\":" + JsonString(BanModBuildSecret.BuildId) + ","
                + "\"BanModSha256\":" + JsonString(SafeGetOwnBanModSha256()) + ","
                + "\"BuildCodeWasMissing\":" + BoolJson(BanModBuildSecret.BuildCodeWasMissing)
                + "}";

            UnityWebRequest v = new UnityWebRequest(ActivationVerifyUrl, "POST");
            v.timeout = RequestTimeoutSeconds;
            v.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            v.downloadHandler = new DownloadHandlerBuffer();
            v.SetRequestHeader("Content-Type", "application/json");

            yield return v.SendWebRequest();

            string verifyText = v.downloadHandler != null ? v.downloadHandler.text : "";

            if (v.result != UnityWebRequest.Result.Success || v.responseCode < 200 || v.responseCode >= 300)
            {
                callback(false);
                yield break;
            }

            ActivationResponse response = null;
            try { response = JsonSerializer.Deserialize<ActivationResponse>(verifyText, JsonOptions); } catch { }

            if (response == null || !response.success || string.IsNullOrWhiteSpace(response.activation_token))
            {
                callback(false);
                yield break;
            }

            _activationToken = response.activation_token;
            callback(true);
        }

        private static IEnumerator SendExtraModsReport(string reason, Action<bool> callback)
        {
            List<DetectedModInfo> mods = new List<DetectedModInfo>();
            bool scanSuccess = false;
            string scanError = "";

            try
            {
                mods = DetectExtraMods();
                scanSuccess = true;
            }
            catch (Exception ex)
            {
                scanSuccess = false;
                scanError = ex.GetType().Name + ": " + ex.Message;
                mods = new List<DetectedModInfo>();
            }

            string body = BuildExtraModsJson(reason, scanSuccess, scanError, mods);

            UnityWebRequest req = new UnityWebRequest(ExtraModsReportUrl, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string text = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result != UnityWebRequest.Result.Success || req.responseCode < 200 || req.responseCode >= 300)
            {
                callback(false);
                yield break;
            }

            ExtraReportResponse response = null;
            try { response = JsonSerializer.Deserialize<ExtraReportResponse>(text, JsonOptions); } catch { }

            bool ok = response != null && response.success && response.valid;
            callback(ok);
        }

        private static List<DetectedModInfo> DetectExtraMods()
        {
            // Report extra mod = DLL fisiche nella cartella BepInEx/plugins.
            // Non conta gli assembly base caricati dal gioco, così non escono 30+ falsi positivi.
            List<DetectedModInfo> result = new List<DetectedModInfo>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string pluginPath = "";
            try { pluginPath = Paths.PluginPath; } catch { }

            if (string.IsNullOrWhiteSpace(pluginPath) || !Directory.Exists(pluginPath))
                return result;

            foreach (string dll in Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories))
            {
                string file = Path.GetFileName(dll);

                if (string.IsNullOrWhiteSpace(file))
                    continue;

                if (file.Equals("BanMod.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!seen.Add(Path.GetFullPath(dll)))
                    continue;

                result.Add(new DetectedModInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    FileName = file,
                    AssemblyName = SafeReadAssemblyName(dll),
                    Version = SafeReadAssemblyVersion(dll),
                    Sha256 = SafeSha256File(dll)
                });
            }

            return result;
        }

        private static string SafeReadAssemblyName(string path)
        {
            try { return AssemblyName.GetAssemblyName(path).FullName; }
            catch { return ""; }
        }

        private static string SafeReadAssemblyVersion(string path)
        {
            try
            {
                Version v = AssemblyName.GetAssemblyName(path).Version;
                return v != null ? v.ToString() : "";
            }
            catch { return ""; }
        }

        private static IEnumerator RequestPremiumAndLoadBins()
        {
            string body = "{"
                + "\"FriendCode\":" + JsonString(_friendCode) + ","
                + "\"PlayerName\":" + JsonString(_playerName) + ","
                + "\"ActivationToken\":" + JsonString(_activationToken) + ","
                + "\"LoginBinSha256\":" + JsonString(BanModLoginRuntime.LoginBinSha256) + ","
                + "\"LoginBinVersion\":" + JsonString(BanModLoginRuntime.LoginBinVersion)
                + "}";

            UnityWebRequest req = new UnityWebRequest(AccessUrl, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(_activationToken))
                req.SetRequestHeader("Authorization", "Bearer " + _activationToken);

            yield return req.SendWebRequest();

            string text = req.downloadHandler != null ? req.downloadHandler.text : "";
            long responseCode = req.responseCode;
            UnityWebRequest.Result result = req.result;
            try { req.Dispose(); } catch { }

            if (result != UnityWebRequest.Result.Success || responseCode < 200 || responseCode >= 300)
            {
                yield break;
            }

            AccessResponse access = null;
            try { access = JsonSerializer.Deserialize<AccessResponse>(text, JsonOptions); }
            catch { }

            if (access == null || !access.success)
            {
                yield break;
            }

            if (access.force_disable_mod || access.force_disable)
            {
                string disableReason = string.IsNullOrWhiteSpace(access.force_disable_reason)
                    ? (string.IsNullOrWhiteSpace(access.reason)
                        ? "BanMod disabilitata temporaneamente dal server."
                        : access.reason)
                    : access.force_disable_reason;

                BanMod.ForceDisableMod(disableReason);
                yield break;
            }

            if (!access.premium_gate_ok)
            {
                StopAllPremiumModules();
                yield break;
            }

            HashSet<string> desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (access.services != null)
            {
                foreach (PremiumService service in access.services)
                {
                    if (service == null)
                        continue;

                    string serviceKey = service.GetKey();

                    // Difesa lato client: un servizio deve risultare consentito,
                    // abilitato e selezionato. Non affidarsi soltanto ad allowed.
                    if (string.IsNullOrWhiteSpace(serviceKey) ||
                        !service.allowed ||
                        !service.enabled ||
                        !service.selected ||
                        !service.GetAutoload())
                        continue;

                    desired.Add(serviceKey);
                }
            }

            string[] loadedKeys = GetLoadedPluginKeys();
            for (int i = 0; i < loadedKeys.Length; i++)
            {
                if (!desired.Contains(loadedKeys[i]))
                    UnloadPremiumModule(loadedKeys[i]);
            }

            foreach (string serviceKey in desired)
            {
                if (LoadedPremiumModules.ContainsKey(serviceKey))
                    continue;

                yield return LoadPremiumBin(serviceKey, _ => { });
            }

        }

        private static void UnloadPremiumModule(string serviceKey)
        {
            if (string.IsNullOrWhiteSpace(serviceKey) ||
                !LoadedPremiumModules.TryGetValue(serviceKey, out LoadedPremiumModule module))
                return;

            if (module.PluginInstances != null)
            {
                for (int i = module.PluginInstances.Count - 1; i >= 0; i--)
                {
                    object pluginInstance = module.PluginInstances[i];
                    if (pluginInstance == null)
                        continue;

                    try
                    {
                        MethodInfo unloadMethod = pluginInstance.GetType().GetMethod(
                            "Unload", BindingFlags.Public | BindingFlags.Instance);
                        unloadMethod?.Invoke(pluginInstance, null);
                    }
                    catch { }
                }
                module.PluginInstances.Clear();
            }

            LoadedPremiumModules.Remove(serviceKey);
        }

        private static IEnumerator LoadPremiumBin(string serviceKey, Action<bool> callback)
        {
            PremiumManifest manifest = null;

            string url = ApiBaseUrl + "/api/premium/" + UnityWebRequest.EscapeURL(serviceKey) + "/manifest";
            string body = "{"
                + "\"FriendCode\":" + JsonString(_friendCode) + ","
                + "\"ActivationToken\":" + JsonString(_activationToken)
                + "}";

            UnityWebRequest req = new UnityWebRequest(url, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string text = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result != UnityWebRequest.Result.Success || req.responseCode < 200 || req.responseCode >= 300)
            {
                callback(false);
                yield break;
            }

            try { manifest = JsonSerializer.Deserialize<PremiumManifest>(text, JsonOptions); }
            catch { }

            if (manifest == null || !manifest.success || !manifest.can_download || string.IsNullOrWhiteSpace(manifest.download_url))
            {
                callback(false);
                yield break;
            }

            byte[] binBytes = null;
            yield return DownloadBytes(manifest.download_url, value => binBytes = value);

            if (binBytes == null || binBytes.Length == 0)
            {
                callback(false);
                yield break;
            }

            try
            {
                string actualSha = Sha256Hex(binBytes);

                if (!string.IsNullOrWhiteSpace(manifest.sha256) &&
                    !string.Equals(actualSha, manifest.sha256, StringComparison.OrdinalIgnoreCase))
                {
                    callback(false);
                    yield break;
                }

                LoadedPremiumModule module = LoadManagedBinFromMemory(serviceKey, binBytes);
                LoadedPremiumModules[serviceKey] = module;
                callback(true);
            }
            catch
            {
                callback(false);
            }
            finally
            {
                try { CryptographicOperations.ZeroMemory(binBytes); } catch { }
            }
        }

        private static IEnumerator SendLobbyStatus()
        {
            _statusRunning = true;

            string friendCode = _friendCode;
            string playerName = _playerName;
            string lobbyCode = "";
            string hostName = "";
            string gameMode = "";
            string status = "";
            string region = "";
            string language = "";
            string platform = "";
            bool isOnline = true;
            bool shareLobby = true;
            bool isHost = false;
            bool isPublic = false;
            bool isPrivate = false;
            int players = 0;
            int maxPlayers = 15;
            int kc = 0;
            int impostorCount = 0;

            bool buildFailed = false;

            try
            {
                PlayerControl player = PlayerControl.LocalPlayer;

                // Riferimenti presi dalle funzioni del vecchio PlayerStatusAutoSender.
                // Non usiamo più le letture generiche del nuovo core per questi campi.
                if (player != null)
                {
                    string oldName = SafeGetOldPlayerName(player);
                    if (!string.IsNullOrWhiteSpace(oldName))
                        playerName = oldName.Trim();
                }

                string oldFriendCode = SafeGetOldFriendCode(player);
                if (!string.IsNullOrWhiteSpace(oldFriendCode))
                    friendCode = oldFriendCode.Trim();

                lobbyCode = SafeGetOldGameCode();
                isHost = SafeGetOldIsHost();
                isPublic = !string.IsNullOrWhiteSpace(lobbyCode) && SafeGetOldIsPublic();
                isPrivate = !string.IsNullOrWhiteSpace(lobbyCode) && !isPublic;
                shareLobby = SafeGetOldShareLobby();

                players = SafeGetOldCurrentLobbyPlayerCount();
                maxPlayers = SafeGetOldMaxPlayers();
                if (maxPlayers <= 0)
                    maxPlayers = 15;

                gameMode = SafeGetOldCurrentLobbyMode();
                status = SafeGetOldCurrentStatus();
                region = SafeGetOldRegion();
                language = SafeGetOldLanguage();
                platform = SafeGetOldPlatform();
                hostName = SafeGetOldHostName();
                kc = SafeGetOldKillCooldown();
                impostorCount = SafeGetOldImpostorCount();

                if (string.IsNullOrWhiteSpace(friendCode))
                    friendCode = _friendCode;

                if (string.IsNullOrWhiteSpace(playerName))
                    playerName = _playerName;

                if (string.IsNullOrWhiteSpace(hostName))
                    hostName = playerName;

                if (!string.IsNullOrWhiteSpace(friendCode))
                    _friendCode = friendCode;

                if (!string.IsNullOrWhiteSpace(playerName))
                    _playerName = playerName;
            }
            catch
            {
                buildFailed = true;
            }

            if (buildFailed)
            {
                _statusRunning = false;
                yield break;
            }

            string playersText = players > 0 ? players + "/" + maxPlayers : "0/" + maxPlayers;

            string body = "{"
                // Nuovo formato server.
                + "\"FriendCode\":" + JsonString(friendCode) + ","
                + "\"PlayerName\":" + JsonString(playerName) + ","
                + "\"LobbyCode\":" + JsonString(lobbyCode) + ","
                + "\"HostName\":" + JsonString(hostName) + ","
                + "\"GameMode\":" + JsonString(gameMode) + ","
                + "\"Mode\":" + JsonString(gameMode) + ","
                + "\"Players\":" + players + ","
                + "\"MaxPlayers\":" + maxPlayers + ","
                + "\"PlayersText\":" + JsonString(playersText) + ","
                + "\"KC\":" + kc + ","
                + "\"KillCooldown\":" + kc + ","
                + "\"ImpostorCount\":" + impostorCount + ","
                + "\"Region\":" + JsonString(region) + ","
                + "\"Language\":" + JsonString(language) + ","
                + "\"Platform\":" + JsonString(platform) + ","
                + "\"Status\":" + JsonString(status) + ","
                + "\"IsOnline\":" + BoolJson(isOnline) + ","
                + "\"ShareLobby\":" + BoolJson(shareLobby) + ","
                + "\"share_lobby\":" + BoolJson(shareLobby) + ","
                + "\"IsHost\":" + BoolJson(isHost) + ","
                + "\"IsPublic\":" + BoolJson(isPublic) + ","
                + "\"IsPrivate\":" + BoolJson(isPrivate) + ","

                // Alias compatibili con vecchio API/PlayerAPISend.
                + "\"ModName\":" + JsonString("BanMod") + ","
                + "\"GameCode\":" + JsonString(lobbyCode) + ","
                + "\"PlayersInLobby\":" + players + ","
                + "\"KillCooldownOld\":" + kc + ","
                + "\"impostor_count\":" + impostorCount
                + "}";


            UnityWebRequest req = new UnityWebRequest(LobbyStatusUrl, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            _statusRunning = false;
        }

        private static string SafeGetOldFriendCode(PlayerControl player)
        {
            try
            {
                string value = BanModIdentity.GetFriendCode(player);
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            catch { }

            try
            {
                string value = BanModIdentity.GetFriendCode();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(_friendCode))
                    return _friendCode.Trim();
            }
            catch { }

            return "";
        }

        private static string SafeGetOldPlayerName(PlayerControl player)
        {
            try
            {
                string realName = ExtendedPlayerControl.GetRealName(player);
                if (!string.IsNullOrWhiteSpace(realName))
                    return realName.Trim();
            }
            catch { }

            try
            {
                string identityName = BanModIdentity.GetPlayerName(player);
                if (!string.IsNullOrWhiteSpace(identityName))
                    return identityName.Trim();
            }
            catch { }

            try
            {
                if (player != null && player.Data != null && !string.IsNullOrWhiteSpace(player.Data.PlayerName))
                    return player.Data.PlayerName.Trim();
            }
            catch { }

            return "Unknown";
        }

        private static string SafeGetOldGameCode()
        {
            try
            {
                if (AmongUsClient.Instance != null)
                    return GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            }
            catch { }

            return "";
        }

        private static bool SafeGetOldIsPublic()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return false;

                if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    return false;

                return AmongUsClient.Instance.IsGamePublic;
            }
            catch
            {
                return false;
            }
        }

        private static bool SafeGetOldIsHost()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; }
            catch { return false; }
        }

        private static bool SafeGetOldShareLobby()
        {
            try
            {
                Type t = typeof(BanMod);

                PropertyInfo p = t.GetProperty("sharelobby", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    object value = p.GetValue(null, null);
                    if (value is bool b)
                        return b;
                }

                FieldInfo f = t.GetField("sharelobby", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (f != null)
                {
                    object value = f.GetValue(null);
                    if (value is bool b)
                        return b;
                }
            }
            catch { }

            return true;
        }

        private static int SafeGetOldCurrentLobbyPlayerCount()
        {
            try { return Utils.GetCurrentLobbyPlayerCount(); }
            catch { }

            try { return SafeGetPlayerCount(); }
            catch { return 0; }
        }

        private static string SafeGetOldCurrentLobbyMode()
        {
            try { return Utils.GetCurrentLobbyMode(); }
            catch { }

            try { return SafeGetGameMode(); }
            catch { return "Unknown"; }
        }

        private static string SafeGetOldCurrentStatus()
        {
            try { return Utils.GetCurrentStatus(); }
            catch { return "Unknown"; }
        }

        private static string SafeGetOldPlatform()
        {
            try { return BanModIdentity.GetPlatform(); }
            catch { return "Unknown"; }
        }

        private static string SafeGetOldRegion()
        {
            try { return Utils.GetRegionName(); }
            catch { return "Unknown"; }
        }

        private static string SafeGetOldLanguage()
        {
            try { return Utils.LanguageUtils.GetLanguageName(Utils.LanguageUtils.GetCurrentGameOptions()); }
            catch { return "Unknown"; }
        }

        private static string SafeGetOldHostName()
        {
            try
            {
                if (AmongUsClient.Instance != null)
                {
                    int hostId = ObjectToInt(ReadInstanceObject(AmongUsClient.Instance, "HostId"));
                    ClientData hostClient = GetClientDataById(hostId) ?? FindClientDataById(hostId);
                    if (hostClient != null && !string.IsNullOrWhiteSpace(hostClient.PlayerName))
                        return hostClient.PlayerName.Trim();
                }
            }
            catch { }

            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
                    return SafeGetOldPlayerName(PlayerControl.LocalPlayer);
            }
            catch { }

            return "";
        }

        private static int SafeGetOldKillCooldown()
        {
            try
            {
                object logicOptions = GameManager.Instance != null ? ReadInstanceObject(GameManager.Instance, "LogicOptions") : null;
                if (logicOptions != null)
                {
                    MethodInfo m = logicOptions.GetType().GetMethod("GetKillCooldown", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (m != null)
                        return ObjectToInt(m.Invoke(logicOptions, null));
                }
            }
            catch { }

            try { return SafeGetKillCooldown(); }
            catch { return 0; }
        }

        private static int SafeGetOldImpostorCount()
        {
            try
            {
                object logicOptions = GameManager.Instance != null ? ReadInstanceObject(GameManager.Instance, "LogicOptions") : null;
                if (logicOptions != null)
                {
                    object value = ReadInstanceObject(logicOptions, "NumImpostors") ??
                                   ReadInstanceObject(logicOptions, "numImpostors") ??
                                   ReadInstanceObject(logicOptions, "ImpostorCount");
                    return ObjectToInt(value);
                }
            }
            catch { }

            return 0;
        }

        private static int SafeGetOldMaxPlayers()
        {
            try { return SafeGetMaxPlayers(); }
            catch { return 15; }
        }


        private static IEnumerator FetchModdedLobbies()
        {
            _lobbyListRunning = true;

            UnityWebRequest req = UnityWebRequest.Get(ActiveLobbiesUrl);
            req.timeout = RequestTimeoutSeconds;
            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

            string text = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
            {
                try
                {
                    ActiveLobbiesResponse response = JsonSerializer.Deserialize<ActiveLobbiesResponse>(text, JsonOptions);
                    if (response != null && response.success && response.lobbies != null)
                    {
                        List<ModdedLobbyInfo> visible = new List<ModdedLobbyInfo>();
                        for (int i = 0; i < response.lobbies.Count; i++)
                        {
                            ModdedLobbyInfo lobby = response.lobbies[i];
                            // Tutte le lobby pubbliche devono essere visibili nella mod.
                            // ShareLobby controlla solo l'esposizione del codice sulla vetrina web.
                            if (lobby == null || !lobby.is_public || lobby.is_private)
                                continue;
                            visible.Add(lobby);
                        }
                        LastModdedLobbies = visible;
                    }
                }
                catch { }
            }

            _lobbyListRunning = false;
        }

        private static IEnumerator DownloadBytes(string url, Action<byte[]> callback)
        {
            byte[] result = null;
            Exception error = null;
            bool done = false;

            try
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);

                            using (HttpResponseMessage response = await client.GetAsync(url))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    error = new Exception("HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase);
                                    return;
                                }

                                result = await response.Content.ReadAsByteArrayAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        System.Threading.Volatile.Write(ref done, true);
                    }
                });
            }
            catch
            {
                callback(null);
                yield break;
            }

            while (!System.Threading.Volatile.Read(ref done))
                yield return null;

            if (error != null)
            {
                callback(null);
                yield break;
            }

            if (result == null || result.Length == 0)
            {
                callback(null);
                yield break;
            }

            callback(result);
        }

        private static LoadedPremiumModule LoadManagedBinFromMemory(string serviceKey, byte[] binBytes)
        {
            Assembly assembly = Assembly.Load(binBytes);
            Type basePluginType = typeof(BepInEx.Unity.IL2CPP.BasePlugin);

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                List<Type> validTypes = new List<Type>();
                if (ex.Types != null)
                {
                    for (int i = 0; i < ex.Types.Length; i++)
                    {
                        if (ex.Types[i] != null)
                            validTypes.Add(ex.Types[i]);
                    }
                }
                assemblyTypes = validTypes.ToArray();
            }

            List<object> pluginInstances = new List<object>();

            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                Type pluginType = assemblyTypes[i];

                if (pluginType == null || pluginType.IsAbstract || !basePluginType.IsAssignableFrom(pluginType))
                    continue;

                object pluginInstance = Activator.CreateInstance(pluginType, true);
                if (pluginInstance == null)
                    throw new InvalidOperationException("Impossibile creare il plugin BepInEx: " + pluginType.FullName);

                MethodInfo loadMethod = pluginType.GetMethod(
                    "Load",
                    BindingFlags.Public | BindingFlags.Instance);

                if (loadMethod == null || loadMethod.GetParameters().Length != 0)
                    throw new MissingMethodException("Load() non trovato nel plugin BepInEx: " + pluginType.FullName);

                loadMethod.Invoke(pluginInstance, null);
                pluginInstances.Add(pluginInstance);

            }

            if (pluginInstances.Count == 0)
            {
                throw new MissingMethodException(
                    "Nessuna classe derivata da BepInEx.Unity.IL2CPP.BasePlugin trovata nel .bin.");
            }

            return new LoadedPremiumModule
            {
                ServiceKey = serviceKey,
                Assembly = assembly,
                PluginInstances = pluginInstances
            };
        }

        public static void StopAllPremiumModules()
        {
            string[] keys = new string[LoadedPremiumModules.Keys.Count];
            LoadedPremiumModules.Keys.CopyTo(keys, 0);

            foreach (string key in keys)
            {
                if (!LoadedPremiumModules.TryGetValue(key, out LoadedPremiumModule module))
                    continue;

                if (module.PluginInstances != null)
                {
                    for (int i = module.PluginInstances.Count - 1; i >= 0; i--)
                    {
                        object pluginInstance = module.PluginInstances[i];
                        if (pluginInstance == null)
                            continue;

                        try
                        {
                            MethodInfo unloadMethod = pluginInstance.GetType().GetMethod(
                                "Unload",
                                BindingFlags.Public | BindingFlags.Instance);

                            unloadMethod?.Invoke(pluginInstance, null);
                        }
                        catch { }
                    }

                    module.PluginInstances.Clear();
                }

                try { module.Assembly = null; } catch { }

                LoadedPremiumModules.Remove(key);
            }

        }

        private static GameIdentity ReadIdentity()
        {
            GameIdentity identity = new GameIdentity();

            // Fonte primaria corretta:
            // FriendsListManager.CheckFriendCodeOnLogin salva il codice in EOSManager.Instance.FriendCode.
            try
            {
                if (DestroyableSingleton<EOSManager>.InstanceExists)
                {
                    string eosFriendCode = Norm(DestroyableSingleton<EOSManager>.Instance.FriendCode);

                    if (!string.IsNullOrWhiteSpace(eosFriendCode))
                    {
                        identity.FriendCode = eosFriendCode;
                        identity.PlayerName = ExtractNameFromFriendCode(eosFriendCode);
                        return identity;
                    }
                }
            }
            catch { }

            // 1) Prima fonte: cache catturata dal constructor di InnerNet.ClientData.
            // Questo avviene prima dello startup generale della mod.
            if (!string.IsNullOrWhiteSpace(_capturedFriendCode))
            {
                identity.FriendCode = _capturedFriendCode;
                identity.PlayerName = string.IsNullOrWhiteSpace(_capturedPlayerName) ? "Unknown" : _capturedPlayerName;
                return identity;
            }

            // 2) Seconda fonte: ClientData locale già esistente.
            try
            {
                ClientData localClient = GetLocalClientData();

                if (localClient != null)
                {
                    identity.FriendCode = Norm(localClient.FriendCode);
                    identity.PlayerName = Norm(localClient.PlayerName);

                    if (!string.IsNullOrWhiteSpace(identity.FriendCode))
                    {
                        CaptureClientData(localClient);
                        return identity;
                    }
                }
            }
            catch { }

            // 3) Fallback vecchio: PlayerControl.LocalPlayer.Data.
            try
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
                {
                    identity.FriendCode = Norm(PlayerControl.LocalPlayer.Data.FriendCode);
                    identity.PlayerName = Norm(PlayerControl.LocalPlayer.Data.PlayerName);

                    if (!string.IsNullOrWhiteSpace(identity.FriendCode))
                        return identity;
                }
            }
            catch { }

            return identity;
        }

        private static ClientData GetLocalClientData()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return null;

                int localClientId = ObjectToInt(ReadInstanceObject(AmongUsClient.Instance, "ClientId"));

                ClientData direct = GetClientDataById(localClientId);

                if (direct != null)
                    return direct;

                return FindClientDataById(localClientId);
            }
            catch
            {
                return null;
            }
        }

        private static ClientData GetClientDataById(int clientId)
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return null;

                MethodInfo method = AmongUsClient.Instance.GetType().GetMethod(
                    "GetClient",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (method == null)
                    return null;

                object value = method.Invoke(AmongUsClient.Instance, new object[] { clientId });
                return value as ClientData;
            }
            catch
            {
                return null;
            }
        }

        private static ClientData FindClientDataById(int clientId)
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return null;

                object clients =
                    ReadInstanceObject(AmongUsClient.Instance, "allClients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "AllClients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "clients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "Clients");

                if (clients == null)
                    return null;

                foreach (object item in EnumerateObjects(clients))
                {
                    ClientData client = item as ClientData;

                    if (client == null)
                        continue;

                    if (client.Id == clientId)
                        return client;
                }
            }
            catch { }

            return null;
        }

        private static IEnumerable<object> EnumerateObjects(object collection)
        {
            if (collection == null)
                yield break;

            IEnumerable enumerable = collection as IEnumerable;

            if (enumerable == null)
                yield break;

            foreach (object item in enumerable)
                yield return item;
        }

        private static bool IsInLobbyOrGame()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return false;

                string state = AmongUsClient.Instance.GameState.ToString();

                return state.IndexOf("Joined", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       state.IndexOf("Started", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeGetLobbyCode()
        {
            try
            {
                object gameId = ReadInstanceObject(AmongUsClient.Instance, "GameId");
                if (gameId == null)
                    return "";

                Type t = FindType("GameCode");
                MethodInfo m = t != null ? t.GetMethod("IntToGameName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;

                if (m != null)
                {
                    object value = m.Invoke(null, new object[] { gameId });
                    return value as string ?? value?.ToString() ?? "";
                }

                return gameId.ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string SafeGetGameMode()
        {
            try
            {
                Type t = FindType("GameOptionsManager");
                object instance = t != null ? ReadStaticObject(t, "Instance") : null;
                object options = ReadInstanceObject(instance, "CurrentGameOptions") ?? ReadInstanceObject(instance, "GameOptions");
                object mode = ReadInstanceObject(options, "GameMode") ?? ReadInstanceObject(instance, "GameMode");
                return mode != null ? mode.ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        private static int SafeGetKillCooldown()
        {
            object value = FindGameOptionValue("KillCooldown");
            return ObjectToInt(value);
        }

        private static int SafeGetMaxPlayers()
        {
            object value =
                FindGameOptionValue("MaxPlayers") ??
                FindGameOptionValue("maxPlayers") ??
                FindGameOptionValue("PlayerLimit") ??
                FindGameOptionValue("NumPlayers");

            int parsed = ObjectToInt(value);

            if (parsed <= 0)
                parsed = 15;

            return parsed;
        }

        private static object FindGameOptionValue(string contains)
        {
            try
            {
                Type t = FindType("GameOptionsManager");
                object instance = t != null ? ReadStaticObject(t, "Instance") : null;
                object options = ReadInstanceObject(instance, "CurrentGameOptions") ?? ReadInstanceObject(instance, "GameOptions");
                return FindNestedValue(options, contains, 3) ?? FindNestedValue(instance, contains, 3);
            }
            catch
            {
                return null;
            }
        }

        private static string SafeGetHostName()
        {
            // Prima fonte: ClientData dell'host.
            try
            {
                if (AmongUsClient.Instance != null)
                {
                    int hostId = ObjectToInt(ReadInstanceObject(AmongUsClient.Instance, "HostId"));

                    ClientData hostClient = GetClientDataById(hostId) ?? FindClientDataById(hostId);

                    if (hostClient != null && !string.IsNullOrWhiteSpace(hostClient.PlayerName))
                        return hostClient.PlayerName.Trim();
                }
            }
            catch { }

            // Fallback: PlayerControl.
            try
            {
                if (PlayerControl.AllPlayerControls == null)
                    return "";

                object hostId = ReadInstanceObject(AmongUsClient.Instance, "HostId");

                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p.Data == null)
                        continue;

                    object ownerId = ReadInstanceObject(p.Data, "OwnerId");

                    if (ownerId != null && hostId != null && ownerId.ToString() == hostId.ToString())
                        return p.Data.PlayerName;
                }
            }
            catch { }

            return "";
        }

        private static int SafeGetPlayerCount()
        {
            try
            {
                int count = 0;

                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl _ in PlayerControl.AllPlayerControls)
                        count++;

                    if (count > 0)
                        return count;
                }
            }
            catch { }

            try
            {
                object clients =
                    ReadInstanceObject(AmongUsClient.Instance, "allClients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "AllClients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "clients") ??
                    ReadInstanceObject(AmongUsClient.Instance, "Clients");

                int count = 0;

                foreach (object item in EnumerateObjects(clients))
                {
                    ClientData client = item as ClientData;

                    if (client != null)
                        count++;
                }

                return count;
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsKnownBaseAssembly(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            return name.Equals("BanMod", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("BanMod.Login", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Il2Cpp", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Mono", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildExtraModsJson(string reason, bool scanSuccess, string scanError, List<DetectedModInfo> mods)
        {
            if (mods == null)
                mods = new List<DetectedModInfo>();

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            // Formato nuovo usato dal server attuale.
            sb.Append("\"FriendCode\":").Append(JsonString(_friendCode)).Append(",");
            sb.Append("\"PlayerName\":").Append(JsonString(_playerName)).Append(",");
            sb.Append("\"Reason\":").Append(JsonString(reason)).Append(",");
            sb.Append("\"ScanSuccess\":").Append(BoolJson(scanSuccess)).Append(",");
            sb.Append("\"ScanError\":").Append(JsonString(scanError)).Append(",");

            // Formato compatibile con BanModExtraModsReporter.cs che mi hai mandato.
            sb.Append("\"mod_name\":").Append(JsonString("BanMod")).Append(",");
            sb.Append("\"player_name\":").Append(JsonString(_playerName)).Append(",");
            sb.Append("\"friend_code\":").Append(JsonString(_friendCode)).Append(",");
            sb.Append("\"platform\":").Append(JsonString(Application.platform.ToString())).Append(",");
            sb.Append("\"game_code\":").Append(JsonString(SafeGetLobbyCode())).Append(",");
            sb.Append("\"scan_success\":").Append(BoolJson(scanSuccess)).Append(",");

            sb.Append("\"extra_mods\":[");
            for (int i = 0; i < mods.Count; i++)
            {
                if (i > 0) sb.Append(",");

                DetectedModInfo m = mods[i];
                string name = m != null ? Norm(m.Name) : "";

                if (string.IsNullOrWhiteSpace(name) && m != null)
                    name = Norm(m.AssemblyName);

                if (string.IsNullOrWhiteSpace(name) && m != null)
                    name = Norm(m.FileName);

                sb.Append(JsonString(name));
            }
            sb.Append("],");

            sb.Append("\"Mods\":[");
            for (int i = 0; i < mods.Count; i++)
            {
                if (i > 0) sb.Append(",");
                DetectedModInfo m = mods[i];
                sb.Append("{");
                sb.Append("\"Name\":").Append(JsonString(m.Name)).Append(",");
                sb.Append("\"FileName\":").Append(JsonString(m.FileName)).Append(",");
                sb.Append("\"AssemblyName\":").Append(JsonString(m.AssemblyName)).Append(",");
                sb.Append("\"Version\":").Append(JsonString(m.Version)).Append(",");
                sb.Append("\"Sha256\":").Append(JsonString(m.Sha256));
                sb.Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static Type FindType(string name)
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in SafeGetTypes(asm))
                    {
                        if (t != null && t.Name == name)
                            return t;
                    }
                }
            }
            catch { }
            return null;
        }

        private static Type[] SafeGetTypes(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch { return Array.Empty<Type>(); }
        }

        private static object ReadStaticObject(Type t, string name)
        {
            try
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null) return f.GetValue(null);

                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (p != null && p.CanRead) return p.GetValue(null, null);
            }
            catch { }

            return null;
        }

        private static object ReadInstanceObject(object obj, string name)
        {
            if (obj == null)
                return null;

            try
            {
                Type t = obj.GetType();

                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(obj);

                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.CanRead) return p.GetValue(obj, null);
            }
            catch { }

            return null;
        }

        private static object FindNestedValue(object obj, string contains, int depth)
        {
            if (obj == null || depth <= 0)
                return null;

            try
            {
                Type t = obj.GetType();

                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (f.Name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                        return f.GetValue(obj);
                }

                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (p.CanRead && p.Name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                        return p.GetValue(obj, null);
                }

                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    object child = null;
                    try { child = f.GetValue(obj); } catch { }
                    object found = FindNestedValue(child, contains, depth - 1);
                    if (found != null) return found;
                }
            }
            catch { }

            return null;
        }

        private static int ObjectToInt(object value)
        {
            if (value == null)
                return 0;

            if (value is int i) return i;
            if (value is float f) return Mathf.RoundToInt(f);
            if (value is double d) return (int)Math.Round(d);

            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }

        private static string SafeGetOwnBanModSha256()
        {
            try
            {
                Assembly assembly = typeof(BanMod).Assembly;
                if (assembly != null && !string.IsNullOrWhiteSpace(assembly.Location) && File.Exists(assembly.Location))
                    return SafeSha256File(assembly.Location);
            }
            catch { }

            try
            {
                string fallback = Path.Combine(Paths.PluginPath, "BanMod.dll");
                if (File.Exists(fallback))
                    return SafeSha256File(fallback);
            }
            catch { }

            return "";
        }

        private static string SafeSha256File(string path)
        {
            try
            {
                using SHA256 sha = SHA256.Create();
                using FileStream fs = File.OpenRead(path);
                byte[] hash = sha.ComputeHash(fs);
                StringBuilder sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
            catch { return ""; }
        }

        private static string SafeAssemblyFileName(Assembly asm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(asm.Location))
                    return "";
                return Path.GetFileName(asm.Location);
            }
            catch { return ""; }
        }

        private static string SafeAssemblySha(Assembly asm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(asm.Location) || !File.Exists(asm.Location))
                    return "";
                return SafeSha256File(asm.Location);
            }
            catch { return ""; }
        }

        private static string Norm(string v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim();

        private static string JsonString(string value)
        {
            if (value == null) value = "";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
        }

        private static string BoolJson(bool value) => value ? "true" : "false";

        private static string Sha256Hex(byte[] data)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }




        private sealed class GameIdentity { public string FriendCode; public string PlayerName; }
        public sealed class DetectedModInfo { public string Name; public string FileName; public string AssemblyName; public string Version; public string Sha256; }
        public sealed class ChallengeResponse { public bool success { get; set; } public string nonce { get; set; } public int expires_in_seconds { get; set; } }
        public sealed class ActivationResponse { public bool success { get; set; } public string activation_token { get; set; } public int expires_in_seconds { get; set; } }
        public sealed class ExtraReportResponse { public bool success { get; set; } public bool valid { get; set; } public string reason { get; set; } }
        public sealed class AccessResponse
        {
            public bool success { get; set; }
            public bool premium_gate_ok { get; set; }
            public bool can_use_mod { get; set; }
            public bool blocked { get; set; }
            public bool force_disable_mod { get; set; }
            public bool force_disable { get; set; }
            public string force_disable_reason { get; set; }
            public string reason { get; set; }
            public int premium_count { get; set; }
            public List<PremiumService> services { get; set; }
        }
        public sealed class PremiumService
        {
            public string key { get; set; }
            public string service_key { get; set; }
            public string serviceKey { get; set; }
            public string feature_key { get; set; }

            public string label { get; set; }
            public string version { get; set; }

            public bool enabled { get; set; }
            public bool allowed { get; set; }
            public bool autoload { get; set; }
            public bool auto_load { get; set; }
            public bool selected { get; set; }
            public bool server_allowed { get; set; }
            public bool integrable { get; set; }

            public bool has_payload { get; set; }
            public bool hasPayload { get; set; }
            public bool has_bin { get; set; }
            public bool hasBin { get; set; }
            public bool has_file { get; set; }
            public bool hasFile { get; set; }

            public string bin { get; set; }
            public string payload { get; set; }
            [JsonPropertyName("file")]
            public string json_file { get; set; }
            public string bin_file { get; set; }
            public string bin_filename { get; set; }
            public string filename { get; set; }
            public string file_name { get; set; }

            public string GetKey()
            {
                if (!string.IsNullOrWhiteSpace(key))
                    return key.Trim();

                if (!string.IsNullOrWhiteSpace(service_key))
                    return service_key.Trim();

                if (!string.IsNullOrWhiteSpace(serviceKey))
                    return serviceKey.Trim();

                if (!string.IsNullOrWhiteSpace(feature_key))
                    return feature_key.Trim();

                return "";
            }

            public bool GetAutoload()
            {
                return autoload || auto_load;
            }

            public string GetBinName()
            {
                if (!string.IsNullOrWhiteSpace(bin)) return bin.Trim();
                if (!string.IsNullOrWhiteSpace(payload)) return payload.Trim();
                if (!string.IsNullOrWhiteSpace(json_file)) return json_file.Trim();
                if (!string.IsNullOrWhiteSpace(bin_file)) return bin_file.Trim();
                if (!string.IsNullOrWhiteSpace(bin_filename)) return bin_filename.Trim();
                if (!string.IsNullOrWhiteSpace(filename)) return filename.Trim();
                if (!string.IsNullOrWhiteSpace(file_name)) return file_name.Trim();
                return "";
            }
        }
        public sealed class LoginManifest { public bool success { get; set; } public bool can_download { get; set; } public string version { get; set; } public string sha256 { get; set; } public int size_bytes { get; set; } public string download_url { get; set; } public string load_mode { get; set; } }
        public sealed class PremiumManifest { public bool success { get; set; } public bool can_download { get; set; } public string service_key { get; set; } public string feature_key { get; set; } public string version { get; set; } public string sha256 { get; set; } public int size_bytes { get; set; } public string download_url { get; set; } public string load_mode { get; set; } }
        public sealed class ActiveLobbiesResponse { public bool success { get; set; } public List<ModdedLobbyInfo> lobbies { get; set; } public int server_time { get; set; } }
        public sealed class ModdedLobbyInfo { public string lobby_code { get; set; } public string player_name { get; set; } public string friend_code { get; set; } public string host_friend_code { get; set; } public string host_name { get; set; } public string game_mode { get; set; } public int players { get; set; } public int kill_cooldown { get; set; } public int last_seen { get; set; } public bool is_public { get; set; } public bool is_private { get; set; } public bool share_lobby { get; set; } }
        private sealed class LoadedPremiumModule { public string ServiceKey; public Assembly Assembly; public List<object> PluginInstances; }
    }





    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }






    [HarmonyPatch(typeof(ClientData), nameof(ClientData.UpdatePlayerName))]
    public static class BanModClientDataNameOnlyPatch
    {
        public static void Postfix(ClientData __instance)
        {
            try
            {
                BanModCore.CaptureClientName(__instance);
            }
            catch { }
        }
    }


    [HarmonyPatch(typeof(FriendsListManager), nameof(FriendsListManager.CheckFriendCodeOnLogin))]
    public static class BanModFriendsListManagerCheckFriendCodePatch
    {
        public static void Postfix()
        {
            try
            {
                BanModCore.TryCaptureEosFriendCode("FriendsListManager.CheckFriendCodeOnLogin");
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(AccountManager), nameof(AccountManager.UpdateVisuals))]
    public static class BanModAccountManagerUpdateVisualsFriendCodePatch
    {
        public static void Postfix()
        {
            try
            {
                BanModCore.TryCaptureEosFriendCode("AccountManager.UpdateVisuals");
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    public static class BanModCoreStartupPatch
    {
        public static void Postfix()
        {
            try { BanModCore.TryStart(); } catch { }
        }
    }
}
