//credits and licenses in the resources folder
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Il2CppInterop.Runtime.Attributes;
using static BanMod.Translator;

namespace BanMod
{
    public class OptionsMenu : MonoBehaviour
    {
        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(750, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private static string configPath = "BAN_DATA/SETTINGS/MenuOptions_config.txt";

        public enum ItemType { Button, Toggle }

        public class MenuItem
        {
            public string Label;
            public string InternalName;
            public ItemType Type;
            public bool ToggleValue;
            public Action OnClick;
        }

        private List<MenuItem> menuItems = new List<MenuItem>();
        private GUIStyle titleStyle;
        private GUIStyle styleOn;
        private GUIStyle styleOff;
        private GUIStyle styleDefault;

        void Start()
        {
            string directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            LoadSettings();
        }

        public static OptionsMenu Instance;
        void Awake()
        {
            Instance = this;
            LoadSettings();
        }

        private void OnEnable()
        {
            MenuRouter.OnPanelChanged += HandlePanelChanged;
        }

        private void OnDisable()
        {
            MenuRouter.OnPanelChanged -= HandlePanelChanged;
        }

        private void HandlePanelChanged(MenuRouter.Panel p)
        {
            showMenu = (p == MenuRouter.Panel.Options);
            if (showMenu)
            {
                SetupMenuContent();
                LoadSettings();
                CenterWindow();
            }
        }

        void Update()
        {
            if (KeyBindOptions.IsBindingActive) return;
            if (Input.GetKeyDown(KeyBindOptions.K15) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.Options)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.Options);
            }
        }

        public void SetupMenuContent()
        {
            menuItems.Clear();
            AddToggle(GetString("Opt_NoCountdown"), "nocountdown", () => BanMod.nocountdown, (v) => BanMod.nocountdown = v);
            AddToggle(GetString("Opt_GM"), "GM", () => BanMod.GM, (v) => BanMod.GM = v);
            AddToggle(GetString("Opt_Teleport"), "Teleport", () => BanMod.Teleport, (v) => BanMod.Teleport = v);
            AddToggle(GetString("Opt_RandomMap"), "randomMap", () => BanMod.randomMap, (v) => BanMod.randomMap = v);
            AddToggle(GetString("Opt_Protection"), "Protection", () => BanMod.Protection, (v) => BanMod.Protection = v);
            AddToggle(GetString("Opt_NoGameEnd"), "NoGameEnd", () => BanMod.NoGameEnd, (v) => BanMod.NoGameEnd = v);
            AddToggle(GetString("Opt_AutoBan"), "AddBanToList", () => BanMod.AddBanToList, (v) => BanMod.AddBanToList = v);
            AddToggle(GetString("Opt_ExcludeFriends"), "ExcludeFriends", () => BanMod.ExcludeFriends, (v) => BanMod.ExcludeFriends = v);
            AddToggle(GetString("Opt_ShareLobby"), "ShareLobbyCode", () => BanMod.sharelobby, (v) => BanMod.sharelobby = v);
            AddToggle(GetString("Opt_NoKillMeeting"), "NoKillMeeting", () => BanMod.NoKillMeeting, (v) => BanMod.NoKillMeeting = v);
            AddToggle(GetString("Opt_ChatLeft"), "ChatLeft", () => BanMod.ChatLeft, (v) => BanMod.ChatLeft = v);
            AddToggle(GetString("Opt_ChangeColor"), "AllowColorChangeAll", () => BanMod.changecolor, (v) => BanMod.changecolor = v);
            AddToggle(GetString("Opt_ChangeColor1"), "AllowColorChangeModerator", () => BanMod.changecolor1, (v) => BanMod.changecolor1 = v);
            AddToggle(GetString("Opt_InfoLobby"), "InfoLobby", () => BanMod.InfoLobby, (v) => BanMod.InfoLobby = v);
            AddToggle(GetString("Opt_DisableRole"), "DisableRole", () => BanMod.DisableRole, (v) => { BanMod.DisableRole = v; MyDisableRoleFunction(v);});
            AddToggle(GetString("Opt_ExtendLobby"), "extendlobby", () => BanMod.extendlobby, (v) => BanMod.extendlobby = v);
            AddToggle(GetString("Opt_VoteLockEnabled"), "VoteLockEnabled", () => BanMod.VoteLockEnabled, (v) => BanMod.VoteLockEnabled = v);
            AddToggle(GetString("Opt_DisableMeetingsAndReports"), "DisableMeetingsAndReports", () => BanMod.DisableMeetingsAndReports, (v) => BanMod.DisableMeetingsAndReports = v);

            menuItems.Add(new MenuItem
            {
                Label = GetString("Opt_Unload"),
                Type = ItemType.Button,
                OnClick = () => { Harmony.UnpatchID("com.GianniBart.BanMod"); BanMod.Instance.Unload(); showMenu = false; }
            });
        }
        [HideFromIl2Cpp]
        private void AddToggle(string label, string internalName, Func<bool> getter, Action<bool> setter)
        {
            menuItems.Add(new MenuItem { Label = label, InternalName = internalName, Type = ItemType.Toggle, ToggleValue = getter(), OnClick = () => { bool newValue = !getter(); setter(newValue); var item = menuItems.Find(x => x.InternalName == internalName); if (item != null) item.ToggleValue = newValue; SaveSettings(); } });
        }
        private void MyDisableRoleFunction(bool enabled)
        {
            Debug.Log("DisableRole cambiato: " + enabled);

            if (enabled)
            {
                BanMod.DisableAllRoles();
            }
        }
        public void SaveSettings()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (var item in menuItems)
                    if (item.Type == ItemType.Toggle)
                        lines.Add($"{item.InternalName}:{item.ToggleValue}");
                File.WriteAllLines(configPath, lines);
            }
            catch (Exception e) { Debug.LogError(e.Message); }
        }

        public void LoadSettings()
        {
            if (!File.Exists(configPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string internalName = parts[0];
                        bool val = bool.Parse(parts[1]);

                        ApplyValueToVariable(internalName, val);

                        var item = menuItems.Find(x => x.InternalName == internalName);
                        if (item != null)
                        {
                            item.ToggleValue = val;
                        }
                    }
                }
                BMLogger.LogInfo("[BanMod] Settings synchronized from file.");
            }
            catch (Exception e) { Debug.LogError("Error loading settings: " + e.Message); }
        }

        private void ApplyValueToVariable(string name, bool value)
        {
            if (name == "nocountdown") BanMod.nocountdown = value;
            if (name == "GM") BanMod.GM = value;
            if (name == "Teleport") BanMod.Teleport = value;
            if (name == "randomMap") BanMod.randomMap = value;
            if (name == "Protection") BanMod.Protection = value;
            if (name == "NoGameEnd") BanMod.NoGameEnd = value;
            if (name == "AddBanToList") BanMod.AddBanToList = value;
            if (name == "ExcludeFriends") BanMod.ExcludeFriends = value;
            if (name == "ShareLobbyCode") BanMod.sharelobby = value;
            if (name == "NoKillMeeting") BanMod.NoKillMeeting = value;
            if (name == "ChatLeft") BanMod.ChatLeft = value;
            if (name == "AllowColorChangeAll") BanMod.changecolor = value;
            if (name == "AllowColorChangeModerator") BanMod.changecolor1 = value;
            if (name == "InfoLobby") BanMod.InfoLobby = value;
            if (name == "DisableRole")
            {
                BanMod.DisableRole = value;
                MyDisableRoleFunction(value);
            }
            if (name == "extendlobby") BanMod.extendlobby = value;
            if (name == "VoteLockEnabled") BanMod.VoteLockEnabled = value;
            if (name == "DisableMeetingsAndReports") BanMod.DisableMeetingsAndReports = value;
        }

        private void InitStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = new Color(1f, 1f, 1f, 1f); 

                styleDefault = new GUIStyle(BanModUiStyles.ButtonDark) { alignment = TextAnchor.MiddleCenter };
                styleDefault.normal.textColor = Color.white;
                styleDefault.hover.textColor = Color.white;

                styleOn = new GUIStyle(BanModUiStyles.ToggleOnBlueOutline) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                styleOn.normal.textColor = Color.white;
                styleOn.hover.textColor = Color.white;

                styleOff = new GUIStyle(BanModUiStyles.ToggleOffDark) { alignment = TextAnchor.MiddleCenter };
                styleOff.normal.textColor = Color.white;
                styleOff.hover.textColor = Color.white;
            }
        }

        public void OpenMenu() { showMenu = true; CenterWindow(); }
        public void CloseMenu() { showMenu = false; }
        private void CenterWindow() { windowRect = new Rect(Screen.width / 2 - windowSize.x / 2, Screen.height / 2 - windowSize.y / 2, windowSize.x, windowSize.y); }

        void OnGUI()
        {
            if (!showMenu) return;
            if (Event.current.isMouse)
            {
                Event.current.Use();
            }
            InitStyles();

            GUI.backgroundColor = Color.black;
            windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)DrawWindow, "", BanModUiStyles.BlackWindow);
        }

        void DrawWindow(int id)
        {
            GUILayout.Label(GetString("MainOptionsTitle"), titleStyle);
            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < menuItems.Count; i += 3)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                DrawItem(menuItems[i]);
                GUILayout.Space(10);

                if (i + 1 < menuItems.Count) DrawItem(menuItems[i + 1]);
                else GUILayout.Space((windowSize.x / 3) - 30);

                GUILayout.Space(10);

                if (i + 2 < menuItems.Count) DrawItem(menuItems[i + 2]);
                else GUILayout.Space((windowSize.x / 3) - 30);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();

            GUI.color = new Color(1f, 1f, 1f, 1f);

            GUI.backgroundColor = new Color(0.8f, 0f, 0f, 1f);
            if (GUILayout.Button(GetString("Opt_Close"), GUILayout.Height(40)))
            {
                MenuRouter.Open(MenuRouter.Panel.None);
            }

            GUI.backgroundColor = Color.white;

            GUI.DragWindow();
        }
        [HideFromIl2Cpp]
        void DrawItem(MenuItem item)
        {
            GUI.backgroundColor = new Color(1f, 1f, 1f, 1f);
            GUI.color = new Color(1f, 1f, 1f, 1f);

            float itemWidth = (windowSize.x / 3) - 40;

            if (item.Type == ItemType.Button)
            {
                if (GUILayout.Button(item.Label, styleDefault, GUILayout.Width(itemWidth), GUILayout.Height(45)))
                    item.OnClick?.Invoke();
            }
            else
            {
                GUIStyle currentStyle = item.ToggleValue ? styleOn : styleOff;
                if (GUILayout.Button(item.Label, currentStyle, GUILayout.Width(itemWidth), GUILayout.Height(45)))
                {
                    item.OnClick?.Invoke();
                }
            }
        }
    }
}