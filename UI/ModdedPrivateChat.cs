//credits and licenses in the resources folder
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using static BanMod.Translator;
using Rect = UnityEngine.Rect;

namespace BanMod
{
    public static class ModdedRegistry
    {
        public static HashSet<byte> ModdedPlayers = new HashSet<byte>();

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
        public static class ChatResetPatch
        {
            public static void Postfix()
            {
                ModdedPlayers.Clear();

                if (PremiumChatUI.Instance != null)
                {
                    MenuRouter.Open(MenuRouter.Panel.None);
                }
            }
        }
    }

    public class PremiumChatUI : MonoBehaviour
    {
        public static PremiumChatUI Instance;

        public bool _v;

        private Rect _wR = new Rect(10, 10, 660, 560);
        private GUIStyle _winSt, _menuBtnStyle, _miniTitleStyle;
        private Texture2D _bgTex;
        private bool _didInitPos = false;

        private bool _showPlaylistInline = false;
        private Vector2 _playlistScroll = Vector2.zero;

        void Awake() => Instance = this;

        public static void ToggleUI()
        {
            if (Instance == null) return;
            if (Instance._v) Instance.CloseUI();
            else Instance.OpenUI();
        }

        private Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        public void OpenUI()
        {
            _v = true;
            if (!_didInitPos)
            {
                _wR.x = 10;
                _wR.y = 10;
                _didInitPos = true;
            }
        }

        public void CloseUI()
        {
            _v = false;
            MenuRouter.Open(MenuRouter.Panel.None);
        }

        void OnGUI()
        {
            if (BanMod.IsBanModDisabled) return;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && !BanMod.chatOpen)
            {
                if (_v) CloseUI();
                else OpenUI();

                Event.current.Use();
            }

            if (!_v) return;

            EnsureStyles();

            _wR.x = Mathf.Clamp(_wR.x, 0, Screen.width - _wR.width);
            _wR.y = Mathf.Clamp(_wR.y, 0, Screen.height - _wR.height);

            ApplyAutoMinHeight();

            _wR = GUI.Window(8181, _wR, (GUI.WindowFunction)_dW, "BanMod by GianniBart", _winSt);
        }

        private void EnsureStyles()
        {
            if (_winSt != null) return;

            _bgTex = MakeTex(Color.black);

            _winSt = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _bgTex },
                onNormal = { background = _bgTex },
                hover = { background = _bgTex },
                onHover = { background = _bgTex },
                active = { background = _bgTex },
                onActive = { background = _bgTex },
                focused = { background = _bgTex },
                onFocused = { background = _bgTex }
            };

            _menuBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _miniTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        private void ApplyAutoMinHeight()
        {
            float menuH = 5f * 40f + 40f + 22f;
            float musicH = 145f + (_showPlaylistInline ? 180f : 0f);
            float need = 100f + menuH + musicH;

            if (_wR.height < need)
                _wR.height = Mathf.Min(need, Screen.height - 10f);
        }

        private void _dW(int id)
        {
            GUI.DragWindow(new Rect(0, 0, _wR.width, 30));
            GUILayout.BeginArea(new Rect(10, 30, _wR.width - 20, _wR.height - 40));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28), GUILayout.Height(24)))
                CloseUI();
            GUILayout.EndHorizontal();

            DrawMenuGrid();
            GUILayout.Space(6);

            DrawInlineMusicPlayer();
            GUILayout.Space(6);

            DrawResetSaveDataButton();

            GUILayout.EndArea();
        }
        private void DrawResetSaveDataButton()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(
                "If you experience errors or glitches, try resetting the saved files.\nWARNING, THIS MAY DELETE SAVED SETTINGS",
                _miniTitleStyle
            );

            GUILayout.Space(4);

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.75f, 0.05f, 1f); 

            if (GUILayout.Button("Reset Save Data", _menuBtnStyle, GUILayout.Height(34f)))
            {
                OptionSaver.ResetSaveData();
            }

            GUI.backgroundColor = oldBg;

            GUILayout.EndVertical();
        }
        private void DrawMenuGrid()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            float colW = (_wR.width - 50f) / 2f;

            DrawMenuRow(GetString("HostControlMenu"), MenuRouter.Panel.Host, GetString("ModeratorControlMenu"), MenuRouter.Panel.Moderator, colW, 36f);
            DrawMenuRow(GetString("MENU_MSG"), MenuRouter.Panel.MsgMenu, GetString("PresetMenu"), MenuRouter.Panel.Presets, colW, 36f);
            DrawMenuRow(GetString("TASK_PLAYERS"), MenuRouter.Panel.PlayerTasks, GetString("Menu_SkinUI"), MenuRouter.Panel.SkinUI, colW, 36f);
            DrawMenuRow(GetString("VisualOptionsTitle"), MenuRouter.Panel.VisualOptions, GetString("Menu_PlayerUI"), MenuRouter.Panel.PlayerUI, colW, 36f);

            DrawReportTranslateRow(colW, 36f);

            if (GUILayout.Button(GetString("Key_Title"), _menuBtnStyle, GUILayout.Width(colW * 2f + 10f), GUILayout.Height(36f)))
                MenuRouter.Toggle(MenuRouter.Panel.Keybinds);

            GUILayout.EndVertical();
        }
        private void DrawReportTranslateRow(float w, float h)
        {
            GUILayout.BeginHorizontal();

            Color oldBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.9f, 0.05f, 0.05f, 1f);
            if (GUILayout.Button("Report", _menuBtnStyle, GUILayout.Width(w), GUILayout.Height(h)))
                BanModCommunicationUi.Instance.ToggleMenu();

            GUILayout.Space(10);

            GUI.backgroundColor = new Color(0.05f, 0.75f, 0.15f, 1f);
            if (GUILayout.Button("Translate", _menuBtnStyle, GUILayout.Width(w), GUILayout.Height(h)))
                EmulateLiveTranslatorCtrlT();

            GUI.backgroundColor = oldBg;

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint INPUT_HARDWARE = 2;

        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_T = 0x54;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint nInputs,
            INPUT[] pInputs,
            int cbSize
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private static void EmulateLiveTranslatorCtrlT()
        {
            INPUT[] inputs =
            {
        // CTRL down
        new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    dwFlags = 0
                }
            }
        },

        // T down
        new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_T,
                    dwFlags = 0
                }
            }
        },

        // T up
        new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_T,
                    dwFlags = KEYEVENTF_KEYUP
                }
            }
        },

        // CTRL up
        new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = VK_CONTROL,
                    dwFlags = KEYEVENTF_KEYUP
                }
            }
        }
    };

            uint sent = SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf(typeof(INPUT))
            );

            if (sent != inputs.Length)
            {
                try
                {
                    UnityEngine.Debug.LogWarning(
                        "[BanMod] Emulazione CTRL+T incompleta. Eventi inviati: "
                        + sent + "/" + inputs.Length
                    );
                }
                catch { }
            }
        }
        private void DrawMenuRow(string l, MenuRouter.Panel lp, string r, MenuRouter.Panel rp, float w, float h)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(l, _menuBtnStyle, GUILayout.Width(w), GUILayout.Height(h)))
                MenuRouter.Toggle(lp);

            GUILayout.Space(10);

            if (GUILayout.Button(r, _menuBtnStyle, GUILayout.Width(w), GUILayout.Height(h)))
                MenuRouter.Toggle(rp);

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawInlineMusicPlayer()
        {
            var mp = CustomMusicPlayer.Instance;

            GUILayout.BeginVertical(GUI.skin.box);

            if (mp == null)
            {
                GUILayout.Label("Music N/A");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=cyan><b>♫</b></color> " + mp.CurrentTrackName, _miniTitleStyle);
            if (GUILayout.Button("Playlist", GUILayout.Width(140), GUILayout.Height(24)))
                _showPlaylistInline = !_showPlaylistInline;
            GUILayout.EndHorizontal();

            float cur = mp.CurrentTime;
            float tot = mp.TotalTime;

            GUI.enabled = false;
            GUILayout.HorizontalSlider(cur, 0f, (tot <= 0f ? 1f : tot));
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", GUILayout.Width(60), GUILayout.Height(26))) mp.PrevTrack();
            if (GUILayout.Button(mp.IsPlaying ? "Pause" : "Play", GUILayout.Width(90), GUILayout.Height(26))) mp.TogglePlayPause();
            if (GUILayout.Button(">>", GUILayout.Width(60), GUILayout.Height(26))) mp.NextTrack();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            mp.Shuffle = GUILayout.Toggle(mp.Shuffle, " Shuffle", mp.Shuffle ? BanModUiStyles.ToggleOnBlueOutline : BanModUiStyles.ToggleOffDark);
            GUILayout.Space(10);
            mp.AutoPlay = GUILayout.Toggle(mp.AutoPlay, " AutoStart", mp.AutoPlay ? BanModUiStyles.ToggleOnBlueOutline : BanModUiStyles.ToggleOffDark);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            mp.Volume = GUILayout.HorizontalSlider(mp.Volume, 0f, 1f, GUILayout.Height(16));
            GUILayout.Label((mp.Volume * 100f).ToString("0") + "%", GUILayout.Width(45));
            GUILayout.EndHorizontal();

            if (_showPlaylistInline)
            {
                _playlistScroll = GUILayout.BeginScrollView(_playlistScroll, GUILayout.Height(160));
                for (int i = 0; i < mp.TrackCount; i++)
                {
                    string n = mp.GetTrackName(i);
                    if (i == mp.CurrentIndex) n = "<color=yellow>▶ " + n + "</color>";
                    if (GUILayout.Button(n, GUI.skin.label)) mp.PlayAtIndex(i);
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }
    }
}
