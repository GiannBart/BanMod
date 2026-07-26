////credits and licenses in the res
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private const string ForceRoleConsumeUrl =
            "https://server.banmod.online/api/forcerole/consume";

        private const string ForceRoleStatusUrl =
            "https://server.banmod.online/api/forcerole/status";

        private const int RequestTimeoutSeconds = 10;

        private static readonly System.Random Random = new System.Random();

        private static bool _gameStartLimitCheckRunning = false;
        private static bool _allowNextGameStart = false;
        private static bool _setRoleUseCommittedForCurrentGame = false;

        // Il terzo tentativo consecutivo senza una risposta valida viene rifiutato.
        // I primi due tentativi senza risposta vengono invece tollerati.
        private const int MaxConsecutiveServerNoResponses = 3;
        private static int _consecutiveServerNoResponses = 0;

        public sealed class ForceRoleLimitDecision
        {
            public bool success { get; set; }
            public bool allowed { get; set; }
            public bool premium { get; set; }
            public int remaining { get; set; }

            [JsonPropertyName("reset_seconds")]
            public int reset_seconds { get; set; }

            public string reason { get; set; }

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
                if (Options.GameMode != null &&
                    string.Equals(Options.GameMode.GetString(), "FFA", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            try
            {
                if (Options.GameMode != null &&
                    (GameModeType)Options.GameMode.GetValue() == GameModeType.FFA)
                    return true;
            }
            catch { }

            return false;
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
            try
            {
                ForcedRoleLog("ForcedRole RequestAccessRefresh ignorato. reason=" + reason);
            }
            catch { }
        }

        public static bool CanUseForcedRoleEffects()
        {
            return true;
        }

        public static bool ShouldRunForcedRoleEffects()
        {
            return true;
        }

        public static void EnableGMForForcedRole(string reason = "")
        {
            try
            {
                EnableGMForForcedRoleWithoutCheck(reason);
            }
            catch { }
        }

        public static void EnableGMForForcedRoleWithoutCheck(string reason = "")
        {
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
                GM = false;
                ForcedRoleLog("ForcedRole GM locale ripristinato a false. Motivo: " + reason);
            }
            catch (Exception e)
            {
                ForcedRoleLog("ForcedRole ERRORE ripristino GM=false: " + e);
            }
        }

        private static IEnumerator ConsumeSelfImpostorUseFromServer(
            byte playerId,
            RoleTypes role,
            Action<ForceRoleLimitDecision> callback)
        {
            ForceRoleLimitDecision denied = new ForceRoleLimitDecision
            {
                success = false,
                allowed = false,
                premium = false,
                remaining = 0,
                reset_seconds = 0,
                reason = "server_error",
                no_response = false
            };

            string friendCode = "";
            string playerName = "";
            string activationToken = "";

            try
            {
                friendCode = BanModCore.GetCurrentFriendCode();
                playerName = BanModCore.GetCurrentPlayerName();
                activationToken = BanModCore.GetCurrentActivationToken();
            }
            catch (Exception ex)
            {
                denied.reason = "local_identity_error";
                ForcedRoleLog("SetRole errore lettura identità locale: " + ex.Message);
            }

            if (string.IsNullOrWhiteSpace(activationToken))
            {
                bool tokenOk = false;
                string tokenValue = "";

                yield return BanModCore.EnsureActivationTokenForApi((ok, token) =>
                {
                    tokenOk = ok;
                    tokenValue = token ?? "";
                });

                if (!tokenOk || string.IsNullOrWhiteSpace(tokenValue))
                {
                    denied.reason = "activation_token_missing";
                    denied.no_response = false;
                    callback?.Invoke(denied);
                    yield break;
                }

                activationToken = tokenValue;
            }

            string body =
                "{"
                + "\"FriendCode\":" + JsonString(friendCode) + ","
                + "\"PlayerName\":" + JsonString(playerName) + ","
                + "\"ActivationToken\":" + JsonString(activationToken) + ","
                + "\"PlayerId\":" + playerId + ","
                + "\"Role\":" + JsonString(role.ToString())
                + "}";

            UnityWebRequest req = new UnityWebRequest(ForceRoleConsumeUrl, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

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

            ForceRoleLimitDecision parsedDecision = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                    parsedDecision = JsonSerializer.Deserialize<ForceRoleLimitDecision>(text);
            }
            catch
            {
                parsedDecision = null;
            }

            try { req.Dispose(); } catch { }

            // Un JSON valido rappresenta sempre una risposta esplicita del server,
            // anche quando allowed=false oppure il codice HTTP non è 2xx.
            if (parsedDecision != null)
            {
                parsedDecision.no_response = false;
                callback?.Invoke(parsedDecision);
                yield break;
            }

            if (requestResult != UnityWebRequest.Result.Success)
            {
                bool timedOut = requestError.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;

                denied.no_response = timedOut || responseCode == 0 || responseCode >= 500;
                denied.reason = timedOut
                    ? "timeout"
                    : responseCode > 0
                        ? "http_" + responseCode
                        : "connection_error";

                callback?.Invoke(denied);
                yield break;
            }

            if (responseCode < 200 || responseCode >= 300)
            {
                denied.reason = "http_" + responseCode;
                denied.no_response = responseCode == 0 || responseCode >= 500;
                callback?.Invoke(denied);
                yield break;
            }

            denied.reason = string.IsNullOrWhiteSpace(text)
                ? "empty_response"
                : "json_parse_error";
            denied.no_response = true;
            callback?.Invoke(denied);
        }

        private static IEnumerator CheckSelfImpostorStatusFromServer(
            byte playerId,
            RoleTypes role,
            Action<ForceRoleLimitDecision> callback)
        {
            ForceRoleLimitDecision denied = new ForceRoleLimitDecision
            {
                success = false,
                allowed = false,
                premium = false,
                remaining = 0,
                reset_seconds = 0,
                reason = "server_error"
            };

            string friendCode = BanModCore.GetCurrentFriendCode();
            string activationToken = BanModCore.GetCurrentActivationToken();

            if (string.IsNullOrWhiteSpace(activationToken))
            {
                bool tokenOk = false;
                string tokenValue = "";
                yield return BanModCore.EnsureActivationTokenForApi((ok, token) =>
                {
                    tokenOk = ok;
                    tokenValue = token ?? "";
                });
                if (!tokenOk || string.IsNullOrWhiteSpace(tokenValue))
                {
                    denied.reason = "activation_token_missing";
                    callback?.Invoke(denied);
                    yield break;
                }
                activationToken = tokenValue;
            }

            string body = "{"
                + "\"FriendCode\":" + JsonString(friendCode) + ","
                + "\"ActivationToken\":" + JsonString(activationToken) + ","
                + "\"PlayerId\":" + playerId + ","
                + "\"Role\":" + JsonString(role.ToString())
                + "}";

            UnityWebRequest req = new UnityWebRequest(ForceRoleStatusUrl, "POST");
            req.timeout = RequestTimeoutSeconds;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            try
            {
                string text = req.downloadHandler != null ? req.downloadHandler.text : "";
                ForceRoleLimitDecision parsed = string.IsNullOrWhiteSpace(text)
                    ? null
                    : JsonSerializer.Deserialize<ForceRoleLimitDecision>(text);
                if (parsed != null)
                {
                    callback?.Invoke(parsed);
                    yield break;
                }
                denied.reason = "http_" + req.responseCode;
            }
            catch
            {
                denied.reason = "json_parse_error";
            }
            finally
            {
                try { req.Dispose(); } catch { }
            }

            callback?.Invoke(denied);
        }

        private static IEnumerator RunSetRoleEffectsAfterServerLimit(byte playerId, RoleTypes role)
        {
            // La scelta è già stata salvata da SetForcedRole. Qui vengono applicati
            // soltanto gli effetti locali necessari per un altro giocatore.

            if (!IsForcedImpostorRole(role) || IsLocalPlayerId(playerId))
                yield break;

            yield return new WaitForSeconds(0.35f);

            PlayerControl player = null;
            for (int i = 0; i < 20; i++)
            {
                player = FindPlayerById(playerId);
                if (player != null && player.Data != null)
                    break;
                yield return new WaitForSeconds(0.25f);
            }

            if (player == null || player.Data == null || player.Data.Disconnected)
                yield break;

            EnableGMForForcedRoleWithoutCheck(
                "Limited ForceRole other player " + player.Data.PlayerName + " -> " + role
            );
        }

        private static bool HandleImmediateServerUnavailable(byte playerId, string reason)
        {
            _gameStartLimitCheckRunning = false;
            _consecutiveServerNoResponses++;

            if (_consecutiveServerNoResponses >= MaxConsecutiveServerNoResponses)
            {
                ClearForcedRole(playerId);
                _setRoleUseCommittedForCurrentGame = false;

                ForcedRoleLog(
                    "SetRole BLOCCATO dopo " + _consecutiveServerNoResponses +
                    " mancate risposte consecutive | reason=" + reason
                );

                // La partita può continuare, ma senza il ruolo forzato.
                return true;
            }

            _setRoleUseCommittedForCurrentGame = true;

            ForcedRoleLog(
                "SetRole consentito senza avviare la richiesta | tentativo=" +
                _consecutiveServerNoResponses + "/" + MaxConsecutiveServerNoResponses +
                " | reason=" + reason
            );

            return true;
        }

        public static bool AllowBeginGameAfterSetRoleLimitCheck(GameStartManager manager)
        {
            try
            {
                if (_allowNextGameStart)
                {
                    _allowNextGameStart = false;
                    return true;
                }

                if (_gameStartLimitCheckRunning)
                    return false;

                // Nessun bypass locale: anche lo sblocco definitivo viene deciso dal server.
                // Per un FriendCode sbloccato il server risponde allowed=true, premium=true
                // e non consuma utilizzi.

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null)
                    return true;

                if (!ForcedRoles.TryGetValue(local.PlayerId, out RoleTypes role) || !IsForcedImpostorRole(role))
                    return true;

                if (manager == null)
                    return true;

                if (AmongUsClient.Instance == null)
                    return HandleImmediateServerUnavailable(local.PlayerId, "amongus_client_null");

                _gameStartLimitCheckRunning = true;
                AmongUsClient.Instance.StartCoroutine(
                    ValidateSelfSetRoleAtActualGameStart(manager, local.PlayerId, role).WrapToIl2Cpp()
                );
                return false;
            }
            catch (Exception ex)
            {
                byte playerId = 255;
                try
                {
                    if (PlayerControl.LocalPlayer != null)
                        playerId = PlayerControl.LocalPlayer.PlayerId;
                }
                catch { }

                ForcedRoleLog("SetRole start check error: " + ex.Message);

                return playerId != 255
                    ? HandleImmediateServerUnavailable(playerId, "start_check_exception")
                    : true;
            }
        }

        private static IEnumerator ValidateSelfSetRoleAtActualGameStart(
            GameStartManager manager,
            byte playerId,
            RoleTypes expectedRole)
        {
            // Un piccolo frame di attesa permette di recepire l'ultima rimozione/cambio ruolo.
            yield return null;

            if (!ForcedRoles.TryGetValue(playerId, out RoleTypes currentRole) ||
                currentRole != expectedRole ||
                !IsForcedImpostorRole(currentRole))
            {
                _gameStartLimitCheckRunning = false;
                RestartBeginGameWithoutConsuming(manager, "ruolo rimosso prima dell'avvio");
                yield break;
            }

            bool callbackCalled = false;
            ForceRoleLimitDecision decision = null;

            // Il consumo viene eseguito prima della selezione dei ruoli. In questo modo
            // un diniego esplicito del server impedisce realmente l'applicazione del ruolo.
            yield return ConsumeSelfImpostorUseFromServer(playerId, currentRole, value =>
            {
                callbackCalled = true;
                decision = value;
            });

            bool roleStillPresent = ForcedRoles.TryGetValue(playerId, out RoleTypes finalRole) &&
                                    finalRole == currentRole &&
                                    IsForcedImpostorRole(finalRole);

            _gameStartLimitCheckRunning = false;

            bool serverDidNotRespond =
                !callbackCalled ||
                decision == null ||
                decision.no_response;

            if (serverDidNotRespond)
            {
                _consecutiveServerNoResponses++;

                string reason = decision != null ? decision.reason : "server_no_callback";

                // Primo e secondo errore consecutivo: il ForceRole viene consentito.
                // Terzo errore consecutivo: il ruolo viene rimosso e la partita parte normalmente.
                if (_consecutiveServerNoResponses >= MaxConsecutiveServerNoResponses)
                {
                    ClearForcedRole(playerId);
                    _setRoleUseCommittedForCurrentGame = false;

                    ForcedRoleLog(
                        "SetRole BLOCCATO dopo " + _consecutiveServerNoResponses +
                        " mancate risposte consecutive | reason=" + reason
                    );

                    RestartBeginGameWithoutConsuming(
                        manager,
                        "terza mancata risposta del server; ForceRole rimosso"
                    );
                    yield break;
                }

                if (!roleStillPresent)
                {
                    RestartBeginGameWithoutConsuming(
                        manager,
                        "ruolo cambiato durante il controllo; nessuna applicazione"
                    );
                    yield break;
                }

                // Non effettuiamo un secondo consumo durante SelectRoles: la richiesta potrebbe
                // essere arrivata al server anche se il client ha ricevuto un timeout.
                _setRoleUseCommittedForCurrentGame = true;

                ForcedRoleLog(
                    "SetRole consentito senza risposta server | tentativo=" +
                    _consecutiveServerNoResponses + "/" + MaxConsecutiveServerNoResponses +
                    " | reason=" + reason
                );

                RestartBeginGameWithoutConsuming(
                    manager,
                    "server non raggiungibile; ForceRole consentito temporaneamente"
                );
                yield break;
            }

            // Una risposta JSON valida, positiva o negativa, interrompe la serie di timeout.
            _consecutiveServerNoResponses = 0;

            if (!decision.allowed)
            {
                ClearForcedRole(playerId);
                _setRoleUseCommittedForCurrentGame = false;

                ForcedRoleLog(
                    "SetRole NEGATO dal server | reason=" + decision.reason +
                    " | resetSeconds=" + decision.reset_seconds
                );

                RestartBeginGameWithoutConsuming(
                    manager,
                    "ForceRole negato dal server; partita avviata senza ruolo forzato"
                );
                yield break;
            }

            if (!roleStillPresent)
            {
                // La richiesta è già stata consumata dal server, ma il ruolo è stato rimosso
                // durante l'attesa. Non viene applicato e non viene inviato un secondo consumo.
                _setRoleUseCommittedForCurrentGame = true;
                RestartBeginGameWithoutConsuming(
                    manager,
                    "ruolo rimosso dopo il consumo server; nessuna applicazione"
                );
                yield break;
            }

            _setRoleUseCommittedForCurrentGame = true;

            ForcedRoleLog(
                "SetRole autorizzato e consumato prima dell'avvio | permanente=" + decision.premium +
                " | usiRimasti=" + decision.remaining +
                " | resetSeconds=" + decision.reset_seconds
            );

            RestartBeginGameWithoutConsuming(
                manager,
                "ForceRole autorizzato dal server"
            );
        }

        public static void ConsumeSetRoleAtActualRoleSelectionIfNeeded()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;

                if (local == null ||
                    !ForcedRoles.TryGetValue(local.PlayerId, out RoleTypes role) ||
                    !IsForcedImpostorRole(role))
                {
                    return;
                }

                // Il consumo/autorizzazione deve essere già avvenuto nel Prefix di BeginGame.
                // Un ruolo inserito troppo tardi o non autorizzato non può arrivare a SelectRoles.
                if (!_setRoleUseCommittedForCurrentGame)
                {
                    ClearForcedRole(local.PlayerId);
                    ForcedRoleLog(
                        "SetRole rimosso in SelectRoles perché non autorizzato prima dell'avvio"
                    );
                }
            }
            catch (Exception ex)
            {
                ForcedRoleLog("SetRole authorization check error: " + ex.Message);
            }
        }

        public static void ResetSetRoleGameConsumptionState()
        {
            _setRoleUseCommittedForCurrentGame = false;
            _gameStartLimitCheckRunning = false;
            _allowNextGameStart = false;
        }

        private static void RestartBeginGameWithoutConsuming(GameStartManager manager, string reason)
        {
            try
            {
                if (manager == null)
                    return;
                _allowNextGameStart = true;
                ForcedRoleLog("BeginGame ripreso: " + reason);
                MethodInfo beginGame = AccessTools.Method(typeof(GameStartManager), "BeginGame");
                if (beginGame == null)
                    throw new MissingMethodException("GameStartManager.BeginGame");
                beginGame.Invoke(manager, null);
            }
            catch (Exception ex)
            {
                _allowNextGameStart = false;
                ForcedRoleLog("Impossibile riprendere BeginGame: " + ex.Message);
            }
        }

        private static void StartSetRoleEffectsAfterServerLimit(byte playerId, RoleTypes role)
        {
            try
            {
                if (AmongUsClient.Instance != null)
                {
                    AmongUsClient.Instance.StartCoroutine(
                        RunSetRoleEffectsAfterServerLimit(playerId, role).WrapToIl2Cpp()
                    );
                }
            }
            catch
            {
            }
        }

        public static void SetForcedRole(byte playerId, RoleTypes role)
        {
            // La scelta viene registrata immediatamente. Il server la autorizza o la
            // rimuove al momento dell'avvio effettivo della partita.
            ForcedRoles[playerId] = role;

            if (IsForcedImpostorRole(role) && !IsLocalPlayerId(playerId))
                StartSetRoleEffectsAfterServerLimit(playerId, role);
        }

        // Compatibilità con vecchie chiamate: non esiste più un bypass locale/premium via .bin.
        // La decisione di uso illimitato arriva esclusivamente dagli endpoint server
        // /api/forcerole/status e /api/forcerole/consume.
        public static void SetForcedRoleNoLimitForPremium(byte playerId, RoleTypes role)
        {
            SetForcedRole(playerId, role);
        }


        public static void Clear()
        {
            ForcedRoles.Clear();
            ResetSetRoleGameConsumptionState();
            ForcedRoleLog("ForcedRoles.Clear()");
        }

        public static void ClearForcedRole(byte playerId)
        {
            if (ForcedRoles.ContainsKey(playerId))
            {
                ForcedRoles.Remove(playerId);
                ForcedRoleLog("ClearForcedRole | playerId=" + playerId + " | count=" + ForcedRoles.Count);
            }
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

            try
            {
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

            return GetConfiguredRoleCount(role) > 0;
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
            return IsFourImpEnabled();
        }
        public static bool IsEnabledSpecialImpostorRole(RoleTypes role)
        {
            if (!SpecialImpostorRoles.Contains(role))
                return false;

            int count = GetConfiguredRoleCount(role);
            int chance = GetConfiguredRoleChance(role);

            return count > 0 && chance > 0;
        }

        public static RoleTypes ResolveAvailableImpostorFallback(
            RoleTypes requestedRole,
            Dictionary<RoleTypes, int> reservedCountByRole,
            System.Random rng)
        {
            if (rng == null)
                rng = new System.Random();

            if (!SpecialImpostorRoles.Contains(requestedRole))
                return requestedRole;

            int requestedConfiguredCount = GetConfiguredRoleCount(requestedRole);
            int requestedReservedCount = reservedCountByRole != null &&
                                         reservedCountByRole.TryGetValue(requestedRole, out int requestedUsed)
                ? requestedUsed
                : 0;

            bool requestedRoleAvailable =
                IsEnabledSpecialImpostorRole(requestedRole) &&
                requestedReservedCount < requestedConfiguredCount;

            if (requestedRoleAvailable)
                return requestedRole;

            List<RoleTypes> availableRoles = SpecialImpostorRoles
                .Where(role => role != requestedRole)
                .Where(IsEnabledSpecialImpostorRole)
                .Where(role =>
                {
                    int configuredCount = GetConfiguredRoleCount(role);
                    int reservedCount = reservedCountByRole != null &&
                                        reservedCountByRole.TryGetValue(role, out int used)
                        ? used
                        : 0;

                    return reservedCount < configuredCount;
                })
                .ToList();

            // L'Impostor base è sempre un fallback valido.
            availableRoles.Add(RoleTypes.Impostor);

            return availableRoles[rng.Next(availableRoles.Count)];
        }

        public static Dictionary<RoleTypes, RoleRateBackup> ReduceRoleOptionsForForcedExactRoles(
            Dictionary<RoleTypes, int> forcedCountByRole)
        {
            Dictionary<RoleTypes, RoleRateBackup> backup =
                new Dictionary<RoleTypes, RoleRateBackup>();

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
                catch { }
            }

            return backup;
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
                try
                {
                    options.RoleOptions.SetRoleRate(kvp.Key, kvp.Value.Count, kvp.Value.Chance);
                }
                catch { }
            }
        }

        public static List<RoleTypes> BuildConfiguredSpecialImpostorPool(System.Random rng)
        {
            List<RoleTypes> pool = new List<RoleTypes>();

            if (rng == null)
                rng = new System.Random();

            List<RoleTypes> enabledRoles = SpecialImpostorRoles
                .Where(role => IsEnabledSpecialImpostorRole(role))
                .OrderBy(_ => rng.Next())
                .ToList();

            foreach (RoleTypes role in enabledRoles)
            {
                if (pool.Count >= 4)
                    break;

                pool.Add(role);
            }

            while (pool.Count < 4)
                pool.Add(RoleTypes.Impostor);

            return pool;
        }

        public static void RemoveAlreadyUsedSpecialImpostorRolesFromPool(
            List<RoleTypes> pool,
            List<PlayerControl> allPlayers)
        {
            if (pool == null || allPlayers == null)
                return;

            foreach (PlayerControl player in allPlayers)
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
            Dictionary<byte, RoleTypes> resolvedForcedRoles,
            System.Random rng)
        {
            if (allPlayers == null || rng == null)
                return;

            if (!ShouldForceFourImpostors(allPlayers.Count))
                return;

            List<RoleTypes> configuredPool = BuildConfiguredSpecialImpostorPool(rng);

            RemoveAlreadyUsedSpecialImpostorRolesFromPool(configuredPool, allPlayers);

            List<PlayerControl> baseImpostors = allPlayers
                .Where(p => p != null && p.Data != null)
                .Where(p => p.Data.RoleType == RoleTypes.Impostor)
                .Where(p =>
                {
                    if (exactAssignedPlayers == null)
                        return true;

                    if (exactAssignedPlayers.Contains(p.PlayerId) &&
                        resolvedForcedRoles != null &&
                        resolvedForcedRoles.TryGetValue(p.PlayerId, out RoleTypes resolvedRole) &&
                        resolvedRole == RoleTypes.Impostor)
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();

            foreach (PlayerControl player in baseImpostors)
            {
                if (configuredPool.Count == 0)
                    break;

                int index = rng.Next(configuredPool.Count);
                RoleTypes selectedRole = configuredPool[index];
                configuredPool.RemoveAt(index);

                ApplyExactRole(player, selectedRole);
            }
        }
        public static void ApplyExactRole(PlayerControl player, RoleTypes role)
        {
            ForcedRoleSystem.ForcedRoleLog(
                "ApplyExactRole | player=" +
                (player != null && player.Data != null ? player.Data.PlayerName : "null") +
                " | role=" + role
            );

            if (player == null || player.Data == null || player.Data.Disconnected)
                return;

            try
            {
                RoleManager.Instance.SetRole(player, role);
            }
            catch { }

            try
            {
                player.RpcSetRole(role, true);
            }
            catch { }
        }

        public static void TrySetNumImpostors(object options, int value)
        {
            if (options == null)
                return;

            try
            {
                AccessTools.Property(options.GetType(), "NumImpostors")?.SetValue(options, value);
            }
            catch { }

            try
            {
                AccessTools.Field(options.GetType(), "NumImpostors")?.SetValue(options, value);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), "BeginGame")]
    [HarmonyPriority(Priority.First)]
    public static class GameStartManager_BeginGame_SetRoleLimitPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            return ForcedRoleSystem.AllowBeginGameAfterSetRoleLimitCheck(__instance);
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    [HarmonyPriority(Priority.Low)]
    public static class RoleManager_SelectRoles_PrimaryLimitedPatch
    {
        public static bool Prefix()
        {
            ForcedRoleSystem.ForcedRoleLog("PrimaryLimited SelectRoles Prefix");

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return true;

            // HideAndSeek vanilla: non facciamo nulla.
            if (!ForcedRoleSystem.IsAmongUsNormalMode())
                return true;

            // FFA ha la sua patch separata.
            if (ForcedRoleSystem.IsFfaModeActive())
                return true;

            // ForceRole non usa più una seconda DLL/.bin: il limite o lo sblocco
            // definitivo vengono gestiti esclusivamente dal server.

            ForcedRoleSystem.ConsumeSetRoleAtActualRoleSelectionIfNeeded();

            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();

            if (Options.Jester.GetBool() && !Jester.JesterSelected)
                Jester.SelectJester();

            List<PlayerControl> allPlayersList = PlayerControl.AllPlayerControls
                .ToArray()
                .Where(ForcedRoleHelpers.IsAliveValidPlayer)
                .ToList();

            HashSet<byte> validPlayerIds = new HashSet<byte>(
                allPlayersList.Select(p => p.PlayerId)
            );

            foreach (byte playerId in ForcedRoleSystem.ForcedRoles.Keys.ToList())
            {
                if (!validPlayerIds.Contains(playerId))
                    ForcedRoleSystem.ClearForcedRole(playerId);
            }

            BanMod.forcedImpostorIds.RemoveAll(playerId => !validPlayerIds.Contains(playerId));

            bool hasForcedExactRoles = ForcedRoleSystem.ForcedRoles.Count > 0;
            bool hasForcedImpostors = BanMod.forcedImpostorIds.Count > 0;
            bool jesterActive = Options.Jester.GetBool() || Jester.ForcedJesterSelected;
            bool taskRun = gameMode == GameModeType.TaskRun;
            bool Ffa = gameMode == GameModeType.FFA;
            bool fourImpActive = !taskRun && ForcedRoleHelpers.ShouldForceFourImpostors(allPlayersList.Count);

            if (!taskRun &&
                !jesterActive &&
                !BanMod.forceImpostor &&
                !hasForcedImpostors &&
                !hasForcedExactRoles &&
                !fourImpActive)
            {
                return true;
            }

            var gameOptions = GameOptionsManager.Instance.CurrentGameOptions;

            bool hasRealJester =
                Options.Jester.GetBool() &&
                Jester.JesterSelected &&
                Jester.JesterId != 255 &&
                allPlayersList.Any(p => p.PlayerId == Jester.JesterId);

            Dictionary<RoleTypes, ForcedRoleHelpers.RoleRateBackup> roleOptionBackup = null;

            try
            {
                int adjustedNumImpostors = taskRun
                    ? 0
                    : gameOptions.GetAdjustedNumImpostors(allPlayersList.Count);

                if (!taskRun && ForcedRoleHelpers.ShouldForceFourImpostors(allPlayersList.Count))
                {
                    int imp = Options.NumImpostor.GetInt();
                    adjustedNumImpostors = Math.Min(imp, allPlayersList.Count);
                }
                int crewSlotsTotal = Math.Max(0, allPlayersList.Count - adjustedNumImpostors);

                System.Random rng = new System.Random();

                HashSet<byte> exactAssignedPlayers = new HashSet<byte>();
                Dictionary<byte, RoleTypes> resolvedForcedRoles =
                    new Dictionary<byte, RoleTypes>();
                Dictionary<RoleTypes, int> appliedForcedCountByRole =
                    new Dictionary<RoleTypes, int>();

                int usedImpostorSlots = 0;
                int usedCrewSlots = 0;

                foreach (var kvp in ForcedRoleSystem.ForcedRoles.ToList())
                {
                    byte playerId = kvp.Key;
                    RoleTypes requestedRole = kvp.Value;

                    PlayerControl player = allPlayersList.FirstOrDefault(p => p.PlayerId == playerId);

                    if (player == null)
                        continue;

                    if (hasRealJester && Jester.IsJester(player))
                        continue;

                    if (exactAssignedPlayers.Contains(player.PlayerId))
                        continue;

                    if (!ForcedRoleHelpers.IsAssignableAtGameStart(requestedRole))
                        continue;

                    RoleTypes resolvedRole = requestedRole;
                    bool requestedIsSpecial =
                        requestedRole != RoleTypes.Crewmate &&
                        requestedRole != RoleTypes.Impostor;

                    if (requestedIsSpecial)
                    {
                        int configuredCount = ForcedRoleHelpers.GetConfiguredRoleCount(requestedRole);
                        int alreadyApplied = appliedForcedCountByRole.TryGetValue(requestedRole, out int n)
                            ? n
                            : 0;

                        bool requestedRoleAvailable =
                            ForcedRoleHelpers.IsRoleAvailableInOptions(requestedRole) &&
                            configuredCount > 0 &&
                            alreadyApplied < configuredCount;

                        if (!requestedRoleAvailable)
                        {
                            if (ForcedRoleSystem.IsForcedImpostorRole(requestedRole))
                            {
                                resolvedRole = ForcedRoleHelpers.ResolveAvailableImpostorFallback(
                                    requestedRole,
                                    appliedForcedCountByRole,
                                    rng
                                );

                                ForcedRoleSystem.ForcedRoleLog(
                                    "SetRole fallback | richiesto=" + requestedRole +
                                    " | applicato=" + resolvedRole +
                                    " | playerId=" + playerId
                                );
                            }
                            else
                            {
                                // I ruoli speciali crew disabilitati continuano a essere saltati.
                                continue;
                            }
                        }
                    }

                    bool isImpostorRole =
                        ForcedRoleSystem.IsForcedImpostorRole(resolvedRole) ||
                        RoleManager.IsImpostorRole(resolvedRole);

                    if (isImpostorRole)
                    {
                        if (usedImpostorSlots >= adjustedNumImpostors)
                            continue;
                    }
                    else
                    {
                        if (usedCrewSlots >= crewSlotsTotal)
                            continue;
                    }

                    exactAssignedPlayers.Add(player.PlayerId);
                    resolvedForcedRoles[player.PlayerId] = resolvedRole;

                    if (!appliedForcedCountByRole.ContainsKey(resolvedRole))
                        appliedForcedCountByRole[resolvedRole] = 0;

                    appliedForcedCountByRole[resolvedRole]++;

                    if (isImpostorRole)
                        usedImpostorSlots++;
                    else
                        usedCrewSlots++;
                }

                if (fourImpActive)
                    roleOptionBackup = null;
                else
                    roleOptionBackup = ForcedRoleHelpers.ReduceRoleOptionsForForcedExactRoles(appliedForcedCountByRole);

                int impostorSlotsRemaining = Math.Max(0, adjustedNumImpostors - usedImpostorSlots);

                List<PlayerControl> impostorsToAssign = new List<PlayerControl>();

                int forcedSlotsAvailable = Math.Max(
                    0,
                    impostorSlotsRemaining - impostorsToAssign.Count
                );

                List<PlayerControl> forcedImpostorCandidates = allPlayersList
                    .Where(p => BanMod.forcedImpostorIds.Contains(p.PlayerId))
                    .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                    .Where(p => !impostorsToAssign.Contains(p))
                    .Where(p => !(hasRealJester && Jester.IsJester(p)))
                    .Take(forcedSlotsAvailable)
                    .ToList();

                impostorsToAssign.AddRange(forcedImpostorCandidates);

                if (impostorsToAssign.Count < impostorSlotsRemaining)
                {
                    int needed = impostorSlotsRemaining - impostorsToAssign.Count;

                    List<PlayerControl> randomImpostorCandidates = allPlayersList
                        .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                        .Where(p => !impostorsToAssign.Contains(p))
                        .Where(p => !(hasRealJester && Jester.IsJester(p)))
                        .OrderBy(_ => rng.Next())
                        .Take(needed)
                        .ToList();

                    impostorsToAssign.AddRange(randomImpostorCandidates);
                }

                List<PlayerControl> crewmatesToAssign = allPlayersList
                    .Where(p => !exactAssignedPlayers.Contains(p.PlayerId))
                    .Where(p => !impostorsToAssign.Contains(p))
                    .Where(p => !(hasRealJester && Jester.IsJester(p)))
                    .ToList();

                if (impostorsToAssign.Count > 0)
                {
                    var impostorInfos = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();

                    foreach (PlayerControl imp in impostorsToAssign)
                        impostorInfos.Add(imp.Data);

                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                        impostorInfos,
                        gameOptions,
                        RoleTeamTypes.Impostor,
                        int.MaxValue,
                        new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Impostor)
                    );
                }

                if (crewmatesToAssign.Count > 0)
                {
                    var crewmateInfos = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();

                    foreach (PlayerControl cm in crewmatesToAssign)
                        crewmateInfos.Add(cm.Data);

                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(
                        crewmateInfos,
                        gameOptions,
                        RoleTeamTypes.Crewmate,
                        int.MaxValue,
                        new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate)
                    );
                }

                foreach (var kvp in resolvedForcedRoles.ToList())
                {
                    byte playerId = kvp.Key;
                    RoleTypes resolvedRole = kvp.Value;

                    PlayerControl player = allPlayersList.FirstOrDefault(p => p.PlayerId == playerId);

                    if (player == null)
                        continue;

                    if (!exactAssignedPlayers.Contains(player.PlayerId))
                        continue;

                    if (hasRealJester && Jester.IsJester(player))
                        continue;

                    ForcedRoleHelpers.ApplyExactRole(player, resolvedRole);
                }

                ForcedRoleHelpers.ApplyFourImpSpecialFill(
                    allPlayersList,
                    exactAssignedPlayers,
                    resolvedForcedRoles,
                    rng
                );

                foreach (var kvp in resolvedForcedRoles.ToList())
                {
                    byte playerId = kvp.Key;
                    RoleTypes resolvedRole = kvp.Value;

                    PlayerControl player = allPlayersList.FirstOrDefault(p => p.PlayerId == playerId);

                    if (player == null)
                        continue;

                    if (!exactAssignedPlayers.Contains(player.PlayerId))
                        continue;

                    if (hasRealJester && Jester.IsJester(player))
                        continue;

                    ForcedRoleHelpers.ApplyExactRole(player, resolvedRole);
                }

                if (hasRealJester)
                {
                    BanMod.forcedImpostorIds.Remove(Jester.JesterId);
                    ForcedRoleSystem.ClearForcedRole(Jester.JesterId);

                    PlayerControl jesterPlayer = allPlayersList.FirstOrDefault(p => p.PlayerId == Jester.JesterId);

                    if (jesterPlayer != null && jesterPlayer.Data != null)
                        ForcedRoleHelpers.ApplyExactRole(jesterPlayer, Jester.GetAssignedRole());
                }

                HashSet<byte> fallbackImpostorIds = new HashSet<byte>(
                    impostorsToAssign.Select(p => p.PlayerId)
                );

                foreach (var kvp in resolvedForcedRoles.ToList())
                {
                    if (exactAssignedPlayers.Contains(kvp.Key) &&
                        ForcedRoleSystem.IsForcedImpostorRole(kvp.Value))
                    {
                        fallbackImpostorIds.Add(kvp.Key);
                    }
                }

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Disconnected)
                        continue;

                    if (pc.Data.Role == null)
                    {
                        RoleTypes fallbackRole = fallbackImpostorIds.Contains(pc.PlayerId)
                            ? RoleTypes.Impostor
                            : RoleTypes.Crewmate;

                        RoleManager.Instance.SetRole(pc, fallbackRole);
                    }

                    pc.Data.Role?.Initialize(pc);
                }

                return false;
            }
            finally
            {
                if (roleOptionBackup != null && roleOptionBackup.Count > 0)
                    ForcedRoleHelpers.RestoreRoleOptions(roleOptionBackup);
            }
        }
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    public static class ForcedRole_EndGameRestoreGMPatch
    {
        public static void Postfix()
        {
            ForcedRoleSystem.RestoreOriginalGM("Fine match");
            ForcedRoleSystem.ResetSetRoleGameConsumptionState();
        }
    }
}