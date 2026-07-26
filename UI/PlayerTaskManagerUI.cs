//credits and licenses in the resources folder
using HarmonyLib;
using InnerNet;
using System;
using System.Linq;
using UnityEngine;

namespace BanMod
{
    public class PlayerTaskManager : MonoBehaviour
    {
        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(930, 600);
        private Vector2 scrollPosition = Vector2.zero;

        private GUIStyle titleStyle;
        private GUIStyle playerStyle;
        private GUIStyle smallInfoStyle;
        private GUIStyle buttonStyle;
        private GUIStyle closeXStyle;
        private GUIStyle boxStyle;

        public static PlayerTaskManager Instance;

        void Awake()
        {
            Instance = this;
            CenterWindow();
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
            showMenu = (p == MenuRouter.Panel.PlayerTasks);

            if (showMenu)
                CenterWindow();
        }

        void Update()
        {
            if (KeyBindOptions.IsBindingActive)
                return;

            if (Input.GetKeyDown(KeyBindOptions.K19) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.PlayerTasks)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.PlayerTasks);
            }
        }

        public void OpenMenu()
        {
            showMenu = true;
            CenterWindow();
        }

        public void CloseMenu()
        {
            showMenu = false;
        }

        private void CenterWindow()
        {
            float width = Mathf.Min(windowSize.x, Screen.width - 40f);
            float height = Mathf.Min(windowSize.y, Screen.height - 40f);

            windowRect = new Rect(
                Screen.width / 2f - width / 2f,
                Screen.height / 2f - height / 2f,
                width,
                height
            );
        }

        void OnGUI()
        {
            if (!showMenu || PlayerControl.LocalPlayer == null)
                return;

            InitStyles();

            GUI.backgroundColor = Color.black;

            windowRect = GUI.Window(
                1,
                windowRect,
                (GUI.WindowFunction)DrawWindow,
                "",
                BanModUiStyles.BlackWindow
            );
        }

        void DrawWindow(int id)
        {
            try
            {
                DrawTopBar();

                GUILayout.Space(8);

                DrawHeader();

                GUILayout.Space(6);

                float scrollHeight = Mathf.Max(windowRect.height - 115f, 160f);

                scrollPosition = GUILayout.BeginScrollView(
                    scrollPosition,
                    false,
                    true,
                    GUILayout.Height(scrollHeight)
                );

                var localPlayer = PlayerControl.LocalPlayer;

                if (localPlayer == null || localPlayer.Data == null)
                {
                    GUILayout.EndScrollView();
                    return;
                }

                bool localIsDead = localPlayer.Data.IsDead;

                PlayerControl[] allPlayers = null;

                try
                {
                    allPlayers = PlayerControl.AllPlayerControls?.ToArray();
                }
                catch
                {
                    allPlayers = null;
                }

                if (allPlayers != null)
                {
                    foreach (var pc in allPlayers.Where(p => p != null && p.Data != null).OrderBy(p => p.PlayerId))
                    {
                        DrawPlayerInfoRow(pc, localPlayer, localIsDead);
                        GUILayout.Space(5);
                    }
                }

                GUILayout.EndScrollView();

                GUILayout.Space(6);

                GUI.backgroundColor = new Color(0.8f, 0f, 0f, 1f);

                if (GUILayout.Button("CLOSE", buttonStyle, GUILayout.Height(36)))
                    MenuRouter.Open(MenuRouter.Panel.None);

                GUI.backgroundColor = Color.white;

                GUI.DragWindow();
            }
            catch (Exception ex)
            {
                BMLogger.Warn($"PlayerTaskManager DrawWindow error: {ex}", "PlayerTaskManager");
            }
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Space(28);

            GUILayout.Label(Translator.GetString("TASK_PLAYERS"), titleStyle, GUILayout.Height(30));

            GUI.backgroundColor = new Color(0.75f, 0f, 0f, 1f);

            if (GUILayout.Button("X", closeXStyle, GUILayout.Width(28), GUILayout.Height(28)))
            {
                MenuRouter.Open(MenuRouter.Panel.None);
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(30));

            GUILayout.Label("PLAYER", smallInfoStyle, GUILayout.Width(200));
            GUILayout.Label("TASK", smallInfoStyle, GUILayout.Width(75));
            GUILayout.Label("LVL", smallInfoStyle, GUILayout.Width(50));
            GUILayout.Label("PLATFORM", smallInfoStyle, GUILayout.Width(130));
            GUILayout.Label("FRIEND CODE", smallInfoStyle, GUILayout.Width(155));
            GUILayout.Label("CID", smallInfoStyle, GUILayout.Width(50));
            GUILayout.Label("MOD", smallInfoStyle, GUILayout.Width(140));
            GUILayout.Label("TP", smallInfoStyle, GUILayout.Width(50));

            GUILayout.EndHorizontal();
        }

        private void DrawPlayerInfoRow(PlayerControl pc, PlayerControl localPlayer, bool localIsDead)
        {
            if (pc == null || pc.Data == null)
                return;

            string name = pc.Data.PlayerName ?? "Unknown";

            int colorId = pc.Data.DefaultOutfit.ColorId;
            Color playerColor = ColorIdToColorSafe(colorId);
            string hexColor = ColorUtility.ToHtmlStringRGB(playerColor);

            bool targetIsDead = false;

            try
            {
                targetIsDead = pc.Data.IsDead;
            }
            catch
            {
            }

            string statusColor = targetIsDead ? "#FF4444" : "#44FF44";
            string statusText = targetIsDead ? " DEAD" : "";

            string taskInfo = GetTaskInfoForPlayer(pc, localIsDead, targetIsDead);

            int level = GetPlayerLevelSafe(pc);
            string platform = GetPlatformSafe(pc);
            string friendCode = GetFriendCodeSafe(pc);
            int clientId = GetClientIdSafe(pc);
            string modInfo = GetModInfoSafe(pc);

            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);

            GUILayout.BeginHorizontal(boxStyle, GUILayout.Height(42));

            string playerLabel =
                $"<color=#{hexColor}>{EscapeRichText(ShortText(name, 18))}</color>" +
                $"<color={statusColor}>{statusText}</color>";

            GUILayout.Label(playerLabel, playerStyle, GUILayout.Width(200));

            GUILayout.Label(taskInfo, playerStyle, GUILayout.Width(75));

            GUILayout.Label(
                $"<color=#FFD700>{level}</color>",
                playerStyle,
                GUILayout.Width(50)
            );

            GUILayout.Label(
                $"<color=#4FC3F7>{EscapeRichText(ShortText(platform, 14))}</color>",
                smallInfoStyle,
                GUILayout.Width(130)
            );

            GUILayout.Label(
                $"<color=#AAAAAA>{EscapeRichText(ShortText(friendCode, 17))}</color>",
                smallInfoStyle,
                GUILayout.Width(155)
            );

            GUILayout.Label(
                $"<color=#FFFFFF>{clientId}</color>",
                playerStyle,
                GUILayout.Width(50)
            );

            GUILayout.Label(
                ShortRichModInfo(modInfo),
                smallInfoStyle,
                GUILayout.Width(140)
            );

            DrawTeleportButton(pc, localPlayer, localIsDead);

            GUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
        }

        private static string GetTaskInfoForPlayer(PlayerControl pc, bool localIsDead, bool targetIsDead)
        {
            try
            {
                if (pc == null || pc.Data == null)
                    return "<color=#777777>-</color>";

                PlayerControl taskPlayer = GetCrewmateByPlayerId(pc.PlayerId);

                if (taskPlayer == null || taskPlayer.Data == null)
                    return "<color=#777777>-</color>";

                if (taskPlayer.Data.Role != null && taskPlayer.Data.Role.IsImpostor)
                    return "<color=#777777>-</color>";

                if (!localIsDead && !targetIsDead)
                    return "<color=#777777>-</color>";

                int total = 0;
                int done = 0;

                if (taskPlayer.Data.Tasks != null)
                {
                    total = taskPlayer.Data.Tasks.Count;

                    foreach (var t in taskPlayer.Data.Tasks)
                    {
                        if (t != null && t.Complete)
                            done++;
                    }
                }

                return $"<color=#00FFFF>{done}/{total}</color>";
            }
            catch
            {
                return "<color=#777777>-</color>";
            }
        }

        private static PlayerControl GetCrewmateByPlayerId(byte playerId)
        {
            try
            {
                if (BanMod.AllCrewmates == null)
                    return null;

                foreach (var pc in BanMod.AllCrewmates)
                {
                    if (pc == null || pc.Data == null)
                        continue;

                    if (pc.PlayerId == playerId)
                        return pc;
                }
            }
            catch
            {
            }

            return null;
        }

        private void DrawTeleportButton(PlayerControl target, PlayerControl localPlayer, bool localIsDead)
        {
            try
            {
                GUI.enabled = localIsDead;

                if (GUILayout.Button("GO", buttonStyle, GUILayout.Width(50), GUILayout.Height(28)))
                {
                    if (localIsDead && target != null && localPlayer != null)
                    {
                        localPlayer.transform.position = target.transform.position;
                    }
                }

                GUI.enabled = true;
            }
            catch
            {
                GUI.enabled = true;
            }
        }

        private void InitStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (playerStyle == null)
            {
                playerStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (smallInfoStyle == null)
            {
                smallInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (closeXStyle == null)
            {
                closeXStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    hover = { textColor = Color.white }
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset
                    {
                        left = 5,
                        right = 5,
                        top = 5,
                        bottom = 5
                    }
                };
            }
        }

        private static int GetPlayerLevelSafe(PlayerControl player)
        {
            try
            {
                if (player == null)
                    return 0;

                if (GameData.Instance == null)
                    return 0;

                var pInfo = GameData.Instance.GetPlayerById(player.PlayerId);

                if (pInfo == null)
                    return 0;

                return (int)(pInfo.PlayerLevel + 1);
            }
            catch
            {
                return 0;
            }
        }

        private static string GetPlatformSafe(PlayerControl player)
        {
            try
            {
                if (player == null)
                    return "Unknown";

                string platform = Utils.GetPlatformName(player);

                if (string.IsNullOrWhiteSpace(platform))
                    return "Unknown";

                return platform;
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetFriendCodeSafe(PlayerControl player)
        {
            try
            {
                string code = player?.Data?.FriendCode;

                if (string.IsNullOrWhiteSpace(code))
                    return "N/A";

                return code;
            }
            catch
            {
                return "N/A";
            }
        }

        private static int GetClientIdSafe(PlayerControl player)
        {
            try
            {
                if (player == null)
                    return -1;

                return player.GetClientId();
            }
            catch
            {
                return -1;
            }
        }

        private static string GetModInfoSafe(PlayerControl player)
        {
            try
            {
                if (player == null)
                    return "<color=#777777>Unknown</color>";

                if (UnifiedRPCHandlerPatch.ModdedClients != null &&
                    UnifiedRPCHandlerPatch.ModdedClients.TryGetValue(player.PlayerId, out string modInfo) &&
                    !string.IsNullOrWhiteSpace(modInfo))
                {
                    return $"<color=#00FF99>{EscapeRichText(modInfo)}</color>";
                }

                if (UnifiedRPCHandlerPatch.IsClientModded(player.PlayerId))
                    return "<color=#00FF99>BanMod</color>";

                return "<color=#777777>Vanilla</color>";
            }
            catch
            {
                return "<color=#777777>Unknown</color>";
            }
        }

        private static Color ColorIdToColorSafe(int colorId)
        {
            try
            {
                if (Palette.PlayerColors != null &&
                    colorId >= 0 &&
                    colorId < Palette.PlayerColors.Length)
                {
                    return Palette.PlayerColors[colorId];
                }
            }
            catch
            {
            }

            return Color.white;
        }

        private static string EscapeRichText(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return "";

                return text
                    .Replace("<", "")
                    .Replace(">", "")
                    .Replace("\n", " ")
                    .Replace("\r", " ");
            }
            catch
            {
                return "";
            }
        }

        private static string ShortText(string text, int maxLength)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return "";

                if (text.Length <= maxLength)
                    return text;

                return text.Substring(0, maxLength - 3) + "...";
            }
            catch
            {
                return "";
            }
        }

        private static string ShortRichModInfo(string modInfo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modInfo))
                    return "<color=#777777>Unknown</color>";

                string clean = modInfo
                    .Replace("<color=#00FF99>", "")
                    .Replace("<color=#777777>", "")
                    .Replace("</color>", "");

                clean = ShortText(clean, 18);

                if (modInfo.Contains("#00FF99"))
                    return $"<color=#00FF99>{EscapeRichText(clean)}</color>";

                return $"<color=#777777>{EscapeRichText(clean)}</color>";
            }
            catch
            {
                return "<color=#777777>Unknown</color>";
            }
        }
    }
}