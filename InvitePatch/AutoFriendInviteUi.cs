using Assets.InnerNet;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Il2CppInterop.Runtime.Attributes;
using BanMod;

namespace BanMod
{
    public class AutoFriendInviteUi : MonoBehaviour
    {
        public static AutoFriendInviteUi Instance;

        public bool showMenu = false;

        private Rect windowRect;
        private Vector2 windowSize = new Vector2(860, 640);
        private Vector2 scrollPosition = Vector2.zero;

        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallStyle;
        private GUIStyle closeButtonStyle;
        private GUIStyle sectionStyle;
        private GUIStyle backgroundBoxStyle;

        private Texture2D blackTexture;
        private bool lastLobbyState = true;
        private int lastScreenWidth = 0;
        private int lastScreenHeight = 0;

        private const int ButtonsPerRow = 2;
        private const float ButtonHeight = 42f;
        private const float ButtonSpacing = 8f;

        private void Awake()
        {
            Instance = this;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastLobbyState = IsLobbySafe();
            CenterWindow();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ResetGuiResources();
        }

        private void Update()
        {
            bool lobbyNow = IsLobbySafe();

            if (lobbyNow != lastLobbyState)
            {
                lastLobbyState = lobbyNow;
                ResetGuiResources();

                if (!lobbyNow)
                    showMenu = false;
            }

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                CenterWindow();
                ResetGuiResources();
            }
        }

        public void ToggleMenu()
        {
            if (showMenu)
                CloseMenu();
            else
                OpenMenu();
        }

        private static AutoFriendInviteUi EnsureInstance()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("AutoFriendInviteUi");
                DontDestroyOnLoad(obj);
                Instance = obj.AddComponent<AutoFriendInviteUi>();
            }

            return Instance;
        }

        public static void ToggleStatic()
        {
            AutoFriendInviteUi ui = EnsureInstance();
            ui.ToggleMenu();
        }

        public static void OpenStatic()
        {
            AutoFriendInviteUi ui = EnsureInstance();
            ui.OpenMenu();
        }

        public void OpenMenu()
        {
            showMenu = false;
            ResetGuiResources();
            scrollPosition = Vector2.zero;
            CenterWindow();

            try
            {
                AutoFriendInviteManager.Load();
            }
            catch
            {
            }

            showMenu = true;
        }

        public void CloseMenu()
        {
            showMenu = false;
            ResetGuiResources();
        }
        public static void ShowMenu()
        {
            AutoFriendInviteUi ui = EnsureInstance();
            ui.OpenMenu();
        }
        private bool IsLobbySafe()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return false;

                return GameStates.isLobby;
            }
            catch
            {
                return false;
            }
        }

        private void ResetGuiResources()
        {
            windowStyle = null;
            titleStyle = null;
            headerStyle = null;
            buttonStyle = null;
            smallStyle = null;
            closeButtonStyle = null;
            sectionStyle = null;
            backgroundBoxStyle = null;

            if (blackTexture != null)
            {
                try
                {
                    Object.Destroy(blackTexture);
                }
                catch
                {
                }

                blackTexture = null;
            }
        }

        private void CenterWindow()
        {
            windowRect = new Rect(
                Screen.width / 2f - windowSize.x / 2f,
                Screen.height / 2f - windowSize.y / 2f,
                windowSize.x,
                windowSize.y
            );
        }

        private void EnsureStyles()
        {
            if (blackTexture == null)
                blackTexture = MakeTex(2, 2, new Color(0f, 0f, 0f, 1f));

            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.normal.background = blackTexture;
                windowStyle.onNormal.background = blackTexture;
                windowStyle.active.background = blackTexture;
                windowStyle.onActive.background = blackTexture;
                windowStyle.focused.background = blackTexture;
                windowStyle.onFocused.background = blackTexture;
                windowStyle.border = new RectOffset
                {
                    left = 1,
                    right = 1,
                    top = 1,
                    bottom = 1
                };
                windowStyle.padding = new RectOffset
                {
                    left = 12,
                    right = 12,
                    top = 12,
                    bottom = 12
                };
            }

            if (backgroundBoxStyle == null)
            {
                backgroundBoxStyle = new GUIStyle(GUI.skin.box);
                backgroundBoxStyle.normal.background = blackTexture;
                backgroundBoxStyle.onNormal.background = blackTexture;
                backgroundBoxStyle.active.background = blackTexture;
                backgroundBoxStyle.onActive.background = blackTexture;
                backgroundBoxStyle.focused.background = blackTexture;
                backgroundBoxStyle.onFocused.background = blackTexture;
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = Color.white;
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                headerStyle.normal.textColor = Color.white;
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = Color.white;
                buttonStyle.focused.textColor = Color.white;
            }

            if (smallStyle == null)
            {
                smallStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
                smallStyle.normal.textColor = Color.white;
            }

            if (closeButtonStyle == null)
            {
                closeButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                closeButtonStyle.normal.textColor = Color.white;
                closeButtonStyle.hover.textColor = Color.white;
                closeButtonStyle.active.textColor = Color.white;
                closeButtonStyle.focused.textColor = Color.white;
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(GUI.skin.box);
                sectionStyle.normal.background = blackTexture;
                sectionStyle.onNormal.background = blackTexture;
                sectionStyle.normal.textColor = Color.white;
                sectionStyle.padding = new RectOffset
                {
                    left = 8,
                    right = 8,
                    top = 8,
                    bottom = 8
                };
            }
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color solidColor = new Color(color.r, color.g, color.b, 1f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = solidColor;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void OnGUI()
        {
            if (!showMenu)
                return;

            if (!IsLobbySafe())
            {
                CloseMenu();
                return;
            }

            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            Color oldBackground = GUI.backgroundColor;
            bool oldEnabled = GUI.enabled;
            int oldDepth = GUI.depth;

            try
            {
                EnsureStyles();

                GUI.depth = -1000;
                GUI.enabled = true;
                GUI.color = new Color(1f, 1f, 1f, 1f);
                GUI.contentColor = new Color(1f, 1f, 1f, 1f);
                GUI.backgroundColor = new Color(0f, 0f, 0f, 1f);

                GUI.Box(windowRect, GUIContent.none, backgroundBoxStyle);
                windowRect = GUI.Window(92871, windowRect, (GUI.WindowFunction)DrawWindowSafe, "", windowStyle);
            }
            catch
            {
                showMenu = false;
                ResetGuiResources();
            }
            finally
            {
                GUI.color = oldColor;
                GUI.contentColor = oldContentColor;
                GUI.backgroundColor = oldBackground;
                GUI.enabled = oldEnabled;
                GUI.depth = oldDepth;
            }
        }

        private void DrawWindowSafe(int id)
        {
            try
            {
                DrawWindow(id);
            }
            catch
            {
                GUILayout.Label("Auto Friend Invite UI error. Please close and reopen this menu.", smallStyle);
                if (GUILayout.Button("X", closeButtonStyle, GUILayout.Width(34), GUILayout.Height(30)))
                    CloseMenu();
            }
        }

        private void DrawWindow(int id)
        {
            DrawTitleBar();
            GUILayout.Space(8);

            DrawTopControls();

            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawLobbyPlayers();
            GUILayout.Space(18);
            DrawSelectedList();

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 42f, 38f));
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("AUTO FRIEND INVITE", titleStyle, GUILayout.Height(34));

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.75f, 0f, 0f, 1f);
            if (GUILayout.Button("X", closeButtonStyle, GUILayout.Width(34), GUILayout.Height(30)))
                CloseMenu();

            GUI.backgroundColor = oldBg;

            GUILayout.EndHorizontal();
        }

        private void DrawTopControls()
        {
            GUILayout.BeginHorizontal();

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = AutoFriendInviteManager.Enabled ? new Color(0f, 1f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
            if (GUILayout.Button(AutoFriendInviteManager.Enabled ? "Stop" : "Start Selected", buttonStyle, GUILayout.Height(38)))
            {
                if (AutoFriendInviteManager.Enabled) AutoFriendInviteManager.StopAutoInvite(true);
                else AutoFriendInviteManager.StartInviteSelected();
            }

            GUI.backgroundColor = new Color(0f, 0.5f, 1f, 1f);
            if (GUILayout.Button("Invite ALL Friends", buttonStyle, GUILayout.Height(38)))
            {
                AutoFriendInviteManager.StartInviteAllFriends();
            }

            GUI.backgroundColor = oldBg;
            GUILayout.EndHorizontal();
        }

        private void ToggleAutoInvite()
        {
            try
            {
                if (AutoFriendInviteManager.Enabled)
                    AutoFriendInviteManager.StopAutoInvite(true);
                else
                    AutoFriendInviteManager.StartAutoInvite();
            }
            catch
            {
            }
        }

        private void SafeLoad()
        {
            try
            {
                AutoFriendInviteManager.Load();
            }
            catch
            {
            }
        }

        private void DrawLobbyPlayers()
        {
            GUILayout.Label("FRIENDS IN LOBBY", headerStyle);
            GUILayout.Space(6);

            List<PlayerControl> players = GetLobbyPlayers();
            List<PlayerControl> playersInList = new List<PlayerControl>();
            List<PlayerControl> playersNotInList = new List<PlayerControl>();

            foreach (PlayerControl p in players)
            {
                string puid = GetPuid(p);

                if (IsSelectedPuidSafe(puid))
                    playersInList.Add(p);
                else
                    playersNotInList.Add(p);
            }

            if (players.Count == 0)
            {
                GUILayout.Label("No players found in lobby.", smallStyle);
                return;
            }

            float columnWidth = (windowRect.width - 54f) / 2f;

            GUILayout.BeginHorizontal();

            DrawLobbyColumn("Already In List", playersInList, true, columnWidth);
            GUILayout.Space(ButtonSpacing);
            DrawLobbyColumn("Not In List", playersNotInList, false, columnWidth);

            GUILayout.EndHorizontal();
        }
        [HideFromIl2Cpp]
        private void DrawLobbyColumn(string title, List<PlayerControl> players, bool selectedColumn, float width)
        {
            GUILayout.BeginVertical(sectionStyle, GUILayout.Width(width));
            GUILayout.Label(title, headerStyle);
            GUILayout.Space(6);

            if (players.Count == 0)
            {
                GUILayout.Label("No players here.", smallStyle);
                GUILayout.EndVertical();
                return;
            }

            foreach (PlayerControl p in players)
            {
                DrawLobbyPlayerButton(p, selectedColumn, width - 18f);
                GUILayout.Space(ButtonSpacing);
            }

            GUILayout.EndVertical();
        }

        private void DrawLobbyPlayerButton(PlayerControl p, bool selected, float width)
        {
            try
            {
                if (p == null || p.gameObject == null || !HasValidPlayerData(p)) return;

                string label = (selected ? "X  " : "✓  ") + GetPlayerName(p);

                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(0.85f, 0.15f, 0.15f, 1f) : new Color(0.2f, 0.85f, 0.25f, 1f);

                if (GUILayout.Button(label, buttonStyle, GUILayout.Width(width), GUILayout.Height(ButtonHeight)))
                {
                    TogglePlayer(p);
                }

                GUI.backgroundColor = oldBg;
            }
            catch
            {
            }
        }

        private void DrawSelectedList()
        {
            GUILayout.Label("SAVED AUTO-INVITE LIST", headerStyle);
            GUILayout.Space(6);

            List<AutoFriendInviteManager.InviteEntry> selected = GetVisibleSelectedEntries();

            if (selected.Count == 0)
            {
                GUILayout.Label("The list is empty. Add players from the lobby.", smallStyle);
                return;
            }

            float btnWidth = GetButtonWidth();

            for (int i = 0; i < selected.Count; i += ButtonsPerRow)
            {
                GUILayout.BeginHorizontal();

                for (int c = 0; c < ButtonsPerRow; c++)
                {
                    int idx = i + c;

                    if (idx < selected.Count)
                    {
                        AutoFriendInviteManager.InviteEntry entry = selected[idx];

                        Color oldBg = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.85f, 0.15f, 0.15f, 1f);

                        if (GUILayout.Button("X  " + GetEntryName(entry), buttonStyle, GUILayout.Width(btnWidth), GUILayout.Height(ButtonHeight)))
                        {
                            ToggleEntry(entry);
                        }

                        GUI.backgroundColor = oldBg;
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(ButtonSpacing);
            }
        }

        private void TogglePlayer(PlayerControl player)
        {
            if (player == null || player.Data == null)
                return;

            string puid = GetPuid(player);
            if (string.IsNullOrEmpty(puid))
                return;

            if (IsSelectedPuidSafe(puid))
                RemoveByPuidSafe(puid);
            else
                AddByPlayerSafe(player);
        }
        [HideFromIl2Cpp]
        private void ToggleEntry(AutoFriendInviteManager.InviteEntry entry)
        {
            if (entry == null)
                return;

            if (string.IsNullOrEmpty(entry.Puid))
                return;

            RemoveByPuidSafe(entry.Puid);
        }
        [HideFromIl2Cpp]
        private List<AutoFriendInviteManager.InviteEntry> GetVisibleSelectedEntries()
        {
            List<AutoFriendInviteManager.InviteEntry> result = new List<AutoFriendInviteManager.InviteEntry>();
            List<AutoFriendInviteManager.InviteEntry> selected = GetSelectedEntriesSafe();

            foreach (AutoFriendInviteManager.InviteEntry entry in selected)
            {
                if (entry == null)
                    continue;

                if (IsLocalPuid(entry.Puid))
                    continue;

                result.Add(entry);
            }

            return result;
        }

        private bool IsLocalPlayer(PlayerControl player)
        {
            if (player == null)
                return false;

            PlayerControl local = GetLocalPlayerSafe();

            if (local == null)
                return false;

            if (player == local)
                return true;

            try
            {
                if (player.PlayerId == local.PlayerId)
                    return true;
            }
            catch
            {
            }

            string playerPuid = GetPuid(player);
            string localPuid = GetPuid(local);

            return !string.IsNullOrEmpty(playerPuid) && playerPuid == localPuid;
        }

        private bool IsLocalPuid(string puid)
        {
            if (string.IsNullOrEmpty(puid))
                return false;

            PlayerControl local = GetLocalPlayerSafe();
            string localPuid = GetPuid(local);

            return !string.IsNullOrEmpty(localPuid) && puid == localPuid;
        }

        private string GetPlayerName(PlayerControl player)
        {
            try
            {
                if (player == null || player.Data == null || string.IsNullOrEmpty(player.Data.PlayerName))
                    return "Unknown Player";

                return player.Data.PlayerName;
            }
            catch
            {
                return "Unknown Player";
            }
        }
        [HideFromIl2Cpp]
        private string GetEntryName(AutoFriendInviteManager.InviteEntry entry)
        {
            if (entry == null)
                return "Unknown Player";

            string reflectedName = GetStringMember(entry, "PlayerName");
            if (!string.IsNullOrEmpty(reflectedName))
                return reflectedName;

            reflectedName = GetStringMember(entry, "Name");
            if (!string.IsNullOrEmpty(reflectedName))
                return reflectedName;

            reflectedName = GetStringMember(entry, "PlayerNameSnapshot");
            if (!string.IsNullOrEmpty(reflectedName))
                return reflectedName;

            return CleanDisplayName(entry.GetLabel());
        }
        [HideFromIl2Cpp]
        private string GetStringMember(object source, string memberName)
        {
            if (source == null)
                return "";

            System.Type type = source.GetType();

            System.Reflection.FieldInfo field = type.GetField(memberName);
            if (field != null)
            {
                object value = field.GetValue(source);
                return value != null ? value.ToString() : "";
            }

            System.Reflection.PropertyInfo property = type.GetProperty(memberName);
            if (property != null)
            {
                object value = property.GetValue(source, null);
                return value != null ? value.ToString() : "";
            }

            return "";
        }

        private string CleanDisplayName(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "Unknown Player";

            string result = label;

            int pipeIndex = result.IndexOf("|");
            if (pipeIndex >= 0)
                result = result.Substring(0, pipeIndex);

            int friendCodeIndex = result.IndexOf("FriendCode");
            if (friendCodeIndex >= 0)
                result = result.Substring(0, friendCodeIndex);

            int friendCodeLowerIndex = result.IndexOf("friendCode");
            if (friendCodeLowerIndex >= 0)
                result = result.Substring(0, friendCodeLowerIndex);

            int colorIndex = result.IndexOf("Color");
            if (colorIndex >= 0)
                result = result.Substring(0, colorIndex);

            result = result.Trim();

            if (result.StartsWith("[") && result.Contains("]"))
            {
                int bracketIndex = result.IndexOf("]");
                result = result.Substring(bracketIndex + 1).Trim();
            }

            return string.IsNullOrEmpty(result) ? "Unknown Player" : result;
        }
        [HideFromIl2Cpp]
        private List<PlayerControl> GetLobbyPlayers()
        {
            List<PlayerControl> result = new List<PlayerControl>();

            if (PlayerControl.AllPlayerControls == null)
                return result;

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                try
                {
                    if (p == null || p.gameObject == null) continue;

                    if (!HasValidPlayerData(p)) continue;
                    if (IsLocalPlayer(p)) continue;

                    string puid = GetPuid(p);
                    if (string.IsNullOrWhiteSpace(puid)) continue;

                    if (!IsFriend(puid)) continue;

                    result.Add(p);
                }
                catch
                {
                }
            }

            return result;
        }

        private PlayerControl GetLocalPlayerSafe()
        {
            try
            {
                return PlayerControl.LocalPlayer;
            }
            catch
            {
                return null;
            }
        }

        private bool HasValidPlayerData(PlayerControl player)
        {
            try
            {
                return player != null && player.Data != null;
            }
            catch
            {
                return false;
            }
        }

        private string GetPuid(PlayerControl player)
        {
            try
            {
                if (player == null || player.Data == null || string.IsNullOrEmpty(player.Data.Puid))
                    return "";

                return player.Data.Puid;
            }
            catch
            {
                return "";
            }
        }

        private bool IsSelectedPuidSafe(string puid)
        {
            if (string.IsNullOrEmpty(puid))
                return false;

            try
            {
                return AutoFriendInviteManager.IsSelectedPuid(puid);
            }
            catch
            {
                return false;
            }
        }

        private void AddByPlayerSafe(PlayerControl player)
        {
            try
            {
                AutoFriendInviteManager.AddByPlayer(player, true);
            }
            catch
            {
            }
        }

        private void RemoveByPuidSafe(string puid)
        {
            if (string.IsNullOrEmpty(puid))
                return;

            try
            {
                AutoFriendInviteManager.RemoveByPuid(puid, true);
            }
            catch
            {
            }
        }
        private bool IsFriend(string puid)
        {
            if (string.IsNullOrWhiteSpace(puid)) return false;

            try
            {
                if (!DestroyableSingleton<FriendsListManager>.InstanceExists) return false;

                FriendsListManager manager = DestroyableSingleton<FriendsListManager>.Instance;
                if (manager.Friends == null) return false;

                for (int i = 0; i < manager.Friends.Count; i++)
                {
                    ResponseFriends friend = manager.Friends[i];
                    if (friend != null && friend.FriendPuid == puid)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }
        [HideFromIl2Cpp]
        private List<AutoFriendInviteManager.InviteEntry> GetSelectedEntriesSafe()
        {
            try
            {
                List<AutoFriendInviteManager.InviteEntry> entries = AutoFriendInviteManager.GetSelectedEntries();
                return entries != null ? entries : new List<AutoFriendInviteManager.InviteEntry>();
            }
            catch
            {
                return new List<AutoFriendInviteManager.InviteEntry>();
            }
        }

        private float GetButtonWidth()
        {
            float contentWidth = windowRect.width - 45f;
            float totalSpacing = ButtonSpacing * (ButtonsPerRow - 1);
            return (contentWidth - totalSpacing) / ButtonsPerRow;
        }
    }
}

[HarmonyPatch(typeof(FriendsListUI), nameof(FriendsListUI.Open))]
public static class AddAutoInviteButtonPatch
{
    public static void Postfix(FriendsListUI __instance)
    {
        string buttonName = "AutoFriendInviteBtn";

        if (__instance.transform.Find(buttonName) != null)
        {
            return;
        }

        GameObject customButtonObj = Object.Instantiate(__instance.PlatformFriendsButton, __instance.transform);
        customButtonObj.name = buttonName;

        customButtonObj.transform.localPosition = new Vector3(9.247619f, 4.801587f, -18f);

        PassiveButton button = customButtonObj.GetComponent<PassiveButton>();
        if (button != null)
        {
            button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            System.Action value = () =>
            {
                AutoFriendInviteUi.OpenStatic();
            };
            button.OnClick.AddListener((UnityAction)value);
        }

        TextMeshPro[] textComponents = customButtonObj.GetComponentsInChildren<TextMeshPro>();
        foreach (TextMeshPro txt in textComponents)
        {
            TextTranslatorTMP translator = txt.GetComponent<TextTranslatorTMP>();
            if (translator != null)
            {
                Object.Destroy(translator);
            }

            txt.text = "Auto Invite";
        }

        customButtonObj.SetActive(true);
    }
}