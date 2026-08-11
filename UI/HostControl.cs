//credits and licenses in the resources folder
using UnityEngine;
using System.Collections.Generic;
using static BanMod.Translator;

namespace BanMod
{
    public class HostControl : MonoBehaviour
    {
        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(750, 600); 
        private Vector2 scrollPosition = Vector2.zero;

        public enum ItemType { Button, Toggle }

        public class MenuItem
        {
            public string Label;
            public ItemType Type;
            public bool ToggleValue;
            public System.Action OnClick;
        }

        private List<MenuItem> menuItems = new List<MenuItem>();
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle exitButtonStyle;

        public static HostControl Instance;
        void Awake()
        {
            Instance = this;
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
            showMenu = (p == MenuRouter.Panel.Host);
            if (showMenu)
            {
                SetupMenuContent();
                CenterWindow();
            }
        }

        void Update()
        {
            if (BanMod.IsBanModDisabled) return;

            if (KeyBindOptions.IsBindingActive) return;

            if (Input.GetKeyDown(KeyBindOptions.K12) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.Host)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.Host);
            }
        }

        public void SetupMenuContent()
        {
            menuItems.Clear();
            menuItems.Add(new MenuItem { Label = GetString("Host_Start"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/start", new string[] { "/start" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("InstantStartAction"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/instantstart", new string[] { "/instantstart" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("CallMeetingAction"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/meeting", new string[] { "/meeting" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("Host_SkipMeeting"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/skipmeeting", new string[] { "/skipmeeting" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("Host_EndMeeting"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/endmeeting", new string[] { "/endmeeting" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("Host_EndGame"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/endgame", new string[] { "/endgame" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("DestroyLobbyAction"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/destroy", new string[] { "/destroy" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("RecreateLobbyAction"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/lobby", new string[] { "/lobby" }, ""); } });
            menuItems.Add(new MenuItem { Label = GetString("Host_SendInfo"), Type = ItemType.Button, OnClick = () => { ChatCommands.HandleCommand("/m", new string[] { "/m" }, ""); } });
        }

        public void OpenMenu()
        {
            SetupMenuContent();
            showMenu = true;
            CenterWindow();
        }

        public void CloseMenu()
        {
            showMenu = false;
        }

        public bool IsOpen() => showMenu;

        void CenterWindow()
        {
            windowRect = new Rect(Screen.width / 2 - windowSize.x / 2, Screen.height / 2 - windowSize.y / 2, windowSize.x, windowSize.y);
        }

        void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = Color.white;

                buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
                buttonStyle.normal.textColor = Color.white;

                exitButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                exitButtonStyle.normal.textColor = Color.white;
            }
        }

        void OnGUI()
        {
            if (!showMenu) return;
            if (Event.current.isMouse)
            {
                Event.current.Use();
            }
            EnsureStyles();

            GUI.backgroundColor = Color.black;
            windowRect = GUI.Window(4, windowRect, (GUI.WindowFunction)DrawWindow, "", BanModUiStyles.BlackWindow);
        }

        void DrawWindow(int id)
        {
            GUILayout.Label(GetString("HostControlMenu"), titleStyle);
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            float btnWidth = (windowSize.x / 3) - 30;
            for (int i = 0; i < menuItems.Count; i += 3)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int col = 0; col < 3; col++)
                {
                    int index = i + col;
                    if (index < menuItems.Count)
                    {
                        var item = menuItems[index];
                        if (item.Type == ItemType.Button)
                        {
                            if (GUILayout.Button(item.Label, buttonStyle, GUILayout.Width(btnWidth), GUILayout.Height(50)))
                            {
                                item.OnClick?.Invoke();
                            }
                        }
                        else if (item.Type == ItemType.Toggle)
                        {
                            bool newValue = GUILayout.Toggle(item.ToggleValue, item.Label + (item.ToggleValue ? " [ON]" : " [OFF]"), item.ToggleValue ? BanModUiStyles.ToggleOnBlueOutline : BanModUiStyles.ToggleOffDark, GUILayout.Width(btnWidth), GUILayout.Height(50));
                            if (newValue != item.ToggleValue)
                            {
                                item.ToggleValue = newValue;
                                item.OnClick?.Invoke();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Space(btnWidth);
                    }
                    if (col < 2) GUILayout.Space(10);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.8f, 0f, 0f, 1f);
            if (GUILayout.Button(GetString("ExitButton"), exitButtonStyle, GUILayout.Height(50)))
            {
                MenuRouter.Open(MenuRouter.Panel.None);
            }
            GUI.backgroundColor = Color.white; 

            GUI.DragWindow();
        }
    }
}