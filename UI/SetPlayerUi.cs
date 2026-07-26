//credits and licenses in the resources folder
using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BanMod.Translator;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public class SetPlayerUi : MonoBehaviour
    {
        public static SetPlayerUi Instance;

        private Rect windowRect;
        private readonly Vector2 windowSize = new(1050, 650);

        private GUIStyle buttonStyle;
        private GUIStyle buttonStyle1;
        private GUIStyle titleStyle;

        public bool showActionMenu = false;
        private PlayerControl selectedPlayer;
        private Vector2 scrollPos;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            windowRect = new Rect(
                Screen.width / 2f - windowSize.x / 2f,
                Screen.height / 2f - windowSize.y / 2f,
                windowSize.x,
                windowSize.y
            );
        }

        private void Update()
        {
            if (KeyBindOptions.IsBindingActive) return;

            if (Input.GetKeyDown(KeyBindOptions.K16) && !BanMod.chatOpen)
            {
                showActionMenu = false;
                selectedPlayer = null;
                CloseActionMenu();
            }
        }

        private void EnsureStyles()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            if (buttonStyle1 == null)
            {
                buttonStyle1 = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.red }
                };
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.cyan }
                };
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (showActionMenu)
            {
                GUI.color = new Color(0f, 0f, 0f, 1f);
                windowRect = GUI.Window(39393, windowRect, (GUI.WindowFunction)DrawActionWindow, "", BanModUiStyles.BlackWindow);
            }
        }

        public void OpenForCurrentPlayer()
        {
            if (PlayerUI.Instance == null)
                return;

            selectedPlayer = PlayerUI.Instance.CurrentSelectedPlayer;
            if (selectedPlayer == null)
                return;

            showActionMenu = true;
        }

        public void OpenActionMenu(PlayerControl pc)
        {
            selectedPlayer = pc;
            if (selectedPlayer == null) return;
            showActionMenu = true;
        }

        private void CloseActionMenu()
        {
            showActionMenu = false;
            selectedPlayer = null;
        }

        private bool CheckAndCloseIfPlayerInvalid()
        {
            if (selectedPlayer == null)
            {
                CloseActionMenu();
                return false;
            }

            var playerInfo = GameData.Instance?.GetPlayerById(selectedPlayer.PlayerId);
            if (playerInfo == null || playerInfo.Disconnected)
            {
                ChatCommands.ShowChat($"<color=#FF0000>Error: Player {selectedPlayer.name} (ID: {selectedPlayer.PlayerId}) is no longer available. Menu closed.</color>");
                CloseActionMenu();
                return false;
            }

            return true;
        }

        private class CategoryBlock
        {
            public string Title;
            public Action DrawContent;

            public CategoryBlock(string title, Action draw)
            {
                Title = title;
                DrawContent = draw;
            }
        }

        private void DrawAssignedPlayersList()
        {
            foreach (var player in BanMod.AllPlayerControls)
            {
                if (player == null || player.Data == null)
                    continue;

                List<string> assignedRoles = new();

                if (ForcedRoleSystem.TryGetForcedRole(player.PlayerId, out var forcedRole))
                    assignedRoles.Add(forcedRole.ToString().ToUpper());

                if (Judge.JudgeSelected && Judge.JudgeId == player.PlayerId)
                    assignedRoles.Add("JUDGE");

                if (Profiler.ProfilerSelected && Profiler.ProfilerId == player.PlayerId)
                    assignedRoles.Add("PROFILER");

                if (Exiler.ExilerSelected && Exiler.ExilerId == player.PlayerId)
                    assignedRoles.Add("EXILER");

                if (Watcher.WatcherSelected && Watcher.WatcherId == player.PlayerId)
                    assignedRoles.Add("WatcherTitle");

                if (Guesser.SpecialKillerSelected && Guesser.SpecialKillerId == player.PlayerId)
                    assignedRoles.Add("GUESSER");

                if (Jester.ForcedJesterSelected && Jester.ForcedJesterId == player.PlayerId)
                    assignedRoles.Add("JESTER");

                string rolesText = assignedRoles.Count > 0
                    ? string.Join(", ", assignedRoles)
                    : "-";

                GUILayout.Label(
                    $"<color=yellow>{player.Data.PlayerName}</color> <color=white>-></color> <color=cyan>{rolesText}</color>",
                    GUI.skin.label
                );
            }
        }

        private void DrawActionWindow(int id)
        {
            if (!CheckAndCloseIfPlayerInvalid())
                return;

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.Space(10);

            GUILayout.Label($"<color=white>PLAYER:</color> <color=yellow>{selectedPlayer.name}</color>", titleStyle);
            GUILayout.Space(15);

            List<CategoryBlock> categories = new()
            {
                new CategoryBlock("IMPOSTOR ROLES", () =>
                {
                    if (GUILayout.Button("CASUAL", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        if (ForcedRoleSystem.TrySetRandomAvailableSpecialImpostorRole(selectedPlayer.PlayerId, out RoleTypes selectedRole))
                        {
                            ChatCommands.ShowChat($"Casual: {selectedPlayer.name} -> {selectedRole}");
                        }
                    }

                    if (GUILayout.Button("IMPOSTOR", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Impostor);
                        ChatCommands.ShowChat($"Impostor: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("VIPER", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Viper);
                        ChatCommands.ShowChat($"Viper: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("PHANTOM", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Phantom);
                        ChatCommands.ShowChat($"Phantom: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("SHAPESHIFTER", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Shapeshifter);
                        ChatCommands.ShowChat($"Shapeshifter: {selectedPlayer.name}");
                    }
                }),

                new CategoryBlock("CREWMATE ROLES", () =>
                {
                    if (GUILayout.Button("SCIENTIST", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Scientist);
                        ChatCommands.ShowChat($"Scientist: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("ENGINEER", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Engineer);
                        ChatCommands.ShowChat($"Engineer: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("NOISEMAKER", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Noisemaker);
                        ChatCommands.ShowChat($"Noisemaker: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("TRACKER", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Tracker);
                        ChatCommands.ShowChat($"Tracker: {selectedPlayer.name}");
                    }

                    if (GUILayout.Button("DETECTIVE", buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        ForcedRoleSystem.SetForcedRole(selectedPlayer.PlayerId, RoleTypes.Detective);
                        ChatCommands.ShowChat($"Detective: {selectedPlayer.name}");
                    }
                }),

                new CategoryBlock("CUSTOM ROLES", () =>
                {
                    if (GUILayout.Button(GetString("PUI_SetExiler"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Exiler.ExilerId = selectedPlayer.PlayerId;
                        Exiler.ExilerSelected = true;
                        ChatCommands.ShowChat($"Exiler: {selectedPlayer.name}");

                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.SetExiler,
                            SendOption.Reliable,
                            -1
                        );

                        writer.Write(selectedPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    
                    if (GUILayout.Button(GetString("PUI_SetJudge"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Judge.JudgeId = selectedPlayer.PlayerId;
                        Judge.JudgeSelected = true;
                        ChatCommands.ShowChat($"Judge: {selectedPlayer.name}");

                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.SetJudge,
                            SendOption.Reliable,
                            -1
                        );

                        writer.Write(selectedPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    if (GUILayout.Button(GetString("PUI_SetProfiler"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Profiler.ProfilerId = selectedPlayer.PlayerId;
                        Profiler.ProfilerSelected = true;
                        ChatCommands.ShowChat($"Profiler: {selectedPlayer.name}");

                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.SetProfiler,
                            SendOption.Reliable,
                            -1
                        );

                        writer.Write(selectedPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    if (GUILayout.Button(GetString("WatcherTitle"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Watcher.WatcherId = selectedPlayer.PlayerId;
                        Watcher.WatcherSelected = true;
                        ChatCommands.ShowChat($"Watcher: {selectedPlayer.name}");

                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.SetWatcher,
                            SendOption.Reliable,
                            -1
                        );

                        writer.Write(selectedPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                    if (GUILayout.Button(GetString("PUI_SetGuesser"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Guesser.SpecialKillerId = selectedPlayer.PlayerId;
                        Guesser.SpecialKillerSelected = true;
                        ChatCommands.ShowChat($"Guesser: {selectedPlayer.name}");

                        var writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId,
                            (byte)CustomRPC.SetSpecialKiller,
                            SendOption.Reliable,
                            -1
                        );

                        writer.Write(selectedPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }

                    if (GUILayout.Button(GetString("PUI_SetJester"), buttonStyle, GUILayout.Height(45)) && AmongUsClient.Instance.AmHost)
                    {
                        Jester.ForcedJesterId = selectedPlayer.PlayerId;
                        Jester.ForcedJesterSelected = true;
                        ChatCommands.ShowChat($"Jester: {selectedPlayer.name}");
                    }
                })
            };

            GUILayout.BeginHorizontal();

            foreach (var category in categories)
            {
                GUILayout.BeginVertical(GUILayout.Width(windowRect.width / 3f - 25f));
                GUILayout.Label(category.Title, titleStyle);
                GUILayout.Space(10);
                category.DrawContent();
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            if (GUILayout.Button(GetString("PUI_RemoveRoles"), buttonStyle1, GUILayout.Height(55)))
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    byte selectedId = selectedPlayer.PlayerId;

                    if (BanMod.forcedImpostorIds.Contains(selectedId))
                    {
                        BanMod.forcedImpostorIds.Remove(selectedId);
                    }

                    if (BanMod.forcedImpostorIds.Count == 0)
                    {
                        BanMod.forceImpostor = false;
                    }

                    ForcedRoleSystem.ClearForcedRole(selectedId);

                    if (Guesser.SpecialKillerSelected && Guesser.SpecialKillerId == selectedId)
                    {
                        Guesser.SpecialKillerId = 255;
                        Guesser.SpecialKillerSelected = false;
                    }

                    if (Jester.JesterSelected && Jester.JesterId == selectedId)
                    {
                        Jester.JesterId = 255;
                        Jester.JesterSelected = false;
                    }

                    if (Jester.ForcedJesterSelected && Jester.ForcedJesterId == selectedId)
                    {
                        Jester.ForcedJesterId = 255;
                        Jester.ForcedJesterSelected = false;
                    }

                    if (Judge.JudgeSelected && Judge.JudgeId == selectedId)
                    {
                        Judge.JudgeId = 255;
                        Judge.JudgeSelected = false;
                    }

                    if (Profiler.ProfilerSelected && Profiler.ProfilerId == selectedId)
                    {
                        Profiler.ProfilerId = 255;
                        Profiler.ProfilerSelected = false;
                    }

                    if (Exiler.ExilerSelected && Exiler.ExilerId == selectedId)
                    {
                        Exiler.ExilerId = 255;
                        Exiler.ExilerSelected = false;
                    }

                    if (Watcher.WatcherSelected && Watcher.WatcherId == selectedId)
                    {
                        Watcher.WatcherId = 255;
                        Watcher.WatcherSelected = false;
                    }
                    ChatCommands.ShowChat($"Removed: {selectedPlayer.name}");
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("ASSIGNED PLAYERS", titleStyle);
            GUILayout.Space(8);

            GUILayout.BeginVertical("box");
            DrawAssignedPlayersList();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(GetString("prevpage"), buttonStyle, GUILayout.Height(45)))
            {
                PlayerControl target = selectedPlayer;
                CloseActionMenu();

                if (target != null && PlayerUI.Instance != null)
                {
                    PlayerUI.Instance.ReopenActionMenu(target);
                }
            }

            if (GUILayout.Button(GetString("ExitButton"), buttonStyle1, GUILayout.Height(45)))
            {
                CloseActionMenu();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }
    }
}