//credits and licenses in the resources folder
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using static BanMod.Translator;

namespace BanMod
{
    public class KeyBindOptions : MonoBehaviour
    {
        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(750, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private static string configPath = "BAN_DATA/SETTINGS/keybinds_config.txt";

        public static KeyCode K1 = KeyCode.Alpha1, K2 = KeyCode.Alpha2,
            K3 = KeyCode.Alpha3, K4 = KeyCode.Alpha4, K5 = KeyCode.Alpha5,
            K6 = KeyCode.Alpha6, K7 = KeyCode.Alpha7, K8 = KeyCode.Alpha8,
            K9 = KeyCode.Alpha9;

        public static KeyCode K12 = KeyCode.Keypad1, K13 = KeyCode.Keypad2,
            K14 = KeyCode.Keypad3, K15 = KeyCode.Keypad4, K16 = KeyCode.Keypad5,
            K17 = KeyCode.Keypad6, K18 = KeyCode.Keypad7, K19 = KeyCode.Keypad8,
            K20 = KeyCode.Keypad9;

        public static KeyCode KKeybindsMenu = KeyCode.F10;

        private string bindingAction = null;
        private GUIStyle titleStyle, exitButtonStyle, bindButtonStyle, labelStyle, categoryStyle, rowBoxStyle;

        public static KeyBindOptions Instance;
        public static bool IsBindingActive = false;

        private float _leftW;
        private float _rightW;

        void Awake()
        {
            Instance = this;
            LoadConfig();
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
            bool shouldOpen = (p == MenuRouter.Panel.Keybinds);

            if (shouldOpen)
            {
                if (!showMenu)
                {
                    showMenu = true;
                    windowRect = new Rect(
                        Screen.width / 2 - windowSize.x / 2,
                        Screen.height / 2 - windowSize.y / 2,
                        windowSize.x, windowSize.y
                    );
                }
                IsBindingActive = true;
            }
            else
            {
                if (showMenu)
                {
                    showMenu = false;
                    bindingAction = null;
                    IsBindingActive = false;
                    SaveConfig();
                }
            }
        }

        void Update()
        {
            if (BanMod.IsBanModDisabled) return;

            if (bindingAction != null) return;

            if (Input.GetKeyDown(KKeybindsMenu) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.Keybinds)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.Keybinds);
            }

        }

        public void ToggleMenu()
        {
            showMenu = !showMenu;
            if (showMenu)
            {
                windowRect = new Rect(Screen.width / 2 - windowSize.x / 2, Screen.height / 2 - windowSize.y / 2, windowSize.x, windowSize.y);
                IsBindingActive = true;
            }
            else
            {
                bindingAction = null;
                IsBindingActive = false;
                SaveConfig();
            }
        }

        public void CloseMenu()
        {
            if (showMenu)
            {
                showMenu = false;
                bindingAction = null;
                IsBindingActive = false;
                SaveConfig();
            }

            if (MenuRouter.Current == MenuRouter.Panel.Keybinds)
                MenuRouter.Open(MenuRouter.Panel.None);
        }

        public static void SaveConfig()
        {
            try
            {
                string directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                List<string> lines = new List<string>
                {
                    $"KillPlayer={K1}", $"ChangeBody={K2}", $"Ban={K3}", $"Kick={K4}",
                    $"ChangeColor={K5}", $"ToggleZoom={K6}", $"LobbyAction={K7}", $"Sabotage={K8}",
                    $"Task={K9}",

                    $"HostOpt={K12}", $"MsgOpt={K13}", $"ModOpt={K14}",
                    $"ModPreset={K15}", $"PlayerOpt={K16}", $"SkinOpt={K17}",
                    $"VisualOpt={K18}", $"TaskOpt={K19}", $"MusicOpt={K20}",

                    $"KeyBind_setting={KKeybindsMenu}"
                };

                File.WriteAllLines(configPath, lines);
            }
            catch (Exception) { }
        }

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string[] lines = File.ReadAllLines(configPath);

                foreach (string line in lines)
                {
                    string[] split = line.Split('=');
                    if (split.Length != 2) continue;
                    string key = split[0].Trim();
                    string value = split[1].Trim();

                    if (!Enum.IsDefined(typeof(KeyCode), value)) continue;

                    KeyCode parsedKey = (KeyCode)Enum.Parse(typeof(KeyCode), value);
                    switch (key)
                    {
                        case "KillPlayer": K1 = parsedKey; break;
                        case "ChangeBody": K2 = parsedKey; break;
                        case "Ban": K3 = parsedKey; break;
                        case "Kick": K4 = parsedKey; break;
                        case "ChangeColor": K5 = parsedKey; break;
                        case "ToggleZoom": K6 = parsedKey; break;
                        case "LobbyAction": K7 = parsedKey; break;
                        case "Sabotage": K8 = parsedKey; break;
                        case "Task": K9 = parsedKey; break;

                        case "HostOpt": K12 = parsedKey; break;
                        case "MsgOpt": K13 = parsedKey; break;
                        case "ModOpt": K14 = parsedKey; break;
                        case "ModPreset": K15 = parsedKey; break;
                        case "PlayerOpt": K16 = parsedKey; break;
                        case "SkinOpt": K17 = parsedKey; break;
                        case "VisualOpt": K18 = parsedKey; break;
                        case "TaskOpt": K19 = parsedKey; break;
                        case "MusicOpt": K20 = parsedKey; break;

                        case "KeyBind_setting": KKeybindsMenu = parsedKey; break;
                    }
                }
            }
            catch (Exception) { }
        }

        void OnGUI()
        {
            if (!showMenu) return;
            if (Event.current.isMouse) Event.current.Use();
            EnsureStyles();

            if (bindingAction != null)
            {
                Event e = Event.current;

                if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                    e.Use();

                if (e.type == EventType.KeyDown)
                {
                    KeyCode key = e.keyCode;
                    if (key == KeyCode.None)
                        key = CharToKeyCode(e.character);

                    if (IsKeyAllowedForBinding(key))
                    {
                        SetNewKeybind(bindingAction, key);
                    }

                    bindingAction = null;
                    e.Use();
                }
            }

            GUI.backgroundColor = Color.black;
            windowRect = GUI.Window(3, windowRect, (GUI.WindowFunction)WindowFunction, "", BanModUiStyles.BlackWindow);
        }

        private bool IsKeyAllowedForBinding(KeyCode key)
        {
            if (key == KeyCode.None) return false;

            if (key == KeyCode.Delete) return false;
            if (key == KeyCode.Escape) return false;

            if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2 ||
                key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 || key == KeyCode.Mouse6)
                return false;

            return true;
        }

        KeyCode CharToKeyCode(char c)
        {
            c = char.ToLowerInvariant(c);

            switch (c)
            {
                case '/': return KeyCode.Slash;
                case '\\': return KeyCode.Backslash;
                case '*': return KeyCode.Asterisk;
                case '+': return KeyCode.Plus;
                case '-': return KeyCode.Minus;
                case '=': return KeyCode.Equals;
                case '.': return KeyCode.Period;
                case ',': return KeyCode.Comma;
                case ';': return KeyCode.Semicolon;
                case ':': return KeyCode.Semicolon;
                case '\'': return KeyCode.Quote;
                case '"': return KeyCode.Quote;
                case '`': return KeyCode.BackQuote;
                case 'ù': return KeyCode.Semicolon;
                case 'è': return KeyCode.LeftBracket;
                case 'é': return KeyCode.LeftBracket;
                case 'ò': return KeyCode.Quote;
                case 'à': return KeyCode.BackQuote;
                case 'ì': return KeyCode.RightBracket;
                case 'ß': return KeyCode.Minus;
                case 'ü': return KeyCode.LeftBracket;
                case 'ö': return KeyCode.Semicolon;
                case 'ä': return KeyCode.Quote;
                case 'ç': return KeyCode.Alpha9;
                case 'ñ': return KeyCode.Semicolon;
                case '0': return KeyCode.Alpha0;
                case '1': return KeyCode.Alpha1;
                case '2': return KeyCode.Alpha2;
                case '3': return KeyCode.Alpha3;
                case '4': return KeyCode.Alpha4;
                case '5': return KeyCode.Alpha5;
                case '6': return KeyCode.Alpha6;
                case '7': return KeyCode.Alpha7;
                case '8': return KeyCode.Alpha8;
                case '9': return KeyCode.Alpha9;
            }

            return KeyCode.None;
        }

        void WindowFunction(int id)
        {
            float innerW = windowSize.x - 40f;
            _leftW = innerW * 0.66f;
            _rightW = innerW * 0.30f;

            GUILayout.Label(GetString("Key_Title"), titleStyle);
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawKeybindList();

            GUILayout.EndScrollView();

            GUI.backgroundColor = new Color(0.8f, 0f, 0f, 1f);
            if (GUILayout.Button(GetString("Key_SaveClose"), exitButtonStyle, GUILayout.Height(45)))
            {
                CloseMenu();
            }
            GUI.backgroundColor = Color.white;
            GUI.DragWindow();
        }

        void DrawKeybindList()
        {
            DrawCategory(GetString("Key_Cat_Actions"));
            DrawRow(GetString("KillPlayerAction"), K1, "KillPlayer");
            DrawRow(GetString("Key_Body"), K2, "ChangeBody");
            DrawRow(GetString("BanPlayerAction"), K3, "Ban");
            DrawRow(GetString("KickPlayerAction"), K4, "Kick");
            DrawRow(GetString("Key_Color"), K5, "ChangeColor");
            DrawRow(GetString("ToggleZoomAction"), K6, "ToggleZoom");
            DrawRow(GetString("Key_Lobby"), K7, "LobbyAction");
            DrawRow(GetString("Key_Sabo"), K8, "Sabotage");
            DrawRow(GetString("Key_Task"), K9, "Task");

            GUILayout.Space(14);

            DrawCategory(GetString("Key_Cat_Menus"));
            DrawRow(GetString("HostControlMenu"), K12, "HostOpt");
            DrawRow(GetString("Key_MsgOpt"), K13, "MsgOpt");
            DrawRow(GetString("ModeratorControlMenu"), K14, "ModOpt");
            DrawRow(GetString("PresetMenu"), K15, "ModPreset");
            DrawRow(GetString("Key_PlayerOpt"), K16, "PlayerOpt");
            DrawRow(GetString("Key_SkinOpt"), K17, "SkinOpt");
            DrawRow(GetString("VisualOptionsTitle"), K18, "VisualOpt");
            DrawRow(GetString("Key_TaskOpt"), K19, "TaskOpt");
            DrawRow(GetString("MusicOpt"), K20, "MusicOpt");
            DrawRow(GetString("Key_KeyBind"), KKeybindsMenu, "KeyBind_setting");

            GUILayout.Space(14);
        }

        void DrawCategory(string title)
        {
            GUILayout.BeginVertical(rowBoxStyle);
            GUILayout.Label(title, categoryStyle);
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        void DrawRow(string name, KeyCode currentKey, string actionId)
        {
            bool isBinding = (bindingAction == actionId);

            string buttonText = isBinding
                ? GetString("Key_Waiting")
                : (currentKey == KeyCode.None ? GetString("Key_None") : currentKey.ToString().ToUpper());

            GUILayout.BeginVertical(rowBoxStyle);
            GUILayout.BeginHorizontal();

            GUILayout.Label(name, labelStyle, GUILayout.Width(_leftW));

            if (isBinding) GUI.backgroundColor = Color.yellow;

            if (GUILayout.Button(buttonText, bindButtonStyle, GUILayout.Width(_rightW), GUILayout.Height(34)))
            {
                bindingAction = actionId;
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawReadOnly(string name, string value)
        {
            GUILayout.BeginVertical(rowBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, labelStyle, GUILayout.Width(_leftW));
            GUI.enabled = false;
            GUILayout.Button(value, bindButtonStyle, GUILayout.Width(_rightW), GUILayout.Height(34));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void SetNewKeybind(string actionID, KeyCode newKey)
        {
            CheckAndClearDuplicate(newKey);

            switch (actionID)
            {
                case "KillPlayer": K1 = newKey; break;
                case "ChangeBody": K2 = newKey; break;
                case "Ban": K3 = newKey; break;
                case "Kick": K4 = newKey; break;
                case "ChangeColor": K5 = newKey; break;
                case "ToggleZoom": K6 = newKey; break;
                case "LobbyAction": K7 = newKey; break;
                case "Sabotage": K8 = newKey; break;
                case "Task": K9 = newKey; break;

                case "HostOpt": K12 = newKey; break;
                case "MsgOpt": K13 = newKey; break;
                case "ModOpt": K14 = newKey; break;
                case "ModPreset": K15 = newKey; break;
                case "PlayerOpt": K16 = newKey; break;
                case "SkinOpt": K17 = newKey; break;
                case "VisualOpt": K18 = newKey; break;
                case "TaskOpt": K19 = newKey; break;
                case "MusicOpt": K20 = newKey; break;

                case "KeyBind_setting": KKeybindsMenu = newKey; break;
            }

            SaveConfig();
        }

        void CheckAndClearDuplicate(KeyCode key)
        {
            if (K1 == key) K1 = KeyCode.None; if (K2 == key) K2 = KeyCode.None;
            if (K3 == key) K3 = KeyCode.None; if (K4 == key) K4 = KeyCode.None;
            if (K5 == key) K5 = KeyCode.None; if (K6 == key) K6 = KeyCode.None;
            if (K7 == key) K7 = KeyCode.None; if (K8 == key) K8 = KeyCode.None;
            if (K9 == key) K9 = KeyCode.None;

            if (K12 == key) K12 = KeyCode.None; if (K13 == key) K13 = KeyCode.None;
            if (K14 == key) K14 = KeyCode.None; if (K15 == key) K15 = KeyCode.None;
            if (K16 == key) K16 = KeyCode.None; if (K17 == key) K17 = KeyCode.None;
            if (K18 == key) K18 = KeyCode.None; if (K19 == key) K19 = KeyCode.None;
            if (K20 == key) K20 = KeyCode.None;
            if (KKeybindsMenu == key) KKeybindsMenu = KeyCode.None;
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            categoryStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            bindButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            exitButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            titleStyle.normal.textColor = Color.white;
            categoryStyle.normal.textColor = Color.cyan;
            labelStyle.normal.textColor = Color.white;

            rowBoxStyle = new GUIStyle(GUI.skin.box);
            rowBoxStyle.padding = new RectOffset();
            rowBoxStyle.padding.left = 10;
            rowBoxStyle.padding.right = 10;
            rowBoxStyle.padding.top = 10;
            rowBoxStyle.padding.bottom = 10;
        }
    }
}