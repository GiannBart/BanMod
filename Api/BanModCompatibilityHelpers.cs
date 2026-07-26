
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using InnerNet;
using UnityEngine;
using UnityEngine.Networking;

namespace BanMod
{
    public static class BanModApiConfig
    {
        public const string ApiBaseUrl = BanModCore.PublicApiBaseUrl;
        public const string TokenRequestUrl = ApiBaseUrl + "/api/token/request";
        public const string PlayerStatusUrl = ApiBaseUrl + "/api/lobby/status";
        public const string LobbyStatusUrl = ApiBaseUrl + "/api/lobby/status";
        public const string LobbyUrl = ApiBaseUrl + "/api/lobbies/active";
        public const string PublicLobbyUrl = ApiBaseUrl + "/api/lobbies/active";
        public const string InternalLobbyUrl = ApiBaseUrl + "/api/lobbies/active";
        public const string CheatersUrl = ApiBaseUrl + "/api/cheaters/list";
        public const string TeamersUrl = ApiBaseUrl + "/api/teamers/list";
        public const string CheatersLegacyUrl = ApiBaseUrl + "/api/cheaters";
        public const string TeamersLegacyUrl = ApiBaseUrl + "/api/teamers";
        public const string CheatersAddUrl = ApiBaseUrl + "/api/cheaters/add";
        public const string TeamersAddUrl = ApiBaseUrl + "/api/teamers/add";
        public const string UpdateInfoUrl = ApiBaseUrl + "/api/update_info";
        public const string ModAccessUrl = ApiBaseUrl + "/api/access";
        public const string ExtraModsReportUrl = ApiBaseUrl + "/api/mod/extra-mods/report";
        public const string ModName = "BanMod";
        public const string FriendCodeOverride = "";
        public const float PlayerStatusIntervalSeconds = 30f;
        public const float AccessCheckIntervalSeconds = 30f;
    }

    public static class BanModApiTokenManager
    {
        public static string Token = "";
        public static readonly string ModId = "BanMod-" + BanMod.PluginVersion;
        public static bool LastTokenRequestWasBlocked { get; private set; }
        public static string LastTokenBlockReason { get; private set; } = "";

        public static void ClearLastTokenBlockState()
        {
            LastTokenRequestWasBlocked = false;
            LastTokenBlockReason = "";
        }

        public static IEnumerator EnsureTokenCoroutine(Action<bool, string> callback)
        {
            ClearLastTokenBlockState();
            string current = BanModCore.GetCurrentActivationToken();
            if (!string.IsNullOrWhiteSpace(current))
            {
                Token = current;
                callback?.Invoke(true, Token);
                yield break;
            }

            bool ok = false;
            string token = "";
            yield return BanModCore.EnsureActivationTokenForApi((success, value) => { ok = success; token = value ?? ""; });
            if (ok && !string.IsNullOrWhiteSpace(token))
                Token = token;
            else
            {
                LastTokenRequestWasBlocked = true;
                LastTokenBlockReason = "Activation token non disponibile.";
            }

            callback?.Invoke(ok && !string.IsNullOrWhiteSpace(Token), Token);
        }

        public static void ApplyAuthHeader(UnityWebRequest request)
        {
            if (request == null)
                return;

            if (string.IsNullOrWhiteSpace(Token))
                Token = BanModCore.GetCurrentActivationToken();

            if (!string.IsNullOrWhiteSpace(Token))
                request.SetRequestHeader("Authorization", "Bearer " + Token);

            request.SetRequestHeader("X-BANMOD-ModId", ModId);
            request.SetRequestHeader("X-BANMOD-FriendCode", BanModIdentity.GetFriendCode());
            request.SetRequestHeader("X-BANMOD-PlayerName", BanModIdentity.GetPlayerName());
            request.SetRequestHeader("X-BANMOD-Platform", BanModIdentity.GetPlatform());
        }

        public static void ClearToken()
        {
            Token = "";
            BanModCore.ClearCurrentActivationToken();
        }

        public static string ExtractJsonString(string json, string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key))
                return fallback;
            try
            {
                string search = "\"" + key + "\"";
                int keyIndex = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) return fallback;
                int colonIndex = json.IndexOf(':', keyIndex);
                if (colonIndex < 0) return fallback;
                int firstQuote = json.IndexOf('"', colonIndex + 1);
                if (firstQuote < 0) return fallback;
                int secondQuote = firstQuote + 1;
                bool escaped = false;
                while (secondQuote < json.Length)
                {
                    char c = json[secondQuote];
                    if (c == '\\' && !escaped) { escaped = true; secondQuote++; continue; }
                    if (c == '"' && !escaped) break;
                    escaped = false;
                    secondQuote++;
                }
                if (secondQuote >= json.Length) return fallback;
                return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1)
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\\", "\\");
            }
            catch { return fallback; }
        }
    }

    public static class BanModIdentity
    {
        public static string GetFriendCode()
        {
            string overrideCode = BanModApiConfig.FriendCodeOverride;
            if (!string.IsNullOrWhiteSpace(overrideCode)) return overrideCode.Trim();
            string fc = BanModCore.GetCurrentFriendCode();
            if (!string.IsNullOrWhiteSpace(fc)) return fc;
            try { return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null ? NormalizeFriendCode(PlayerControl.LocalPlayer.Data.FriendCode) : ""; } catch { return ""; }
        }

        public static string GetFriendCode(PlayerControl player)
        {
            try { return player != null && player.Data != null ? NormalizeFriendCode(player.Data.FriendCode) : ""; } catch { return ""; }
        }

        public static string GetFriendCode(ClientData player)
        {
            try { return player != null ? NormalizeFriendCode(player.FriendCode) : ""; } catch { return ""; }
        }

        public static string GetPlayerName()
        {
            string n = BanModCore.GetCurrentPlayerName();
            if (!string.IsNullOrWhiteSpace(n)) return n;
            try { return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null ? (PlayerControl.LocalPlayer.Data.PlayerName ?? "").Trim() : ""; } catch { return ""; }
        }

        public static string GetPlayerName(PlayerControl player)
        {
            try { return player != null && player.Data != null ? (player.Data.PlayerName ?? "").Trim() : ""; } catch { return ""; }
        }

        public static string GetPlayerName(ClientData player)
        {
            try { return player != null ? (player.PlayerName ?? "").Trim() : ""; } catch { return ""; }
        }

        public static string GetPlatform()
        {
            try { return Application.platform.ToString(); } catch { return "Unknown"; }
        }

        public static string GetPlatform(ClientData player)
        {
            try { return player != null && player.PlatformData != null ? player.PlatformData.Platform.ToString() : GetPlatform(); }
            catch { return GetPlatform(); }
        }

        private static string NormalizeFriendCode(string fc)
        {
            return string.IsNullOrWhiteSpace(fc) ? "" : fc.Trim();
        }
    }

    public static class BanModJson
    {
        public static string Escape(string value)
        {
            if (value == null) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        public static string StringValue(string value)
        {
            return "\"" + Escape(value ?? "") + "\"";
        }

        public static string BoolValue(bool value)
        {
            return value ? "true" : "false";
        }
    }

    public sealed class BanModDeniedPlayer
    {
        public string friendCode { get; set; }
        public string hashedPuid { get; set; }
        public string accountId { get; set; }
        public string playerName { get; set; }
        public string name { get; set; }
        public string platform { get; set; }
        public string hackUsed { get; set; }
        public string Reason { get; set; }
    }

    internal sealed class BanModCheatersResponse
    {
        public bool success { get; set; }
        public List<BanModDeniedPlayer> cheaters { get; set; }
    }

    internal sealed class BanModTeamersResponse
    {
        public bool success { get; set; }
        public List<BanModDeniedPlayer> teamers { get; set; }
    }

    public static class BanModDeniedLists
    {
        // Adapter di compatibilità: la gestione reale dei file è affidata
        // esclusivamente a CheaterManager e TeamerManager, che usano:
        // ./BAN_DATA/DENIED/Cheater.txt
        // ./BAN_DATA/DENIED/Teamer.txt
        // Non vengono cercati o creati altri file e non esiste polling periodico.

        public static List<BanModDeniedPlayer> Cheaters { get; private set; } =
            new List<BanModDeniedPlayer>();

        public static List<BanModDeniedPlayer> Teamers { get; private set; } =
            new List<BanModDeniedPlayer>();

        public static int LastRefreshUnix { get; private set; }
        public static string LastError { get; private set; } = "";

        private static bool refreshStarted;

        public static IEnumerator RefreshAllCoroutine()
        {
            if (refreshStarted)
                yield break;

            refreshStarted = true;
            LastError = "";

            Task cheaterTask = null;
            Task teamerTask = null;

            try
            {
                cheaterTask = CheaterManager.SyncFromServerAsync();
                teamerTask = TeamerManager.SyncFromServerAsync();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                yield break;
            }

            while ((cheaterTask != null && !cheaterTask.IsCompleted) ||
                   (teamerTask != null && !teamerTask.IsCompleted))
            {
                yield return null;
            }

            try
            {
                ReplaceRows(Cheaters, CheaterManager.GetSnapshot());
                ReplaceRows(Teamers, TeamerManager.GetSnapshot());

                if ((cheaterTask != null && cheaterTask.IsFaulted) ||
                    (teamerTask != null && teamerTask.IsFaulted))
                {
                    LastError = "Errore durante la sincronizzazione iniziale delle liste.";
                }

                try { LastRefreshUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
                catch { LastRefreshUnix = 0; }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        public static void ReportCheaterDetected(
            string friendCode,
            string hashedPuid = "",
            string playerName = "",
            string platform = "",
            string reason = "")
        {
            CheaterManager.AddDetected(friendCode, hashedPuid, playerName, platform, reason);
        }

        public static void ReportTeamerDetected(
            string friendCode,
            string hashedPuid = "",
            string playerName = "",
            string platform = "",
            string reason = "")
        {
            TeamerManager.AddDetected(friendCode, hashedPuid, playerName, platform, reason);
        }

        public static bool IsCheater(string friendCode, string hashedPuid = "")
        {
            return CheaterManager.CheckList(friendCode, hashedPuid);
        }

        public static bool IsTeamer(string friendCode, string hashedPuid = "")
        {
            return TeamerManager.CheckList(friendCode, hashedPuid);
        }

        private static void ReplaceRows(
            List<BanModDeniedPlayer> destination,
            List<BanModDeniedPlayer> source)
        {
            destination.Clear();

            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                BanModDeniedPlayer row = source[i];
                if (row != null)
                    destination.Add(row);
            }
        }
    }

    public enum ModFeature
    {
        ForceRole,
        Translate
    }

    public static class ModAccessGuard
    {
        private static bool _refreshRunning = false;
        public static bool ServerChecked { get; private set; }
        public static bool IsModBlocked { get; private set; }
        public static string ModBlockReason { get; private set; } = "";
        public static bool ServerReachable { get; private set; }
        public static float LastValidServerDataTime { get; private set; }

        public static bool IsHost()
        {
            try { return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost; } catch { return false; }
        }

        public static bool CanUseMod()
        {
            return !IsModBlocked;
        }

        public static bool CanUse(ModFeature feature)
        {
            return CanUseFeatureCurrentStateFailClosedSilent(feature);
        }

        public static bool IsFeatureBlocked(ModFeature feature)
        {
            // Compat con vecchio codice: alcune feature leggevano "blocked" come flag premium attivo.
            // Qui ritorna true quando la feature è effettivamente presente nei flag premium.
            return IsFeaturePremiumLoaded(feature);
        }

        public static bool CanUseFeatureCurrentStateFailClosedSilent(ModFeature feature)
        {
            return IsFeaturePremiumLoaded(feature);
        }

        public static void StartRefreshIfPossible(bool force = false)
        {
            try
            {
                if (_refreshRunning || AmongUsClient.Instance == null) return;
                AmongUsClient.Instance.StartCoroutine(RefreshAccessWrapper(force).WrapToIl2Cpp());
            }
            catch { }
        }

        private static IEnumerator RefreshAccessWrapper(bool force)
        {
            _refreshRunning = true;
            yield return RefreshAccessFromServer(force);
            _refreshRunning = false;
        }

        public static IEnumerator RefreshAccessFromServer(bool force = false)
        {
            bool ok = false;
            string token = "";
            yield return BanModApiTokenManager.EnsureTokenCoroutine((success, value) => { ok = success; token = value ?? ""; });
            ServerChecked = true;
            ServerReachable = ok;
            if (ok) LastValidServerDataTime = Time.time;
            IsModBlocked = !ok;
            ModBlockReason = ok ? "" : "Premium non autorizzato dal server.";
        }

        public static IEnumerator RefreshFeatureDecisionCoroutine(ModFeature feature, Action<bool> callback)
        {
            yield return RefreshAccessFromServer(true);
            bool blocked = !IsFeaturePremiumLoaded(feature);
            callback?.Invoke(blocked);
        }

        public static void ApplyAccessJsonResponse(string responseText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseText)) return;
                string lower = responseText.ToLowerInvariant();
                if (lower.Contains("premium_gate_ok\":false") || lower.Contains("access_denied") || lower.Contains("blocked") || lower.Contains("extra_report_invalid"))
                {
                    IsModBlocked = true;
                    ModBlockReason = "Accesso premium negato dal server.";
                    BanModCore.StopAllPremiumModules();
                }
            }
            catch { }
        }

        private static bool IsFeaturePremiumLoaded(ModFeature feature)
        {
            try
            {
                switch (feature)
                {
                    case ModFeature.ForceRole:
                        // ForceRole/SetRole è parte della mod principale.
                        // Il limite e l'eventuale sblocco permanente sono gestiti dal server,
                        // non da un payload premium .bin.
                        return true;
                    case ModFeature.Translate:
                        return OptionalPluginAvailability.Translate;
                    default:
                        return false;
                }
            }
            catch { return false; }
        }
    }

    public static class ModUpdater
    {
        private const int UpdateTimeoutSeconds = 60;
        private const string UpdateLatestUrl = BanModApiConfig.ApiBaseUrl + "/api/update/latest";

        public static bool hasUpdate = false;
        public static bool hasOptionalUpdate = false;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static bool isMandatory = false;
        public static bool isInstalling = false;
        public static Version latestVersion = null;
        public static string latestTitle = "";
        public static string downloadUrl = "";
        public static string latestSha256 = "";
        public static string lastError = "";

        public sealed class StartupUpdateResult
        {
            public bool Checked { get; set; }
            public bool UpdateAvailable { get; set; }
            public bool Mandatory { get; set; }
            public bool UpdateStarted { get; set; }
            public bool BlockStartup { get; set; }
            public string Error { get; set; } = "";
        }

        private sealed class UpdateLatestResponse
        {
            public bool success { get; set; }
            public bool update_available { get; set; }
            public string latest_version { get; set; }
            public string release_version { get; set; }
            public bool mandatory { get; set; }
            public string sha256 { get; set; }
            public long size_bytes { get; set; }
            public string release_notes { get; set; }
            public string download_url { get; set; }
            public string reason { get; set; }
        }

        public static IEnumerator CheckAtStartupCoroutine(Action<StartupUpdateResult> callback)
        {
            StartupUpdateResult result = new StartupUpdateResult();

            hasUpdate = false;
            hasOptionalUpdate = false;
            isBroken = false;
            isChecked = false;
            isMandatory = false;
            isInstalling = false;
            latestVersion = null;
            latestTitle = "";
            downloadUrl = "";
            latestSha256 = "";
            lastError = "";

            string currentVersion = "";
            try { currentVersion = Convert.ToString(BanMod.PluginVersion) ?? ""; } catch { }

            string currentSha256 = SafeGetOwnSha256();
            string url = UpdateLatestUrl
                + "?current_version=" + UnityWebRequest.EscapeURL(currentVersion ?? "")
                + "&current_sha256=" + UnityWebRequest.EscapeURL(currentSha256 ?? "");

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = UpdateTimeoutSeconds;
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            string responseText = "";
            try { responseText = request.downloadHandler != null ? request.downloadHandler.text : ""; } catch { }

            if (request.result != UnityWebRequest.Result.Success ||
                request.responseCode < 200 || request.responseCode >= 300)
            {
                result.Checked = false;
                result.Error = "Update check HTTP=" + request.responseCode + " " + (request.error ?? "");
                lastError = result.Error;
                isBroken = true;
                isChecked = true;
                request.Dispose();
                callback?.Invoke(result);
                yield break;
            }

            UpdateLatestResponse response = null;
            try
            {
                response = JsonSerializer.Deserialize<UpdateLatestResponse>(
                    responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                result.Checked = false;
                result.Error = "Update JSON non valido: " + ex.Message;
                lastError = result.Error;
                isBroken = true;
                isChecked = true;
                request.Dispose();
                callback?.Invoke(result);
                yield break;
            }

            request.Dispose();

            if (response == null || !response.success)
            {
                result.Checked = false;
                result.Error = response != null && !string.IsNullOrWhiteSpace(response.reason)
                    ? response.reason
                    : "Risposta update non valida";
                lastError = result.Error;
                isBroken = true;
                isChecked = true;
                callback?.Invoke(result);
                yield break;
            }

            string serverVersion = !string.IsNullOrWhiteSpace(response.release_version)
                ? response.release_version.Trim()
                : (response.latest_version ?? "").Trim();

            result.Checked = true;
            result.UpdateAvailable = response.update_available;
            result.Mandatory = response.mandatory;
            result.BlockStartup = response.update_available && response.mandatory;

            hasUpdate = response.update_available;
            isMandatory = response.update_available && response.mandatory;
            hasOptionalUpdate = response.update_available && !response.mandatory;
            downloadUrl = response.download_url ?? "";
            latestSha256 = (response.sha256 ?? "").Trim().ToLowerInvariant();
            latestTitle = string.IsNullOrWhiteSpace(serverVersion)
                ? "BanMod update"
                : "BanMod " + serverVersion;
            isChecked = true;

            Version parsedVersion;
            if (Version.TryParse(serverVersion, out parsedVersion))
                latestVersion = parsedVersion;

            // Aggiornamento facoltativo: memorizza lo stato, ma non interrompe l'avvio.
            if (!response.update_available || !response.mandatory)
            {
                callback?.Invoke(result);
                yield break;
            }

            // Aggiornamento obbligatorio: viene scaricato senza token e senza passare
            // dai gate di attivazione/extra-mod. Il server deve lasciare pubblici gli
            // endpoint /api/update/latest e /api/update/download.
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                result.Error = "Aggiornamento obbligatorio senza download_url";
                lastError = result.Error;
                isBroken = true;
                callback?.Invoke(result);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(latestSha256) ||
                !Regex.IsMatch(latestSha256, "^[0-9a-f]{64}$", RegexOptions.IgnoreCase))
            {
                result.Error = "Aggiornamento obbligatorio senza SHA256 ufficiale valido";
                lastError = result.Error;
                isBroken = true;
                callback?.Invoke(result);
                yield break;
            }

            bool started = false;
            string installError = "";
            yield return DownloadStageAndLaunchCoroutine(
                downloadUrl,
                latestSha256,
                (ok, error) =>
                {
                    started = ok;
                    installError = error ?? "";
                });

            result.UpdateStarted = started;
            result.Error = installError;
            lastError = installError;
            isBroken = !started;

            callback?.Invoke(result);
        }

        private static IEnumerator DownloadStageAndLaunchCoroutine(
            string url,
            string expectedSha256,
            Action<bool, string> callback)
        {
            isInstalling = true;

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = UpdateTimeoutSeconds;
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success ||
                request.responseCode < 200 || request.responseCode >= 300)
            {
                string error = "Download update fallito HTTP=" + request.responseCode + " " + (request.error ?? "");
                request.Dispose();
                isInstalling = false;
                callback?.Invoke(false, error);
                yield break;
            }

            byte[] bytes = null;
            try { bytes = request.downloadHandler != null ? request.downloadHandler.data : null; } catch { }
            request.Dispose();

            if (bytes == null || bytes.Length == 0)
            {
                isInstalling = false;
                callback?.Invoke(false, "DLL aggiornamento vuota");
                yield break;
            }

            string actualSha256 = Sha256Hex(bytes);
            if (!string.IsNullOrWhiteSpace(expectedSha256) &&
                !string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                isInstalling = false;
                callback?.Invoke(false,
                    "SHA256 update non valido. Atteso=" + expectedSha256 + " Ricevuto=" + actualSha256);
                yield break;
            }

            string targetPath = SafeGetOwnDllPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                isInstalling = false;
                callback?.Invoke(false, "Percorso BanMod.dll non trovato");
                yield break;
            }

            bool launchSucceeded = false;
            string launchError = "";

            try
            {
                string stagedPath = targetPath + ".update";
                File.WriteAllBytes(stagedPath, bytes);

                // Verifica anche il file realmente scritto su disco.
                string stagedSha256 = SafeSha256File(stagedPath);
                if (!string.Equals(stagedSha256, actualSha256, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(stagedPath); } catch { }
                    launchError = "Verifica SHA256 del file temporaneo fallita";
                }
                else
                {
                    string scriptPath = CreateReplacementScript(targetPath, stagedPath);
                    if (string.IsNullOrWhiteSpace(scriptPath))
                    {
                        try { File.Delete(stagedPath); } catch { }
                        launchError = "Impossibile creare il processo di aggiornamento";
                    }
                    else
                    {
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = scriptPath,
                            UseShellExecute = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        System.Diagnostics.Process.Start(psi);
                        launchSucceeded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                launchError = ex.GetType().Name + ": " + ex.Message;
            }

            if (!launchSucceeded)
            {
                isInstalling = false;
                callback?.Invoke(false, launchError);
                yield break;
            }

            callback?.Invoke(true, "");

            // Il .bat aspetta la chiusura di questo processo e poi sostituisce
            // esattamente la DLL da cui BanMod è stata caricata.
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        private static string CreateReplacementScript(string targetPath, string stagedPath)
        {
            try
            {
                int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                string scriptPath = Path.Combine(
                    Path.GetTempPath(),
                    "BanModUpdater_" + pid + "_" + DateTime.UtcNow.Ticks + ".bat");

                string target = EscapeBatchValue(targetPath);
                string staged = EscapeBatchValue(stagedPath);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("setlocal");
                sb.AppendLine("set \"BANMOD_PID=" + pid + "\"");
                sb.AppendLine(":wait_for_game");
                sb.AppendLine("tasklist /FI \"PID eq %BANMOD_PID%\" 2>NUL | find /I \"%BANMOD_PID%\" >NUL");
                sb.AppendLine("if not errorlevel 1 (");
                sb.AppendLine("  timeout /t 1 /nobreak >NUL");
                sb.AppendLine("  goto wait_for_game");
                sb.AppendLine(")");
                sb.AppendLine("copy /Y \"" + staged + "\" \"" + target + "\" >NUL");
                sb.AppendLine("if errorlevel 1 (");
                sb.AppendLine("  exit /b 1");
                sb.AppendLine(")");
                sb.AppendLine("del /Q \"" + staged + "\" >NUL 2>&1");
                sb.AppendLine("del /Q \"%~f0\" >NUL 2>&1");

                File.WriteAllText(scriptPath, sb.ToString(), Encoding.ASCII);
                return scriptPath;
            }
            catch
            {
                return "";
            }
        }

        private static string EscapeBatchValue(string value)
        {
            return (value ?? "").Replace("%", "%%").Replace("\"", "\"\"");
        }

        private static string SafeGetOwnDllPath()
        {
            try
            {
                Assembly assembly = typeof(BanMod).Assembly;
                if (assembly != null &&
                    !string.IsNullOrWhiteSpace(assembly.Location) &&
                    File.Exists(assembly.Location))
                    return Path.GetFullPath(assembly.Location);
            }
            catch { }

            try
            {
                string fallback = Path.Combine(Paths.PluginPath, "BanMod.dll");
                if (File.Exists(fallback))
                    return Path.GetFullPath(fallback);
            }
            catch { }

            return "";
        }

        private static string SafeGetOwnSha256()
        {
            string path = SafeGetOwnDllPath();
            return string.IsNullOrWhiteSpace(path) ? "" : SafeSha256File(path);
        }

        private static string SafeSha256File(string path)
        {
            try
            {
                using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
                using (FileStream stream = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch
            {
                return "";
            }
        }

        private static string Sha256Hex(byte[] bytes)
        {
            try
            {
                using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(bytes ?? Array.Empty<byte>());
                    StringBuilder sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch
            {
                return "";
            }
        }

        // Compatibilità con eventuali pulsanti/UI legacy: per un update facoltativo
        // apre il download URL pubblicato dal server.
        public static void StartUpdate(string url)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                    Application.OpenURL(url);
            }
            catch { }
        }
    }
}
