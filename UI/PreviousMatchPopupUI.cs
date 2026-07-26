//credits and licenses in the resources folder
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;

namespace BanMod
{
    public class PreviousMatchSummaryUi : MonoBehaviour
    {
        public static PreviousMatchSummaryUi Instance;

        public enum Panel
        {
            Main,
            Roles,
            Kills,
            Tasks,
            Protections
        }

        public bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(1080, 720);
        private Vector2 scrollPosition = Vector2.zero;
        private Panel currentPanel = Panel.Main;

        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle exitButtonStyle;
        private GUIStyle backButtonStyle;
        private GUIStyle textStyle;
        private GUIStyle sectionStyle;
        private GUIStyle tableHeaderStyle;
        private GUIStyle tableCellStyle;
        private GUIStyle tableHeaderRowStyle;
        private GUIStyle tableRowStyle;
        private GUIStyle tableRowAltStyle;

        private Texture2D windowTex;
        private Texture2D sectionTex;
        private Texture2D buttonTex;
        private Texture2D buttonHoverTex;
        private Texture2D buttonActiveTex;
        private Texture2D dangerTex;
        private Texture2D backTex;
        private Texture2D tableHeaderTex;
        private Texture2D tableRowTex;
        private Texture2D tableRowAltTex;

        private const int ButtonsPerRow = 2;
        private const float ButtonHeight = 54f;
        private const float ButtonSpacing = 12f;

        public class MenuItem
        {
            public string Label;
            public Action OnClick;
        }

        private sealed class OverviewRow
        {
            public byte PlayerId;
            public string Name = "";
            public string VanillaRole = "";
            public string CustomRole = "";
            public string Tasks = "";
            public string AssignedProtections = "";
            public string EffectiveProtections = "";
            public string Kills = "";
        }

        private readonly List<MenuItem> menuItems = new();

        public static void ShowMenu()
        {
            if (Instance == null)
            {
                var obj = new GameObject("PreviousMatchSummaryUi");
                DontDestroyOnLoad(obj);
                Instance = obj.AddComponent<PreviousMatchSummaryUi>();
            }

            Instance.OpenMenu();
        }

        void Awake()
        {
            Instance = this;
            CenterWindow();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            DestroyTexture(windowTex);
            DestroyTexture(sectionTex);
            DestroyTexture(buttonTex);
            DestroyTexture(buttonHoverTex);
            DestroyTexture(buttonActiveTex);
            DestroyTexture(dangerTex);
            DestroyTexture(backTex);
            DestroyTexture(tableHeaderTex);
            DestroyTexture(tableRowTex);
            DestroyTexture(tableRowAltTex);
        }

        private void DestroyTexture(Texture2D tex)
        {
            if (tex != null)
                UnityEngine.Object.Destroy(tex);
        }

        public void OpenMenu()
        {
            showMenu = true;
            currentPanel = Panel.Main;
            scrollPosition = Vector2.zero;
            SetupMenuContent();
            CenterWindow();
        }

        public void CloseMenu()
        {
            showMenu = false;
            scrollPosition = Vector2.zero;
        }

        private void SetupMenuContent()
        {
            menuItems.Clear();

            menuItems.Add(new MenuItem { Label = GetString("Roles"), OnClick = () => OpenPanel(Panel.Roles) });
            menuItems.Add(new MenuItem { Label = GetString("KillsLabel"), OnClick = () => OpenPanel(Panel.Kills) });
            menuItems.Add(new MenuItem { Label = GetString("PrevMatchBtnTasks"), OnClick = () => OpenPanel(Panel.Tasks) });
            menuItems.Add(new MenuItem { Label = GetString("PrevMatchBtnProtections"), OnClick = () => OpenPanel(Panel.Protections) });
        }

        private void OpenPanel(Panel panel)
        {
            currentPanel = panel;
            scrollPosition = Vector2.zero;
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

        private Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.hideFlags = HideFlags.HideAndDontSave;

            return tex;
        }

        private void EnsureStyles()
        {
            bool invalid =
                windowStyle == null ||
                windowTex == null ||
                sectionTex == null ||
                buttonTex == null ||
                buttonHoverTex == null ||
                buttonActiveTex == null ||
                dangerTex == null ||
                backTex == null ||
                tableHeaderTex == null ||
                tableRowTex == null ||
                tableRowAltTex == null;

            if (!invalid && windowStyle.normal.background == null)
                invalid = true;

            if (!invalid && sectionStyle != null && sectionStyle.normal.background == null)
                invalid = true;

            if (!invalid && buttonStyle != null && buttonStyle.normal.background == null)
                invalid = true;

            if (!invalid)
                return;

            windowStyle = null;
            titleStyle = null;
            headerStyle = null;
            buttonStyle = null;
            exitButtonStyle = null;
            backButtonStyle = null;
            textStyle = null;
            sectionStyle = null;
            tableHeaderStyle = null;
            tableCellStyle = null;
            tableHeaderRowStyle = null;
            tableRowStyle = null;
            tableRowAltStyle = null;

            windowTex = MakeTex(Color.black);
            sectionTex = MakeTex(Color.black);
            buttonTex = MakeTex(new Color(0.02f, 0.02f, 0.02f, 1f));
            buttonHoverTex = MakeTex(new Color(0.02f, 0.10f, 0.18f, 1f));
            buttonActiveTex = MakeTex(new Color(0.02f, 0.10f, 0.18f, 1f));
            dangerTex = MakeTex(new Color(0.48f, 0.12f, 0.12f, 1f));
            backTex = MakeTex(new Color(0.02f, 0.02f, 0.02f, 1f));
            tableHeaderTex = MakeTex(Color.black);
            tableRowTex = MakeTex(Color.black);
            tableRowAltTex = MakeTex(new Color(0.03f, 0.03f, 0.03f, 1f));

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset { left = 16, right = 16, top = 16, bottom = 16 },
                border = new RectOffset { left = 6, right = 6, top = 6, bottom = 6 }
            };
            windowStyle.normal.background = windowTex;
            windowStyle.onNormal.background = windowTex;
            windowStyle.focused.background = windowTex;
            windowStyle.onFocused.background = windowTex;
            windowStyle.active.background = windowTex;
            windowStyle.onActive.background = windowTex;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                wordWrap = true
            };
            titleStyle.normal.textColor = new Color(0.97f, 0.97f, 0.97f);

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                richText = true,
                wordWrap = true
            };
            headerStyle.normal.textColor = new Color(0.88f, 0.92f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true,
                padding = new RectOffset { left = 12, right = 12, top = 10, bottom = 10 },
                fixedHeight = ButtonHeight
            };
            buttonStyle.normal.background = buttonTex;
            buttonStyle.hover.background = buttonHoverTex;
            buttonStyle.active.background = buttonActiveTex;
            buttonStyle.focused.background = buttonTex;
            buttonStyle.onNormal.background = buttonTex;
            buttonStyle.onHover.background = buttonHoverTex;
            buttonStyle.onActive.background = buttonActiveTex;
            buttonStyle.onFocused.background = buttonTex;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;
            buttonStyle.focused.textColor = Color.white;
            buttonStyle.onNormal.textColor = Color.white;
            buttonStyle.onHover.textColor = Color.white;
            buttonStyle.onActive.textColor = Color.white;
            buttonStyle.onFocused.textColor = Color.white;

            backButtonStyle = new GUIStyle(buttonStyle);
            backButtonStyle.normal.background = backTex;
            backButtonStyle.hover.background = buttonHoverTex;
            backButtonStyle.active.background = buttonActiveTex;
            backButtonStyle.focused.background = backTex;
            backButtonStyle.onNormal.background = backTex;
            backButtonStyle.onHover.background = buttonHoverTex;
            backButtonStyle.onActive.background = buttonActiveTex;
            backButtonStyle.onFocused.background = backTex;

            exitButtonStyle = new GUIStyle(buttonStyle);
            exitButtonStyle.normal.background = dangerTex;
            exitButtonStyle.hover.background = dangerTex;
            exitButtonStyle.active.background = dangerTex;
            exitButtonStyle.focused.background = dangerTex;
            exitButtonStyle.onNormal.background = dangerTex;
            exitButtonStyle.onHover.background = dangerTex;
            exitButtonStyle.onActive.background = dangerTex;
            exitButtonStyle.onFocused.background = dangerTex;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };
            textStyle.normal.textColor = new Color(0.93f, 0.93f, 0.93f);

            sectionStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset { left = 14, right = 14, top = 14, bottom = 14 },
                margin = new RectOffset { left = 0, right = 0, top = 0, bottom = 12 }
            };
            sectionStyle.normal.background = sectionTex;
            sectionStyle.onNormal.background = sectionTex;
            sectionStyle.focused.background = sectionTex;
            sectionStyle.onFocused.background = sectionTex;
            sectionStyle.active.background = sectionTex;
            sectionStyle.onActive.background = sectionTex;

            tableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                wordWrap = true
            };
            tableHeaderStyle.normal.textColor = Color.white;

            tableCellStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                wordWrap = true
            };
            tableCellStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

            tableHeaderRowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 },
                margin = new RectOffset { left = 0, right = 0, top = 0, bottom = 6 }
            };
            tableHeaderRowStyle.normal.background = tableHeaderTex;
            tableHeaderRowStyle.onNormal.background = tableHeaderTex;
            tableHeaderRowStyle.focused.background = tableHeaderTex;
            tableHeaderRowStyle.onFocused.background = tableHeaderTex;
            tableHeaderRowStyle.active.background = tableHeaderTex;
            tableHeaderRowStyle.onActive.background = tableHeaderTex;

            tableRowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 },
                margin = new RectOffset { left = 0, right = 0, top = 0, bottom = 6 }
            };
            tableRowStyle.normal.background = tableRowTex;
            tableRowStyle.onNormal.background = tableRowTex;
            tableRowStyle.focused.background = tableRowTex;
            tableRowStyle.onFocused.background = tableRowTex;
            tableRowStyle.active.background = tableRowTex;
            tableRowStyle.onActive.background = tableRowTex;

            tableRowAltStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 },
                margin = new RectOffset { left = 0, right = 0, top = 0, bottom = 6 }
            };
            tableRowAltStyle.normal.background = tableRowAltTex;
            tableRowAltStyle.onNormal.background = tableRowAltTex;
            tableRowAltStyle.focused.background = tableRowAltTex;
            tableRowAltStyle.onFocused.background = tableRowAltTex;
            tableRowAltStyle.active.background = tableRowAltTex;
            tableRowAltStyle.onActive.background = tableRowAltTex;
        }

        void OnGUI()
        {
            if (!showMenu) return;

            EnsureStyles();

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;

            windowRect = GUI.Window(987654, windowRect, (GUI.WindowFunction)DrawWindow, "", windowStyle);
        }

        private void DrawWindow(int id)
        {
            var snap = PreviousMatchPopupTracker.LastSnapshot;

            GUILayout.Space(2);
            GUILayout.Label(GetWindowTitle(), titleStyle, GUILayout.Height(40));
            GUILayout.Space(10);

            if (snap == null)
            {
                GUILayout.BeginVertical(sectionStyle);
                GUILayout.Label(GetString("PrevMatchNoData"), textStyle);
                GUILayout.EndVertical();

                GUILayout.Space(8);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetString("ExitButton"), exitButtonStyle, GUILayout.Width(180), GUILayout.Height(48)))
                    CloseMenu();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 48f));
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            switch (currentPanel)
            {
                case Panel.Main:
                    DrawMainMenu(snap);
                    break;
                case Panel.Roles:
                    DrawRolesPanel(snap);
                    break;
                case Panel.Kills:
                    DrawKillsPanel(snap);
                    break;
                case Panel.Tasks:
                    DrawTasksPanel(snap);
                    break;
                case Panel.Protections:
                    DrawProtectionsPanel(snap);
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            if (currentPanel != Panel.Main)
            {
                if (GUILayout.Button(GetString("PreviousPage"), backButtonStyle, GUILayout.Width(180), GUILayout.Height(48)))
                {
                    currentPanel = Panel.Main;
                    scrollPosition = Vector2.zero;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(GetString("ExitButton"), exitButtonStyle, GUILayout.Width(180), GUILayout.Height(48)))
                CloseMenu();

            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 48f));
        }

        private string GetWindowTitle()
        {
            return currentPanel switch
            {
                Panel.Main => GetString("PrevMatchTitle"),
                Panel.Roles => GetString("Roles"),
                Panel.Kills => GetString("KillsLabel"),
                Panel.Tasks => GetString("PrevMatchBtnTasks"),
                Panel.Protections => GetString("PrevMatchBtnProtections"),
                _ => GetString("PrevMatchTitle")
            };
        }

        private float GetButtonWidth()
        {
            float contentWidth = windowRect.width - 64f;
            float totalSpacing = ButtonSpacing * (ButtonsPerRow - 1);
            return (contentWidth - totalSpacing) / ButtonsPerRow;
        }
        [HideFromIl2Cpp]
        private float[] GetTableColumnWidths()
        {
            float contentWidth = Mathf.Max(900f, windowRect.width - 120f);

            return new float[]
            {
                contentWidth * 0.15f, 
                contentWidth * 0.16f, 
                contentWidth * 0.15f, 
                contentWidth * 0.10f, 
                contentWidth * 0.16f, 
                contentWidth * 0.18f, 
                contentWidth * 0.08f  
            };
        }

        private void DrawCell(string text, float width)
        {
            GUILayout.Label(string.IsNullOrWhiteSpace(text) ? "<color=#7F8795>-</color>" : text, tableCellStyle, GUILayout.Width(width));
        }

        private void DrawHeaderCell(string text, float width)
        {
            GUILayout.Label(text, tableHeaderStyle, GUILayout.Width(width));
        }
        [HideFromIl2Cpp]
        private void DrawMainMenu(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label(GetString("PrevMatchMainHeader"), headerStyle);
            GUILayout.Space(8);

            if (snap.JesterWin)
            {
                if (!string.IsNullOrWhiteSpace(snap.JesterName))
                    GUILayout.Label($"{GetString("JesterWins")}: {snap.JesterName}", textStyle);
                else
                    GUILayout.Label(GetString("JesterWins"), textStyle);

                GUILayout.Space(6);
            }

            if (!string.IsNullOrWhiteSpace(snap.LastImmortalPlayerName))
            {
                GUILayout.Label(string.Format(GetString("ImmortalPlayerReport"), snap.LastImmortalPlayerName), textStyle);
                GUILayout.Space(6);
            }

            if (!string.IsNullOrWhiteSpace(snap.LastTaskCompleterName))
            {
                GUILayout.Label(
                    string.Format(
                        GetString("LastTaskCompleter"),
                        snap.LastTaskCompleterName,
                        snap.LastTaskCompleterDone,
                        snap.LastTaskCompleterTotal),
                    textStyle
                );
                GUILayout.Space(6);
            }

            GUILayout.Label(string.Format(GetString("PrevMatchQuickKills"), snap.KillerStats.Count), textStyle);
            int totalTasksDone = snap.TaskStats.Sum(t => t.Done);
            int totalTasksAll = snap.TaskStats.Sum(t => t.Total);
            GUILayout.Label(string.Format(GetString("PrevMatchQuickTasks"), totalTasksDone, totalTasksAll), textStyle);
            GUILayout.Label(string.Format(GetString("PrevMatchQuickProtections"), snap.ProtectionStats.Count), textStyle);

            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            float btnWidth = GetButtonWidth();

            for (int i = 0; i < menuItems.Count; i += ButtonsPerRow)
            {
                GUILayout.BeginHorizontal();

                for (int c = 0; c < ButtonsPerRow; c++)
                {
                    int idx = i + c;
                    if (idx < menuItems.Count)
                    {
                        if (GUILayout.Button(menuItems[idx].Label, buttonStyle, GUILayout.Width(btnWidth), GUILayout.Height(ButtonHeight)))
                            menuItems[idx].OnClick?.Invoke();
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(ButtonSpacing);
            }

            GUILayout.EndVertical();
        }
        [HideFromIl2Cpp]
        private List<OverviewRow> BuildOverviewRows(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            var roleMap = new Dictionary<byte, PreviousMatchPopupTracker.PlayerRoleStat>();
            var taskMap = new Dictionary<byte, PreviousMatchPopupTracker.TaskStat>();
            var protectionMap = new Dictionary<byte, PreviousMatchPopupTracker.ProtectionStat>();
            var killMap = new Dictionary<byte, PreviousMatchPopupTracker.KillerStat>();

            if (snap.PlayerRoles != null)
                foreach (var x in snap.PlayerRoles)
                    roleMap[x.PlayerId] = x;

            if (snap.TaskStats != null)
                foreach (var x in snap.TaskStats)
                    taskMap[x.PlayerId] = x;

            if (snap.ProtectionStats != null)
                foreach (var x in snap.ProtectionStats)
                    protectionMap[x.PlayerId] = x;

            if (snap.KillerStats != null)
                foreach (var x in snap.KillerStats)
                    killMap[x.PlayerId] = x;

            var allIds = new HashSet<byte>();
            foreach (var id in roleMap.Keys) allIds.Add(id);
            foreach (var id in taskMap.Keys) allIds.Add(id);
            foreach (var id in protectionMap.Keys) allIds.Add(id);
            foreach (var id in killMap.Keys) allIds.Add(id);

            var rows = new List<OverviewRow>();

            foreach (var id in allIds)
            {
                roleMap.TryGetValue(id, out var role);
                taskMap.TryGetValue(id, out var task);
                protectionMap.TryGetValue(id, out var protection);
                killMap.TryGetValue(id, out var kill);

                string name =
                    role != null && !string.IsNullOrWhiteSpace(role.Name) ? role.Name :
                    task != null && !string.IsNullOrWhiteSpace(task.Name) ? task.Name :
                    protection != null && !string.IsNullOrWhiteSpace(protection.Name) ? protection.Name :
                    kill != null && !string.IsNullOrWhiteSpace(kill.RealName) ? kill.RealName :
                    GetString("UnknownPlayerName");

                rows.Add(new OverviewRow
                {
                    PlayerId = id,
                    Name = name,
                    VanillaRole = role != null && !string.IsNullOrWhiteSpace(role.VanillaRoleName)
                        ? role.VanillaRoleName
                        : "<color=#7F8795>-</color>",
                    CustomRole = role != null && !string.IsNullOrWhiteSpace(role.CustomRoleName)
                        ? role.CustomRoleName
                        : "<color=#7F8795>-</color>",
                    Tasks = task != null
                        ? $"{task.Done}/{task.Total}"
                        : "<color=#7F8795>-</color>",
                    AssignedProtections = protection != null && protection.AssignedCount > 0
                        ? protection.AssignedCount.ToString()
                        : "<color=#7F8795>-</color>",
                    EffectiveProtections = protection != null && protection.EffectiveSaveCount > 0
                        ? protection.EffectiveSaveCount.ToString()
                        : "<color=#7F8795>-</color>",
                    Kills = kill != null && kill.KillCount > 0
                        ? kill.KillCount.ToString()
                        : "<color=#7F8795>-</color>"
                });
            }

            return rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        [HideFromIl2Cpp]
        private void DrawOverviewTable(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            var rows = BuildOverviewRows(snap);
            var widths = GetTableColumnWidths();

            GUILayout.BeginHorizontal(tableHeaderRowStyle);
            DrawHeaderCell("Nome", widths[0]);
            DrawHeaderCell(GetString("Roles"), widths[1]);
            DrawHeaderCell("Custom", widths[2]);
            DrawHeaderCell(GetString("PrevMatchBtnTasks"), widths[3]);
            DrawHeaderCell(GetString("ProtectionAssignedHeader"), widths[4]);
            DrawHeaderCell(GetString("ProtectionEffectiveHeader"), widths[5]);
            DrawHeaderCell(GetString("KillsLabel"), widths[6]);
            GUILayout.EndHorizontal();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                GUILayout.BeginHorizontal(i % 2 == 0 ? tableRowStyle : tableRowAltStyle);
                DrawCell(row.Name, widths[0]);
                DrawCell(row.VanillaRole, widths[1]);
                DrawCell(row.CustomRole, widths[2]);
                DrawCell(row.Tasks, widths[3]);
                DrawCell(row.AssignedProtections, widths[4]);
                DrawCell(row.EffectiveProtections, widths[5]);
                DrawCell(row.Kills, widths[6]);
                GUILayout.EndHorizontal();
            }
        }
        [HideFromIl2Cpp]
        private void DrawRolesPanel(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            GUILayout.BeginVertical(sectionStyle);
            DrawOverviewTable(snap);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);

            bool hasAny = false;

            if (snap.JesterWin)
            {
                hasAny = true;

                if (!string.IsNullOrWhiteSpace(snap.JesterName))
                    GUILayout.Label($"{GetString("JesterWins")}: {snap.JesterName}", textStyle);
                else
                    GUILayout.Label(GetString("JesterWins"), textStyle);

                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.GuesserName) && !string.IsNullOrEmpty(snap.GuessedTargetName))
            {
                hasAny = true;

                if (!string.IsNullOrWhiteSpace(snap.GuessedRoleName))
                {
                    GUILayout.Label(
                        !snap.SpecialKillerFailed
                            ? string.Format(GetString("GuesserSuccessRole"), snap.GuesserName, snap.GuessedTargetName, snap.GuessedRoleName)
                            : string.Format(GetString("GuesserFailRole"), snap.GuesserName, snap.GuessedTargetName, snap.GuessedRoleName),
                        textStyle
                    );
                }
                else
                {
                    GUILayout.Label(
                        !snap.SpecialKillerFailed
                            ? string.Format(GetString("GuessSuccessMessage"), snap.GuesserName, snap.GuessedTargetName)
                            : string.Format(GetString("GuessFailMessage"), snap.GuesserName, snap.GuessedTargetName),
                        textStyle
                    );
                }

                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.PresidentName) && !string.IsNullOrEmpty(snap.PresidentTargetName))
            {
                hasAny = true;

                if (!snap.PresidentExeFailed)
                    GUILayout.Label(string.Format(GetString("PresidentExile"), snap.PresidentName, snap.PresidentTargetName), textStyle);
                else if (!snap.PresidentKillFailed)
                    GUILayout.Label(string.Format(GetString("PresidentKill"), snap.PresidentName, snap.PresidentTargetName), textStyle);
                else
                    GUILayout.Label(string.Format(GetString("PresidentFail"), snap.PresidentName, snap.PresidentTargetName), textStyle);

                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.PhantomName) && !string.IsNullOrEmpty(snap.PhantomTargetName))
            {
                hasAny = true;
                GUILayout.Label(
                    !snap.PhantomFailed
                        ? string.Format(GetString("GuessSuccessMessage"), snap.PhantomName, snap.PhantomTargetName)
                        : string.Format(GetString("GuessFailMessage"), snap.PhantomName, snap.PhantomTargetName),
                    textStyle
                );
                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.ViperName) && !string.IsNullOrEmpty(snap.ViperTargetName))
            {
                hasAny = true;
                GUILayout.Label(
                    !snap.ViperFailed
                        ? string.Format(GetString("GuessSuccessMessage"), snap.ViperName, snap.ViperTargetName)
                        : string.Format(GetString("GuessFailMessage"), snap.ViperName, snap.ViperTargetName),
                    textStyle
                );
                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.ShapeName) && !string.IsNullOrEmpty(snap.ShapeTargetName))
            {
                hasAny = true;
                GUILayout.Label(
                    !snap.ShapeFailed
                        ? string.Format(GetString("GuessSuccessMessage"), snap.ShapeName, snap.ShapeTargetName)
                        : string.Format(GetString("GuessFailMessage"), snap.ShapeName, snap.ShapeTargetName),
                    textStyle
                );
                GUILayout.Space(10);
            }

            if (!string.IsNullOrEmpty(snap.ImpostorName) && !string.IsNullOrEmpty(snap.ImpostorTargetName))
            {
                hasAny = true;
                GUILayout.Label(
                    !snap.ImpostorFailed
                        ? string.Format(GetString("ImpostorRoleSuccess"), snap.ImpostorName, snap.ImpostorTargetName)
                        : string.Format(GetString("ImpostorRoleFail"), snap.ImpostorName, snap.ImpostorTargetName),
                    textStyle
                );
                GUILayout.Space(10);
            }

            if (!hasAny)
                GUILayout.Label(GetString("PrevMatchNoRoleData"), textStyle);

            GUILayout.EndVertical();
        }
        [HideFromIl2Cpp]
        private void DrawKillsPanel(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            GUILayout.BeginVertical(sectionStyle);

            if (snap.KillerStats.Count == 0)
            {
                GUILayout.Label(GetString("KillSummaryEmpty"), textStyle);
            }
            else
            {
                foreach (var killer in snap.KillerStats.OrderByDescending(x => x.KillCount).ThenBy(x => x.RealName))
                {
                    GUILayout.Label(string.Format(GetString("KillSummaryLine"), killer.RealName, killer.KillCount), textStyle);
                    GUILayout.Space(8);
                }
            }

            GUILayout.EndVertical();
        }
        [HideFromIl2Cpp]
        private void DrawTasksPanel(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            GUILayout.BeginVertical(sectionStyle);

            if (snap.TaskStats.Count == 0)
            {
                GUILayout.Label(GetString("TaskSummaryEmpty"), textStyle);
            }
            else
            {
                foreach (var task in snap.TaskStats.OrderByDescending(t => t.Done).ThenBy(t => t.Name))
                {
                    GUILayout.Label(string.Format(GetString("TaskSummaryLine"), task.Name, task.Done, task.Total), textStyle);
                    GUILayout.Space(8);
                }
            }

            GUILayout.Space(10);

            if (!string.IsNullOrWhiteSpace(snap.LastTaskCompleterName))
            {
                GUILayout.Label(
                    string.Format(
                        GetString("LastTaskCompleter"),
                        snap.LastTaskCompleterName,
                        snap.LastTaskCompleterDone,
                        snap.LastTaskCompleterTotal),
                    textStyle
                );
            }
            else
            {
                GUILayout.Label(GetString("LastTaskCompleterNone"), textStyle);
            }

            GUILayout.EndVertical();
        }
        [HideFromIl2Cpp]
        private void DrawProtectionsPanel(PreviousMatchPopupTracker.MatchSnapshot snap)
        {
            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label(GetString("ProtectionAssignedHeader"), headerStyle);
            GUILayout.Space(8);

            var assignedList = snap.ProtectionStats
                .Where(x => x.AssignedCount > 0)
                .OrderByDescending(x => x.AssignedCount)
                .ThenBy(x => x.Name)
                .ToList();

            if (assignedList.Count == 0)
            {
                GUILayout.Label(GetString("ProtectionAssignedEmpty"), textStyle);
            }
            else
            {
                foreach (var p in assignedList)
                {
                    GUILayout.Label(string.Format(GetString("FormatNameValue"), p.Name, p.AssignedCount), textStyle);
                    GUILayout.Space(8);
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label(GetString("ProtectionEffectiveHeader"), headerStyle);
            GUILayout.Space(8);

            var effectiveList = snap.ProtectionStats
                .Where(x => x.EffectiveSaveCount > 0)
                .OrderByDescending(x => x.EffectiveSaveCount)
                .ThenBy(x => x.Name)
                .ToList();

            if (effectiveList.Count == 0)
            {
                GUILayout.Label(GetString("ProtectionEffectiveEmpty"), textStyle);
            }
            else
            {
                foreach (var p in effectiveList)
                {
                    if (p.IsImmortalPlayer)
                        GUILayout.Label(string.Format(GetString("ProtectionSummaryImmortalLine"), p.Name, p.EffectiveSaveCount), textStyle);
                    else
                        GUILayout.Label(string.Format(GetString("FormatNameValue"), p.Name, p.EffectiveSaveCount), textStyle);

                    GUILayout.Space(8);
                }
            }

            GUILayout.EndVertical();
        }
    }
}