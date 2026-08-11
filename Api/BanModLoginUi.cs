using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BanMod
{
    /// <summary>
    /// Login UI implemented with Unity IMGUI (OnGUI/GUILayout).
    ///
    /// This deliberately avoids Canvas, EventSystem, UnityEngine.UI.Button,
    /// TMP_InputField and managed UnityAction listeners. The implementation
    /// follows the same interaction path already used by ModeratorUi.
    /// </summary>
    public sealed class BanModLoginUi : MonoBehaviour
    {
        private const int WindowId = 62031;
        private const float WindowWidth = 780f;
        private const float MinWindowHeight = 590f;
        private const float MaxWindowHeight = 920f;

        private static readonly Regex UsernameRegex = new Regex(
            "^[A-Za-z0-9_.-]{3,24}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static BanModLoginUi Instance;
        public static bool IsOpen => Instance != null && Instance._visible;

        private Rect _windowRect;
        private Vector2 _scrollPosition = Vector2.zero;

        private LoginMenuModel _model;
        private string _username = "";
        private string _statusMessage = "";
        private bool _statusIsError;
        private bool _visible;
        private bool _busy;

        private bool _cursorCaptured;
        private bool _oldCursorVisible;
        private CursorLockMode _oldCursorLock;

        private GUIStyle _titleStyle;
        private GUIStyle _descriptionStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _saveButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _lockedUsernameStyle;
        private GUIStyle _usernameInputStyle;
        private GUIStyle _hintStyle;

        public static void EnsureCreated()
        {
            if (Instance != null || BanMod.Instance == null)
                return;

            try
            {
                Instance = BanMod.Instance.AddComponent<BanModLoginUi>();
            }
            catch { }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!_visible)
                return;

            try
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            catch
            {
                // Cursor state is best effort only.
            }

            HandleUsernameKeyboardInput();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _visible = false;
            BanModLoginSubmitBridge.ClearCallback();
            RestoreCursor();
        }

        /// <summary>
        /// Called by BanModLoginRuntimeHost. This signature is IL2CPP-safe.
        /// </summary>
        public void ShowFromJson(string json)
        {
            LoginMenuModel model;
            try
            {
                model = JsonSerializer.Deserialize<LoginMenuModel>(json ?? "{}", JsonOptions);
            }
            catch (Exception ex)
            {
                _statusMessage = "Invalid login menu data: " + ex.Message;
                _statusIsError = true;
                _visible = true;
                CaptureCursor();
                CenterWindow(MinWindowHeight);
                return;
            }

            _model = model ?? new LoginMenuModel();
            if (_model.services == null)
                _model.services = new List<LoginServiceModel>();

            _username = _model.username ?? "";
            _statusMessage = "";
            _statusIsError = false;
            _busy = false;
            _visible = true;
            _scrollPosition = Vector2.zero;

            float height = CalculateWindowHeight(_model.services.Count);
            CenterWindow(height);
            CaptureCursor();

        }

        public void SetStatus(string message, bool isError)
        {
            _statusMessage = message ?? "";
            _statusIsError = isError;

        }

        public void SetBusy(bool busy, string message)
        {
            _busy = busy;
            if (!string.IsNullOrWhiteSpace(message))
            {
                _statusMessage = message;
                _statusIsError = false;
            }
        }

        public void Close()
        {
            _visible = false;
            _busy = false;
            BanModLoginSubmitBridge.ClearCallback();
            RestoreCursor();
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            try
            {
                EnsureStyles();
                GUI.backgroundColor = Color.black;
                _windowRect = GUI.Window(
                    WindowId,
                    _windowRect,
                    (GUI.WindowFunction)DrawWindow,
                    "",
                    BanModUiStyles.BlackWindow);
            }
            catch { }
        }

        private void DrawWindow(int id)
        {
            if (_model == null)
            {
                GUILayout.Label("BANMOD LOGIN", _titleStyle, GUILayout.Height(42f));
                GUILayout.Space(8f);
                GUILayout.Label("Login data is not available.", _statusStyle);
                GUI.DragWindow();
                return;
            }

            GUILayout.Label("BANMOD LOGIN", _titleStyle, GUILayout.Height(42f));
            GUILayout.Space(6f);

            string description = _model.username_required
                ? "Choose a permanent username. It will be linked to your Friend Code and cannot be changed."
                : "Your username is locked. Select the optional modules you want to use.";

            GUILayout.Label(description, _descriptionStyle, GUILayout.Height(48f));
            GUILayout.Space(12f);

            GUILayout.Label("USERNAME", _sectionStyle, GUILayout.Height(28f));

            if (_model.username_required)
            {
                bool showCaret = ((int)(Time.realtimeSinceStartup * 2f) & 1) == 0;
                string shownUsername = string.IsNullOrEmpty(_username)
                    ? "Type your username..."
                    : _username + (showCaret ? "|" : "");

                GUILayout.Label(
                    shownUsername,
                    _usernameInputStyle,
                    GUILayout.Height(44f));

                GUILayout.Label(
                    "Type directly on the keyboard. Backspace deletes. Allowed: letters, numbers, dot, dash and underscore.",
                    _hintStyle,
                    GUILayout.Height(38f));
            }
            else
            {
                GUILayout.Label(
                    string.IsNullOrWhiteSpace(_username) ? "(not available)" : _username,
                    _lockedUsernameStyle,
                    GUILayout.Height(44f));
            }

            GUILayout.Space(14f);
            GUILayout.Label("OPTIONAL MODULES", _sectionStyle, GUILayout.Height(28f));
            GUILayout.Space(4f);

            float servicesHeight = Mathf.Clamp(60f + _model.services.Count * 50f, 90f, 330f);
            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(servicesHeight),
                GUILayout.ExpandWidth(true));

            if (_model.services.Count == 0)
            {
                GUILayout.Space(14f);
                GUILayout.Label(
                    "No optional modules are currently available for this account.",
                    _descriptionStyle,
                    GUILayout.Height(50f));
            }
            else
            {
                for (int i = 0; i < _model.services.Count; i++)
                {
                    LoginServiceModel service = _model.services[i];
                    if (service == null)
                        continue;

                    string label = string.IsNullOrWhiteSpace(service.label)
                        ? (service.key ?? "MODULE")
                        : service.label;

                    Color previousBackground = GUI.backgroundColor;
                    GUI.backgroundColor = service.selected
                        ? new Color(0.12f, 0.58f, 0.28f, 1f)
                        : new Color(0.17f, 0.20f, 0.27f, 1f);

                    bool previousEnabled = GUI.enabled;
                    GUI.enabled = !_busy;

                    if (GUILayout.Button(
                        (service.selected ? "[X]  " : "[ ]  ") + label,
                        _buttonStyle,
                        GUILayout.Height(43f),
                        GUILayout.ExpandWidth(true)))
                    {
                        service.selected = !service.selected;
                    }

                    GUI.enabled = previousEnabled;
                    GUI.backgroundColor = previousBackground;
                    GUILayout.Space(5f);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(8f);

            if (!string.IsNullOrWhiteSpace(_statusMessage))
            {
                Color previousContent = GUI.contentColor;
                GUI.contentColor = _statusIsError
                    ? new Color(1f, 0.42f, 0.42f, 1f)
                    : new Color(0.55f, 0.92f, 1f, 1f);

                GUILayout.Label(_statusMessage, _statusStyle);
                GUI.contentColor = previousContent;
                GUILayout.Space(5f);
            }

            bool wasEnabled = GUI.enabled;
            Color oldBackground = GUI.backgroundColor;
            GUI.enabled = !_busy;
            GUI.backgroundColor = _busy
                ? new Color(0.25f, 0.28f, 0.32f, 1f)
                : new Color(0.06f, 0.55f, 0.75f, 1f);

            if (GUILayout.Button(
                _busy ? "PLEASE WAIT..." : "SAVE",
                _saveButtonStyle,
                GUILayout.Height(52f),
                GUILayout.ExpandWidth(true)))
            {
                Submit();
            }

            GUI.backgroundColor = oldBackground;
            GUI.enabled = wasEnabled;

            Event current = Event.current;
            if (!_busy && current != null && current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                Submit();
                current.Use();
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 46f));
        }

        private void HandleUsernameKeyboardInput()
        {
            if (!_visible || _busy || _model == null || !_model.username_required)
                return;

            string input;
            try
            {
                input = Input.inputString;
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(input))
                return;

            bool changed = false;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (ch == '\b')
                {
                    if (!string.IsNullOrEmpty(_username))
                    {
                        _username = _username.Substring(0, _username.Length - 1);
                        changed = true;
                    }

                    continue;
                }

                if (ch == '\n' || ch == '\r')
                {
                    Submit();
                    return;
                }

                if (_username.Length >= 24 || !IsAllowedUsernameCharacter(ch))
                    continue;

                _username += ch;
                changed = true;
            }

            if (changed && _statusIsError)
            {
                _statusMessage = "";
                _statusIsError = false;
            }
        }

        private static bool IsAllowedUsernameCharacter(char ch)
        {
            return (ch >= 'A' && ch <= 'Z') ||
                   (ch >= 'a' && ch <= 'z') ||
                   (ch >= '0' && ch <= '9') ||
                   ch == '_' ||
                   ch == '-' ||
                   ch == '.';
        }

        private void Submit()
        {
            if (_busy || _model == null)
                return;

            string username = _model.username_required
                ? (_username ?? "").Trim()
                : (_model.username ?? "").Trim();

            if (_model.username_required && !UsernameRegex.IsMatch(username))
            {
                SetStatus(
                    "Invalid username. Use 3-24 letters, numbers, dot, dash or underscore.",
                    true);
                return;
            }

            List<string> selected = new List<string>();
            for (int i = 0; i < _model.services.Count; i++)
            {
                LoginServiceModel service = _model.services[i];
                if (service != null && service.selected && !string.IsNullOrWhiteSpace(service.key))
                    selected.Add(service.key);
            }

            string json;
            try
            {
                json = JsonSerializer.Serialize(new LoginSubmission
                {
                    username = username,
                    selected_services = selected
                });
            }
            catch (Exception ex)
            {
                SetStatus("Could not prepare the configuration: " + ex.Message, true);
                return;
            }

            _busy = true;
            _statusIsError = false;
            _statusMessage = _model.username_required
                ? "Checking username availability..."
                : "Saving module preferences...";


            try
            {
                // login.bin performs the authenticated server-side availability
                // check and registration. Errors/suggestions return through
                // SetStatus, while a successful response closes this menu.
                BanModLoginSubmitBridge.Submit(json);
            }
            catch (Exception ex)
            {
                _busy = false;
                SetStatus("Could not submit the configuration: " + ex.Message, true);
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.22f, 0.90f, 1f, 1f) }
            };

            _descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft
            };
            SetPadding(_buttonStyle, 18, 12, 8, 8);

            _saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _lockedUsernameStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.72f, 0.92f, 1f, 1f) }
            };
            SetPadding(_lockedUsernameStyle, 14, 10, 8, 8);

            _usernameInputStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            SetPadding(_usernameInputStyle, 14, 10, 8, 8);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.76f, 0.82f, 1f) }
            };
        }

        private static void SetPadding(GUIStyle style, int left, int right, int top, int bottom)
        {
            if (style == null)
                return;

            try
            {
                RectOffset padding = style.padding;
                if (padding == null)
                    return;

                padding.left = left;
                padding.right = right;
                padding.top = top;
                padding.bottom = bottom;
            }
            catch { }
        }

        private static float CalculateWindowHeight(int serviceCount)
        {
            int boundedCount = Mathf.Clamp(serviceCount, 0, 10);
            return Mathf.Clamp(465f + boundedCount * 49f, MinWindowHeight, MaxWindowHeight);
        }

        private void CenterWindow(float height)
        {
            _windowRect = new Rect(
                Screen.width * 0.5f - WindowWidth * 0.5f,
                Screen.height * 0.5f - height * 0.5f,
                WindowWidth,
                height);
        }

        private void CaptureCursor()
        {
            if (_cursorCaptured)
                return;

            _cursorCaptured = true;
            _oldCursorVisible = Cursor.visible;
            _oldCursorLock = Cursor.lockState;

            try
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            catch
            {
                // Cursor state is best effort only.
            }

            HandleUsernameKeyboardInput();
        }

        private void RestoreCursor()
        {
            if (!_cursorCaptured)
                return;

            try
            {
                Cursor.visible = _oldCursorVisible;
                Cursor.lockState = _oldCursorLock;
            }
            catch
            {
                // Cursor state is best effort only.
            }

            _cursorCaptured = false;
        }

        private sealed class LoginMenuModel
        {
            public bool username_required { get; set; }
            public string username { get; set; }
            public List<LoginServiceModel> services { get; set; }
        }

        private sealed class LoginServiceModel
        {
            public string key { get; set; }
            public string label { get; set; }
            public bool selected { get; set; }
        }

        private sealed class LoginSubmission
        {
            public string username { get; set; }
            public List<string> selected_services { get; set; }
        }
    }
}
