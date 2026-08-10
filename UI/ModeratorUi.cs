//credits and licenses in the resources folder
using UnityEngine;
using System.Collections.Generic;
using BanMod;
using System;
using Il2CppInterop.Runtime.Attributes;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public class ModeratorUi : MonoBehaviour
    {
        public static ModeratorUi Instance;

        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(750, 600);
        private Vector2 scrollPosition = Vector2.zero;

        private bool selectingPlayer = false;
        private bool selectingColor = false;

        // Per le azioni RPC non usiamo più stringhe/commandi chat.
        private ModeratorAction pendingPlayerAction;
        private ModeratorAction pendingColorAction;

        public enum ItemType { Button, Header }

        public class MenuItem
        {
            public string Label;
            public ItemType Type;
            public Action OnClick;
        }

        private List<MenuItem> menuItems = new();
        private GUIStyle titleStyle, headerStyle, buttonStyle, exitButtonStyle;

        private const int ButtonsPerRow = 3;
        private const float ButtonHeight = 45f;
        private const float ButtonSpacing = 10f;

        void Awake() => Instance = this;

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
            showMenu = (p == MenuRouter.Panel.Moderator);
            if (showMenu)
            {
                SetupMenuContent();
                selectingPlayer = false;
                selectingColor = false;
                CenterWindow();
            }
        }

        void Update()
        {
            var player = PlayerControl.LocalPlayer;
            if (player?.Data == null) return;

            if (KeyBindOptions.IsBindingActive) return;

            if (Input.GetKeyDown(KeyBindOptions.K14) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.Moderator)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.Moderator);
            }
        }

        public void SetupMenuContent()
        {
            menuItems.Clear();

            menuItems.Add(new MenuItem { Label = GetString("Mod_Header_Game"), Type = ItemType.Header });

            menuItems.Add(new MenuItem
            {
                Label = GetString("Mod_Priv_Pub"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.TogglePublicPrivate)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("Mod_Btn_NormalStart"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.StartGame)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("InstantStartAction"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.InstantStart)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("CallMeetingAction"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.CallMeeting)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("Mod_Btn_EndMeeting"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.EndMeeting)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("Mod_Btn_EndGame"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.EndGame)
            });

            menuItems.Add(new MenuItem { Label = GetString("Mod_Header_Players"), Type = ItemType.Header });

            // Mantengo la schermata di selezione colore originale:
            // il colore viene usato SOLO per individuare il player,
            // poi all'host viene inviato il PlayerId reale.
            menuItems.Add(new MenuItem
            {
                Label = GetString("KickPlayerAction"),
                Type = ItemType.Button,
                OnClick = () => StartColorSelection(ModeratorAction.Kick)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("BanPlayerAction"),
                Type = ItemType.Button,
                OnClick = () => StartColorSelection(ModeratorAction.Ban)
            });

            // L'azione colore del nuovo sistema lavora sul player selezionato
            // e usa la stessa RandomFreeColor già prevista da ModeratorAuthority.
            menuItems.Add(new MenuItem
            {
                Label = GetString("Mod_Btn_Color"),
                Type = ItemType.Button,
                OnClick = () => StartPlayerSelection(ModeratorAction.RandomFreeColor)
            });

            // Funzioni locali originali: restano invariate e non passano dalla chat.
            menuItems.Add(new MenuItem
            {
                Label = "Copy_Outfit",
                Type = ItemType.Button,
                OnClick = () => StartCopySelection()
            });

            menuItems.Add(new MenuItem
            {
                Label = "Reset_Outfit",
                Type = ItemType.Button,
                OnClick = () => Utils.RestoreOriginalOutfit(PlayerControl.LocalPlayer)
            });

            menuItems.Add(new MenuItem { Label = GetString("Mod_Header_Lobby"), Type = ItemType.Header });

            menuItems.Add(new MenuItem
            {
                Label = GetString("DestroyLobbyAction"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.DestroyLobby)
            });

            menuItems.Add(new MenuItem
            {
                Label = GetString("RecreateLobbyAction"),
                Type = ItemType.Button,
                OnClick = () => ModeratorAuthority.Request(ModeratorAction.SpawnLobby)
            });
        }

        public void OpenMenu()
        {
            showMenu = true;
            selectingPlayer = false;
            selectingColor = false;
            CenterWindow();
        }

        public void CloseMenu() => showMenu = false;

        void CenterWindow()
        {
            windowRect = new Rect(
                Screen.width / 2 - windowSize.x / 2,
                Screen.height / 2 - windowSize.y / 2,
                windowSize.x,
                windowSize.y
            );
        }

        void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            headerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle ??= new GUIStyle(GUI.skin.button);
            exitButtonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (!showMenu) return;
            EnsureStyles();

            GUI.backgroundColor = Color.black;
            windowRect = GUI.Window(2, windowRect, (GUI.WindowFunction)DrawWindow, "", BanModUiStyles.BlackWindow);
        }

        void DrawWindow(int id)
        {
            string title =
                selectingColor ? GetString("Mod_SelectColor") :
                selectingPlayer ? GetString("Mod_SelectTarget") :
                GetString("ModeratorControlMenu");

            GUILayout.Label(title, titleStyle, GUILayout.Height(40));
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (selectingColor)
                DrawColorSelection();
            else if (selectingPlayer)
                DrawPlayerSelection();
            else
                DrawMainMenu();

            GUILayout.EndScrollView();

            if (selectingPlayer || selectingColor)
            {
                if (GUILayout.Button(GetString("PreviousPage"), GUILayout.Height(40)))
                {
                    selectingPlayer = false;
                    selectingColor = false;
                }
            }

            GUI.backgroundColor = new Color(0.8f, 0f, 0f);
            if (GUILayout.Button(GetString("ExitButton"), exitButtonStyle, GUILayout.Height(45)))
                MenuRouter.Open(MenuRouter.Panel.None);

            GUI.backgroundColor = Color.white;
            GUI.DragWindow();
        }

        float GetButtonWidth()
        {
            float contentWidth = windowRect.width - 40f;
            float totalSpacing = ButtonSpacing * (ButtonsPerRow - 1);
            return (contentWidth - totalSpacing) / ButtonsPerRow;
        }

        void DrawMainMenu()
        {
            float btnWidth = GetButtonWidth();
            List<MenuItem> group = new();

            foreach (var item in menuItems)
            {
                if (item.Type == ItemType.Header)
                {
                    FlushButtons(group, btnWidth);
                    group.Clear();

                    GUILayout.Space(10);
                    GUILayout.Label(item.Label, headerStyle);
                    GUILayout.Space(6);
                }
                else
                {
                    group.Add(item);
                }
            }

            FlushButtons(group, btnWidth);
        }

        void DrawColorSelection()
        {
            float btnWidth = GetButtonWidth();

            for (int i = 0; i <= 18; i += ButtonsPerRow)
            {
                GUILayout.BeginHorizontal();

                for (int c = 0; c < ButtonsPerRow; c++)
                {
                    int colorId = i + c;

                    if (colorId <= 18)
                    {
                        string label = ColorIdToName(colorId);

                        if (GUILayout.Button(label, buttonStyle, GUILayout.Width(btnWidth), GUILayout.Height(ButtonHeight)))
                        {
                            // La selezione per colore rimane identica lato UI,
                            // ma NON viene più scritto "/kick red" o "/ban blue" in chat.
                            PlayerControl target = null;

                            foreach (var p in PlayerControl.AllPlayerControls)
                            {
                                if (p?.Data?.DefaultOutfit == null || p.Data.Disconnected)
                                    continue;

                                if (p.Data.DefaultOutfit.ColorId == colorId)
                                {
                                    target = p;
                                    break;
                                }
                            }

                            if (target != null)
                            {
                                ModeratorAuthority.Request(
                                    pendingColorAction,
                                    target.PlayerId
                                );
                            }

                            selectingColor = false;
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(ButtonSpacing);
            }
        }

        void DrawPlayerSelection()
        {
            float btnWidth = GetButtonWidth();
            var players = new List<PlayerControl>();

            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p?.Data != null && p.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    players.Add(p);
            }

            for (int i = 0; i < players.Count; i += ButtonsPerRow)
            {
                GUILayout.BeginHorizontal();

                for (int c = 0; c < ButtonsPerRow; c++)
                {
                    int idx = i + c;

                    if (idx < players.Count)
                    {
                        var p = players[idx];

                        if (GUILayout.Button(p.Data.PlayerName, buttonStyle, GUILayout.Width(btnWidth), GUILayout.Height(ButtonHeight)))
                        {
                            if (_copySelection)
                            {
                                Utils.SaveOriginalOutfit(PlayerControl.LocalPlayer);
                                Utils.CopyOutfit(p, PlayerControl.LocalPlayer);
                                _copySelection = false;
                            }
                            else
                            {
                                ModeratorAuthority.Request(
                                    pendingPlayerAction,
                                    p.PlayerId
                                );
                            }

                            selectingPlayer = false;
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(ButtonSpacing);
            }
        }

        [HideFromIl2Cpp]
        void FlushButtons(List<MenuItem> items, float width)
        {
            for (int i = 0; i < items.Count; i += ButtonsPerRow)
            {
                GUILayout.BeginHorizontal();

                for (int c = 0; c < ButtonsPerRow; c++)
                {
                    int idx = i + c;

                    if (idx < items.Count)
                    {
                        if (GUILayout.Button(items[idx].Label, buttonStyle, GUILayout.Width(width), GUILayout.Height(ButtonHeight)))
                            items[idx].OnClick?.Invoke();
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(ButtonSpacing);
            }
        }

        private bool _copySelection = false;

        void StartPlayerSelection(ModeratorAction action)
        {
            pendingPlayerAction = action;
            _copySelection = false;
            selectingPlayer = true;
            selectingColor = false;
        }

        void StartCopySelection()
        {
            _copySelection = true;
            selectingPlayer = true;
            selectingColor = false;
        }

        void StartColorSelection(ModeratorAction action)
        {
            pendingColorAction = action;
            _copySelection = false;
            selectingColor = true;
            selectingPlayer = false;
        }
    }
}
