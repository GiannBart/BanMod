// credits and licenses in the resources folder
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
        // New multi-list storage.
        private const string SavePath = "BAN_DATA/INVITE/FriendLists.txt";

        // Old single-list file. If found, it is imported into "Default".
        private const string LegacySavePath = "BAN_DATA/INVITE/FriendsList.txt";

        public const string DefaultListName = "Default";
        public const string AllFriendsListName = "All Friends";

        public static bool Enabled = false;

        // Each custom list has its own PUID -> InviteEntry dictionary.
        private static readonly Dictionary<string, Dictionary<string, InviteEntry>> Lists =
            new Dictionary<string, Dictionary<string, InviteEntry>>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<InviteEntry> ActiveQueue = new List<InviteEntry>();

        private static string currentListName = DefaultListName;

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

        // ------------------------------------------------------------
        // LIST MANAGEMENT
        // ------------------------------------------------------------

        public static void Load()
        {
            Lists.Clear();

            // Always keep at least one editable list.
            Lists[DefaultListName] = new Dictionary<string, InviteEntry>();

            // First run after upgrading: import the old single list.
            if (!File.Exists(SavePath))
            {
                ImportLegacyList();

                if (!Lists.ContainsKey(currentListName))
                    currentListName = DefaultListName;

                return;
            }

            string[] lines = File.ReadAllLines(SavePath);
            string activeList = DefaultListName;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Trim();

                if (line.StartsWith("@LIST|", StringComparison.Ordinal))
                {
                    string listName = SafeListName(line.Substring(6));

                    if (string.IsNullOrWhiteSpace(listName) ||
                        string.Equals(listName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
                    {
                        activeList = DefaultListName;
                        continue;
                    }

                    if (!Lists.ContainsKey(listName))
                        Lists[listName] = new Dictionary<string, InviteEntry>();

                    activeList = listName;
                    continue;
                }

                InviteEntry entry = ParseLine(line);

                if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                    continue;

                if (!Lists.ContainsKey(activeList))
                    Lists[activeList] = new Dictionary<string, InviteEntry>();

                Lists[activeList][entry.Puid] = entry;
            }

            if (!Lists.ContainsKey(currentListName))
                currentListName = DefaultListName;
        }

        private static void ImportLegacyList()
        {
            if (!File.Exists(LegacySavePath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(LegacySavePath);
                Dictionary<string, InviteEntry> defaultList = Lists[DefaultListName];

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    InviteEntry entry = ParseLine(lines[i].Trim());

                    if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                        continue;

                    defaultList[entry.Puid] = entry;
                }

                Save();
            }
            catch
            {
                // Keep the old file untouched if migration fails.
            }
        }

        public static void Save()
        {
            string dir = Path.GetDirectoryName(SavePath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            List<string> lines = new List<string>();

            foreach (KeyValuePair<string, Dictionary<string, InviteEntry>> listPair in Lists)
            {
                string listName = SafeListName(listPair.Key);

                if (string.IsNullOrWhiteSpace(listName))
                    continue;

                lines.Add("@LIST|" + listName);

                foreach (InviteEntry entry in listPair.Value.Values)
                {
                    lines.Add(
                        Safe(entry.Puid) + "|" +
                        Safe(entry.FriendCode) + "|" +
                        Safe(entry.DisplayName)
                    );
                }
            }

            File.WriteAllLines(SavePath, lines.ToArray());
        }

        public static List<string> GetListNames(bool includeAllFriends)
        {
            Load();

            List<string> result = new List<string>();

            if (includeAllFriends)
                result.Add(AllFriendsListName);

            List<string> custom = new List<string>(Lists.Keys);
            custom.Sort(StringComparer.OrdinalIgnoreCase);
            result.AddRange(custom);

            return result;
        }

        public static string GetCurrentListName()
        {
            Load();
            return currentListName;
        }

        public static bool SetCurrentList(string listName)
        {
            Load();

            listName = SafeListName(listName);

            if (string.Equals(listName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
            {
                currentListName = AllFriendsListName;
                return true;
            }

            if (!Lists.ContainsKey(listName))
                return false;

            currentListName = listName;
            return true;
        }

        public static bool CreateList(string listName, bool feedback)
        {
            Load();

            listName = SafeListName(listName);

            if (!IsValidCustomListName(listName))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>Invalid or reserved list name.</color>");
                return false;
            }

            if (Lists.ContainsKey(listName))
            {
                if (feedback)
                    ShowChat("<color=#ffcc00>List already exists:</color> " + listName);
                return false;
            }

            Lists[listName] = new Dictionary<string, InviteEntry>();
            currentListName = listName;
            Save();

            if (feedback)
                ShowChat("<color=#00ff00>Created list:</color> " + listName);

            return true;
        }

        public static bool RenameList(string oldName, string newName, bool feedback)
        {
            Load();

            oldName = SafeListName(oldName);
            newName = SafeListName(newName);

            if (!Lists.ContainsKey(oldName))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>List not found:</color> " + oldName);
                return false;
            }

            if (!IsValidCustomListName(newName))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>Invalid or reserved new list name.</color>");
                return false;
            }

            if (Lists.ContainsKey(newName))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>A list with that name already exists.</color>");
                return false;
            }

            Dictionary<string, InviteEntry> entries = Lists[oldName];
            Lists.Remove(oldName);
            Lists[newName] = entries;

            if (string.Equals(currentListName, oldName, StringComparison.OrdinalIgnoreCase))
                currentListName = newName;

            Save();

            if (feedback)
                ShowChat("<color=#00ff00>Renamed list:</color> " + oldName + " -> " + newName);

            return true;
        }

        public static bool DeleteList(string listName, bool feedback)
        {
            Load();

            listName = SafeListName(listName);

            if (!Lists.ContainsKey(listName))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>List not found:</color> " + listName);
                return false;
            }

            // Keep Default so the old UI/API always has somewhere to save.
            if (string.Equals(listName, DefaultListName, StringComparison.OrdinalIgnoreCase))
            {
                if (feedback)
                    ShowChat("<color=#ff5555>The Default list cannot be deleted.</color>");
                return false;
            }

            Lists.Remove(listName);

            if (string.Equals(currentListName, listName, StringComparison.OrdinalIgnoreCase))
                currentListName = DefaultListName;

            Save();

            if (feedback)
                ShowChat("<color=#ffcc00>Deleted list:</color> " + listName);

            return true;
        }

        public static int GetListCount(string listName)
        {
            return GetEntries(listName).Count;
        }

        public static List<InviteEntry> GetEntries(string listName)
        {
            Load();

            if (string.Equals(listName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
                return GetAllFriendEntries();

            if (!Lists.ContainsKey(listName))
                return new List<InviteEntry>();

            return new List<InviteEntry>(Lists[listName].Values);
        }

        private static bool IsValidCustomListName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (string.Equals(name, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        // ------------------------------------------------------------
        // ENTRY MANAGEMENT
        // ------------------------------------------------------------

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

        private static string SafeListName(string value)
        {
            return Safe(value).Trim();
        }

        // Backwards-compatible: old UI gets entries from the currently selected custom list.
        public static List<InviteEntry> GetSelectedEntries()
        {
            Load();

            if (string.Equals(currentListName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
                return GetAllFriendEntries();

            if (!Lists.ContainsKey(currentListName))
                currentListName = DefaultListName;

            return new List<InviteEntry>(Lists[currentListName].Values);
        }

        public static bool IsSelectedPuid(string puid)
        {
            return IsPuidInList(currentListName, puid);
        }

        public static bool IsPuidInList(string listName, string puid)
        {
            if (string.IsNullOrWhiteSpace(puid))
                return false;

            Load();

            if (string.Equals(listName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
            {
                List<InviteEntry> all = GetAllFriendEntries();

                for (int i = 0; i < all.Count; i++)
                {
                    if (string.Equals(all[i].Puid, puid, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            return Lists.ContainsKey(listName) && Lists[listName].ContainsKey(puid);
        }

        // Backwards-compatible: adds to the currently selected editable list.
        public static void AddByPlayer(PlayerControl player, bool feedback)
        {
            string targetList = currentListName;

            if (string.Equals(targetList, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
                targetList = DefaultListName;

            AddByPlayerToList(targetList, player, feedback);
        }

        public static void AddByPlayerToList(string listName, PlayerControl player, bool feedback)
        {
            Load();

            listName = SafeListName(listName);

            if (!Lists.ContainsKey(listName))
            {
                if (!CreateList(listName, false))
                {
                    if (feedback)
                        ShowChat("<color=#ff5555>Unable to create/select list:</color> " + listName);
                    return;
                }

                Load();
            }

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
                    ShowChat("<color=#ff5555>This player does not have a valid PUID.</color>");
                return;
            }

            InviteEntry entry = new InviteEntry
            {
                Puid = puid,
                FriendCode = friendCode,
                DisplayName = displayName
            };

            AddEntryToList(listName, entry, feedback);
        }

        public static bool AddFriendByCodeToList(string listName, string friendCode, bool feedback)
        {
            InviteEntry entry = FindFriendEntryByCode(friendCode);

            if (entry == null)
            {
                if (feedback)
                    ShowChat("<color=#ff5555>Friend code not found in current friend list:</color> " + friendCode);
                return false;
            }

            AddEntryToList(listName, entry, feedback);
            return true;
        }

        public static void AddEntryToList(string listName, InviteEntry entry, bool feedback)
        {
            Load();

            listName = SafeListName(listName);

            if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                return;

            if (!Lists.ContainsKey(listName))
            {
                if (!CreateList(listName, false))
                    return;

                Load();
            }

            Lists[listName][entry.Puid] = entry;
            Save();

            if (feedback)
                ShowChat("<color=#00ff00>Added to " + listName + ":</color> " + entry.GetLabel());
        }

        // Backwards-compatible: removes from current list.
        public static void RemoveByPuid(string puid, bool feedback)
        {
            RemoveByPuidFromList(currentListName, puid, feedback);
        }

        public static void RemoveByPuidFromList(string listName, string puid, bool feedback)
        {
            Load();

            if (string.IsNullOrWhiteSpace(puid))
                return;

            if (!Lists.ContainsKey(listName))
                return;

            InviteEntry removed = null;

            if (Lists[listName].ContainsKey(puid))
                removed = Lists[listName][puid];

            bool ok = Lists[listName].Remove(puid);
            Save();

            if (feedback)
            {
                if (ok)
                    ShowChat("<color=#ffcc00>Removed from " + listName + ":</color> " +
                             (removed != null ? removed.GetLabel() : puid));
                else
                    ShowChat("<color=#ff5555>PUID not found in " + listName + ":</color> " + puid);
            }
        }

        // ------------------------------------------------------------
        // UI ENTRY POINT
        // ------------------------------------------------------------

        public static void OpenListMenu()
        {
            Load();
            AutoFriendInviteUi.OpenStatic();
        }

        // ------------------------------------------------------------
        // INVITE QUEUE
        // ------------------------------------------------------------

        // Backwards-compatible: invites current list.
        public static void StartAutoInvite()
        {
            StartInviteList(currentListName);
        }

        public static void StartInviteSelected()
        {
            StartInviteList(currentListName);
        }

        public static void StartInviteList(string listName)
        {
            if (!IsValidLobby())
            {
                Enabled = false;
                ShowChat("<color=#ff5555>Auto-invite is only available as host in an online lobby.</color>");
                return;
            }

            List<InviteEntry> entries;

            if (string.Equals(listName, AllFriendsListName, StringComparison.OrdinalIgnoreCase))
            {
                entries = GetAllFriendEntries();
            }
            else
            {
                entries = GetEntries(listName);
            }

            if (entries.Count == 0)
            {
                Enabled = false;
                ShowChat("<color=#ff5555>The invite list is empty:</color> " + listName);
                return;
            }

            ActiveQueue.Clear();

            // Deduplicate the queue by PUID.
            Dictionary<string, bool> added = new Dictionary<string, bool>();

            for (int i = 0; i < entries.Count; i++)
            {
                InviteEntry entry = entries[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.Puid))
                    continue;

                if (added.ContainsKey(entry.Puid))
                    continue;

                added[entry.Puid] = true;
                ActiveQueue.Add(entry);
            }

            StartQueueProcess(listName);
        }

        public static void StartInviteAllFriends()
        {
            StartInviteList(AllFriendsListName);
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
                    AdvanceQueue();

                return;
            }

            InviteEntry entry = ActiveQueue[queueIndex];

            isWaitingForResponse = true;
            waitStartTime = Time.time;

            SendInvite(entry);
        }

        private static void StartQueueProcess(string label)
        {
            if (ActiveQueue.Count == 0)
                return;

            queueIndex = 0;
            isWaitingForResponse = false;
            Enabled = true;

            ShowChat(
                "<color=#00ff00>Auto-invite started:</color> " +
                label + " (" + ActiveQueue.Count + " players)"
            );
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

        private static void InviteResultCallback(
            ResponseState cb,
            Response<ResponseFriendsListRequest> response)
        {
            AdvanceQueue();
        }

        // ------------------------------------------------------------
        // FRIEND LOOKUPS / VIRTUAL "TUTTI" LIST
        // ------------------------------------------------------------

        private static List<InviteEntry> GetAllFriendEntries()
        {
            List<InviteEntry> result = new List<InviteEntry>();

            if (!DestroyableSingleton<FriendsListManager>.InstanceExists)
                return result;

            FriendsListManager manager = DestroyableSingleton<FriendsListManager>.Instance;

            if (manager == null || manager.Friends == null)
                return result;

            for (int i = 0; i < manager.Friends.Count; i++)
            {
                ResponseFriends friend = manager.Friends[i];

                if (friend == null || string.IsNullOrWhiteSpace(friend.FriendPuid))
                    continue;

                string cachedName = "";

                try
                {
                    cachedName = AmongUs.Data.DataManager.Player.Friends.GetCachedName(friend.FriendPuid);
                }
                catch { }

                result.Add(new InviteEntry
                {
                    Puid = friend.FriendPuid,
                    FriendCode = friend.FriendCode,
                    DisplayName = string.IsNullOrWhiteSpace(cachedName)
                        ? friend.FriendCode
                        : cachedName
                });
            }

            return result;
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
                        DisplayName = string.IsNullOrWhiteSpace(cachedName)
                            ? friend.FriendCode
                            : cachedName
                    };
                }
            }

            return null;
        }

        public static void ShowChat(string msg)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(
                PlayerControl.LocalPlayer,
                msg
            );
        }
    }
}
