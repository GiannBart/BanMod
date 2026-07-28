////credits and licenses in the res
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using AmongUs.GameOptions;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace BanMod
{
    public static class ForcedRoleSystem
    {
        public static readonly Dictionary<byte, RoleTypes> ForcedRoles =
            new Dictionary<byte, RoleTypes>();

        public static bool GM = false;

        private const string ForceRoleAuthorizeUrl =
            BanModCore.PublicApiBaseUrl + "/api/forcerole/authorize";

        private const string ForceRoleCommitUrl =
            BanModCore.PublicApiBaseUrl + "/api/forcerole/commit";

        private const string ForceRoleStatusUrl =
            BanModCore.PublicApiBaseUrl + "/api/forcerole/status";

        private const int RequestTimeoutSeconds = 10;

        private static readonly System.Random Random = new System.Random();

        private static bool _accessRefreshRunning = false;
        private static int _stateGeneration = 0;

        // Stato dell'ultimo controllo server. Serve soltanto a validare SetRole
        // e non intercetta mai l'avvio della partita.
        private static bool _serverAccessKnown = false;
        private static bool _serverAccessAllowed = false;
        private static bool _serverSelfRoleAllowed = false;
        private static bool _serverPermanentUnlock = false;

        // Il ruolo locale viene soltanto marcato dopo SelectRoles. L'utilizzo
        // viene autorizzato e registrato alla fine della partita.
        private static bool _selfRoleAssignedInCurrentGame = false;
        private static byte _assignedSelfRolePlayerId = byte.MaxValue;
        private static RoleTypes _assignedSelfRole = default;

        private static void ClearGameAssignmentState()
        {
            _selfRoleAssignedInCurrentGame = false;
            _assignedSelfRolePlayerId = byte.MaxValue;
            _assignedSelfRole = default;
        }

        public sealed class ForceRoleLimitDecision
        {
            public bool success { get; set; }
            public bool allowed { get; set; }
            public bool premium { get; set; }

            [JsonPropertyName("self_allowed")]
            public bool self_allowed { get; set; }

            public int remaining { get; set; }

            [JsonPropertyName("reset_seconds")]
            public int reset_seconds { get; set; }

            public string reason { get; set; }

            [JsonPropertyName("authorization_id")]
            public string authorization_id { get; set; }

            [JsonPropertyName("attempt_id")]
            public string attempt_id { get; set; }

            public bool committed { get; set; }

            // Proprietà locale: non viene letta né scritta nel JSON del server.
            // true significa timeout, errore di connessione, HTTP 5xx, risposta vuota
            // o JSON non valido. Un JSON valido con allowed=false è invece un diniego.
            [JsonIgnore]
            public bool no_response { get; set; }
        }

        public static bool IsForcedImpostorRole(RoleTypes role)
        {
            return role == RoleTypes.Impostor ||
                   role == RoleTypes.Viper ||
                   role == RoleTypes.Shapeshifter ||
                   role == RoleTypes.Phantom;
        }

        public static bool IsLocalPlayerId(byte playerId)
        {
            try
            {
                return PlayerControl.LocalPlayer != null &&
                       PlayerControl.LocalPlayer.PlayerId == playerId;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasForcedSelfImpostorRole()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null)
                    return false;

                return ForcedRoles.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out RoleTypes role) &&
                       IsForcedImpostorRole(role);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAmongUsNormalMode()
        {
            try
            {
                return GameOptionsManager.Instance != null &&
                       GameOptionsManager.Instance.CurrentGameOptions != null &&
                       GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsFfaModeActive()
        {
            try
            {
                if (Options.GameMode == null)
                    return false;

                string modeName = Options.GameMode.GetString();
                if (string.Equals(modeName, "FFA", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(modeName, "Free For All", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(modeName, "FreeForAll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch { }

            try
            {
                int modeValue = Options.GameMode.GetValue();
                if (modeValue == (int)GameModeType.FFA || modeValue == 6)
                    return true;
            }
            catch { }

            return false;
        }

        // In FFA ForceRole deve essere completamente inattivo: niente GM,
        // niente chiamate /forcerole e nessuna selezione che possa riapparire
        // quando si torna a una modalità normale.
        public static void DisableForFfa()
        {
            _stateGeneration++;
            ForcedRoles.Clear();
            ClearGameAssignmentState();
            _accessRefreshRunning = false;
            _serverAccessKnown = false;
            _serverAccessAllowed = false;
            _serverSelfRoleAllowed = false;
            _serverPermanentUnlock = false;
            RestoreOriginalGM("FFA attivo");
        }

        public static void ForcedRoleLog(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    return;

                if (!message.Contains("ForceRole ON") &&
                    !message.Contains("ForceRole OFF") &&
                    !message.Contains("ForcedRole") &&
                    !message.Contains("PrimaryLimited") &&
                    !message.Contains("SetRole") &&
                    !message.Contains("ApplyExactRole"))
                    return;

                BMLogger.LogWarning("[ForcedRoleBlock] " + message);
            }
            catch
            {
                try
                {
                    if (!string.IsNullOrEmpty(message))
                        Debug.LogWarning("[ForcedRoleBlock] " + message);
                }
                catch { }
            }
        }

        public static void RequestAccessRefresh(string reason = "")
        {
            if (IsFfaModeActive())
                return;

            try
            {
                if (_accessRefreshRunning || AmongUsClient.Instance == null)
                    return;

                _accessRefreshRunning = true;
                AmongUsClient.Instance.StartCoroutine(
                    RefreshForcedRoleAccessFromServer(reason).WrapToIl2Cpp()
                );
            }
            catch (Exception ex)
            {
                _accessRefreshRunning = false;
                ForcedRoleLog("SetRole access refresh error: " + ex.Message);
            }
        }

        public static bool CanUseForcedRoleEffects()
        {
            return !IsFfaModeActive() && _serverAccessKnown && _serverAccessAllowed;
        }

        public static bool ShouldRunForcedRoleEffects()
        {
            return CanUseForcedRoleEffects();
        }

        private static bool HasForcedRoleOnLocalPlayer()
        {
            try
            {
                return PlayerControl.LocalPlayer != null &&
                       ForcedRoles.ContainsKey(PlayerControl.LocalPlayer.PlayerId);
            }
            catch
            {
                return false;
            }
        }

        private static bool HasForcedRoleOnOtherPlayer()
        {
            try
            {
                byte localId = PlayerControl.LocalPlayer != null
                    ? PlayerControl.LocalPlayer.PlayerId
                    : byte.MaxValue;

                return ForcedRoles.Keys.Any(playerId => playerId != localId);
            }
            catch
            {
                return false;
            }
        }

        private static void UpdateGMState(string reason = "")
        {
            if (IsFfaModeActive())
            {
                RestoreOriginalGM("FFA attivo");
                return;
            }

            try
            {
                bool shouldEnable =
                    _serverAccessKnown &&
                    _serverAccessAllowed &&
                    !_serverPermanentUnlock &&
                    HasForcedRoleOnOtherPlayer() &&
                    !HasForcedRoleOnLocalPlayer();

                if (shouldEnable)
                    EnableGMForForcedRoleWithoutCheck(reason);
                else
                    RestoreOriginalGM(reason);
            }
            catch (Exception ex)
            {
                RestoreOriginalGM("GM state error: " + ex.Message);
            }
        }

        public static void EnableGMForForcedRole(string reason = "")
        {
            UpdateGMState(reason);
        }

        public static void EnableGMForForcedRoleWithoutCheck(string reason = "")
        {
            if (IsFfaModeActive())
                return;

            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;

                if (!GM)
                {
                    GM = true;
                    ForcedRoleLog("ForcedRole GM locale impostato su true. Motivo: " + reason);
                }
            }
            catch (Exception e)
            {
                ForcedRoleLog("ForcedRole ERRORE impostazione GM=true: " + e);
            }
        }

        public static void RestoreOriginalGM(string reason = "")
        {
            try
            {
                if (GM)
                {
                    GM = false;
                    ForcedRoleLog("ForcedRole GM locale ripristinato a false. Motivo: " + reason);
                }
            }
            catch (Exception e)
            {
                ForcedRoleLog("ForcedRole ERRORE ripristino GM=false: " + e);
            }
        }

        private static IEnumerator GetFreshApiIdentity(
            Action<bool, string, string, string> callback)
        {
            string friendCode = "";
            string playerName = "";

            try
            {
                friendCode = BanModCore.GetCurrentFriendCode() ?? "";
                playerName = BanModCore.GetCurrentPlayerName() ?? "";
            }
            catch (Exception ex)
            {
                ForcedRoleLog("SetRole errore identità locale: " + ex.Message);
                callback?.Invoke(false, "", "", "");
                yield break;
            }

            bool tokenOk = false;
            string activationToken = "";

            // Viene invocato per ogni richiesta: BanModCore può così rinnovare un
            // token scaduto invece di riutilizzarlo soltanto perché non è vuoto.
            yield return BanModCore.EnsureActivationTokenForApi((ok, token) =>
            {
                tokenOk = ok;
                activationToken = token ?? "";
            });

            bool identityOk = tokenOk &&
                              !string.IsNullOrWhiteSpace(friendCode) &&
                              !string.IsNullOrWhiteSpace(activationToken);
            callback?.Invoke(identityOk, friendCode, playerName, activationToken);
        }

        private static IEnumerator SendForceRoleRequest(
            string url,
            string friendCode,
            string body,
            Action<ForceRoleLimitDecision> callback)
        {
            ForceRoleLimitDecision denied = new ForceRoleLimitDecision
            {
                success = false,
                allowed = false,
                premium = false,
                self_allowed = false,
                remaining = 0,
                reset_seconds = 0,
                reason = "server_error",
                authorization_id = "",
                attempt_id = "",
                committed = false,
                no_response = false
            };

            UnityWebRequest req = new UnityWebRequest(url, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-BANMOD-FriendCode", friendCode ?? "");
            BanModApiTokenManager.ApplyAuthHeader(req);

            yield return req.SendWebRequest();

            string text = "";
            string requestError = "";
            UnityWebRequest.Result requestResult = req.result;
            long responseCode = req.responseCode;

            try
            {
                text = req.downloadHandler != null ? req.downloadHandler.text : "";
                requestError = req.error ?? "";
            }
            catch { }

            ForceRoleLimitDecision parsed = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                    parsed = JsonSerializer.Deserialize<ForceRoleLimitDecision>(text);
            }
            catch
            {
                parsed = null;
            }

            try { req.Dispose(); } catch { }

            if (parsed != null)
            {
                parsed.no_response = false;
                callback?.Invoke(parsed);
                yield break;
            }

            if (responseCode == 204)
            {
                denied.reason = "client_not_trusted";
                denied.no_response = false;
                callback?.Invoke(denied);
                yield break;
            }

            bool timedOut = requestError.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;

            if (requestResult != UnityWebRequest.Result.Success)
            {
                denied.no_response = timedOut || responseCode == 0 || responseCode >= 500;
                denied.reason = timedOut
                    ? "timeout"
                    : responseCode > 0
                        ? "http_" + responseCode
                        : "connection_error";
                callback?.Invoke(denied);
                yield break;
            }

            denied.reason = string.IsNullOrWhiteSpace(text)
                ? "empty_response"
                : "json_parse_error";
            denied.no_response = true;
            callback?.Invoke(denied);
        }

        private static IEnumerator CheckForcedRoleStatusFromServer(
            Action<ForceRoleLimitDecision> callback)
        {
            bool identityOk = false;
            string friendCode = "";
            string playerName = "";
            string activationToken = "";

            yield return GetFreshApiIdentity((ok, fc, pn, token) =>
            {
                identityOk = ok;
                friendCode = fc;
                playerName = pn;
                activationToken = token;
            });

            if (!identityOk)
            {
                callback?.Invoke(new ForceRoleLimitDecision
                {
                    success = false,
                    allowed = false,
                    self_allowed = false,
                    reason = "activation_token_missing",
                    no_response = false
                });
                yield break;
            }

            string body = "{"
                + "\"FriendCode\":" + JsonString(friendCode) + ","
                + "\"PlayerName\":" + JsonString(playerName) + ","
                + "\"ActivationToken\":" + JsonString(activationToken)
                + "}";

            yield return SendForceRoleRequest(
                ForceRoleStatusUrl,
                friendCode,
                body,
                callback
            );
        }

        private static IEnumerator AuthorizeSelfRoleFromServer(
            byte playerId,
            RoleTypes role,
            string attemptId,
            Action<ForceRoleLimitDecision> callback)
        {
            bool identityOk = false;
            string friendCode = "";
            string playerName = "";
            string activationToken = "";

            yield return GetFreshApiIdentity((ok, fc, pn, token) =>
            {
                identityOk = ok;
                friendCode = fc;
                playerName = pn;
                activationToken = token;
            });

            if (!identityOk)
            {
                callback?.Invoke(new ForceRoleLimitDecision
                {
                    success = false,
                    allowed = false,
                    self_allowed = false,
                    reason = "activation_token_missing",
                    no_response = false
                });
                yield break;
            }

            if (string.IsNullOrWhiteSpace(attemptId))
                attemptId = Guid.NewGuid().ToString("N");

            string body = "{"
                + "\"FriendCode\":" + JsonString(friendCode) + ","
                + "\"PlayerName\":" + JsonString(playerName) + ","
                + "\"ActivationToken\":" + JsonString(activationToken) + ","
                + "\"AttemptId\":" + JsonString(attemptId) + ","
                + "\"TargetIsSelf\":true,"
                + "\"PlayerId\":" + playerId + ","
                + "\"Role\":" + JsonString(role.ToString())
                + "}";

            yield return SendForceRoleRequest(
                ForceRoleAuthorizeUrl,
                friendCode,
                body,
                callback
            );
        }

        private static IEnumerator CommitSelfRoleUseToServer(
            byte playerId,
            RoleTypes role,
            string authorizationId,
            string attemptId,
            Action<ForceRoleLimitDecision> callback)
        {
            ForceRoleLimitDecision finalDecision = null;

            for (int requestAttempt = 1; requestAttempt <= 3; requestAttempt++)
            {
                bool identityOk = false;
                string friendCode = "";
                string playerName = "";
                string activationToken = "";

                yield return GetFreshApiIdentity((ok, fc, pn, token) =>
                {
                    identityOk = ok;
                    friendCode = fc;
                    playerName = pn;
                    activationToken = token;
                });

                if (!identityOk)
                {
                    finalDecision = new ForceRoleLimitDecision
                    {
                        success = false,
                        allowed = false,
                        reason = "activation_token_missing",
                        no_response = false
                    };
                    break;
                }

                string body = "{"
                    + "\"FriendCode\":" + JsonString(friendCode) + ","
                    + "\"PlayerName\":" + JsonString(playerName) + ","
                    + "\"ActivationToken\":" + JsonString(activationToken) + ","
                    + "\"AuthorizationId\":" + JsonString(authorizationId) + ","
                    + "\"AttemptId\":" + JsonString(attemptId) + ","
                    + "\"PlayerId\":" + playerId + ","
                    + "\"Role\":" + JsonString(role.ToString()) + ","
                    + "\"Assigned\":true,"
                    + "\"AssignedRole\":" + JsonString(role.ToString())
                    + "}";

                ForceRoleLimitDecision decision = null;
                yield return SendForceRoleRequest(
                    ForceRoleCommitUrl,
                    friendCode,
                    body,
                    value => decision = value
                );

                finalDecision = decision;
                if (decision != null && !decision.no_response)
                    break;

                if (requestAttempt < 3)
                    yield return new WaitForSeconds(1.5f);
            }

            callback?.Invoke(finalDecision);
        }

        private static void ClearForcedRolesForServerDenial(string reason)
        {
            _stateGeneration++;
            ForcedRoles.Clear();
            ClearGameAssignmentState();
            RestoreOriginalGM(reason);
            ForcedRoleLog("SetRole rimossi dal server | reason=" + reason);
        }

        private static IEnumerator RefreshForcedRoleAccessFromServer(string reason)
        {
            int requestGeneration = _stateGeneration;

            if (IsFfaModeActive())
            {
                _accessRefreshRunning = false;
                yield break;
            }

            ForceRoleLimitDecision decision = null;
            yield return CheckForcedRoleStatusFromServer(value => decision = value);

            _accessRefreshRunning = false;

            // La risposta appartiene a una selezione/lobby ormai resettata.
            if (requestGeneration != _stateGeneration || IsFfaModeActive())
                yield break;

            // Un timeout o un errore temporaneo non cancella SetRole. Il controllo
            // verrà ripetuto alla prossima modifica; l'avvio della partita non viene coinvolto.
            if (decision == null || decision.no_response)
            {
                ForcedRoleLog(
                    "SetRole status non disponibile; selezione mantenuta" +
                    " | reason=" + (decision != null ? decision.reason : "no_response")
                );
                yield break;
            }

            _serverAccessKnown = true;

            // allowed=false riguarda l'accesso generale alla funzione.
            if (!decision.allowed)
            {
                _serverAccessAllowed = false;
                _serverSelfRoleAllowed = false;
                _serverPermanentUnlock = false;
                ClearForcedRolesForServerDenial(decision.reason ?? "server_denied");
                yield break;
            }

            _serverAccessAllowed = true;
            _serverSelfRoleAllowed = decision.self_allowed || decision.premium;
            _serverPermanentUnlock = decision.premium;

            // Il limite personale non vieta i ruoli sugli altri. Se gli usi
            // personali sono terminati, viene rimossa soltanto la selezione locale.
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local != null &&
                    !_serverPermanentUnlock &&
                    !_serverSelfRoleAllowed &&
                    ForcedRoles.ContainsKey(local.PlayerId))
                {
                    ForcedRoles.Remove(local.PlayerId);
                    ClearGameAssignmentState();
                    ForcedRoleLog("SetRole locale rimosso: limite orario raggiunto");
                }
            }
            catch { }

            UpdateGMState("Access refresh: " + reason);
        }

        public static void MarkSelfRoleAssignmentForEndGame()
        {
            if (IsFfaModeActive())
                return;

            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null ||
                    local.Data == null ||
                    local.Data.Role == null ||
                    !ForcedRoles.TryGetValue(local.PlayerId, out RoleTypes requestedRole))
                {
                    ClearGameAssignmentState();
                    return;
                }

                if (local.Data.RoleType != requestedRole)
                {
                    ClearGameAssignmentState();
                    ForcedRoleLog(
                        "SetRole locale non marcato per il consumo" +
                        " | requested=" + requestedRole +
                        " | actual=" + local.Data.RoleType
                    );
                    return;
                }

                _selfRoleAssignedInCurrentGame = true;
                _assignedSelfRolePlayerId = local.PlayerId;
                _assignedSelfRole = requestedRole;

                ForcedRoleLog(
                    "SetRole locale assegnato; consumo rimandato a fine partita" +
                    " | playerId=" + local.PlayerId +
                    " | role=" + requestedRole
                );
            }
            catch (Exception ex)
            {
                ClearGameAssignmentState();
                ForcedRoleLog("SetRole assignment mark error: " + ex.Message);
            }
        }

        private static IEnumerator RegisterCompletedSelfRoleUse(
            byte playerId,
            RoleTypes role)
        {
            string attemptId = Guid.NewGuid().ToString("N");
            ForceRoleLimitDecision authorization = null;

            // L'autorizzazione viene richiesta a partita conclusa. In questo modo
            // non scade durante una partita lunga e l'avvio della partita non viene mai atteso.
            for (int requestAttempt = 1; requestAttempt <= 3; requestAttempt++)
            {
                yield return AuthorizeSelfRoleFromServer(
                    playerId,
                    role,
                    attemptId,
                    value => authorization = value
                );

                if (authorization != null && !authorization.no_response)
                    break;

                if (requestAttempt < 3)
                    yield return new WaitForSeconds(1.5f);
            }

            if (authorization == null ||
                authorization.no_response ||
                !authorization.success ||
                !authorization.allowed)
            {
                ForcedRoleLog(
                    "SetRole uso non registrato a fine partita" +
                    " | reason=" + (authorization != null
                        ? authorization.reason
                        : "no_response")
                );
                yield break;
            }

            if (authorization.premium)
            {
                ForcedRoleLog("SetRole permanente: nessun utilizzo da incrementare");
                yield break;
            }

            string authorizationId = authorization.authorization_id ?? "";
            string authorizedAttemptId = string.IsNullOrWhiteSpace(authorization.attempt_id)
                ? attemptId
                : authorization.attempt_id;

            if (string.IsNullOrWhiteSpace(authorizationId))
            {
                ForcedRoleLog("SetRole uso non registrato: authorization_id mancante");
                yield break;
            }

            ForceRoleLimitDecision commit = null;
            yield return CommitSelfRoleUseToServer(
                playerId,
                role,
                authorizationId,
                authorizedAttemptId,
                value => commit = value
            );

            bool committed = commit != null &&
                             commit.success &&
                             (commit.committed || commit.premium);

            if (committed)
            {
                ForcedRoleLog(
                    "SetRole utilizzo incrementato a fine partita" +
                    " | remaining=" + commit.remaining +
                    " | reason=" + commit.reason
                );
            }
            else
            {
                ForcedRoleLog(
                    "SetRole commit non confermato a fine partita" +
                    " | reason=" + (commit != null ? commit.reason : "no_response")
                );
            }
        }

        public static void FinishSetRoleGame()
        {
            bool shouldRegisterUse =
                !IsFfaModeActive() &&
                _selfRoleAssignedInCurrentGame &&
                _assignedSelfRolePlayerId != byte.MaxValue;

            byte playerId = _assignedSelfRolePlayerId;
            RoleTypes role = _assignedSelfRole;

            // Il reset è immediato e indipendente dalla rete.
            _stateGeneration++;
            ForcedRoles.Clear();
            ClearGameAssignmentState();
            _accessRefreshRunning = false;
            _serverAccessKnown = false;
            _serverAccessAllowed = false;
            _serverSelfRoleAllowed = false;
            _serverPermanentUnlock = false;
            RestoreOriginalGM("Fine match");

            if (!shouldRegisterUse)
                return;

            try
            {
                if (AmongUsClient.Instance != null)
                {
                    AmongUsClient.Instance.StartCoroutine(
                        RegisterCompletedSelfRoleUse(playerId, role).WrapToIl2Cpp()
                    );
                }
            }
            catch (Exception ex)
            {
                ForcedRoleLog("SetRole avvio commit fine partita fallito: " + ex.Message);
            }
        }

        public static void SetForcedRole(byte playerId, RoleTypes role)
        {
            if (IsFfaModeActive())
            {
                DisableForFfa();
                return;
            }

            bool isLocalPlayer = IsLocalPlayerId(playerId);

            if (_serverAccessKnown && !_serverAccessAllowed)
            {
                ForcedRoleLog("SetRole rifiutato: accesso server negato");
                return;
            }

            if (isLocalPlayer &&
                _serverAccessKnown &&
                !_serverPermanentUnlock &&
                !_serverSelfRoleAllowed)
            {
                ForcedRoleLog("SetRole locale rifiutato: limite orario raggiunto");
                return;
            }

            bool alreadyHadRole = ForcedRoles.TryGetValue(playerId, out RoleTypes previousRole);
            bool roleActuallyChanged = !alreadyHadRole || previousRole != role;

            if (isLocalPlayer && roleActuallyChanged)
            {
                ClearGameAssignmentState();
                ForcedRoleLog(
                    "SetRole scelta locale modificata" +
                    " | playerId=" + playerId +
                    " | previousRole=" + (alreadyHadRole ? previousRole.ToString() : "none") +
                    " | newRole=" + role
                );
            }

            ForcedRoles[playerId] = role;

            // Impostare anche sé stessi spegne immediatamente GM. Per un ruolo
            // soltanto sugli altri, GM si accende solo dopo conferma server e solo
            // per utenti normali, mai per lo sblocco permanente.
            UpdateGMState("SetForcedRole");
            RequestAccessRefresh("role_changed");
        }

        public static void SetForcedRoleNoLimitForPremium(byte playerId, RoleTypes role)
        {
            // Nessun bypass locale: lo sblocco permanente viene sempre letto dal server.
            SetForcedRole(playerId, role);
        }

        public static void Clear()
        {
            _stateGeneration++;
            ForcedRoles.Clear();
            ClearGameAssignmentState();
            _accessRefreshRunning = false;
            RestoreOriginalGM("ForcedRoles.Clear");
            ForcedRoleLog("ForcedRoles.Clear()");
        }

        public static void ClearForcedRole(byte playerId)
        {
            if (IsLocalPlayerId(playerId) || _assignedSelfRolePlayerId == playerId)
                ClearGameAssignmentState();

            if (ForcedRoles.Remove(playerId))
            {
                ForcedRoleLog(
                    "ClearForcedRole | playerId=" + playerId +
                    " | count=" + ForcedRoles.Count
                );
            }

            UpdateGMState("ClearForcedRole");
        }

        public static PlayerControl FindPlayerById(byte playerId)
        {
            try
            {
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                {
                    if (player != null && player.PlayerId == playerId)
                        return player;
                }
            }
            catch { }

            try
            {
                return BanMod.AllAlivePlayerControls
                    .FirstOrDefault(p => p != null && p.PlayerId == playerId);
            }
            catch
            {
                return null;
            }
        }


        public static bool TryGetForcedRole(byte playerId, out RoleTypes role)
        {
            return ForcedRoles.TryGetValue(playerId, out role);
        }

        public static bool TrySetRandomAvailableSpecialImpostorRole(
            byte playerId,
            out RoleTypes selectedRole)
        {
            selectedRole = default;

            if (IsFfaModeActive())
                return false;

            try
            {
                if (_serverAccessKnown && !_serverAccessAllowed)
                    return false;

                HashSet<RoleTypes> alreadyChosenRoles = ForcedRoles
                    .Where(x => x.Key != playerId)
                    .Select(x => x.Value)
                    .Where(role => ForcedRoleHelpers.SpecialImpostorRoles.Contains(role))
                    .ToHashSet();

                RoleTypes[] possibleRoles = ForcedRoleHelpers.SpecialImpostorRoles
                    .Where(ForcedRoleHelpers.IsEnabledSpecialImpostorRole)
                    .Where(role => !alreadyChosenRoles.Contains(role))
                    .ToArray();

                if (possibleRoles.Length == 0)
                    return false;

                selectedRole = possibleRoles[Random.Next(possibleRoles.Length)];

                SetForcedRole(playerId, selectedRole);
                return true;
            }
            catch
            {
                selectedRole = default;
                return false;
            }
        }

        private static string JsonString(string value)
        {
            if (value == null)
                value = "";

            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + "\"";
        }
    }

    public static class ForcedRoleHelpers
    {
        public class RoleRateBackup
        {
            public int Count;
            public int Chance;

            public RoleRateBackup(int count, int chance)
            {
                Count = count;
                Chance = chance;
            }
        }

        public static readonly RoleTypes[] VanillaSpecialAssignableRoles =
        {
            RoleTypes.Scientist,
            RoleTypes.Engineer,
            RoleTypes.Shapeshifter,
            RoleTypes.Noisemaker,
            RoleTypes.Phantom,
            RoleTypes.Tracker,
            RoleTypes.Detective,
            RoleTypes.Viper
        };

        public static readonly RoleTypes[] SpecialImpostorRoles =
        {
            RoleTypes.Viper,
            RoleTypes.Shapeshifter,
            RoleTypes.Phantom
        };

        public static bool IsAliveValidPlayer(PlayerControl p)
        {
            return p != null &&
                   p.Data != null &&
                   !p.Data.Disconnected &&
                   !p.Data.IsDead;
        }

        public static bool IsAssignableAtGameStart(RoleTypes role)
        {
            switch (role)
            {
                case RoleTypes.Crewmate:
                case RoleTypes.Impostor:
                case RoleTypes.Scientist:
                case RoleTypes.Engineer:
                case RoleTypes.Shapeshifter:
                case RoleTypes.Noisemaker:
                case RoleTypes.Phantom:
                case RoleTypes.Tracker:
                case RoleTypes.Detective:
                case RoleTypes.Viper:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsVanillaSpecialAssignableRole(RoleTypes role)
        {
            return VanillaSpecialAssignableRoles.Contains(role);
        }

        public static int GetConfiguredRoleCount(RoleTypes role)
        {
            try
            {
                return GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetNumPerGame(role);
            }
            catch
            {
                return 0;
            }
        }

        public static int GetConfiguredRoleChance(RoleTypes role)
        {
            try
            {
                return GameOptionsManager.Instance.CurrentGameOptions.RoleOptions.GetChancePerGame(role);
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsRoleAvailableInOptions(RoleTypes role)
        {
            if (role == RoleTypes.Crewmate || role == RoleTypes.Impostor)
                return true;

            return GetConfiguredRoleCount(role) > 0 &&
                   GetConfiguredRoleChance(role) > 0;
        }

        public static bool IsFourImpEnabled()
        {
            try
            {
                return Options.MoreImp.GetBool();
            }
            catch
            {
                return false;
            }
        }

        public static bool ShouldForceFourImpostors(int alivePlayersCount)
        {
            return IsFourImpEnabled() && alivePlayersCount > 12;
        }

        public static bool IsEnabledSpecialImpostorRole(RoleTypes role)
        {
            if (!SpecialImpostorRoles.Contains(role))
                return false;

            int count = GetConfiguredRoleCount(role);
            int chance = GetConfiguredRoleChance(role);

            return count > 0 && chance > 0;
        }

        public static Dictionary<RoleTypes, RoleRateBackup> ReduceRoleOptionsForForcedExactRoles(
            Dictionary<RoleTypes, int> forcedCountByRole)
        {
            Dictionary<RoleTypes, RoleRateBackup> backup = new Dictionary<RoleTypes, RoleRateBackup>();

            if (forcedCountByRole == null || forcedCountByRole.Count == 0)
                return backup;

            var options = GameOptionsManager.Instance.CurrentGameOptions;
            if (options == null || options.RoleOptions == null)
                return backup;

            foreach (var kvp in forcedCountByRole)
            {
                RoleTypes role = kvp.Key;
                int forcedCount = kvp.Value;

                if (forcedCount <= 0)
                    continue;

                if (!IsVanillaSpecialAssignableRole(role))
                    continue;

                int originalCount = GetConfiguredRoleCount(role);
                int originalChance = GetConfiguredRoleChance(role);

                backup[role] = new RoleRateBackup(originalCount, originalChance);

                int newCount = Math.Max(0, originalCount - forcedCount);
                int newChance = newCount <= 0 ? 0 : originalChance;

                try
                {
                    options.RoleOptions.SetRoleRate(role, newCount, newChance);

                }
                catch (Exception)
                {
                }
            }

            return backup;
        }
        public static Dictionary<RoleTypes, RoleRateBackup> ForceFourImpSpecialRoleRates()
        {
            Dictionary<RoleTypes, RoleRateBackup> backup = new Dictionary<RoleTypes, RoleRateBackup>();

            try
            {
                var options = GameOptionsManager.Instance.CurrentGameOptions;
                if (options == null || options.RoleOptions == null)
                    return backup;

                BackupAndSetRoleRate(options, backup, RoleTypes.Viper, 2, 100);
                BackupAndSetRoleRate(options, backup, RoleTypes.Phantom, 2, 100);
                BackupAndSetRoleRate(options, backup, RoleTypes.Shapeshifter, 2, 100);
            }
            catch
            {
            }

            return backup;
        }

        private static void BackupAndSetRoleRate(
            IGameOptions options,
            Dictionary<RoleTypes, RoleRateBackup> backup,
            RoleTypes role,
            int count,
            int chance)
        {
            try
            {
                if (!backup.ContainsKey(role))
                {
                    backup[role] = new RoleRateBackup(
                        GetConfiguredRoleCount(role),
                        GetConfiguredRoleChance(role)
                    );
                }

                options.RoleOptions.SetRoleRate(role, count, chance);
            }
            catch
            {
            }
        }
        public static void RestoreRoleOptions(Dictionary<RoleTypes, RoleRateBackup> backup)
        {
            if (backup == null || backup.Count == 0)
                return;

            var options = GameOptionsManager.Instance.CurrentGameOptions;
            if (options == null || options.RoleOptions == null)
                return;

            foreach (var kvp in backup)
            {
                RoleTypes role = kvp.Key;
                RoleRateBackup data = kvp.Value;

                try
                {
                    options.RoleOptions.SetRoleRate(role, data.Count, data.Chance);

                }
                catch (Exception)
                {
                }
            }
        }

        public static List<RoleTypes> BuildConfiguredSpecialImpostorPool(System.Random rng)
        {
            List<RoleTypes> pool = new List<RoleTypes>();
            List<RoleTypes> enabledRoles = new List<RoleTypes>();

            foreach (RoleTypes role in SpecialImpostorRoles)
            {
                int count = GetConfiguredRoleCount(role);
                int chance = GetConfiguredRoleChance(role);

                if (count <= 0 || chance <= 0)
                    continue;

                enabledRoles.Add(role);

                for (int i = 0; i < count; i++)
                    pool.Add(role);
            }

            if (enabledRoles.Count == 0)
            {
                enabledRoles.AddRange(SpecialImpostorRoles);
            }

            if (enabledRoles.Count == 1)
            {
                while (pool.Count < 4)
                    pool.Add(enabledRoles[0]);

                return pool.Take(4).ToList();
            }

            if (enabledRoles.Count == 2)
            {
                List<RoleTypes> balancedPool = new List<RoleTypes>();

                foreach (RoleTypes role in enabledRoles)
                {
                    int existingCount = pool.Count(r => r == role);
                    int targetCount = Math.Min(2, Math.Max(1, existingCount));

                    for (int i = 0; i < targetCount; i++)
                        balancedPool.Add(role);
                }

                int fillIndex = 0;

                while (balancedPool.Count < 4)
                {
                    RoleTypes role = enabledRoles[fillIndex % enabledRoles.Count];
                    balancedPool.Add(role);
                    fillIndex++;
                }

                return balancedPool.Take(4).ToList();
            }

            while (pool.Count < 4)
            {
                RoleTypes randomRole = enabledRoles[rng.Next(enabledRoles.Count)];
                pool.Add(randomRole);
            }

            return pool.Take(4).ToList();
        }

        public static void RemoveAlreadyUsedSpecialImpostorRolesFromPool(
            List<RoleTypes> pool,
            List<PlayerControl> allPlayers)
        {
            if (pool == null || allPlayers == null)
                return;

            foreach (var player in allPlayers)
            {
                if (player == null || player.Data == null || player.Data.Role == null)
                    continue;

                RoleTypes currentRole = player.Data.RoleType;

                if (!SpecialImpostorRoles.Contains(currentRole))
                    continue;

                int index = pool.IndexOf(currentRole);

                if (index >= 0)
                    pool.RemoveAt(index);
            }
        }

        public static void ApplyFourImpSpecialFill(
            List<PlayerControl> allPlayers,
            HashSet<byte> exactAssignedPlayers,
            System.Random rng)
        {
            if (allPlayers == null || rng == null)
                return;

            if (!ShouldForceFourImpostors(allPlayers.Count))
                return;

            List<RoleTypes> configuredPool = BuildConfiguredSpecialImpostorPool(rng);

            RemoveAlreadyUsedSpecialImpostorRolesFromPool(configuredPool, allPlayers);

            var baseImpostors = allPlayers
                .Where(p => p != null && p.Data != null)
                .Where(p => p.Data.RoleType == RoleTypes.Impostor)
                .Where(p =>
                {
                    if (exactAssignedPlayers == null)
                        return true;

                    if (exactAssignedPlayers.Contains(p.PlayerId) &&
                        ForcedRoleSystem.TryGetForcedRole(p.PlayerId, out RoleTypes forcedRole) &&
                        forcedRole == RoleTypes.Impostor)
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();

            foreach (var player in baseImpostors)
            {
                if (configuredPool.Count == 0)
                    break;

                int index = rng.Next(configuredPool.Count);
                RoleTypes selectedRole = configuredPool[index];
                configuredPool.RemoveAt(index);

                ApplyExactRole(player, selectedRole);

            }
        }

        public static void SendForcedRoleChatMessage(PlayerControl player, RoleTypes role)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;

                if (PlayerControl.LocalPlayer == null)
                    return;

                string message = Translator.GetString("Roleset");
                PlayerControl.LocalPlayer.RpcSendChat(message);
            }
            catch
            {
            }
        }

        public static void ApplyExactRole(PlayerControl player, RoleTypes role)
        {
            ForcedRoleSystem.ForcedRoleLog($"ApplyExactRole SOLO ruolo | player={player?.Data?.PlayerName ?? "null"} | role={role}");
            if (player == null || player.Data == null)
                return;

            try
            {
                RoleManager.Instance.SetRole(player, role);
            }
            catch
            {
            }

            try
            {
                player.RpcSetRole(role, false);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class RoleManager_SelectRoles_Patch
    {
        public static bool Prefix()
        {
            ForcedRoleSystem.ForcedRoleLog("RoleManager.SelectRoles Prefix chiamato");

            if (AmongUsClient.Instance == null)
            {
                ForcedRoleSystem.ForcedRoleLog("SelectRoles stop: AmongUsClient.Instance null");
                return true;
            }

            if (!AmongUsClient.Instance.AmHost)
            {
                ForcedRoleSystem.ForcedRoleLog("SelectRoles stop: non host");
                return true;
            }

            if (ForcedRoleSystem.IsFfaModeActive())
            {
                ForcedRoleSystem.DisableForFfa();
                return true;
            }

            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();

            if (Options.Jester.GetBool() && !Jester.JesterSelected)
            {
                Jester.SelectJester();
            }

            var allPlayersList = BanMod.AllAlivePlayerControls
                .Where(ForcedRoleHelpers.IsAliveValidPlayer)
                .ToList();

            bool hasForcedExactRoles = ForcedRoleSystem.ForcedRoles.Count > 0;
            bool hasForcedImpostors = BanMod.forcedImpostorIds.Count > 0;
            bool jesterActive = Options.Jester.GetBool() || Jester.ForcedJesterSelected;
            bool hideAndSeek = GameManager.Instance.IsHideAndSeek();
            bool taskRun = gameMode == GameModeType.TaskRun;
            bool fourImpActive = !taskRun && !hideAndSeek && ForcedRoleHelpers.ShouldForceFourImpostors(allPlayersList.Count);

            ForcedRoleSystem.ForcedRoleLog($"SelectRoles flags | taskRun={taskRun} hideAndSeek={hideAndSeek} jesterActive={jesterActive} forceImpostor={BanMod.forceImpostor} hasForcedImpostors={hasForcedImpostors} hasForcedExactRoles={hasForcedExactRoles} fourImpActive={fourImpActive} forcedRolesCount={ForcedRoleSystem.ForcedRoles.Count}");

            if (!taskRun &&
                !hideAndSeek &&
                !jesterActive &&
                !BanMod.forceImpostor &&
                !hasForcedImpostors &&
                !hasForcedExactRoles &&
                !fourImpActive)
            {
                ForcedRoleSystem.ForcedRoleLog("SelectRoles usa vanilla: nessuna condizione attiva");
                return true;
            }

            var gameOptions = GameOptionsManager.Instance.CurrentGameOptions;

            if (GameManager.Instance.IsHideAndSeek())
            {
                int impostorsRequired = Options.NumSeekers != null ? Options.NumSeekers.GetInt() : 1;

                var hnsOptions = GameOptionsManager.Instance.CurrentGameOptions.Cast<HideNSeekGameOptionsV10>();
                if (hnsOptions != null)
                    hnsOptions.NumImpostors = impostorsRequired;

                for (int i = 0; i < impostorsRequired; i++)
                {
                    string selectedName = Options.SeekerSelections[i].GetString();

                    if (selectedName != "Round-robin")
                    {
                        var foundPlayer = allPlayersList.Find(p => p.Data.PlayerName == selectedName);
                        if (foundPlayer != null && !BanMod.forcedImpostorIds.Contains(foundPlayer.PlayerId))
                        {
                            BanMod.forcedImpostorIds.Add(foundPlayer.PlayerId);
                        }
                    }
                }

                var randHns = new System.Random();

                List<PlayerControl> forcedImpostors;
                if (Options.Jester.GetBool())
                {
                    forcedImpostors = allPlayersList
                        .Where(p => BanMod.forcedImpostorIds.Contains(p.PlayerId))
                        .Where(p => !Jester.IsJester(p))
                        .ToList();
                }
                else
                {
                    forcedImpostors = allPlayersList
                        .Where(p => BanMod.forcedImpostorIds.Contains(p.PlayerId))
                        .ToList();
                }

                if (forcedImpostors.Count > impostorsRequired)
                    forcedImpostors = forcedImpostors.Take(impostorsRequired).ToList();

                var impostorsToAssign = new List<PlayerControl>(forcedImpostors);

                if (impostorsToAssign.Count < impostorsRequired)
                {
                    int needed = impostorsRequired - impostorsToAssign.Count;

                    IEnumerable<PlayerControl> candidates = allPlayersList
                        .Where(p => !impostorsToAssign.Contains(p));

                    if (Options.Jester.GetBool())
                    {
                        candidates = candidates.Where(p => !Jester.IsJester(p));
                    }

                    impostorsToAssign.AddRange(
                        candidates
                            .OrderBy(_ => randHns.Next())
                            .Take(needed)
                            .ToList()
                    );
                }

                var impostorInfos = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                foreach (var imp in impostorsToAssign)
                    impostorInfos.Add(imp.Data);

                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                    impostorInfos,
                    gameOptions,
                    RoleTeamTypes.Impostor,
                    int.MaxValue,
                    new Il2CppSystem.Nullable<RoleTypes>()
                );

                List<NetworkedPlayerInfo> crewmateInfos;
                if (Options.Jester.GetBool())
                {
                    crewmateInfos = allPlayersList
                        .Where(p => !impostorsToAssign.Contains(p))
                        .Where(p => !Jester.IsJester(p))
                        .Select(p => p.Data)
                        .ToList();
                }
                else
                {
                    crewmateInfos = allPlayersList
                        .Where(p => !impostorsToAssign.Contains(p))
                        .Select(p => p.Data)
                        .ToList();
                }

                var il2cppCrewmates = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                foreach (var cm in crewmateInfos)
                    il2cppCrewmates.Add(cm);

                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                    il2cppCrewmates,
                    gameOptions,
                    RoleTeamTypes.Crewmate,
                    int.MaxValue,
                    new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate)
                );

                foreach (var pc in PlayerControl.AllPlayerControls)
                    pc.Data.Role?.Initialize(pc);

                return false;
            }

            bool hasRealJester =
                Options.Jester.GetBool() &&
                Jester.JesterSelected &&
                Jester.JesterId != 255;


            Dictionary<RoleTypes, ForcedRoleHelpers.RoleRateBackup> roleOptionBackup = null;

            try
            {
                int adjustedNumImpostors = taskRun
                    ? 0
                    : gameOptions.GetAdjustedNumImpostors(allPlayersList.Count);

                if (!taskRun && ForcedRoleHelpers.ShouldForceFourImpostors(allPlayersList.Count))
                {
                    adjustedNumImpostors = Math.Min(4, allPlayersList.Count);

                }

                if (!taskRun && allPlayersList.Count > 0)
                {
                    int requiredForcedImpostorSlots = ForcedRoleSystem.ForcedRoles
                        .Where(entry => allPlayersList.Any(p => p.PlayerId == entry.Key))
                        .Count(entry => RoleManager.IsImpostorRole(entry.Value));

                    adjustedNumImpostors = Math.Max(
                        adjustedNumImpostors,
                        Math.Min(requiredForcedImpostorSlots, allPlayersList.Count)
                    );
                }

                int crewSlotsTotal = Math.Max(0, allPlayersList.Count - adjustedNumImpostors);

                var rng = new System.Random();

                var exactAssignedPlayers = new HashSet<byte>();
                var appliedForcedCountByRole = new Dictionary<RoleTypes, int>();

                int usedImpostorSlots = 0;
                int usedCrewSlots = 0;

                foreach (var kvp in ForcedRoleSystem.ForcedRoles.ToList())
                {
                    byte playerId = kvp.Key;
                    RoleTypes forcedRole = kvp.Value;

                    var player = allPlayersList.FirstOrDefault(p => p.PlayerId == playerId);
                    if (player == null)
                        continue;

                    if (hasRealJester && Jester.IsJester(player))
                    {
                        continue;
                    }

                    if (exactAssignedPlayers.Contains(player.PlayerId))
                        continue;

                    if (!ForcedRoleHelpers.IsAssignableAtGameStart(forcedRole))
                    {
                        continue;
                    }

                    bool isImpostorRole = RoleManager.IsImpostorRole(forcedRole);
                    bool isSpecial = forcedRole != RoleTypes.Crewmate && forcedRole != RoleTypes.Impostor;

                    if (isImpostorRole)
                    {
                        if (usedImpostorSlots >= adjustedNumImpostors)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (usedCrewSlots >= crewSlotsTotal)
                        {
                            continue;
                        }
                    }

                    if (isSpecial && !fourImpActive)
                    {
                        if (!ForcedRoleHelpers.IsRoleAvailableInOptions(forcedRole))
                        {
                            continue;
                        }

                        int configuredCount = ForcedRoleHelpers.GetConfiguredRoleCount(forcedRole);
                        int alreadyApplied = appliedForcedCountByRole.TryGetValue(forcedRole, out var n) ? n : 0;

                        if (configuredCount > 0 && alreadyApplied >= configuredCount)
                        {
                            continue;
                        }
                    }

                    exactAssignedPlayers.Add(player.PlayerId);

                    if (!appliedForcedCountByRole.ContainsKey(forcedRole))
                        appliedForcedCountByRole[forcedRole] = 0;

                    appliedForcedCountByRole[forcedRole]++;

                    if (isImpostorRole)
                        usedImpostorSlots++;
                    else
                        usedCrewSlots++;
                }

                if (fourImpActive)
                {
                    roleOptionBackup = ForcedRoleHelpers.ForceFourImpSpecialRoleRates();
                }
                else
                {
                    roleOptionBackup = ForcedRoleHelpers.ReduceRoleOptionsForForcedExactRoles(
                        appliedForcedCountByRole
                    );
                }

                int impostorSlotsRemaining = Math.Max(0, adjustedNumImpostors - usedImpostorSlots);

                var impostorsToAssign = new List<PlayerControl>();

                var forcedImpostorCandidates = allPlayersList
                    .Where(p => BanMod.forcedImpostorIds.Contains(p.PlayerId))
                    .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                    .Where(p => !(hasRealJester && Jester.IsJester(p)))
                    .ToList();

                if (forcedImpostorCandidates.Count > impostorSlotsRemaining)
                    forcedImpostorCandidates = forcedImpostorCandidates.Take(impostorSlotsRemaining).ToList();

                impostorsToAssign.AddRange(forcedImpostorCandidates);

                if (impostorsToAssign.Count < impostorSlotsRemaining)
                {
                    int needed = impostorSlotsRemaining - impostorsToAssign.Count;

                    var randomImpostorCandidates = allPlayersList
                        .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                        .Where(p => !impostorsToAssign.Contains(p))
                        .Where(p => !(hasRealJester && Jester.IsJester(p)))
                        .OrderBy(_ => rng.Next())
                        .Take(needed)
                        .ToList();

                    impostorsToAssign.AddRange(randomImpostorCandidates);
                }

                var crewmatesToAssign = allPlayersList
                    .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                    .Where(p => !impostorsToAssign.Contains(p))
                    .Where(p => !(hasRealJester && Jester.IsJester(p)))
                    .ToList();

                if (impostorsToAssign.Count > 0)
                {
                    var impostorInfos = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var imp in impostorsToAssign)
                        impostorInfos.Add(imp.Data);

                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                        impostorInfos,
                        gameOptions,
                        RoleTeamTypes.Impostor,
                        int.MaxValue,
                        new Il2CppSystem.Nullable<RoleTypes>()
                    );
                }

                if (crewmatesToAssign.Count > 0)
                {
                    var crewmateInfos = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var cm in crewmatesToAssign)
                        crewmateInfos.Add(cm.Data);

                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                        crewmateInfos,
                        gameOptions,
                        RoleTeamTypes.Crewmate,
                        int.MaxValue,
                        new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate)
                    );
                }

                foreach (var kvp in ForcedRoleSystem.ForcedRoles.ToList())
                {
                    byte playerId = kvp.Key;
                    RoleTypes forcedRole = kvp.Value;

                    var player = allPlayersList.FirstOrDefault(p => p.PlayerId == playerId);
                    if (player == null)
                        continue;

                    if (!exactAssignedPlayers.Contains(player.PlayerId))
                        continue;

                    if (hasRealJester && Jester.IsJester(player))
                        continue;

                    ForcedRoleHelpers.ApplyExactRole(player, forcedRole);
                }

                ForcedRoleHelpers.ApplyFourImpSpecialFill(
                    allPlayersList,
                    exactAssignedPlayers,
                    rng
                );

                if (hasRealJester)
                {
                    BanMod.forcedImpostorIds.Remove(Jester.JesterId);
                    ForcedRoleSystem.ClearForcedRole(Jester.JesterId);

                    var jesterPlayer = allPlayersList.FirstOrDefault(p => p.PlayerId == Jester.JesterId);
                    if (jesterPlayer != null && jesterPlayer.Data != null)
                    {
                        ForcedRoleHelpers.ApplyExactRole(jesterPlayer, Jester.GetAssignedRole());
                    }
                }

                //foreach (var pc in PlayerControl.AllPlayerControls)
                //{
                //    try
                //    {
                //        pc.Data.Role?.Initialize(pc);
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data != null)
                    {
                        if (pc.Data.Role == null)
                        {
                            RoleManager.Instance.SetRole(pc, RoleTypes.Crewmate);
                        }
                        pc.Data.Role?.Initialize(pc);
                    }
                }

                ForcedRoleSystem.MarkSelfRoleAssignmentForEndGame();
                return false;
            }
            //finally
            //{
            //    ForcedRoleHelpers.RestoreRoleOptions(roleOptionBackup);
            //}
            finally
            {
                if (roleOptionBackup != null && roleOptionBackup.Count > 0)
                {
                    ForcedRoleHelpers.RestoreRoleOptions(roleOptionBackup);
                }
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    public static class ForcedRole_EndGameRestoreGMPatch
    {
        public static void Postfix()
        {
            ForcedRoleSystem.FinishSetRoleGame();
        }
    }


}