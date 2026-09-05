//credits and licenses in the resources folder
using Assets.InnerNet;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using BanMod;

namespace BanMod
{
    public static class AutoFriendInviteManager
    {
        private const string SavePath = "BAN_DATA/INVITE/FriendsList.txt";

        public static bool Enabled = false;

        private static readonly Dictionary<string, InviteEntry> Selected = new Dictionary<string, InviteEntry>();
        private static readonly List<InviteEntry> ActiveQueue = new List<InviteEntry>();

        private static int queueIndex = 0;

        private static bool isWaitingForResponse = false;
        private static float waitStartTime = 0f;
        private const float MaxTimeoutSeconds = 10f; 

        public class InviteEntry
        {
            public string Puid;
            public string FriendCode;
            public string DisplayName;

            public string GetLabel()
            {
                string name = string.IsNullOrWhiteSpace(DisplayName) ? "Unknown" : DisplayName;
                string code = string.IsNullOrWhiteSpace(FriendCode) ? Puid : FriendCode;
                return name + " | " + code;
            }
        }

        public static void Load()
        {
            Selected.Clear();

            if (!File.Exists(SavePath))
                return;

            string[] lines = File.ReadAllLines(SavePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                InviteEntry entry = ParseLine(line.Trim());

                if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                    continue;

                if (!Selected.ContainsKey(entry.Puid))
                    Selected.Add(entry.Puid, entry);
            }
        }

        public static void Save()
        {
            string dir = Path.GetDirectoryName(SavePath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            List<string> lines = new List<string>();

            foreach (InviteEntry entry in Selected.Values)
            {
                lines.Add(
                    Safe(entry.Puid) + "|" +
                    Safe(entry.FriendCode) + "|" +
                    Safe(entry.DisplayName)
                );
            }

            File.WriteAllLines(SavePath, lines.ToArray());
        }

        private static InviteEntry ParseLine(string line)
        {
            if (!line.Contains("|"))
            {
                return new InviteEntry
                {
                    Puid = line,
                    FriendCode = "",
                    DisplayName = line
                };
            }

            string[] parts = line.Split('|');

            return new InviteEntry
            {
                Puid = parts.Length > 0 ? parts[0] : "",
                FriendCode = parts.Length > 1 ? parts[1] : "",
                DisplayName = parts.Length > 2 ? parts[2] : ""
            };
        }

        private static string Safe(string value)
        {
            return (value ?? "").Replace("|", "").Replace("\r", "").Replace("\n", "");
        }

        public static List<InviteEntry> GetSelectedEntries()
        {
            Load();

            List<InviteEntry> result = new List<InviteEntry>();

            foreach (InviteEntry entry in Selected.Values)
                result.Add(entry);

            return result;
        }

        public static bool IsSelectedPuid(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid))
                return false;

            Load();
            return Selected.ContainsKey(puid);
        }


        public static void AddByPlayer(PlayerControl player, bool feedback)
        {
            Load();

            if (player == null || player.Data == null)
            {
                if (feedback)
                    ShowChat("<color=#ff5555>Invalid player.</color>");

                return;
            }

            string puid = player.Data.Puid;
            string friendCode = player.Data.FriendCode;
            string displayName = player.Data.PlayerName;

            if (string.IsNullOrWhiteSpace(puid))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>This player does not have a valid PUID, so it cannot be saved for invites.</color>");

                return;
            }

            InviteEntry entry = new InviteEntry
            {
                Puid = puid,
                FriendCode = friendCode,
                DisplayName = displayName
            };

            AddEntry(entry, feedback);
        }

        private static void AddEntry(InviteEntry entry, bool feedback)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                return;

            Selected[entry.Puid] = entry;
            Save();

            if (feedback)
                ShowChat("<color=#00ff00>Added to auto-invite:</color> " + entry.GetLabel());
        }

        public static void RemoveByPuid(string puid, bool feedback)
        {
            Load();

            if (string.IsNullOrWhiteSpace(puid))
                return;

            InviteEntry removed = null;

            if (Selected.ContainsKey(puid))
                removed = Selected[puid];

            bool ok = Selected.Remove(puid);
            Save();

            if (feedback)
            {
                if (ok)
                    ShowChat("<color=#ffcc00>Removed from auto-invite:</color> " + (removed != null ? removed.GetLabel() : puid));
                else
                    ShowChat("<color=#ff5555>PUID not found in the list:</color> " + puid);
            }
        }

        public static void OpenListMenu()
        {
            Load();
            AutoFriendInviteUi.OpenStatic();
        }

        public static void StartAutoInvite()
        {
            Load();

            if (Selected.Count == 0)
            {
                Enabled = false;
                ShowChat("<color=#ff5555>The auto-invite list is empty.</color>");
                return;
            }

            if (!IsValidLobby())
            {
                Enabled = false;
                ShowChat("<color=#ff5555>Auto-invite is only available as host in an online lobby.</color>");
                return;
            }

            ActiveQueue.Clear();

            foreach (InviteEntry entry in Selected.Values)
                ActiveQueue.Add(entry);

            queueIndex = 0;
            isWaitingForResponse = false; 
            Enabled = true;

            ShowChat("<color=#00ff00>Auto-invite ON.</color> I will invite " + ActiveQueue.Count + " players and then stop.");
        }

        public static void StopAutoInvite(bool feedback)
        {
            Enabled = false;
            isWaitingForResponse = false; 
            ActiveQueue.Clear();
            queueIndex = 0;

            if (feedback)
                ShowChat("<color=#ff5555>Auto-invite OFF.</color>");
        }

        public static void Update()
        {
            if (!Enabled)
                return;

            if (!IsValidLobby() || ActiveQueue.Count == 0)
            {
                StopAutoInvite(false);
                return;
            }

            if (queueIndex >= ActiveQueue.Count)
            {
                StopAutoInvite(false);
                ShowChat("<color=#00ff00>Auto-invite completed.</color>");
                return;
            }

            if (isWaitingForResponse)
            {
                if (Time.time - waitStartTime > MaxTimeoutSeconds)
                {
                    AdvanceQueue();
                }
                return; 
            }

            InviteEntry entry = ActiveQueue[queueIndex];

            isWaitingForResponse = true; 
            waitStartTime = Time.time;  

            SendInvite(entry);
        }

        private static void AdvanceQueue()
        {
            queueIndex++;
            isWaitingForResponse = false; 
        }

        private static bool IsValidLobby()
        {
            if (!AmongUsClient.Instance)
                return false;

            if (!AmongUsClient.Instance.AmHost)
                return false;

            if (!GameStates.isLobby)
                return false;

            if (SceneManager.GetActiveScene().name != "OnlineGame")
                return false;

            string roomCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

            if (string.IsNullOrWhiteSpace(roomCode))
                return false;

            return true;
        }

        private static void SendInvite(InviteEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
            {
                AdvanceQueue();
                return;
            }

            try
            {
                if (AmongUsClient.Instance == null)
                {
                    StopAutoInvite(false);
                    return;
                }

                var manager = DestroyableSingleton<FriendsListManager>.Instance;

                if (manager == null)
                {
                    StopAutoInvite(false);
                    return;
                }

                string roomCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

                if (string.IsNullOrWhiteSpace(roomCode))
                {
                    AdvanceQueue();
                    return;
                }

                manager.SendGameInvite(
                    entry.Puid,
                    roomCode,
                    (Il2CppSystem.Action<ResponseState,
                    Response<ResponseFriendsListRequest>>)InviteResultCallback
                );
            }
            catch (System.Exception)
            {
                AdvanceQueue();
            }
        }

        private static void InviteResultCallback(ResponseState cb, Response<ResponseFriendsListRequest> response)
        {
            AdvanceQueue();
        }

        private static InviteEntry FindFriendEntryByCode(string friendCode)
        {
            if (string.IsNullOrWhiteSpace(friendCode))
                return null;

            if (!DestroyableSingleton<FriendsListManager>.InstanceExists)
                return null;

            FriendsListManager manager = DestroyableSingleton<FriendsListManager>.Instance;

            if (manager.Friends == null)
                return null;

            for (int i = 0; i < manager.Friends.Count; i++)
            {
                ResponseFriends friend = manager.Friends[i];

                if (friend == null)
                    continue;

                if (string.Equals(friend.FriendCode, friendCode, StringComparison.OrdinalIgnoreCase))
                {
                    string cachedName = "";

                    try
                    {
                        cachedName = AmongUs.Data.DataManager.Player.Friends.GetCachedName(friend.FriendPuid);
                    }
                    catch { }

                    return new InviteEntry
                    {
                        Puid = friend.FriendPuid,
                        FriendCode = friend.FriendCode,
                        DisplayName = string.IsNullOrWhiteSpace(cachedName) ? friend.FriendCode : cachedName
                    };
                }
            }

            return null;
        }
      
        public static void StartInviteAllFriends()
        {
            if (!IsValidLobby()) return;

            if (!DestroyableSingleton<FriendsListManager>.InstanceExists) return;

            var manager = DestroyableSingleton<FriendsListManager>.Instance;
            ActiveQueue.Clear();

            foreach (var friend in manager.Friends)
            {
                ActiveQueue.Add(new InviteEntry
                {
                    Puid = friend.FriendPuid,
                    FriendCode = friend.FriendCode,
                    DisplayName = "Friend" 
                });
            }

            StartQueueProcess();
        }

        public static void StartInviteSelected()
        {
            Load(); 
            if (Selected.Count == 0) return;

            ActiveQueue.Clear();
            ActiveQueue.AddRange(Selected.Values);

            StartQueueProcess();
        }

        private static void StartQueueProcess()
        {
            if (ActiveQueue.Count == 0) return;
            queueIndex = 0;
            isWaitingForResponse = false;
            Enabled = true;
            ShowChat($"<color=#00ff00>Auto-invite started for {ActiveQueue.Count} players.</color>");
        }
        public static void ShowChat(string msg)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);
        }
    }
}