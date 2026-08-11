using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine.Networking;

namespace BanMod
{
    internal static class BanModActiveLobbyApi
    {
        private const string ActiveLobbiesUrl = BanModCore.PublicApiBaseUrl + "/api/lobbies/active";
        private const string PublicLobbiesUrl = BanModCore.PublicApiBaseUrl + "/api/lobbies/public";
        private const int TimeoutSeconds = 10;

        private static readonly List<BanModActiveLobbyInfo> CachedLobbies =
            new List<BanModActiveLobbyInfo>();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static IEnumerator GetActiveLobbiesCoroutine(
            Action<List<BanModActiveLobbyInfo>> onSuccess,
            Action<string> onError)
        {
            string[] urls = new string[] { ActiveLobbiesUrl, PublicLobbiesUrl };
            string lastError = "";

            for (int attempt = 0; attempt < urls.Length; attempt++)
            {
                UnityWebRequest request = UnityWebRequest.Get(urls[attempt]);
                request.timeout = TimeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                string text = "";
                try
                {
                    text = request.downloadHandler != null ? request.downloadHandler.text : "";
                }
                catch
                {
                    text = "";
                }

                bool accepted = false;
                List<BanModActiveLobbyInfo> lobbies = null;

                try
                {
                    if (request.result != UnityWebRequest.Result.Success ||
                        request.responseCode < 200 ||
                        request.responseCode >= 300)
                    {
                        lastError = "HTTP=" + request.responseCode + " " + text;
                    }
                    else
                    {
                        BanModActiveLobbiesResponse response =
                            JsonSerializer.Deserialize<BanModActiveLobbiesResponse>(text, JsonOptions);

                        if (response == null || !response.success)
                        {
                            lastError = "Risposta lobby non valida";
                        }
                        else
                        {
                            List<BanModActiveLobbyInfo> received =
                                response.lobbies ?? new List<BanModActiveLobbyInfo>();
                            lobbies = new List<BanModActiveLobbyInfo>();
                            HashSet<string> seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            // I server legacy non valorizzano sempre is_public. Un codice
                            // valido e non privato è sufficiente per considerarli compatibili.
                            for (int i = 0; i < received.Count; i++)
                            {
                                BanModActiveLobbyInfo lobby = received[i];
                                if (!IsVisibleLobby(lobby))
                                    continue;

                                string code = lobby.GetCode();
                                if (!string.IsNullOrWhiteSpace(code) && !seenCodes.Add(code.Trim()))
                                    continue;

                                lobbies.Add(lobby);
                            }

                            accepted = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
                finally
                {
                    try { request.Dispose(); } catch { }
                }

                if (accepted)
                {
                    CachedLobbies.Clear();
                    CachedLobbies.AddRange(lobbies ?? new List<BanModActiveLobbyInfo>());
                    onSuccess?.Invoke(new List<BanModActiveLobbyInfo>(CachedLobbies));
                    yield break;
                }
            }

            // Un breve problema di rete non deve svuotare il browser lobby già
            // popolato. Restituiamo l'ultima cache valida e segnaliamo errore solo
            // quando non abbiamo mai ricevuto alcun elenco.
            if (CachedLobbies.Count > 0)
            {
                onSuccess?.Invoke(new List<BanModActiveLobbyInfo>(CachedLobbies));
                yield break;
            }

            onError?.Invoke(string.IsNullOrWhiteSpace(lastError) ? "Lobby API non disponibile" : lastError);
        }

        private static bool IsVisibleLobby(BanModActiveLobbyInfo lobby)
        {
            if (lobby == null || lobby.is_private)
                return false;

            if (lobby.is_public)
                return true;

            return !string.IsNullOrWhiteSpace(lobby.GetCode());
        }

        public static List<BanModActiveLobbyInfo> GetCachedLobbies()
        {
            try
            {
                return new List<BanModActiveLobbyInfo>(CachedLobbies);
            }
            catch
            {
                return new List<BanModActiveLobbyInfo>();
            }
        }

        public static BanModActiveLobbyInfo FindCachedLobby(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            string normalized = code.Trim().ToUpperInvariant();

            try
            {
                for (int i = 0; i < CachedLobbies.Count; i++)
                {
                    BanModActiveLobbyInfo lobby = CachedLobbies[i];
                    if (lobby == null)
                        continue;

                    string lobbyCode = lobby.GetCode();
                    if (string.IsNullOrWhiteSpace(lobbyCode))
                        continue;

                    if (string.Equals(
                            lobbyCode.Trim().ToUpperInvariant(),
                            normalized,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return lobby;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }

    internal sealed class BanModActiveLobbiesResponse
    {
        public bool success { get; set; }
        public int server_time { get; set; }
        public List<BanModActiveLobbyInfo> lobbies { get; set; }
    }

    internal sealed class BanModActiveLobbyInfo
    {
        // Nuovo formato /api/lobbies/active.
        public string lobby_code { get; set; }
        public string player_name { get; set; }
        public string friend_code { get; set; }
        public string host_friend_code { get; set; }
        public string host_name { get; set; }
        public string game_mode { get; set; }
        public string mode { get; set; }
        public int players { get; set; }
        public int max_players { get; set; }
        public string players_text { get; set; }
        public int kill_cooldown { get; set; }
        public int kc { get; set; }
        public int impostor_count { get; set; }
        public string platform { get; set; }
        public int last_seen { get; set; }

        // Alias compatibili con il vecchio browser.
        public string game_code { get; set; }
        public int players_count { get; set; }
        public string status { get; set; }
        public bool is_public { get; set; }
        public bool is_private { get; set; }
        public bool is_host { get; set; }
        public bool share_lobby { get; set; }
        public bool code_hidden { get; set; }
        public string region { get; set; }
        public string language { get; set; }

        public string GetCode()
        {
            if (!string.IsNullOrWhiteSpace(lobby_code))
                return lobby_code;

            if (!string.IsNullOrWhiteSpace(game_code))
                return game_code;

            return "";
        }

        public string GetHost()
        {
            if (!string.IsNullOrWhiteSpace(host_name))
                return host_name;

            if (!string.IsNullOrWhiteSpace(player_name))
                return player_name;

            return "";
        }

        public string GetHostFriendCode()
        {
            if (!string.IsNullOrWhiteSpace(host_friend_code))
                return host_friend_code;

            return friend_code ?? "";
        }

        public string GetMode()
        {
            if (!string.IsNullOrWhiteSpace(game_mode))
                return game_mode;

            if (!string.IsNullOrWhiteSpace(mode))
                return mode;

            return "";
        }

        public int GetPlayers()
        {
            if (players > 0)
                return players;

            if (players_count > 0)
                return players_count;

            return 0;
        }

        public int GetMaxPlayers()
        {
            return max_players > 0 ? max_players : 15;
        }

        public string GetPlayersText()
        {
            if (!string.IsNullOrWhiteSpace(players_text))
                return players_text;

            int current = GetPlayers();
            int max = GetMaxPlayers();

            if (current <= 0)
                return "";

            return current + "/" + max;
        }

        public int GetKillCooldown()
        {
            if (kill_cooldown > 0)
                return kill_cooldown;

            if (kc > 0)
                return kc;

            return 0;
        }
    }
}
