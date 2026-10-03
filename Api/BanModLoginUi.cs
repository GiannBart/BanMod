//credits and licenses in the resources folder/
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BanMod
{
    public sealed class BanModLoginUi : MonoBehaviour
    {
        private const int WindowId = 62031;
        private const float WindowWidth = 640f;
        private const float UsernameWindowHeight = 430f;
        private const float LoadingWindowHeight = 300f;

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
        private GUIStyle _saveButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _usernameInputStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _loadingStyle;

        public static void EnsureCreated()
        {
            if (Instance != null)
                return;

            if (BanMod.Instance == null)
                return;

            try
            {
                Instance = BanMod.Instance.AddComponent<BanModLoginUi>();
                if (Instance != null)
                    Debug.Log("[BANMOD][LOGIN] BanModLoginUi component created.");
            }
            catch (Exception ex)
            {
                try { Debug.LogError("[BANMOD][LOGIN] Failed to create BanModLoginUi: " + ex); } catch { }
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!_visible)
                return;

            if (_model != null && _model.username_required)
            {
                try
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                catch { }

                HandleUsernameKeyboardInput();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _visible = false;
            BanModLoginSubmitBridge.ClearCallback();
            RestoreCursor();
        }

        public void ShowFromJson(string json)
        {
            LoginMenuModel model;
            try
            {
                model = JsonSerializer.Deserialize<LoginMenuModel>(json ?? "{}", JsonOptions);
            }
            catch (Exception ex)
            {
                _model = new LoginMenuModel { loading_only = true };
                _statusMessage = "Invalid login data: " + ex.Message;
                _statusIsError = true;
                _busy = false;
                _visible = true;
                CenterWindow(LoadingWindowHeight);
                return;
            }

            _model = model ?? new LoginMenuModel();
            _username = _model.username_required ? (_model.username ?? "") : "";
            _statusMessage = "";
            _statusIsError = false;
            _busy = _model.loading_only || !_model.username_required;
            _visible = true;

            if (_model.username_required)
            {
                CenterWindow(UsernameWindowHeight);
                CaptureCursor();
            }
            else
            {
                RestoreCursor();
                CenterWindow(LoadingWindowHeight);
            }

            try
            {
                Debug.Log(
                    _model.username_required
                        ? "[BANMOD][LOGIN] Username registration UI visible."
                        : "[BANMOD][LOGIN] Automatic service loading UI visible.");
            }
            catch { }
        }

        public void ShowLoading(string message)
        {
            _model = new LoginMenuModel
            {
                username_required = false,
                loading_only = true,
                username = ""
            };
            _username = "";
            _statusMessage = message ?? "";
            _statusIsError = false;
            _busy = true;
            _visible = true;
            RestoreCursor();
            CenterWindow(LoadingWindowHeight);
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

                Color oldBackground = GUI.backgroundColor;
                GUI.backgroundColor = Color.white;

                _windowRect = GUI.Window(
                    WindowId,
                    _windowRect,
                    (GUI.WindowFunction)DrawWindow,
                    "",
                    BanModUiStyles.BlackWindow);

                GUI.backgroundColor = oldBackground;
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

            if (_model.username_required)
                DrawUsernameRegistration();
            else
                DrawLoadingView();

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 46f));
        }

        private void DrawUsernameRegistration()
        {
            GUILayout.Label("BANMOD LOGIN", _titleStyle, GUILayout.Height(42f));
            GUILayout.Space(8f);
            GUILayout.Label(
                "Choose the username associated with this FriendCode.",
                _descriptionStyle,
                GUILayout.Height(48f));
            GUILayout.Space(14f);

            GUILayout.Label("USERNAME", _sectionStyle, GUILayout.Height(28f));

            bool showCaret = !_busy && ((int)(Time.realtimeSinceStartup * 2f) & 1) == 0;
            string shownUsername = string.IsNullOrEmpty(_username)
                ? "Type your username..."
                : _username + (showCaret ? "|" : "");

            GUILayout.Label(shownUsername, _usernameInputStyle, GUILayout.Height(46f));
            GUILayout.Label(
                "Type directly on the keyboard. Allowed: letters, numbers, dot, dash and underscore (3-24 characters).",
                _hintStyle,
                GUILayout.Height(42f));

            GUILayout.Space(12f);
            DrawStatus();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !_busy;

            if (GUILayout.Button(
                _busy ? "PLEASE WAIT..." : "CONTINUE",
                _saveButtonStyle,
                GUILayout.Height(52f),
                GUILayout.ExpandWidth(true)))
            {
                Submit();
            }

            GUI.enabled = wasEnabled;

            Event current = Event.current;
            if (!_busy && current != null && current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                Submit();
                current.Use();
            }
        }

        private void DrawLoadingView()
        {
            GUILayout.Label("BANMOD LOGIN", _titleStyle, GUILayout.Height(42f));
            GUILayout.Space(16f);

            int phase = ((int)(Time.realtimeSinceStartup * 2.5f)) % 4;
            string dots = new string('.', phase);
            string loadingText = "Loading compatible services" + dots;

            GUILayout.Label(
                _busy ? loadingText : "Service loading finished.",
                _loadingStyle,
                GUILayout.Height(56f));

            GUILayout.Space(10f);
            GUILayout.Label(
                "Premium modules are selected automatically only when the server marks them as available and compatible.",
                _descriptionStyle,
                GUILayout.Height(60f));

            GUILayout.Space(8f);
            DrawStatus();
        }

        private void DrawStatus()
        {
            if (string.IsNullOrWhiteSpace(_statusMessage))
                return;

            Color previousContent = GUI.contentColor;
            GUI.contentColor = _statusIsError
                ? new Color(1f, 0.42f, 0.42f, 1f)
                : new Color(0.55f, 0.92f, 1f, 1f);

            GUILayout.Label(_statusMessage, _statusStyle, GUILayout.Height(38f));
            GUI.contentColor = previousContent;
            GUILayout.Space(5f);
        }

        private void HandleUsernameKeyboardInput()
        {
            if (!_visible || _busy || _model == null || !_model.username_required)
                return;

            string input;
            try { input = Input.inputString; }
            catch { return; }

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
            if (_busy || _model == null || !_model.username_required)
                return;

            string username = (_username ?? "").Trim();
            if (!UsernameRegex.IsMatch(username))
            {
                SetStatus(
                    "Invalid username. Use 3-24 letters, numbers, dot, dash or underscore.",
                    true);
                return;
            }

            string json;
            try
            {
                json = JsonSerializer.Serialize(new LoginSubmission
                {
                    username = username
                });
            }
            catch (Exception ex)
            {
                SetStatus("Could not prepare username registration: " + ex.Message, true);
                return;
            }

            _busy = true;
            _statusIsError = false;
            _statusMessage = "Registering username...";

            try
            {
                BanModLoginSubmitBridge.Submit(json);
            }
            catch (Exception ex)
            {
                _busy = false;
                SetStatus("Could not submit username registration: " + ex.Message, true);
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(BanModUiStyles.TitleLabel)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = BanModUiStyles.AccentHoverColor;

            _descriptionStyle = new GUIStyle(BanModUiStyles.BodyLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _sectionStyle = new GUIStyle(BanModUiStyles.HeaderLabel)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _saveButtonStyle = new GUIStyle(BanModUiStyles.AccentButton)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _statusStyle = new GUIStyle(BanModUiStyles.BodyLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _usernameInputStyle = new GUIStyle(BanModUiStyles.DarkBox)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };
            _usernameInputStyle.normal.textColor = Color.white;
            SetPadding(_usernameInputStyle, 14, 10, 8, 8);

            _hintStyle = new GUIStyle(BanModUiStyles.MutedLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _loadingStyle = new GUIStyle(BanModUiStyles.HeaderLabel)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _loadingStyle.normal.textColor = BanModUiStyles.AccentHoverColor;
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
            catch { }
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
            catch { }

            _cursorCaptured = false;
        }

        private sealed class LoginMenuModel
        {
            public bool username_required { get; set; }
            public bool loading_only { get; set; }
            public string username { get; set; }
        }

        private sealed class LoginSubmission
        {
            public string username { get; set; }
        }
    }
}
