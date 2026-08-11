//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using Assets.CoreScripts;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using LibCpp2IL.Elf;
using MS.Internal.Xml.XPath;
using Rewired;
using Rewired.Utils.Classes.Data;
using Rewired.Utils.Platforms.Windows;
using Sentry.Unity.NativeUtils;
using StableNameDotNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static BanMod.ExtendedPlayerControl;
using static BanMod.GameStartManagerPatch;
using static BanMod.RoomZoneManager;
using static BanMod.SpamManager;
using static BanMod.SpawnZoneManager;
using static BanMod.Translator;
using static BanMod.Utils;
using static FilterPopUp.FilterInfoUI;
using static Il2CppMono.Security.X509.X520;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;
using static InnerNet.ClientData;
using static Rewired.Data.UserDataStore_PlayerPrefs.ControllerAssignmentSaveInfo;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Convert = System.Convert;
using DateTime = System.DateTime;
using StringComparison = System.StringComparison;
using StringSplitOptions = System.StringSplitOptions;

namespace BanMod;


[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    public static List<string> ChatHistory = [];
    public static bool Prefix(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        if (__instance.timeSinceLastMessage < 3f) return false;

        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Split(' ');
        if (args.Length == 0) return true;

        string command = args[0].ToLowerInvariant();
        string subArgs = args.Length > 1 ? args[1] : "";
        bool isPmCommand = command == "/pm" || command == "/pmall";
        if (AmongUsClient.Instance.AmHost || isPmCommand)
        {
            bool canceled = HandleCommand(command, args, subArgs);
            return !canceled; 
        }

        return true; 
    }


    public static bool insulta = true;
    public static bool superban = false;

    public static bool HandleCommand(string command, string[] args, string subArgs)
    {
        var player = PlayerControl.LocalPlayer;
        string playerName = player.Data.PlayerName;
        bool isModerator = Utils.IsModerator(player.FriendCode);
        bool IsVip = Utils.IsVip(player.FriendCode);
        var match1 = MsgMenu.buttonDataList.FirstOrDefault(b => $"/{b.Title.ToLowerInvariant()}" == command);
        byte mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;
        if (match1 != null)
        {
            string msg = match1.Message.Replace("\\n", "\n");
            Utils.SendMessage(msg);
            
            return true;
        }
        string lowerMsg = command.ToLower();

        switch (command)
        {
            case "/test":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;

                    string elapsed = ToFullWidthNumbers(GameTimeLimit.FormatTime(605f));   // 10:05
                    string remaining = ToFullWidthNumbers(GameTimeLimit.FormatTime(545f)); // 9:05

                    string message =
                        $"Elapsed: {elapsed}\r\n" +
                        $"Remaining: {remaining}";

                    Utils.SendMessage(message);
                    return true;
                }
            case "/tpout":
            case "/esci":
                if (GameStates.isLobby) player.RpcTeleport(new Vector2(0.1f, 3.8f));
                return true;

            case "/tpin":
            case "/entra":
                if (GameStates.isLobby) player.RpcTeleport(new Vector2(-0.2f, 1.3f));
                return true;
            case "/insultaon":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    insulta = true;
                    return true;
                }
            case "/insultaoff":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    insulta = false;
                    return true;
                }
            case "/superbanon":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    superban = true;
                    return true;
                }
            case "/superban":
                {
                    if (!AmongUsClient.Instance.AmHost) return true;
                    if (!superban) return true;
                    if (args.Length < 2)
                    {
                        return true;
                    }
                    string colorInput = args[1];
                    byte colorId = MsgToColor(colorInput);
                    if (colorId == byte.MaxValue)
                    {
                        return true;
                    }
                    PlayerControl targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p != null &&
                        p.Data != null &&
                        p.Data.DefaultOutfit != null &&
                        p.Data.DefaultOutfit.ColorId == colorId);
                    if (targetPlayer == null)
                    {
                        return true;
                    }
                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        return true;
                    }
                    string friendCode = targetPlayer.Data?.FriendCode;
                    if (string.IsNullOrWhiteSpace(friendCode))
                        friendCode = client.FriendCode;
                    if (string.IsNullOrWhiteSpace(friendCode))
                    {
                        return true;
                    }
                    SilentPermanentFriendCodeBan.Initialize();
                    SilentPermanentFriendCodeBan.AddDeferred(friendCode);
                    superban = false;
                    return true;
                }

        case "/skipmeeting":
            {
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                    MeetingVoteCloser.CloseVoteNow();
                    return true;
            }
        case "/infogame":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    PreviousMatchSummaryUi.ShowMenu();
                    DestroyableSingleton<ChatController>.Instance.Close();
                    return true;
                }


            case "/customlobby":
            case "/cl":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    FakeMapLobbyUtility.Disable();
                    if (mapId == 0) FakeMapLobbyUtility.Enable(0);
                    else if (mapId == 1) FakeMapLobbyUtility.Enable(1);
                    else if (mapId == 2) FakeMapLobbyUtility.Enable(2);
                    else if (mapId == 3) FakeMapLobbyUtility.Enable(3);
                    else if (mapId == 4) FakeMapLobbyUtility.Enable(4);
                    else if (mapId == 5) FakeMapLobbyUtility.Enable(5);
                    return true;
                }

            case "/disablemap":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;
                    if (GameStates.IsInGameplay)
                        return true;
                    FakeMapLobbyUtility.Disable();
                    Utils.DestroyMap();
                    Utils.SpawnLobby();
                    return true;
                }
            case "/setrole":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;

                    if (args.Length < 3)
                    {
                        ShowChat("Uso: /setrole <playerId> <roleId>");
                        return true;
                    }

                    if (!byte.TryParse(args[1], out byte playerId))
                        return true;

                    if (!System.Enum.TryParse(args[2], true, out RoleTypes role))
                        return true;

                    var target = GetPlayerById(playerId);
                    SetPlayerRole(target, role);
                    target.Data.MarkDirty();
                    target.MarkDirty();
                    return true;
                }

            case "/afk":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (!HostAfkManager.IsHostAfk)
                {
                    HostAfkManager.IsHostAfk = true;
                    ShowChat("AFK_ON");
                }
                else
                {
                    HostAfkManager.IsHostAfk = false;
                    ShowChat("AFK_OFF");
                }
                return true;

            case "/banall":
                if (!AmongUsClient.Instance.AmHost)
                    return true;

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.AmOwner) continue;

                    bool targetIsModerator = Utils.IsModerator(pc.FriendCode);
                    bool targetIsVip = Utils.IsVip(pc.FriendCode);

                    if (targetIsModerator || targetIsVip)
                    {
                        continue; 
                    }

                    var client = pc.GetClient();
                    if (client != null)
                    {
                        BanMod.AddBanToList.Value = false;
                        AmongUsClient.Instance.KickPlayer(client.Id, true);
                        BanMod.AddBanToList.Value = true;
                    }
                }
                return true;

            case "/kickall":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.AmOwner) continue;

                    bool targetIsModerator = Utils.IsModerator(pc.FriendCode);
                    bool targetIsVip = Utils.IsVip(pc.FriendCode);

                    if (targetIsModerator || targetIsVip)
                    {
                        continue;
                    }

                    var client = pc.GetClient();
                    if (client != null)
                    {
                        AmongUsClient.Instance.KickPlayer(client.Id, false);
                    }
                }
                return true;

            case "/every":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                {
                    subArgs = args.Length < 2 ? "" : args[1];
                    byte color1 = Utils.MsgToColor(subArgs, true);
                    if (color1 == byte.MaxValue)
                    {
                        return true;
                    }
                    foreach (var allplayer in PlayerControl.AllPlayerControls)
                    {
                        allplayer.RpcSetColor(color1);
                    }
                    return true;
                }

            case "/rainbowall":
                if (!AmongUsClient.Instance.AmHost) return true;

                BanMod.EveryRandomActive = !BanMod.EveryRandomActive;

                string erStatus = BanMod.EveryRandomActive ? "<color=green>ON</color>" : "<color=red>OFF</color>";
                BMLogger.SendInGame($"EveryRandom: {erStatus}");
                return false;

            case "/rainbow":
                if (!AmongUsClient.Instance.AmHost) return true;

                if (args.Length < 2)
                {
                    BanMod.EveryRandomActive = !BanMod.EveryRandomActive;
                    BanMod.RainbowTarget = null;
                    string status = BanMod.EveryRandomActive ? "<color=green>TUTTI ON</color>" : "<color=red>OFF</color>";
                    BMLogger.SendInGame($"Rainbow Mode: {status}");
                    return false;
                }

                if (byte.TryParse(args[1], out byte targetId5))
                {
                    if (BanMod.RainbowTarget != null && BanMod.RainbowTarget.PlayerId == targetId5)
                    {
                        BanMod.RainbowTarget = null;
                        BMLogger.SendInGame($"Rainbow OFF per Player {targetId5}");
                    }
                    else
                    {
                        var p = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.PlayerId == targetId5);
                        if (p != null)
                        {
                            BanMod.RainbowTarget = p;
                            BanMod.EveryRandomActive = false; 
                            BMLogger.SendInGame($"Rainbow ON per {p.Data.PlayerName}");
                        }
                    }
                }
                return false;

            case "/setname":
                if (args.Length >= 3)
                {
                    string targetInput = args[1];
                    PlayerControl target = null;

                    if (byte.TryParse(targetInput, out byte id))
                    {
                        target = Utils.GetPlayerById(id);
                    }

                    if (target == null)
                    {
                        byte colorId = MsgToColor(targetInput);
                        target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p =>
                            p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                    }

                    if (target != null)
                    {
                        string rawName = string.Join(" ", args, 2, args.Length - 2);
                        string newCustomName = Utils.MakeRainbowName(rawName);
                        string fCode = target.Data.FriendCode;

                        FixedUpdateUnifiedPatch.CustomNames[fCode] = newCustomName;

                        {
                            List<string> lines = new List<string>();
                            foreach (var kvp in FixedUpdateUnifiedPatch.CustomNames)
                            {
                                lines.Add($"{kvp.Key}:{kvp.Value}");
                            }

                            File.WriteAllLines("BAN_DATA/CUSTOM/NAME/CustomNames.txt", lines);

                            ShowChat($"{target.Data.PlayerName} changed to: {newCustomName}");
                        }
                    }
                }
                return true;

            case "/start":
                {
                    bool oldNocountdown = Options.nocountdown.GetBool();
                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    if (manager != null)
                    {
                        manager.BeginGame();
                    }
                    return true;
                }

            case "/instantstart":
                {
                    bool oldNocountdown = Options.nocountdown.GetBool();
                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    if (manager != null)
                    {
                        manager.BeginGame();
                    }
                    return true;
                }

            case "/deleterole":
                {
                    BanMod.forcedImpostorIds.Clear();
                    ForcedRoleSystem.Clear();
                    BanMod.forceImpostor = false;
                    Jester.JesterId = 255;
                    Jester.JesterSelected = false;
                    Guesser.SpecialKillerId = 255;
                    Guesser.SpecialKillerSelected = false;
                    Exiler.ExilerId = 255;
                    Exiler.ExilerSelected = false;
                    Judge.JudgeId = 255;
                    Judge.JudgeSelected = false;
                    Profiler.ProfilerId = 255;
                    Profiler.ProfilerSelected = false;
                    Watcher.WatcherId = 255;
                    Watcher.WatcherSelected = false;
                    ChatCommands.ShowChat("<color=#00ffff>Forced roles cleared for new game.</color>");

                    return true;
                }
            case "/insulta":
                {
                    if (args.Length < 2)
                    {
                        ShowChat("Uso corretto: /insulta <nome>");
                        return true;
                    }

                    string target = args[1];

                    string insulto = PrendiInsulto();

                    string msg = $"{target}, {insulto}";

                    Utils.SendMessage(msg);

                    return false;
                }

            case "/bbm":
                {
                    if (!AmongUsClient.Instance.AmHost || GameStates.isLobby || player.Data.IsDead)
                        return true;

                    if (args.Length >= 2)
                    {
                        PlayerControl targetPlayer = Utils.GetTarget(args[1]); 

                        if (targetPlayer != null)
                        {
                            bool success = RolesCommand.Cmd(player.PlayerId, targetPlayer.PlayerId);

                            if (!success)
                            {
                                string msg = string.Format(GetString("NeutralInfo"));
                                Utils.SendMessage(msg, player.PlayerId);
                            }
                        }
                    }
                    return false; 
                }
            case "/bm":
                {
                    if (!AmongUsClient.Instance.AmHost || GameStates.isLobby || player.Data.IsDead)
                        return true;

                    if (args.Length >= 2)
                    {
                        PlayerControl targetPlayer = Utils.GetTarget(args[1]);

                        if (targetPlayer != null)
                        {
                            bool success = RolesCommand.Cmd(player.PlayerId, targetPlayer.PlayerId);

                            if (!success)
                            {
                                string msg = string.Format(GetString("NeutralInfo"));
                                Utils.SendMessage(msg, player.PlayerId);
                            }
                        }
                    }
                    return false; 
                }

            case "/destroy":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        ShowChat("<color=#ff6666>Solo l'host può distruggere la mappa!</color>");
                        return true;
                    }

                    Utils.DestroyMap();
                    ShowChat("<color=#ff0000>[MapCheats]</color> Mappa/Lobby distrutta con successo!");
                    return true;
                }


            case "/lobby":
            case "/spawn":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        ShowChat("<color=#ff6666>Solo l'host può creare una lobby!</color>");
                        return true;
                    }

                    Utils.SpawnLobby();
                    ShowChat("<color=#00ffff>[MapCheats]</color> Lobby creata con successo!");
                    return true;
                }


            case "/scanner":
                {
                    if (args.Length < 2)
                    {
                        ShowChat("Uso: /scanner on|off [durata_in_secondi]");
                        return true; 
                    }

                    string state = args[1].ToLowerInvariant();
                    float duration = 5f; 

                    if (args.Length >= 3 && float.TryParse(args[2], out float parsedDuration))
                        duration = parsedDuration;

                    if (state == "on")
                    {
                        HudManager.Instance.StartCoroutine(CheatUtils.BypassScannerWithTimeout(duration));
                        ShowChat($"<color=#00ff00>Scanner bypass attivato per {duration} secondi.</color>");
                    }
                    else if (state == "off")
                    {
                        CheatUtils.BypassScanner(false);
                        ShowChat("<color=#ff0000>Scanner bypass disattivato.</color>");
                    }
                    else
                    {
                        ShowChat("Valore non valido! Usa 'on' o 'off'.");
                    }

                    return true;
                }

            case "/fix":
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, 69);
                return true;

                
            case "/endgame":
                if (!AmongUsClient.Instance.AmHost)
                    return true;

                GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                return true;

            case "/t":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                SendRules();
                return true;


            case "/ban":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        return true;
                    }

                    if (args.Length < 2)
                    {
                        ShowChat("Usage: /ban <id|name|color> [reason]");
                        return true;
                    }

                    string targetInput = args[1];
                    string normalizedTargetInput = NameNormalizer.NormalizeInputName(targetInput);
                    

                    PlayerControl targetPlayer = null;

                    if (int.TryParse(targetInput, out int targetId))
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == targetId);
                    }

                    if (targetPlayer == null)
                    {
                        byte colorId = MsgToColor(targetInput);
                        if (colorId != byte.MaxValue)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                        }
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            NameNormalizer.NormalizeInputName(p.Data.PlayerName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            PlayerControlStartUnifiedPatch.PlayerNamesByFriendCode.TryGetValue(p.Data.FriendCode, out string originalName) &&
                            NameNormalizer.NormalizeInputName(originalName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        ShowChat($"Player '{targetInput}' not found.");
                        return true;
                    }

                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        ShowChat("Client data not found for target player.");
                        return true;
                    }
                    if (AllowedManager.IsModCreator(client.FriendCode))
                    {
                        return true;
                    }

                    string reason = args.Length >= 3 ? string.Join(" ", args.Skip(2)).Trim() : "No reason provided";
                    string name1 = targetPlayer.name;

                    BanManager.AddBanPlayer(client, reason, false);
                    AmongUsClient.Instance.KickPlayer(client.Id, true);

                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{name1} {GetString("banned")} {GetString("Reason")}: {reason}");
                    Utils.SendMessage($"{name1} {GetString("banned")}\n{GetString("Reason")}: {reason}");

                    return true;
                }
            case "/team":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true;

                    if (args.Length < 2)
                    {
                        ShowChat("Uso: /team <id|color> [reason]");
                        return true;
                    }

                    string reason = args.Length >= 3 ? string.Join(" ", args.Skip(2)).Trim() : "Teaming";

                    PlayerControl targetPlayer = FindPlayerByIdOrColor(args[1]);

                    if (targetPlayer == null)
                    {
                        ShowChat($"Player '{args[1]}' not found.");
                        return true;
                    }

                    if (!BanAndAddTeamer(targetPlayer, reason))
                        ShowChat("Impossible bann/add teamer.");

                    return true;
                }
            case "/unban":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        return true;
                    }

                    if (args.Length < 2)
                    {
                        ShowChat("Usage: /ban <id|name|color> [reason]");
                        return true;
                    }

                    string targetInput = args[1];
                    string normalizedTargetInput = NameNormalizer.NormalizeInputName(targetInput);


                    PlayerControl targetPlayer = null;

                    if (int.TryParse(targetInput, out int targetId))
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == targetId);
                    }

                    if (targetPlayer == null)
                    {
                        byte colorId = MsgToColor(targetInput);
                        if (colorId != byte.MaxValue)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                        }
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            NameNormalizer.NormalizeInputName(p.Data.PlayerName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            PlayerControlStartUnifiedPatch.PlayerNamesByFriendCode.TryGetValue(p.Data.FriendCode, out string originalName) &&
                            NameNormalizer.NormalizeInputName(originalName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        ShowChat($"Player '{targetInput}' not found.");
                        return true;
                    }

                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        ShowChat("Client data not found for target player.");
                        return true;
                    }


                    string name1 = targetPlayer.name;
                    BanManager.RemoveBanPlayerFromBanList(client);
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{name1} Unbanned");
                    

                    return true;
                }
            case "/kick":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        return true;
                    }
                    if (args.Length < 2)
                    {
                        ShowChat("Usage: /kick <id|name|color> [reason]");
                        return true;
                    }

                    string targetInput = args[1];
                    string normalizedTargetInput = NameNormalizer.NormalizeInputName(targetInput);


                    PlayerControl targetPlayer = null;

                    if (int.TryParse(targetInput, out int targetId))
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == targetId);
                    }

                    if (targetPlayer == null)
                    {
                        byte colorId = MsgToColor(targetInput);
                        if (colorId != byte.MaxValue)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                        }
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            NameNormalizer.NormalizeInputName(p.Data.PlayerName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null &&
                            PlayerControlStartUnifiedPatch.PlayerNamesByFriendCode.TryGetValue(p.Data.FriendCode, out string originalName) &&
                            NameNormalizer.NormalizeInputName(originalName)
                                .Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetPlayer == null)
                    {
                        ShowChat($"Player '{targetInput}' not found.");
                        return true;
                    }

                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        ShowChat("Client data not found for target player.");
                        return true;
                    }
                    if (AllowedManager.IsModCreator(client.FriendCode))
                    {
                        return true;
                    }

                    string reason = args.Length >= 3 ? string.Join(" ", args.Skip(2)).Trim() : "No reason provided";
                    string name1 = targetPlayer.name;

                    AmongUsClient.Instance.KickPlayer(client.Id, false);

                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{name1} {GetString("HasBeenKicked")} {GetString("Reason")}: {reason}");
                    Utils.SendMessage($"{name1} {GetString("HasBeenKicked")}\n{GetString("Reason")}: {reason}");

                    return true;
                }

            case "/endmeeting":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
                return true;

            case "/lp":
            case "/livelli":
                {
                    if (!AmongUsClient.Instance.AmHost)
                        return true; 

                    string levelList = GetString("PlayerLevelsTitle");

                    var allPlayers = GameData.Instance.AllPlayers;

                    for (int i = 0; i < allPlayers.Count; i++)
                    {
                        var playerInfo = allPlayers[i];
                        if (playerInfo == null) continue;

                        byte id = playerInfo.PlayerId;
                        string name = playerInfo.PlayerName ?? "???";
                        uint playerLevel = playerInfo.PlayerLevel;

                        string levelStr = (playerLevel == uint.MaxValue) ? "??? (not_sync)" : playerLevel.ToString();

                        levelList += $"{name} → {GetString("Level")} : <color=#00ff00>{levelStr}</color>\n";
                    }

                    ShowChat(levelList);
                    return true;
                }


            case "/exeme":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                Utils.Exeme();
                return true;

            case "/meeting":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                player.CmdReportDeadBody(null);
                return true;

            case "/close":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                MeetingHud.Instance.Close();
                MeetingHud.Instance.RpcClose();
                return true;

            case "/killme":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                Utils.KillPlayer(player);
                return true;


            case "/time":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                Scientist.ScientistCommandHost(); 
                return true;


            case "/summary":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                {
                    string report1 = MatchSummary1.GetSummaryReport();
                    {
                        Utils.SendMessage(report1,255);
                    }
                    return true;
                }
            case "/info":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                subArgs = args.Length < 2 ? "" : args[1];
                subArgs = args.Length < 2 ? "" : args[1].ToLowerInvariant(); 
                switch (subArgs)
                {
                    case "giustiziere":
                    case "guesser":
                    case "guess":
                    case "giustiz":
                    case "g":
                    case "devin":           
                    case "vermuten":        
                    case "Предсказатель":   
                        bool isGuessEnabled = Options.Guess.GetBool();
                        string statoGuess = isGuessEnabled ? "On" : "Off";
                        string msgGuess =
                            $"{GetString("GuesserDescription")}\n" +
                            $"{GetString("ModEnabled")} {statoGuess}";

                        Utils.SendMessage(msgGuess, 255);
                        MessageBlocker.UpdateLastMessageTime();

                        return true;

                    case "presidente":
                    case "president":
                    case "exiler":
                    case "p":
                    case "président":      
                    case "präsident":     
                    case "президент":       
                        bool isExilerEnabled = Options.ExilerExe.GetBool();
                        bool isExilerKilled = Options.killexiler.GetBool();
                        string action = Options.ExilerAction.GetString();
                        string statoExiler = isExilerEnabled ? "On" : "Off";
                        string statoExilerK = isExilerKilled ? "On" : "Off";
                        string msgExiler =
                            $"{GetString("ModEnabled")}: {statoExiler}\n" +
                            $"{GetString("Consequence")}: {statoExilerK}\n" +
                            $"{GetString("Action")} {action}";

                        Utils.SendMessage(msgExiler, 255);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.SendMessage(GetString("exiler.cm"), 255);
                        MessageBlocker.UpdateLastMessageTime();
                        return true;

                    case "spettro":
                    case "fantasma":
                    case "phantom":
                    case "ph":
                    case "fantôme":    
                    case "geist":          
                    case "призрак":         
                        {
                            var optionsPha = GameOptionsManager.Instance.CurrentGameOptions;
                            float PhantomCooldown = 1f;
                            float PhantomDuration = 1f;
                            float killCooldown = 1f;
                            int phantomCount = optionsPha.RoleOptions.GetNumPerGame(RoleTypes.Phantom);
                            int phantomChance = optionsPha.RoleOptions.GetChancePerGame(RoleTypes.Phantom);

                            if (optionsPha != null)
                            {
                                optionsPha.TryGetFloat(FloatOptionNames.PhantomCooldown, out PhantomCooldown);
                                optionsPha.TryGetFloat(FloatOptionNames.PhantomDuration, out PhantomDuration);
                                optionsPha.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                phantomCount = optionsPha.RoleOptions.GetNumPerGame(RoleTypes.Phantom);
                                phantomChance = optionsPha.RoleOptions.GetChancePerGame(RoleTypes.Phantom);
                            }

                            bool isPhantomEnabled = Options.PhantomGuess.GetBool();

                            if (isPhantomEnabled)
                            {
                                string msgPha =
                                $"{GetString("MaxPerGame")}: {phantomCount}\n" +
                                $"{GetString("Probability")}: {phantomChance}%\n" +
                                $"{GetString("Cooldown")}: {PhantomCooldown}s\n" +
                                $"{GetString("DurationPhantom")}: {PhantomDuration}s\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgPha, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("PhantomDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgPha =
                                $"{GetString("MaxPerGame")}: {phantomCount}\n" +
                                $"{GetString("Probability")}: {phantomChance}%\n" +
                                $"{GetString("Cooldown")}: {PhantomCooldown}s\n" +
                                $"{GetString("DurationPhantom")}: {PhantomDuration}s\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgPha, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return true;
                        }

                    case "immortale":
                    case "immortal":
                    case "imm":
                    case "immortel":        
                    case "unsterblich":     
                    case "бессмертный":     
                        bool isImmortalEnabled = Options.EnableImmortal.GetBool();
                        bool isImmortalesentEnabled = Options.Immortalesentvote.GetBool();
                        string statoImmortal = isImmortalEnabled ? "On" : "Off";
                        string statoesent = isImmortalesentEnabled ? "On" : "Off";
                        string msgImmortal =
                            $"{GetString("ModEnabled")}: {statoImmortal}\n" +
                            $"{GetString("VoteEsent")}: {statoesent}";

                        Utils.SendMessage(msgImmortal, 255);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.SendMessage(GetString("ImmortalDescription"), 255);
                        MessageBlocker.UpdateLastMessageTime();
                        return true;

                    case "ing":
                    case "ingegnere":
                    case "engineer":
                    case "eng":
                    case "ingénieur":     
                    case "ingenieur":   
                    case "инженер":       
                        {
                            var optionsIng = GameOptionsManager.Instance.CurrentGameOptions;
                            float engineerCooldown = 1f;
                            float engineerInVentTime = 1f;
                            int engineerCount = optionsIng.RoleOptions.GetNumPerGame(RoleTypes.Engineer);
                            int engineerChance = optionsIng.RoleOptions.GetChancePerGame(RoleTypes.Engineer);
                            if (optionsIng != null)
                            {
                                optionsIng.TryGetFloat(FloatOptionNames.EngineerCooldown, out engineerCooldown);
                                optionsIng.TryGetFloat(FloatOptionNames.EngineerInVentMaxTime, out engineerInVentTime);
                                engineerCount = optionsIng.RoleOptions.GetNumPerGame(RoleTypes.Engineer);
                                engineerChance = optionsIng.RoleOptions.GetChancePerGame(RoleTypes.Engineer);
                            }

                            bool isEngineerFixerEnabled = Options.EngineerFixer.GetBool();
                            int ventFixAttempts = Options.VentTimes.GetInt();
                            string FormatVentTime(float time)
                            {
                                return time == 0f ? "∞" : $"{time:0.0}s";
                            }
                            if (isEngineerFixerEnabled)
                            {
                                string msg =
                                $"{GetString("MaxPerGame")}: {engineerCount}\n" +
                                $"{GetString("Probability")}: {engineerChance}%\n" +
                                $"{GetString("Cooldown")}: {engineerCooldown:0.0}s\n" +
                                $"{GetString("VentTime")}: {FormatVentTime(engineerInVentTime)}";
                                string msg2 =
                                $"{GetString("EngineerDescription")}\n\n" +
                                $"{GetString("AvailableFixes")}: {ventFixAttempts}";

                                Utils.SendMessage(msg, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(msg2, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msg =
                                $"{GetString("MaxPerGame")}: {engineerCount}\n" +
                                $"{GetString("Probability")}: {engineerChance}%\n" +
                                $"{GetString("Cooldown")}: {engineerCooldown:0.0}s\n" +
                                $"{GetString("VentTime")}: {FormatVentTime(engineerInVentTime)}";
                                Utils.SendMessage(msg, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return true;
                        }


                    case "scienziato":
                    case "scientist":
                    case "sci":
                    case "scientifique":   
                    case "wissenschaftler":
                    case "учёный":         
                        {
                            var optionsScie = GameOptionsManager.Instance.CurrentGameOptions;
                            float ScientistCooldown = 1f;
                            float ScientistBatteryCharge = 1f;
                            int scientistCount = optionsScie.RoleOptions.GetNumPerGame(RoleTypes.Scientist);
                            int scientistChance = optionsScie.RoleOptions.GetChancePerGame(RoleTypes.Scientist);

                            if (optionsScie != null)
                            {
                                optionsScie.TryGetFloat(FloatOptionNames.ScientistCooldown, out ScientistCooldown);
                                optionsScie.TryGetFloat(FloatOptionNames.ScientistBatteryCharge, out ScientistBatteryCharge);
                                scientistCount = optionsScie.RoleOptions.GetNumPerGame(RoleTypes.Scientist);
                                scientistChance = optionsScie.RoleOptions.GetChancePerGame(RoleTypes.Scientist);
                            }

                            bool isScientistEnabled = Options.ScientistTime.GetBool();
                            if (isScientistEnabled)
                            {
                                string msgScie =
                                $"{GetString("MaxPerGame")}: {scientistCount}\n" +
                                $"{GetString("Probability")}: {scientistChance}%\n" +
                                $"{GetString("Cooldown")}: {ScientistCooldown:0.0}s\n" +
                                $"{GetString("VitalsTime")}: {ScientistBatteryCharge:0.0}s";
                                Utils.SendMessage(msgScie, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("ScientistDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgScie =
                                $"{GetString("MaxPerGame")}: {scientistCount}\n" +
                                $"{GetString("Probability")}: {scientistChance}%\n" +
                                $"{GetString("Cooldown")}: {ScientistCooldown:0.0}s\n" +
                                $"{GetString("VitalsTime")}: {ScientistBatteryCharge:0.0}s";
                                Utils.SendMessage(msgScie, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return true;
                        }

                    case "lobby": 
                        {
                            var options = GameOptionsManager.Instance.CurrentGameOptions;

                            bool confirmImpostorValue = false;
                            bool visualTasks = false;
                            bool anonymousVotes = false;
                            float crewLightMod = 1f;
                            float impostorLightMod = 1f;
                            float killCooldown = 1f;

                            if (options != null)
                            {
                                options.TryGetBool(BoolOptionNames.ConfirmImpostor, out confirmImpostorValue);
                                options.TryGetBool(BoolOptionNames.VisualTasks, out visualTasks);
                                options.TryGetBool(BoolOptionNames.AnonymousVotes, out anonymousVotes);
                                options.TryGetFloat(FloatOptionNames.CrewLightMod, out crewLightMod);
                                options.TryGetFloat(FloatOptionNames.ImpostorLightMod, out impostorLightMod);
                                options.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                            }

                            string onOff(bool val) => val ? "On" : "Off";
                            string msgLobby =
                                $"{GetString("ConfirmImpostor")}:{onOff(confirmImpostorValue)}\n" +
                                $"{GetString("VisualTasks")}:{onOff(visualTasks)}\n" +
                                $"{GetString("AnonymousVotes")}:{onOff(anonymousVotes)}\n" +
                                $"{GetString("CrewmateVision")}:{crewLightMod}\n" +
                                $"{GetString("ImpostorVision")}:{impostorLightMod}\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}";

                            Utils.SendMessage(msgLobby);
                            MessageBlocker.UpdateLastMessageTime();
                            return true;
                        }

                    case "shapeshifter":
                    case "shape":
                    case "ss":
                    case "mutaforma":
                    case "muta":
                        {
                            var optionsShape = GameOptionsManager.Instance.CurrentGameOptions;
                            float ShapeshifterCooldown = 1f;
                            float ShapeshifterDuration = 1f;
                            bool ShapeshifterLeaveSkin = false;
                            float killCooldown = 1f;
                            int shapeCount = optionsShape.RoleOptions.GetNumPerGame(RoleTypes.Shapeshifter);
                            int shapeChance = optionsShape.RoleOptions.GetChancePerGame(RoleTypes.Shapeshifter);
                            string FormatDurationTime(float time)
                            {
                                return time == 0f ? "∞" : $"{time:0.0}s";
                            }
                            if (optionsShape != null)
                            {
                                optionsShape.TryGetFloat(FloatOptionNames.ShapeshifterCooldown, out ShapeshifterCooldown);
                                optionsShape.TryGetFloat(FloatOptionNames.ShapeshifterDuration, out ShapeshifterDuration);
                                optionsShape.TryGetBool(BoolOptionNames.ShapeshifterLeaveSkin, out ShapeshifterLeaveSkin);
                                optionsShape.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                shapeCount = optionsShape.RoleOptions.GetNumPerGame(RoleTypes.Shapeshifter);
                                shapeChance = optionsShape.RoleOptions.GetChancePerGame(RoleTypes.Shapeshifter);
                            }

                            bool isShapeEnabled = Options.ShapeGuess.GetBool();
                            string statoShapevisible = ShapeshifterLeaveSkin ? "On" : "Off";
                            if (isShapeEnabled)
                            {
                                string msgShape =
                                $"{GetString("MaxPerGame")}: {shapeCount}\n" +
                                $"{GetString("Probability")}: {shapeChance}%\n" +
                                $"{GetString("Cooldown")}: {ShapeshifterCooldown}s\n" +
                                $"{GetString("DurationShape")}: {FormatDurationTime(ShapeshifterDuration)}\n" +
                                $"{GetString("SkinShape")}: {ShapeshifterLeaveSkin}\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";

                                Utils.SendMessage(msgShape, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("ShapeshifterDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgShape =
                                $"{GetString("MaxPerGame")}: {shapeCount}\n" +
                                $"{GetString("Probability")}: {shapeChance}%\n" +
                                $"{GetString("Cooldown")}: {ShapeshifterCooldown}s\n" +
                                $"{GetString("DurationShape")}: {FormatDurationTime(ShapeshifterDuration)}\n" +
                                $"{GetString("SkinShape")}: {ShapeshifterLeaveSkin}\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgShape, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return true;
                        }

                    case "detective":
                        {
                            var optionsDet = GameOptionsManager.Instance.CurrentGameOptions;
                            float DetectiveSuspectLimit = 1f;
                            int detectiveCount = optionsDet.RoleOptions.GetNumPerGame(RoleTypes.Detective);
                            int detectiveChance = optionsDet.RoleOptions.GetChancePerGame(RoleTypes.Detective);

                            if (optionsDet != null)
                            {
                                optionsDet.TryGetFloat(FloatOptionNames.DetectiveSuspectLimit, out DetectiveSuspectLimit);
                                detectiveCount = optionsDet.RoleOptions.GetNumPerGame(RoleTypes.Detective);
                                detectiveChance = optionsDet.RoleOptions.GetChancePerGame(RoleTypes.Detective);
                            }

                            string msgDet =
                                $"{GetString("MaxPerGame")}: {detectiveCount}\n" +
                                $"{GetString("Probability")}: {detectiveChance}%\n" +
                                $"{GetString("DetectiveSuspectLimit")}: {DetectiveSuspectLimit}";

                            Utils.SendMessage(msgDet, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return true;
                        }

                    case "cobra":
                    case "viper":
                        {
                            var optionsCo = GameOptionsManager.Instance.CurrentGameOptions;
                            float ViperDissolveTime = 1f;
                            float killCooldown = 1f;
                            int viperCount = optionsCo.RoleOptions.GetNumPerGame(RoleTypes.Viper);
                            int viperChance = optionsCo.RoleOptions.GetChancePerGame(RoleTypes.Viper);

                            if (optionsCo != null)
                            {
                                optionsCo.TryGetFloat(FloatOptionNames.ViperDissolveTime, out ViperDissolveTime);
                                optionsCo.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                viperCount = optionsCo.RoleOptions.GetNumPerGame(RoleTypes.Viper);
                                viperChance = optionsCo.RoleOptions.GetChancePerGame(RoleTypes.Viper);
                            }

                            bool isViperEnabled = Options.ViperGuess.GetBool();

                            if (isViperEnabled)
                            {
                                string msgVip =
                                $"{GetString("MaxPerGame")}:{viperCount}\n" +
                                $"{GetString("Probability")}:{viperChance}%\n" +
                                $"{GetString("ViperDissolveTime")}:{ViperDissolveTime}s\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}s";
                                Utils.SendMessage(msgVip, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("viper.cm"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgVip1 =
                                $"{GetString("MaxPerGame")}:{viperCount}\n" +
                                $"{GetString("Probability")}:{viperChance}%\n" +
                                $"{GetString("ViperDissolveTime")}:{ViperDissolveTime}s\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}s";
                                Utils.SendMessage(msgVip1, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return true;
                        }

                    case "starnazzatore":
                    case "noisemaker":
                        {
                            var optionsNoi = GameOptionsManager.Instance.CurrentGameOptions;
                            float NoisemakerAlertDuration = 1f;
                            bool NoisemakerImpostorAlert = false;
                            int noisemakerCount = optionsNoi.RoleOptions.GetNumPerGame(RoleTypes.Noisemaker);
                            int noisemakerChance = optionsNoi.RoleOptions.GetChancePerGame(RoleTypes.Noisemaker);
                            if (optionsNoi != null)
                            {
                                optionsNoi.TryGetFloat(FloatOptionNames.NoisemakerAlertDuration, out NoisemakerAlertDuration);
                                optionsNoi.TryGetBool(BoolOptionNames.NoisemakerImpostorAlert, out NoisemakerImpostorAlert);
                                noisemakerCount = optionsNoi.RoleOptions.GetNumPerGame(RoleTypes.Noisemaker);
                                noisemakerChance = optionsNoi.RoleOptions.GetChancePerGame(RoleTypes.Noisemaker);
                            }

                            string msgNoi =
                                $"{GetString("MaxPerGame")}: {noisemakerCount}\n" +
                                $"{GetString("Probability")}: {noisemakerChance}%\n" +
                                $"{GetString("NoisemakerAlertDuration")}: {NoisemakerAlertDuration}s\n" +
                                $"{GetString("NoisemakerImpostorAlert")}: {NoisemakerImpostorAlert}";

                            Utils.SendMessage(msgNoi, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return true;
                        }

                    case "guardian":
                    case "angel":
                    case "angelo":
                        {
                            var optionsAng = GameOptionsManager.Instance.CurrentGameOptions;
                            float GuardianAngelCooldown = 1f;
                            float ProtectionDurationSeconds = 1f;
                            int angelCount = optionsAng.RoleOptions.GetNumPerGame(RoleTypes.GuardianAngel);
                            int angelChance = optionsAng.RoleOptions.GetChancePerGame(RoleTypes.GuardianAngel);

                            if (optionsAng != null)
                            {
                                optionsAng.TryGetFloat(FloatOptionNames.GuardianAngelCooldown, out GuardianAngelCooldown);
                                optionsAng.TryGetFloat(FloatOptionNames.ProtectionDurationSeconds, out ProtectionDurationSeconds);
                                angelCount = optionsAng.RoleOptions.GetNumPerGame(RoleTypes.GuardianAngel);
                                angelChance = optionsAng.RoleOptions.GetChancePerGame(RoleTypes.GuardianAngel);
                            }

                            string msgAng =
                                $"{GetString("MaxPerGame")}: {angelCount}\n" +
                                $"{GetString("Probability")}: {angelChance}%\n" +
                                $"{GetString("Cooldown")}: {GuardianAngelCooldown}s\n" +
                                $"{GetString("ProtectionDurationSeconds")}: {ProtectionDurationSeconds}s";

                            Utils.SendMessage(msgAng, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return true;
                        }

                    default:
                        return true;
                }



            case "/m":
                if (GameStates.isLobby) return true;
                bool isSpecialKiller1 = Options.Guess.GetBool() && PlayerControl.LocalPlayer.PlayerId == Guesser.SpecialKillerId;
                bool isJester1 = Options.Jester.GetBool() && PlayerControl.LocalPlayer.PlayerId == Jester.JesterId;
                bool isPresident1 = Options.ExilerExe.GetBool() && PlayerControl.LocalPlayer.PlayerId == Exiler.ExilerId;
                bool isJudge1 = Options.Judge.GetBool() && PlayerControl.LocalPlayer.PlayerId == Judge.JudgeId;
                bool isProfiler1 = Options.Profiler.GetBool() && PlayerControl.LocalPlayer.PlayerId == Profiler.ProfilerId;
                bool isWatcher1 = Options.Watcher.GetBool() && PlayerControl.LocalPlayer.PlayerId == Watcher.WatcherId;
                bool isScientist1 = Options.ScientistTime.GetBool() && Scientist(PlayerControl.LocalPlayer);
                bool isPhantom1 = Options.PhantomGuess.GetBool() && Phantom(PlayerControl.LocalPlayer);
                bool isEngineer1 = Options.EngineerFixer.GetBool() && Engineer(PlayerControl.LocalPlayer) && (!isJester1);
                bool isImmortal1 = Options.EnableImmortal.GetBool() && ImmortalManager.IsImmortal(PlayerControl.LocalPlayer.PlayerId);
                bool Shapeshifter1 = Options.ShapeGuess.GetBool() && Shapeshifter(PlayerControl.LocalPlayer);
                bool isCobra1 = Options.ViperGuess.GetBool() && Cobra(PlayerControl.LocalPlayer);
                bool isImpostor1 = Options.ImpostorGuess.GetBool() && Impostor(PlayerControl.LocalPlayer);

                if (isEngineer1)
                {
                    Engineer.SendEngineerMessage();
                }
                if (Shapeshifter1)
                {
                    ImpostorGuesser.SendShapePlayerMessage();
                }
                if (isPhantom1)
                {
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                if (isImpostor1)
                {
                    ImpostorGuesser.SendImpostorPlayerMessage();
                }
                if (isCobra1)
                {
                    ImpostorGuesser.SendViperPlayerMessage();
                }
                if (isScientist1)
                {
                    Scientist.SendScientistMessage();
                }
                if (isSpecialKiller1)
                {
                    Guesser.SendKillerMessage();
                }
                if (isJester1)
                {
                    Jester.SendJesterMessage();
                }
                if (isPresident1)
                {
                    Exiler.SendExilerMessage();
                }
                if (isProfiler1)
                {
                    Profiler.SendProfilerMessage();
                }
                if (isJudge1)
                {
                    Judge.SendJudgeMessage();
                }
                if (isWatcher1)
                {
                    Watcher.SendWatcherMessage();
                }
                if (isImmortal1)
                {
                    string msg = GetString("ImmortalSelfMessage");
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                }
                if (!isSpecialKiller1 && !isJester1 && !isWatcher1 && !isPresident1 && !isScientist1 && !isPhantom1 && !isEngineer1 && !isImmortal1 && !Shapeshifter1 && !isCobra1 && !isImpostor1)
                {
                    string msg = string.Format(GetString("NeutralInfo"));
                    Utils.SendMessage(msg, PlayerControl.LocalPlayer.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();

                }
                return true;

            case "/role":
                if (!AmongUsClient.Instance.AmHost)
                    return true;

                if (Options.EngineerFixer.GetBool())
                {
                    Engineer.SendEngineerMessage();
                }
                if (Options.PhantomGuess.GetBool())
                {
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                if (Options.ViperGuess.GetBool())
                {
                    ImpostorGuesser.SendViperPlayerMessage();
                }
                if (Options.ShapeGuess.GetBool())
                {
                    ImpostorGuesser.SendShapePlayerMessage();
                }
                if (Options.ScientistTime.GetBool())
                {
                    Scientist.SendScientistMessage();
                }
                if (Options.Guess.GetBool())
                {
                    Guesser.SendKillerMessage();
                }
                if (Options.Jester.GetBool())
                {
                    Jester.SendJesterMessage();
                }
                if (Options.ExilerExe.GetBool())
                {
                    Exiler.SendExilerMessage();
                }
                if (Options.Judge.GetBool())
                {
                    Judge.SendJudgeMessage();
                }
                if (Options.Profiler.GetBool())
                {
                    Profiler.SendProfilerMessage();
                }
                if (Options.Watcher.GetBool())
                {
                    Watcher.SendWatcherMessage();
                }
                return true;

            case "/coms":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Viper ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.Comms, 128);
                }
                return true;


            case "/o2":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Viper ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.LifeSupp, 128);
                }
                return true;

            case "/reactor":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Viper ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.Reactor, 128);
                }
                return true;

            case "/light":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Viper ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    byte electricalSabotageId = 4;
                    for (int i = 0; i < 5; i++)
                    {
                        electricalSabotageId |= (byte)(1 << i);

                    }
                    electricalSabotageId |= 128;

                    SabotageManager.TryActivateSabotage(SystemTypes.Electrical, electricalSabotageId);
                }
                return true;


            case "/colour":
            case "/color":
            case "/colore":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                subArgs = args.Length < 2 ? "" : args[1];
                var color = Utils.MsgToColor(subArgs, true);
                if (color == byte.MaxValue)
                {
                    return true;
                }
                PlayerControl.LocalPlayer.RpcSetColor(color);
                return true;

            case "/sg":
            case "/setspecialkiller":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId1))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId1 &&
                        p.Data != null &&
                        !p.Data.IsDead &&
                        p.Data.Role?.TeamType != RoleTeamTypes.Impostor);

                    if (target != null)
                    {
                        Guesser.SpecialKillerId = targetId1;
                        Guesser.SpecialKillerSelected = true;
                        ShowChat($"Special Killer set to {target.name} (ID: {targetId1}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSpecialKiller, SendOption.Reliable, -1);
                            writer.Write(targetId1);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Special Killer.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId1} is invalid (dead or an impostor).");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setsk ");
                }
                return true;

            case "/sj":
            case "/setjester":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId3))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId3 &&
                        p.Data != null &&
                        !p.Data.IsDead &&
                        p.Data.Role?.TeamType != RoleTeamTypes.Impostor);

                    if (target != null)
                    {
                        Jester.JesterId = targetId3;
                        Jester.JesterSelected = true;
                        ShowChat($"Jester set to {target.name} (ID: {targetId3}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJester, SendOption.Reliable, -1);
                            writer.Write(targetId3);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Jester.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId3} is invalid (dead or an impostor).");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setjester ");
                }
                return true;

            case "/se":
            case "/setexiler":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId2))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId2 &&
                        p.Data != null &&
                        !p.Data.IsDead );

                    if (target != null)
                    {
                        Exiler.ExilerId = targetId2;
                        Exiler.ExilerSelected = true;
                        ShowChat($"Exiler set to {target.name} (ID: {targetId2}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExiler, SendOption.Reliable, -1);
                            writer.Write(targetId2);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Exiler.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId2} is invalid");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setexiler ");
                }
                return true;


         


            case "/setjudge":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId6))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId6 &&
                        p.Data != null &&
                        !p.Data.IsDead);

                    if (target != null)
                    {
                        Judge.JudgeId = targetId6;
                        Judge.JudgeSelected = true;
                        ShowChat($"Judge set to {target.name} (ID: {targetId6}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJudge, SendOption.Reliable, -1);
                            writer.Write(targetId6);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Judge.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId6} is invalid");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setjudge ");
                }
                return true;

            case "/setprofiler":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId7))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId7 &&
                        p.Data != null &&
                        !p.Data.IsDead);

                    if (target != null)
                    {
                        Profiler.ProfilerId = targetId7;
                        Profiler.ProfilerSelected = true;
                        ShowChat($"Profiler set to {target.name} (ID: {targetId7}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetProfiler, SendOption.Reliable, -1);
                            writer.Write(targetId7);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Profiler.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId7} is invalid");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setprofiler ");
                }
                return true;

            case "/sw":
            case "/setwatcher":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId4))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId4 &&
                        p.Data != null &&
                        !p.Data.IsDead);

                    if (target != null)
                    {
                        Watcher.WatcherId = targetId4;
                        Watcher.WatcherSelected = true;
                        ShowChat($"Watcher set to {target.name} (ID: {targetId4}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetWatcher, SendOption.Reliable, -1);
                            writer.Write(targetId4);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }
                        else
                        {
                            ShowChat("Only the host can set the Watcher.");
                        }
                    }
                    else
                    {
                        ShowChat($"Player with ID {targetId4} is invalid");
                    }
                }
                else
                {
                    ShowChat("Correct use: /setwatcher ");
                }
                return true;

            case "/all":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; 
                }
                Utils.ShowCommand();
                Utils.ShowCommand3();
                return true;

            case "/help":
            case "/aiuto":
            case "/hilfe":
            case "/aide":
            case "/помощь":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; 
                }
                Utils.ShowCommand4();
                return true;

            case "/dn": return AppendToFile("DenyName.txt", string.Join(" ", subArgs), "AddedtoDenynamelist");
            case "/ddn": return RemoveFromFile("DenyName.txt", string.Join(" ", subArgs), "DeletedtoDenynamelist");
            case "/dw": return AppendToFile("BanWords.txt", string.Join(" ", subArgs), "AddedtoDenyWordlist");
            case "/ddw": return RemoveFromFile("BanWords.txt", string.Join(" ", subArgs), "DeletedtoDenyWord"); 
            case "/ds": return AppendToFile("SpamStart.txt", string.Join(" ", subArgs), "AddedtoDenystartlistlist");
            case "/dds": return RemoveFromFile("SpamStart.txt", string.Join(" ", subArgs), "DeletedtoDenystartlist");
            case "/addvip": return AllowedManager.ManageVip(subArgs, add: true);
            case "/deletevip": return AllowedManager.ManageVip(subArgs, add: false);
            case "/addmod": return AllowedManager.ManageModerator(subArgs, add: true);
            case "/deletemod": return AllowedManager.ManageModerator(subArgs, add: false);

            case "/id":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; 
                }
                string msg5 = GetString("PlayerIdList") + string.Join("\n", BanMod.AllPlayerControls
                    .Where(p => p != null)
                    .Select(p => $"{p.PlayerId} ({NumberToWords(p.PlayerId)}) → {p.Data.PlayerName}")); 
                ShowChat(msg5);
                return true;


            case "/level":
                if (int.TryParse(subArgs, out int level) && level is >= 1 and <= 99999)
                {
                    uint lvl = Convert.ToUInt32(level - 1);

                    player.RpcSetLevel(lvl);
                    DataManager.Player.stats.level = lvl;
                    DataManager.Player.Save();

                    BanMod.spoofLevel.Value = level.ToString();
                    BanMod.Instance.Config.Save();
                    ShowChat("Livello impostato a " + subArgs);
                }
                else
                {
                    ShowChat("Livello troppo alto");
                }
                return true;

            case "/say":
            case "/scrivi":
                return PrivateMessage1(args);

            case "/chat":
                return ChatColorManager.SetColoredText(subArgs);

        }

        return false;
    }

    static bool AppendToFile(string file, string value, string msgKey)
    {
        File.AppendAllText($"./BAN_DATA/DENIED/{file}", $"\n{value}");
        ShowChat(value + GetString(msgKey));
        return true;
    }

    static bool RemoveFromFile(string file, string value, string msgKey)
    {
        var lines = File.ReadAllLines($"./BAN_DATA/DENIED/{file}").Where(line => !line.Contains(value)).ToList();
        File.WriteAllLines($"./BAN_DATA/DENIED/{file}", lines);
        ShowChat(value + GetString(msgKey));
        return true;
    }
    public static bool ComandoExeUsed = false;
    public static bool ComandoRoomUsed = false;

    
    static bool PrivateMessage1(string[] args)
    {
        if (args.Length < 2) return false;

        string msg;
        byte id = byte.MaxValue; 

        if (byte.TryParse(args[1], out byte parsedId) && args.Length >= 3)
        {
            id = parsedId;
            msg = string.Join(" ", args.Skip(2));
        }
        else
        {
            msg = string.Join(" ", args.Skip(1));
        }

        var target = id == byte.MaxValue ? null : GetPlayerById(id);
        if (id != byte.MaxValue && target == null) return false;
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, id);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, id);
            MessageBlocker.UpdateLastMessageTime();
        }

        string recipient = id == byte.MaxValue ? GetString("Everyone") : target.Data.PlayerName;
        ShowChat($"{GetString("MessageSentTo")} {recipient}");
        return true;
    }
    private static Stack<NetworkedPlayerInfo.PlayerOutfit> savedOutfits2 = new Stack<NetworkedPlayerInfo.PlayerOutfit>();
    private static Stack<string> savedNames2 = new Stack<string>();
    public static class ChatColorManager
    {
        public static readonly Dictionary<string, string> colorMap = new()
    {
        { "gold", "#C6A25A" },{ "brown2", "#7F582D" },{ "beige", "#B6AA9E" },{ "bluegreen", "#00CEC8" },{ "maize", "#FBEC5D" },
        { "candy", "#FF004D" },{ "wine", "#722F37" },
        { "white", "#FFFFFF" }, { "bianco", "#FFFFFF" }, { "weiß", "#FFFFFF" }, { "белый", "#FFFFFF" }, { "blanc", "#FFFFFF" },
        { "blu", "#0000FF" }, { "blue", "#0000FF" }, { "blau", "#0000FF" }, { "синий", "#0000FF" }, { "bleu", "#0000FF" },
        { "verde", "#00FF00" }, { "green", "#00FF00" }, { "grün", "#00FF00" }, { "зелёный", "#00FF00" }, { "vert", "#00FF00" },
        { "fucsia", "#FF00FF" }, { "fuchsia", "#FF00FF" }, { "fuchsie", "#FF00FF" }, { "фуксия", "#FF00FF" }, { "pink", "#FFC0CB" },
        { "arancio", "#FFA500" }, { "arancione", "#FFA500" }, { "orange", "#FFA500" }, { "оранжевый", "#FFA500" },
        { "giallo", "#FFFF00" }, { "gialla", "#FFFF00" }, { "yellow", "#FFFF00" }, { "gelb", "#FFFF00" }, { "жёлтый", "#FFFF00" }, { "jaune", "#FFFF00" },
        { "nero", "#000000" }, { "nera", "#000000" }, { "black", "#000000" }, { "schwarz", "#000000" }, { "чёрный", "#000000" }, { "noir", "#000000" },
        { "viola", "#800080" }, { "purple", "#800080" }, { "lila", "#800080" }, { "фиолетовый", "#800080" }, { "violet", "#800080" },
        { "marrone", "#8B4513" }, { "brown", "#8B4513" }, { "braun", "#8B4513" }, { "коричневый", "#8B4513" }, { "marron", "#8B4513" },
        { "ciano", "#00FFFF" }, { "azzurro", "#00FFFF" }, { "azzurra", "#00FFFF" }, { "cyan", "#00FFFF" }, { "hellblau", "#00FFFF" }, { "голубой", "#00FFFF" }, { "bleu clair", "#00FFFF" },
        { "bordo", "#800000" }, { "bordeaux", "#800000" }, { "maroon", "#800000" }, { "kastanienbraun", "#800000" }, { "бордовый", "#800000" },
        { "rosa", "#FFC0CB" }, { "confetto", "#FFC0CB" }, { "розовый", "#FFC0CB" }, { "rose", "#FFC0CB" },
        { "crema", "#FFFACD" }, { "cream", "#FFFACD" }, { "creme", "#FFFACD" }, { "кремовый", "#FFFACD" }, { "crème", "#FFFACD" },{ "banana", "#FFFACD" },{ "banan", "#FFFACD" },
        { "lime", "#BFFF00" }, { "limette", "#BFFF00" }, { "лайм", "#BFFF00" }, { "citron vert", "#BFFF00" },
        { "grigio", "#808080" }, { "grigia", "#808080" }, { "gray", "#808080" }, { "grau", "#808080" }, { "серый", "#808080" }, { "gris", "#808080" },
        { "tortora", "#D2B48C" }, { "taupe", "#D2B48C" }, { "таупе", "#D2B48C" }, { "tan", "#D2B48C" },
        { "corallo", "#FF7F50" }, { "coral", "#FF7F50" }, { "koralle", "#FF7F50" }, { "коралловый", "#FF7F50" }, { "corail", "#FF7F50" },
        { "rosso", "#FF0000" }, { "rossa", "#FF0000" }, { "red", "#FF0000" }, { "rot", "#FF0000" }, { "красный", "#FF0000" }, { "rouge", "#FF0000" }
    };

        public static Color? currentChatColor = null;

        public static bool SetColoredText(string colorKey)
        {
            string hex = null;
            if (colorMap.TryGetValue(colorKey.ToLower(), out hex) || Regex.IsMatch(colorKey, "^#([0-9A-Fa-f]{6})$"))
            {
                string colorString = hex ?? colorKey;

                if (ColorUtility.TryParseHtmlString(colorString, out Color newColor))
                {
                    currentChatColor = newColor;
                    return true;
                }
            }

            currentChatColor = null; 
            return false;
        }
    }
    private static PlayerControl FindPlayerByIdOrColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (byte.TryParse(input, out byte playerId))
        {
            PlayerControl byId = BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                p.PlayerId == playerId);

            if (byId != null)
                return byId;
        }

        byte colorId = MsgToColor(input);
        if (colorId != byte.MaxValue)
        {
            PlayerControl byColor = BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                p.Data.DefaultOutfit != null &&
                p.Data.DefaultOutfit.ColorId == colorId);

            if (byColor != null)
                return byColor;
        }

        return null;
    }

    private static bool BanAndAddTeamer(PlayerControl targetPlayer, string reason)
    {
        if (targetPlayer == null)
            return false;

        ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
        if (client == null)
            return false;

        if (AllowedManager.IsModCreator(client.FriendCode))
            return false;

        if (BanMod.IsProtected(client))
            return false;

        string finalReason = string.IsNullOrWhiteSpace(reason) ? "Teaming" : reason;
        string targetName = targetPlayer.Data?.PlayerName ?? targetPlayer.name ?? "Player";

        BanManager.AddBanPlayer(client, finalReason, false);

        TeamerManager.AddPlayer(client, finalReason);

        AmongUsClient.Instance.KickPlayer(client.Id, true);

        NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
            HudManager.Instance.Notifier,
            $"{targetName} banned and added to Teamers. Reason: {finalReason}"
        );

        ShowChat($"{targetName} banned and added to Teamers.\nReason: {finalReason}");

        return true;
    }
    public static void CmdPrivate_Public()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        DestroyableSingleton<GameStartManager>.Instance.MakePublic();
    }
    public static void ShowChat(string msg) => DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);

    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;


        if (!AmongUsClient.Instance.AmHost)
            return;


        string[] args = text.Split(' ');
        string command = args[0].ToLowerInvariant();
        string subArg = args.Length > 1 ? args[1].ToLowerInvariant() : "";
        string subArgs = args.Length > 1 ? args[1] : "";
        string playerName = player.Data.PlayerName;
        bool isVip = Utils.IsVip(player.FriendCode);
        bool isModerator = Utils.IsModerator(player.FriendCode);
        string lowerMsg = text.ToLower();

        switch (command)
        {
            case "/sbanon":
                {
                    canceled = true;
                    if (!isModerator) return;
                    superban = true;
                    return;
                }
            case "/sban":
                {
                    canceled = true;

                    if (!superban) return;
                    if (args.Length < 2) return;
                    if (!isModerator) return;

                    string colorInput = args[1];

                    byte colorId = MsgToColor(colorInput);

                    if (colorId == byte.MaxValue)
                        return;

                    PlayerControl targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p != null &&
                        p.Data != null &&
                        p.Data.DefaultOutfit != null &&
                        p.Data.DefaultOutfit.ColorId == colorId);

                    if (targetPlayer == null)
                        return;

                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);

                    if (client == null)
                        return;

                    string friendCode = targetPlayer.Data?.FriendCode;

                    if (string.IsNullOrWhiteSpace(friendCode))
                        friendCode = client.FriendCode;

                    if (string.IsNullOrWhiteSpace(friendCode))
                        return;

                    SilentPermanentFriendCodeBan.Initialize();
                    SilentPermanentFriendCodeBan.AddDeferred(friendCode);
                    superban = false;
                    return;
                }

            case "/public":
            case "/private":
                {
                    if (!isModerator) return;
                    {
                        CmdPrivate_Public();
                    }
                    return;
                }
            case "/end":
            case "/End":
            case "/Close":
            case "/close":
                {
                    Judge.TryHandleEndCommand(player, command, out canceled);
                    return;
                }

            case "/instantstart":
                {
                    if (!isModerator) return;
                    bool oldNocountdown = Options.nocountdown.GetBool();
                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    if (manager != null)
                    {
                        manager.BeginGame();
                    }
                    return;
                }

            case "/start":
                {
                    if (!isModerator) return;
                    bool oldNocountdown = Options.nocountdown.GetBool();
                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    if (manager != null)
                    {
                        manager.BeginGame();
                    }
                    return;
                }

            case "/meeting":
                {
                    if (!isModerator) return;
                    player.CmdReportDeadBody(null);
                    return;
                }
            case "/destroy":
                {
                    if (!isModerator) return;
                    Utils.DestroyMap();
                    ShowChat("<color=#ff0000>[MapCheats]</color> Map/Lobby successfully destroyed!");
                    return;
                }

            case "/spawn":
            case "/lobby":
                {
                    if (!isModerator) return;
                    Utils.SpawnLobby();
                    ShowChat("<color=#00ffff>[MapCheats]</color> Lobby successfully created!");
                    return;
                }
            case "/endgame":
                {
                    if (!isModerator) return;
                    GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                    return;
                }

            case "/endmeeting":
                if (!isModerator) return;
                PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
                return;

            case "/every":
                if (!isModerator) return;
                {
                    subArgs = args.Length < 2 ? "" : args[1];
                    byte color = Utils.MsgToColor(subArgs, true);
                    if (color == byte.MaxValue)
                    {
                        return;
                    }
                    foreach (var allplayer in PlayerControl.AllPlayerControls)
                    {
                        allplayer.RpcSetColor(color);
                    }
                    return;
                }

            case "/ban":
                {
                    if (!isModerator) return;

                    if (args.Length < 2)
                    {
                        return;
                    }

                    string targetInput = args[1];
                    PlayerControl targetPlayer = null;

                    byte colorId = MsgToColor(targetInput);
                    if (colorId != byte.MaxValue)
                    {
                        targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                    }

                    if (targetPlayer == null)
                    {
                        return;
                    }

                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        return;
                    }

                    if (BanMod.IsProtected(client))
                    {
                        return;
                    }

                    BanManager.AddBanPlayer(client, "ModeratorBan", true);

                    try
                    {
                        BanMod.AddBanToList.Value = false;
                        AmongUsClient.Instance.KickPlayer(client.Id, true);
                    }
                    finally
                    {
                        BanMod.AddBanToList.Value = true;
                    }

                    return;
                }

            case "/kick":
                {
                    if (!isModerator) return;
                    if (args.Length < 2)
                    {
                        return;
                    }
                    string targetInput = args[1];
                    PlayerControl targetPlayer = null;
                    if (targetPlayer == null)
                    {
                        byte colorId = MsgToColor(targetInput);
                        if (colorId != byte.MaxValue)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                        }
                    }

                    if (targetPlayer == null)
                    {
                        return;
                    }
                    ClientData client = AmongUsClient.Instance?.GetClient(targetPlayer.OwnerId);
                    if (client == null)
                    {
                        return;
                    }
                    if (BanMod.IsProtected(client))
                    {
                        return;
                    }
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    return;
                }

            case "/summary":
                {
                    if (!isModerator) return;
                    string report1 = MatchSummary1.GetSummaryReport();
                    {
                        Utils.SendMessage(report1, 255);
                    }
                    return;
                }

            case "/colour":
            case "/color":
            case "/colore":
                {
                    subArgs = args.Length < 2 ? "" : args[1];
                    var color = Utils.MsgToColor(subArgs, true);
                    if (color == byte.MaxValue)
                        break;
                    if (!GameStates.isLobby) return;
                    if (Options.AllowColorChangeModerator.GetBool() && (isModerator || isVip))
                        player.RpcSetColor(color);
                    else if (Options.AllowColorChangeAll.GetBool())
                        player.RpcSetColor(color);
                    else return;

                    break;
                }

            case "/bm":
                {
                    if (GameStates.isLobby || player.Data.IsDead) return;

                    if (args.Length >= 2)
                    {
                        PlayerControl targetPlayer = Utils.GetTarget(args[1]);

                        if (targetPlayer != null)
                        {
                            RolesCommand.Cmd(player.PlayerId, targetPlayer.PlayerId);
                        }
                    }
                    return;
                }

            case "/m":
                if (GameStates.isLobby) return;
                bool isSpecialKiller1 = Options.Guess.GetBool() && player.PlayerId == Guesser.SpecialKillerId;
                bool isJester1 = Options.Jester.GetBool() && player.PlayerId == Jester.JesterId;
                bool isPresident1 = Options.ExilerExe.GetBool() && player.PlayerId == Exiler.ExilerId;
                bool isJudge1 = Options.Judge.GetBool() && player.PlayerId == Judge.JudgeId;
                bool isProfiler1 = Options.Profiler.GetBool() && player.PlayerId == Profiler.ProfilerId;
                bool isWatcher1 = Options.Watcher.GetBool() && player.PlayerId == Watcher.WatcherId;
                bool isScientist1 = Options.ScientistTime.GetBool() && Scientist(player);
                bool isPhantom1 = Options.PhantomGuess.GetBool() && Phantom(player);
                bool isCobra1 = Options.ViperGuess.GetBool() && Cobra(player);
                bool isImpostor1 = Options.ImpostorGuess.GetBool() && Impostor(player);
                bool isEngineer1 = Options.EngineerFixer.GetBool() && Engineer(player) && (!isJester1); ;
                bool isImmortal1 = Options.EnableImmortal.GetBool() && ImmortalManager.IsImmortal(player.PlayerId);
                bool Shapeshifter1 = Options.ShapeGuess.GetBool() && Shapeshifter(player);

                if (isEngineer1)
                {
                    Engineer.SendEngineerMessage();
                }
                if (Shapeshifter1)
                {
                    ImpostorGuesser.SendShapePlayerMessage();
                }
                if (isPhantom1)
                {
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                if (isImpostor1)
                {
                    ImpostorGuesser.SendImpostorPlayerMessage();
                }
                if (isCobra1)
                {
                    ImpostorGuesser.SendViperPlayerMessage();
                }
                if (isScientist1)
                {
                    Scientist.SendScientistMessage();
                }
                if (isSpecialKiller1)
                {
                    Guesser.SendKillerMessage();
                }
                if (isJester1)
                {
                    Jester.SendJesterMessage();
                }
                if (isPresident1)
                {
                    Exiler.SendExilerMessage();
                }
                if (isJudge1)
                {
                    Judge.SendJudgeMessage();
                }
                if (isProfiler1)
                {
                    Profiler.SendProfilerMessage();
                }
                if (isWatcher1)
                {
                    Watcher.SendWatcherMessage();
                }
                if (isImmortal1)
                {
                    string msg = GetString("ImmortalSelfMessage");
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                }
                if (!isSpecialKiller1 && !isJester1 && !isWatcher1 && !isPresident1 && !isScientist1 && !isPhantom1 && !isEngineer1 && !isImmortal1 && !Shapeshifter1 && !isCobra1 && !isImpostor1)
                {
                    string msg = string.Format(GetString("NeutralInfo"));
                    Utils.SendMessage(msg, player.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();

                }
                return;

            case "/insulta":
                bool IsVip = Utils.IsVip(player.FriendCode);
                if (!GameStates.isLobby) return;
                if (!insulta) return;
                if (!IsVip) return;
                {
                    if (args.Length < 2)
                    {
                        ShowChat("Uso corretto: /insulta <nome>");
                        return;
                    }

                    string target = args[1];

                    string insulto = PrendiInsulto();

                    string msg = $"{target}, {insulto}";

                    Utils.SendMessage(msg);

                    return;
                }

            case "/info":
                subArgs = args.Length < 2 ? "" : args[1];
                subArgs = args.Length < 2 ? "" : args[1].ToLowerInvariant(); 
                switch (subArgs)
                {
                    case "giustiziere":
                    case "guesser":
                    case "guess":
                    case "giustiz":
                    case "g":
                    case "devin":           
                    case "vermuten":        
                    case "Предсказатель":   
                        bool isGuessEnabled = Options.Guess.GetBool();
                        string statoGuess = isGuessEnabled ? "On" : "Off";
                        string msgGuess =
                            $"{GetString("GuesserDescription")}\n" +
                            $"{GetString("ModEnabled")} {statoGuess}";

                        Utils.SendMessage(msgGuess, 255);
                        MessageBlocker.UpdateLastMessageTime();

                        return;

                    case "presidente":
                    case "president":
                    case "exiler":
                    case "p":
                    case "président":     
                    case "präsident":     
                    case "президент":      
                        bool isExilerEnabled = Options.ExilerExe.GetBool();
                        bool isExilerKilled = Options.killexiler.GetBool();
                        string action = Options.ExilerAction.GetString();
                        string statoExiler = isExilerEnabled ? "On" : "Off";
                        string statoExilerK = isExilerKilled ? "On" : "Off";
                        string msgExiler =
                            $"{GetString("ModEnabled")}: {statoExiler}\n" +
                            $"{GetString("Consequence")}: {statoExilerK}\n" +
                            $"{GetString("Action")} {action}";

                        Utils.SendMessage(msgExiler, 255);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.SendMessage(GetString("exiler.cm"), 255);
                        MessageBlocker.UpdateLastMessageTime();
                        return;

                  
                    case "spettro":
                    case "fantasma":
                    case "phantom":
                    case "ph":
                    case "fantôme":        
                    case "geist":         
                    case "призрак":         
                        {
                            var optionsPha = GameOptionsManager.Instance.CurrentGameOptions;
                            float PhantomCooldown = 1f;
                            float PhantomDuration = 1f;
                            float killCooldown = 1f;
                            int phantomCount = optionsPha.RoleOptions.GetNumPerGame(RoleTypes.Phantom);
                            int phantomChance = optionsPha.RoleOptions.GetChancePerGame(RoleTypes.Phantom);

                            if (optionsPha != null)
                            {
                                optionsPha.TryGetFloat(FloatOptionNames.PhantomCooldown, out PhantomCooldown);
                                optionsPha.TryGetFloat(FloatOptionNames.PhantomDuration, out PhantomDuration);
                                optionsPha.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                phantomCount = optionsPha.RoleOptions.GetNumPerGame(RoleTypes.Phantom);
                                phantomChance = optionsPha.RoleOptions.GetChancePerGame(RoleTypes.Phantom);
                            }

                            bool isPhantomEnabled = Options.PhantomGuess.GetBool();

                            if (isPhantomEnabled)
                            {
                                string msgPha =
                                $"{GetString("MaxPerGame")}: {phantomCount}\n" +
                                $"{GetString("Probability")}: {phantomChance}%\n" +
                                $"{GetString("Cooldown")}: {PhantomCooldown}s\n" +
                                $"{GetString("DurationPhantom")}: {PhantomDuration}s\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgPha, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("PhantomDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgPha =
                                $"{GetString("MaxPerGame")}: {phantomCount}\n" +
                                $"{GetString("Probability")}: {phantomChance}%\n" +
                                $"{GetString("Cooldown")}: {PhantomCooldown}s\n" +
                                $"{GetString("DurationPhantom")}: {PhantomDuration}s\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgPha, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return;
                        }

                    case "immortale":
                    case "immortal":
                    case "imm":
                    case "immortel":      
                    case "unsterblich":    
                    case "бессмертный":     
                        bool isImmortalEnabled = Options.EnableImmortal.GetBool();
                        bool isImmortalesentEnabled = Options.Immortalesentvote.GetBool();
                        string statoImmortal = isImmortalEnabled ? "On" : "Off";
                        string statoesent = isImmortalesentEnabled ? "On" : "Off";
                        string msgImmortal =
                            $"{GetString("ModEnabled")}: {statoImmortal}\n" +
                            $"{GetString("VoteEsent")}: {statoesent}";

                        Utils.SendMessage(msgImmortal, 255);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.SendMessage(GetString("ImmortalDescription"), 255);
                        MessageBlocker.UpdateLastMessageTime();
                        return;

                    case "ing":
                    case "ingegnere":
                    case "engineer":
                    case "eng":
                    case "ingénieur":     
                    case "ingenieur":      
                    case "инженер":         
                        {
                            var optionsIng = GameOptionsManager.Instance.CurrentGameOptions;
                            float engineerCooldown = 1f;
                            float engineerInVentTime = 1f;
                            int engineerCount = optionsIng.RoleOptions.GetNumPerGame(RoleTypes.Engineer);
                            int engineerChance = optionsIng.RoleOptions.GetChancePerGame(RoleTypes.Engineer);
                            if (optionsIng != null)
                            {
                                optionsIng.TryGetFloat(FloatOptionNames.EngineerCooldown, out engineerCooldown);
                                optionsIng.TryGetFloat(FloatOptionNames.EngineerInVentMaxTime, out engineerInVentTime);
                                engineerCount = optionsIng.RoleOptions.GetNumPerGame(RoleTypes.Engineer);
                                engineerChance = optionsIng.RoleOptions.GetChancePerGame(RoleTypes.Engineer);
                            }

                            bool isEngineerFixerEnabled = Options.EngineerFixer.GetBool();
                            int ventFixAttempts = Options.VentTimes.GetInt();
                            string FormatVentTime(float time)
                            {
                                return time == 0f ? "∞" : $"{time:0.0}s";
                            }
                            if (isEngineerFixerEnabled)
                            {
                                string msg =
                                $"{GetString("MaxPerGame")}: {engineerCount}\n" +
                                $"{GetString("Probability")}: {engineerChance}%\n" +
                                $"{GetString("Cooldown")}: {engineerCooldown:0.0}s\n" +
                                $"{GetString("VentTime")}: {FormatVentTime(engineerInVentTime)}";
                                string msg2 =
                                $"{GetString("EngineerDescription")}\n\n" +
                                $"{GetString("AvailableFixes")}: {ventFixAttempts}";

                                Utils.SendMessage(msg, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(msg2, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msg =
                                $"{GetString("MaxPerGame")}: {engineerCount}\n" +
                                $"{GetString("Probability")}: {engineerChance}%\n" +
                                $"{GetString("Cooldown")}: {engineerCooldown:0.0}s\n" +
                                $"{GetString("VentTime")}: {FormatVentTime(engineerInVentTime)}";
                                Utils.SendMessage(msg, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return;
                        }


                    case "scienziato":
                    case "scientist":
                    case "sci":
                    case "scientifique":    
                    case "wissenschaftler": 
                    case "учёный":          
                        {
                            var optionsScie = GameOptionsManager.Instance.CurrentGameOptions;
                            float ScientistCooldown = 1f;
                            float ScientistBatteryCharge = 1f;
                            int scientistCount = optionsScie.RoleOptions.GetNumPerGame(RoleTypes.Scientist);
                            int scientistChance = optionsScie.RoleOptions.GetChancePerGame(RoleTypes.Scientist);

                            if (optionsScie != null)
                            {
                                optionsScie.TryGetFloat(FloatOptionNames.ScientistCooldown, out ScientistCooldown);
                                optionsScie.TryGetFloat(FloatOptionNames.ScientistBatteryCharge, out ScientistBatteryCharge);
                                scientistCount = optionsScie.RoleOptions.GetNumPerGame(RoleTypes.Scientist);
                                scientistChance = optionsScie.RoleOptions.GetChancePerGame(RoleTypes.Scientist);
                            }

                            bool isScientistEnabled = Options.ScientistTime.GetBool();
                            if (isScientistEnabled)
                            {
                                string msgScie =
                                $"{GetString("MaxPerGame")}: {scientistCount}\n" +
                                $"{GetString("Probability")}: {scientistChance}%\n" +
                                $"{GetString("Cooldown")}: {ScientistCooldown:0.0}s\n" +
                                $"{GetString("VitalsTime")}: {ScientistBatteryCharge:0.0}s";
                                Utils.SendMessage(msgScie, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("ScientistDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgScie =
                                $"{GetString("MaxPerGame")}: {scientistCount}\n" +
                                $"{GetString("Probability")}: {scientistChance}%\n" +
                                $"{GetString("Cooldown")}: {ScientistCooldown:0.0}s\n" +
                                $"{GetString("VitalsTime")}: {ScientistBatteryCharge:0.0}s";
                                Utils.SendMessage(msgScie, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return;
                        }

                    case "lobby":  
                        {
                            var options = GameOptionsManager.Instance.CurrentGameOptions;

                            bool confirmImpostorValue = false;
                            bool visualTasks = false;
                            bool anonymousVotes = false;
                            float crewLightMod = 1f;
                            float impostorLightMod = 1f;
                            float killCooldown = 1f;

                            if (options != null)
                            {
                                options.TryGetBool(BoolOptionNames.ConfirmImpostor, out confirmImpostorValue);
                                options.TryGetBool(BoolOptionNames.VisualTasks, out visualTasks);
                                options.TryGetBool(BoolOptionNames.AnonymousVotes, out anonymousVotes);
                                options.TryGetFloat(FloatOptionNames.CrewLightMod, out crewLightMod);
                                options.TryGetFloat(FloatOptionNames.ImpostorLightMod, out impostorLightMod);
                                options.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                            }

                            string onOff(bool val) => val ? "On" : "Off";
                            string msgLobby =
                                $"{GetString("ConfirmImpostor")}:{onOff(confirmImpostorValue)}\n" +
                                $"{GetString("VisualTasks")}:{onOff(visualTasks)}\n" +
                                $"{GetString("AnonymousVotes")}:{onOff(anonymousVotes)}\n" +
                                $"{GetString("CrewmateVision")}:{crewLightMod}\n" +
                                $"{GetString("ImpostorVision")}:{impostorLightMod}\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}";

                            Utils.SendMessage(msgLobby);
                            MessageBlocker.UpdateLastMessageTime();
                            return;
                        }

                    case "shapeshifter":
                    case "shape":
                    case "ss":
                    case "mutaforma":
                    case "muta":
                        {
                            var optionsShape = GameOptionsManager.Instance.CurrentGameOptions;
                            float ShapeshifterCooldown = 1f;
                            float ShapeshifterDuration = 1f;
                            bool ShapeshifterLeaveSkin = false;
                            float killCooldown = 1f;
                            int shapeCount = optionsShape.RoleOptions.GetNumPerGame(RoleTypes.Shapeshifter);
                            int shapeChance = optionsShape.RoleOptions.GetChancePerGame(RoleTypes.Shapeshifter);
                            string FormatDurationTime(float time)
                            {
                                return time == 0f ? "∞" : $"{time:0.0}s";
                            }
                            if (optionsShape != null)
                            {
                                optionsShape.TryGetFloat(FloatOptionNames.ShapeshifterCooldown, out ShapeshifterCooldown);
                                optionsShape.TryGetFloat(FloatOptionNames.ShapeshifterDuration, out ShapeshifterDuration);
                                optionsShape.TryGetBool(BoolOptionNames.ShapeshifterLeaveSkin, out ShapeshifterLeaveSkin);
                                optionsShape.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                shapeCount = optionsShape.RoleOptions.GetNumPerGame(RoleTypes.Shapeshifter);
                                shapeChance = optionsShape.RoleOptions.GetChancePerGame(RoleTypes.Shapeshifter);
                            }

                            bool isShapeEnabled = Options.ShapeGuess.GetBool();
                            string statoShapevisible = ShapeshifterLeaveSkin ? "On" : "Off";
                            if (isShapeEnabled)
                            {
                                string msgShape =
                                $"{GetString("MaxPerGame")}: {shapeCount}\n" +
                                $"{GetString("Probability")}: {shapeChance}%\n" +
                                $"{GetString("Cooldown")}: {ShapeshifterCooldown}s\n" +
                                $"{GetString("DurationShape")}: {FormatDurationTime(ShapeshifterDuration)}\n" +
                                $"{GetString("SkinShape")}: {ShapeshifterLeaveSkin}\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";

                                Utils.SendMessage(msgShape, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("ShapeshifterDescription"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgShape =
                                $"{GetString("MaxPerGame")}: {shapeCount}\n" +
                                $"{GetString("Probability")}: {shapeChance}%\n" +
                                $"{GetString("Cooldown")}: {ShapeshifterCooldown}s\n" +
                                $"{GetString("DurationShape")}: {FormatDurationTime(ShapeshifterDuration)}\n" +
                                $"{GetString("SkinShape")}: {ShapeshifterLeaveSkin}\n" +
                                $"{GetString("KillCooldown")}: {killCooldown}s";
                                Utils.SendMessage(msgShape, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return;
                        }

                    case "detective":
                        {
                            var optionsDet = GameOptionsManager.Instance.CurrentGameOptions;
                            float DetectiveSuspectLimit = 1f;
                            int detectiveCount = optionsDet.RoleOptions.GetNumPerGame(RoleTypes.Detective);
                            int detectiveChance = optionsDet.RoleOptions.GetChancePerGame(RoleTypes.Detective);

                            if (optionsDet != null)
                            {
                                optionsDet.TryGetFloat(FloatOptionNames.DetectiveSuspectLimit, out DetectiveSuspectLimit);
                                detectiveCount = optionsDet.RoleOptions.GetNumPerGame(RoleTypes.Detective);
                                detectiveChance = optionsDet.RoleOptions.GetChancePerGame(RoleTypes.Detective);
                            }

                            string msgDet =
                                $"{GetString("MaxPerGame")}: {detectiveCount}\n" +
                                $"{GetString("Probability")}: {detectiveChance}%\n" +
                                $"{GetString("DetectiveSuspectLimit")}: {DetectiveSuspectLimit}";

                            Utils.SendMessage(msgDet, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return;
                        }

                    case "cobra":
                    case "viper":
                        {
                            var optionsCo = GameOptionsManager.Instance.CurrentGameOptions;
                            float ViperDissolveTime = 1f;
                            float killCooldown = 1f;
                            int viperCount = optionsCo.RoleOptions.GetNumPerGame(RoleTypes.Viper);
                            int viperChance = optionsCo.RoleOptions.GetChancePerGame(RoleTypes.Viper);

                            if (optionsCo != null)
                            {
                                optionsCo.TryGetFloat(FloatOptionNames.ViperDissolveTime, out ViperDissolveTime);
                                optionsCo.TryGetFloat(FloatOptionNames.KillCooldown, out killCooldown);
                                viperCount = optionsCo.RoleOptions.GetNumPerGame(RoleTypes.Viper);
                                viperChance = optionsCo.RoleOptions.GetChancePerGame(RoleTypes.Viper);
                            }

                            bool isViperEnabled = Options.ViperGuess.GetBool();

                            if (isViperEnabled)
                            {
                                string msgVip =
                                $"{GetString("MaxPerGame")}:{viperCount}\n" +
                                $"{GetString("Probability")}:{viperChance}%\n" +
                                $"{GetString("ViperDissolveTime")}:{ViperDissolveTime}s\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}s";
                                Utils.SendMessage(msgVip, 255);
                                MessageBlocker.UpdateLastMessageTime();
                                Utils.SendMessage(GetString("viper.cm"), 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                string msgVip1 =
                                $"{GetString("MaxPerGame")}:{viperCount}\n" +
                                $"{GetString("Probability")}:{viperChance}%\n" +
                                $"{GetString("ViperDissolveTime")}:{ViperDissolveTime}s\n" +
                                $"{GetString("KillCooldown")}:{killCooldown}s";
                                Utils.SendMessage(msgVip1, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            return;
                        }

                    case "starnazzatore":
                    case "noisemaker":
                        {
                            var optionsNoi = GameOptionsManager.Instance.CurrentGameOptions;
                            float NoisemakerAlertDuration = 1f;
                            bool NoisemakerImpostorAlert = false;
                            int noisemakerCount = optionsNoi.RoleOptions.GetNumPerGame(RoleTypes.Noisemaker);
                            int noisemakerChance = optionsNoi.RoleOptions.GetChancePerGame(RoleTypes.Noisemaker);
                            if (optionsNoi != null)
                            {
                                optionsNoi.TryGetFloat(FloatOptionNames.NoisemakerAlertDuration, out NoisemakerAlertDuration);
                                optionsNoi.TryGetBool(BoolOptionNames.NoisemakerImpostorAlert, out NoisemakerImpostorAlert);
                                noisemakerCount = optionsNoi.RoleOptions.GetNumPerGame(RoleTypes.Noisemaker);
                                noisemakerChance = optionsNoi.RoleOptions.GetChancePerGame(RoleTypes.Noisemaker);
                            }

                            string msgNoi =
                                $"{GetString("MaxPerGame")}: {noisemakerCount}\n" +
                                $"{GetString("Probability")}: {noisemakerChance}%\n" +
                                $"{GetString("NoisemakerAlertDuration")}: {NoisemakerAlertDuration}s\n" +
                                $"{GetString("NoisemakerImpostorAlert")}: {NoisemakerImpostorAlert}";

                            Utils.SendMessage(msgNoi, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return;
                        }

                    case "guardian":
                    case "angel":
                    case "angelo":
                        {
                            var optionsAng = GameOptionsManager.Instance.CurrentGameOptions;
                            float GuardianAngelCooldown = 1f;
                            float ProtectionDurationSeconds = 1f;
                            int angelCount = optionsAng.RoleOptions.GetNumPerGame(RoleTypes.GuardianAngel);
                            int angelChance = optionsAng.RoleOptions.GetChancePerGame(RoleTypes.GuardianAngel);

                            if (optionsAng != null)
                            {
                                optionsAng.TryGetFloat(FloatOptionNames.GuardianAngelCooldown, out GuardianAngelCooldown);
                                optionsAng.TryGetFloat(FloatOptionNames.ProtectionDurationSeconds, out ProtectionDurationSeconds);
                                angelCount = optionsAng.RoleOptions.GetNumPerGame(RoleTypes.GuardianAngel);
                                angelChance = optionsAng.RoleOptions.GetChancePerGame(RoleTypes.GuardianAngel);
                            }

                            string msgAng =
                                $"{GetString("MaxPerGame")}: {angelCount}\n" +
                                $"{GetString("Probability")}: {angelChance}%\n" +
                                $"{GetString("Cooldown")}: {GuardianAngelCooldown}s\n" +
                                $"{GetString("ProtectionDurationSeconds")}: {ProtectionDurationSeconds}s";

                            Utils.SendMessage(msgAng, 255);
                            MessageBlocker.UpdateLastMessageTime();
                            return;
                        }

                    default:
                        return;
                }


            case "/t":
                if (!Options.TcommandforAll.GetBool())
                    return;
                SendRules();
                return;

            default:
                    if (SpamManager.CheckStart(player, text) ||
                        SpamManager.CheckWord(player, text)) return;

                return;

        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    public static class ChatUpdatePatch_SendMessage
    {
        private static float lastMessageTime = -3.15f;
        private static float timeToWait = 3.15f;

        public static void Postfix(ChatController __instance)
        {
            if (BanMod.IsBanModDisabled) return;
            if (BanMod.MessagesToSend.Count == 0) return;
            if (Time.time - lastMessageTime < timeToWait) return;
            var localPlayer = PlayerControl.LocalPlayer;

            var (msg, sendTo) = BanMod.MessagesToSend[0];

            if (sendTo != byte.MaxValue)
            {
                var player = BanMod.AllPlayerControls.FirstOrDefault(p =>
                    p != null && p.PlayerId == sendTo && p.Data != null && !p.Data.Disconnected);

                if (player == null)
                {
                    BanMod.MessagesToSend.RemoveAt(0);
                    Debug.LogWarning($"[SendMessage] Messaggio per PlayerId {sendTo} annullato: giocatore disconnesso.");
                    return;
                }

            }
            else
            {
                foreach (var player in BanMod.AllPlayerControls.Where(p => p != null && p.Data != null && !p.Data.Disconnected))
                {
                    if (player.PlayerId == localPlayer.PlayerId) continue;

                }
            }

            BanMod.MessagesToSend.RemoveAt(0);
            lastMessageTime = Time.time;

            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo)?.GetClientId() ?? -1;
            string originalName = localPlayer.Data.PlayerName;
            if (clientId == -1)
            {
                FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(localPlayer, msg);
            }

            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);

            writer.StartMessage(clientId);

            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();

            lastMessageTime = Time.time;
        }
    }
}