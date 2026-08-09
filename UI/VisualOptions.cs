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
    public class VisualOptions : MonoBehaviour
    {
        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(750, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private static string configPath = "BAN_DATA/SETTINGS/VisualOptions_config.txt";

        public enum ItemType { Button, Toggle }

        private class MenuItem
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

        public static VisualOptions Instance;
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
            showMenu = (p == MenuRouter.Panel.VisualOptions);
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
            if (Input.GetKeyDown(KeyBindOptions.K18) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.VisualOptions)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.VisualOptions);
            }
        }

        public void SetupMenuContent()
        {
            menuItems.Clear();
            AddToggle(GetString("Vis_CustomNames"), "UseCustomNames", () => BanMod.UseCustomNames, v => BanMod.UseCustomNames = v);
            AddToggle(GetString("Vis_ShowInfo"), "ShowInfo", () => BanMod.ShowInfo, v => BanMod.ShowInfo = v);
            AddToggle(GetString("Vis_NoName"), "ShowNoName", () => BanMod.ShowNoName, v => BanMod.ShowNoName = v);
            AddToggle(GetString("Vis_VipModTag"), "ShowVipModTag", () => BanMod.ShowVipModTag, v => BanMod.ShowVipModTag = v);
            AddToggle(GetString("Vis_ColorName"), "ShowColorName", () => BanMod.ShowColorName, v => BanMod.ShowColorName = v);
            AddToggle(GetString("Vis_ShowLevel"), "level", () => BanMod.level, v => BanMod.level = v);
            AddToggle(GetString("Vis_TaskProgress"), "Taskremain", () => BanMod.Taskremain, v => BanMod.Taskremain = v);

        }
        [HideFromIl2Cpp]
        private void AddToggle(string label, string internalName, Func<bool> getter, Action<bool> setter)
        {
            menuItems.Add(new MenuItem
            {
                Label = label,
                InternalName = internalName,
                Type = ItemType.Toggle,
                ToggleValue = getter(),
                OnClick = () =>
                {
                    bool newValue = !getter();
                    setter(newValue);

                    var item = menuItems.Find(x => x.InternalName == internalName);
                    if (item != null)
                        item.ToggleValue = newValue;

                    if (internalName == "ActiveLobbyDecorations")
                        Utils.ResetLobby();

                    SaveSettings();
                }
            });
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
                        if (bool.TryParse(parts[1], out bool val))
                        {
                            ApplyValueToVariable(internalName, val);

                            var item = menuItems.Find(x => x.InternalName == internalName);
                            if (item != null)
                            {
                                item.ToggleValue = val;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild) Debug.LogError("[VisualOptions] Error loading: " + e.Message);
            }
        }

        private void ApplyValueToVariable(string name, bool value)
        {
            if (name == "UseCustomNames") BanMod.UseCustomNames = value;
            if (name == "ShowInfo") BanMod.ShowInfo = value;
            if (name == "ShowVipModTag") BanMod.ShowVipModTag = value;
            if (name == "ShowIdInMeeting") BanMod.namewithid = value;
            if (name == "level") BanMod.level = value;
            if (name == "Taskremain") BanMod.Taskremain = value;
        }

        private void UpdateMenuItemVisual(string internalName, bool value)
        {
            var item = menuItems.Find(x => x.InternalName == internalName);
            if (item != null)
            {
                item.ToggleValue = value;
            }
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
            GUILayout.Label(GetString("VisualOptionsTitle"), titleStyle);
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
            if (GUILayout.Button(GetString("ExitButton"), GUILayout.Height(42)))
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
