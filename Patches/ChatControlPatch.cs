//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using InnerNet;
using Innersloth.IO;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;
using static BanMod.Utils;

namespace BanMod;

//public static class ChatCommandSuggestions
//{
//    public struct CommandInfo
//    {
//        public string Command;
//        public string DescriptionKey;
//
//        public CommandInfo(string command, string descriptionKey)
//        {
//            Command = command;
//            DescriptionKey = descriptionKey;
//        }
//
//        public string Description
//        {
//            get
//            {
//                string translated = Translator.GetString(DescriptionKey);
//
//                if (string.IsNullOrWhiteSpace(translated) || translated == DescriptionKey)
//                    return DescriptionKey;
//
//                return translated;
//            }
//        }
//    }
//
//    public static readonly CommandInfo[] Commands =
//    {
//        new("/list", "CmdDesc_List"),
//        new("/all", "CmdDesc_All"),
//        new("/help", "CmdDesc_Help"),
//        new("/ban", "CmdDesc_Ban"),
//        new("/banall", "CmdDesc_BanAll"),
//        new("/kick", "CmdDesc_Kick"),
//        new("/kickall", "CmdDesc_KickAll"),
//        new("/unban", "CmdDesc_Unban"),
//
//        new("/INFOGAME", "CmdDesc_InfoGame"),
//        new("/customlobby", "CmdDesc_CustomLobby"),
//        new("/cl", "CmdDesc_CustomLobbyAlias"),
//        new("/disablemap", "CmdDesc_DisableMap"),
//        new("/afk", "CmdDesc_Afk"),
//        new("/every", "CmdDesc_Every"),
//        new("/rainbowall", "CmdDesc_RainbowAll"),
//        new("/rainbow", "CmdDesc_Rainbow"),
//        new("/setname", "CmdDesc_SetName"),
//        new("/start", "CmdDesc_Start"),
//        new("/instantstart", "CmdDesc_InstantStart"),
//        new("/destroy", "CmdDesc_Destroy"),
//        new("/lobby", "CmdDesc_Lobby"),
//        new("/endgame", "CmdDesc_EndGame"),
//        new("/t", "CmdDesc_T"),
//        new("/endmeeting", "CmdDesc_EndMeeting"),
//        new("/lp", "CmdDesc_Levels"),
//        new("/livelli", "CmdDesc_Levels"),
//        new("/exeme", "CmdDesc_Exeme"),
//        new("/meeting", "CmdDesc_Meeting"),
//        new("/close", "CmdDesc_Close"),
//        new("/killme", "CmdDesc_KillMe"),
//        new("/summary", "CmdDesc_Summary"),
//        new("/info", "CmdDesc_Info"),
//        new("/m", "CmdDesc_M"),
//        new("/role", "CmdDesc_Role"),
//        new("/colour", "CmdDesc_Color"),
//        new("/color", "CmdDesc_Color"),
//        new("/colore", "CmdDesc_Color"),
//        new("/aiuto", "CmdDesc_Help"),
//        new("/dn", "CmdDesc_Dn"),
//        new("/ddn", "CmdDesc_Ddn"),
//        new("/dw", "CmdDesc_Dw"),
//        new("/ddw", "CmdDesc_Ddw"),
//        new("/ds", "CmdDesc_Ds"),
//        new("/dds", "CmdDesc_Dds"),
//        new("/addvip", "CmdDesc_AddVip"),
//        new("/deletevip", "CmdDesc_DeleteVip"),
//        new("/addmod", "CmdDesc_AddMod"),
//        new("/deletemod", "CmdDesc_DeleteMod"),
//        new("/id", "CmdDesc_Id"),
//        new("/level", "CmdDesc_Level"),
//        new("/say", "CmdDesc_Say"),
//        new("/chat", "CmdDesc_Chat"),
//        new("/cmd", "CmdDesc_Cmd"),
//        new("/public", "CmdDesc_PublicPrivate"),
//        new("/private", "CmdDesc_PublicPrivate"),
//        new("/bm", "CmdDesc_Bm"),
//        new("/bbm", "CmdDesc_Bbm")
//    };
//
//    public static readonly CommandInfo[] InfoSubCommands =
//    {
//        new("/info lobby", "CmdDesc_Info_Lobby"),
//
//        new("/info guesser", "CmdDesc_Info_Guesser"),
//        new("/info giustiziere", "CmdDesc_Info_Guesser"),
//        new("/info guess", "CmdDesc_Info_Guesser"),
//        new("/info g", "CmdDesc_Info_Guesser"),
//        new("/info devin", "CmdDesc_Info_Guesser"),
//        new("/info vermuten", "CmdDesc_Info_Guesser"),
//        new("/info Предсказатель", "CmdDesc_Info_Guesser"),
//
//        new("/info president", "CmdDesc_Info_Exiler"),
//        new("/info presidente", "CmdDesc_Info_Exiler"),
//        new("/info exiler", "CmdDesc_Info_Exiler"),
//        new("/info p", "CmdDesc_Info_Exiler"),
//        new("/info président", "CmdDesc_Info_Exiler"),
//        new("/info präsident", "CmdDesc_Info_Exiler"),
//        new("/info президент", "CmdDesc_Info_Exiler"),
//
//        new("/info phantom", "CmdDesc_Info_Phantom"),
//        new("/info ph", "CmdDesc_Info_Phantom"),
//        new("/info spettro", "CmdDesc_Info_Phantom"),
//        new("/info fantasma", "CmdDesc_Info_Phantom"),
//        new("/info fantôme", "CmdDesc_Info_Phantom"),
//        new("/info geist", "CmdDesc_Info_Phantom"),
//        new("/info призрак", "CmdDesc_Info_Phantom"),
//
//        new("/info immortal", "CmdDesc_Info_Immortal"),
//        new("/info immortale", "CmdDesc_Info_Immortal"),
//        new("/info imm", "CmdDesc_Info_Immortal"),
//        new("/info immortel", "CmdDesc_Info_Immortal"),
//        new("/info unsterblich", "CmdDesc_Info_Immortal"),
//        new("/info бессмертный", "CmdDesc_Info_Immortal"),
//
//        new("/info engineer", "CmdDesc_Info_Engineer"),
//        new("/info ingegnere", "CmdDesc_Info_Engineer"),
//        new("/info ing", "CmdDesc_Info_Engineer"),
//        new("/info eng", "CmdDesc_Info_Engineer"),
//        new("/info ingénieur", "CmdDesc_Info_Engineer"),
//        new("/info ingenieur", "CmdDesc_Info_Engineer"),
//        new("/info инженер", "CmdDesc_Info_Engineer"),
//
//        new("/info scientist", "CmdDesc_Info_Scientist"),
//        new("/info scienziato", "CmdDesc_Info_Scientist"),
//        new("/info sci", "CmdDesc_Info_Scientist"),
//        new("/info scientifique", "CmdDesc_Info_Scientist"),
//        new("/info wissenschaftler", "CmdDesc_Info_Scientist"),
//        new("/info учёный", "CmdDesc_Info_Scientist"),
//
//        new("/info shapeshifter", "CmdDesc_Info_Shapeshifter"),
//        new("/info shape", "CmdDesc_Info_Shapeshifter"),
//        new("/info ss", "CmdDesc_Info_Shapeshifter"),
//        new("/info mutaforma", "CmdDesc_Info_Shapeshifter"),
//        new("/info muta", "CmdDesc_Info_Shapeshifter"),
//
//        new("/info detective", "CmdDesc_Info_Detective"),
//
//        new("/info cobra", "CmdDesc_Info_Viper"),
//        new("/info viper", "CmdDesc_Info_Viper"),
//
//        new("/info noisemaker", "CmdDesc_Info_Noisemaker"),
//        new("/info starnazzatore", "CmdDesc_Info_Noisemaker"),
//
//        new("/info guardian", "CmdDesc_Info_Guardian"),
//        new("/info angel", "CmdDesc_Info_Guardian"),
//        new("/info angelo", "CmdDesc_Info_Guardian")
//    };
//
//    public static List<CommandInfo> GetMatches(string input)
//    {
//        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
//            return new List<CommandInfo>();
//
//        string lower = input.ToLowerInvariant();
//
//        if (lower.StartsWith("/info "))
//        {
//            return InfoSubCommands
//                .Where(c => c.Command.ToLowerInvariant().StartsWith(lower))
//                .Take(6)
//                .ToList();
//        }
//
//        if (lower == "/info")
//        {
//            List<CommandInfo> result = new();
//            result.Add(Commands.First(c => c.Command.Equals("/info", StringComparison.OrdinalIgnoreCase)));
//            result.AddRange(InfoSubCommands.Take(5));
//            return result;
//        }
//
//        return Commands
//            .Where(c => c.Command.ToLowerInvariant().StartsWith(lower))
//            .Take(6)
//            .ToList();
//    }
//
//    public static string GetCompletion(string input)
//    {
//        List<CommandInfo> matches = GetMatches(input);
//
//        if (matches.Count == 0)
//            return input;
//
//        return matches[0].Command + " ";
//    }
//}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class DefaultModdedChatResetPatch
{
    public static void Postfix()
    {
        if (BanMod.IsBanModDisabled) return;
        ChatControllerUpdatePatch.ResetUiState();
        ModdedOriginalChatManager.Reset();
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatControllerUpdatePatch
{
    private static SpriteRenderer QuickChatIcon;
    private static SpriteRenderer OpenBanMenuIcon;
    private static SpriteRenderer OpenKeyboardIcon;

    public static int CurrentHistorySelection = -1;
    public static bool timelastmessage;
    public static ChatController Instance;

    //    private static ChatBubble CommandSuggestionBubble;
    //    private static string LastCommandSuggestionInput = "";
    //    private static string LastCommandSuggestionText = "";

    private static readonly FieldInfo ChatNotifyDotField =
        typeof(ChatController).GetField(
            "chatNotifyDot",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

    private static readonly FieldInfo ChatNotificationField =
        typeof(ChatController).GetField(
            "chatNotification",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );


    //    private static readonly MethodInfo AlignAllBubblesMethod =
    //        AccessTools.Method(typeof(ChatController), "AlignAllBubbles");

    public static void ResetUiState()
    {
        //        CommandSuggestionBubble = null;
        //        LastCommandSuggestionInput = "";
        //        LastCommandSuggestionText = "";
    }

    public static void Prefix()
    {
        if (BanMod.IsBanModDisabled) return;
        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
        {
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;
        }
    }

    public static void Postfix(ChatController __instance)
    {
        if (__instance == null)
            return;

        Instance = __instance;

        timelastmessage = __instance.timeSinceLastMessage > 3.15f;

        if (__instance.freeChatField != null && __instance.freeChatField.textArea != null)
        {
            __instance.freeChatField.textArea.characterLimit = 120;
            __instance.freeChatField.textArea.AllowSymbols = true;
            __instance.freeChatField.textArea.AllowEmail = true;
            __instance.freeChatField.textArea.allowAllCharacters = true;
        }

        ApplyChatTheme(__instance);

        HandleClipboardAndHistory(__instance);

        //        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        //        {
        //            HandleCommandAutocomplete(__instance);
        //            UpdateCommandSuggestionBubble(__instance);
        //        }
        //        else
        //        {
        //            RemoveCommandSuggestionBubble(__instance);
        //        }
    }

    //    private static void HandleCommandAutocomplete(ChatController chat)
    //    {
    //        if (chat == null || chat.freeChatField == null || chat.freeChatField.textArea == null)
    //            return;
    //
    //        if (!chat.freeChatField.textArea.hasFocus)
    //            return;
    //
    //        string input = chat.freeChatField.textArea.text ?? "";
    //
    //        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
    //            return;
    //
    //        List<ChatCommandSuggestions.CommandInfo> matches = ChatCommandSuggestions.GetMatches(input);
    //
    //        if (matches.Count == 0)
    //            return;
    //
    //        if (Input.GetKeyDown(KeyCode.Tab))
    //        {
    //            string completed = ChatCommandSuggestions.GetCompletion(input);
    //            chat.freeChatField.textArea.SetText(completed);
    //
    //            RemoveCommandSuggestionBubble(chat);
    //        }
    //    }

    //    private static void UpdateCommandSuggestionBubble(ChatController chat)
    //    {
    //        if (chat == null ||
    //            chat.freeChatField == null ||
    //            chat.freeChatField.textArea == null ||
    //            chat.chatBubblePool == null ||
    //            chat.scroller == null ||
    //            chat.scroller.Inner == null)
    //        {
    //            return;
    //        }
    //
    //        string input = chat.freeChatField.textArea.text ?? "";
    //
    //        if (!chat.freeChatField.textArea.hasFocus ||
    //            string.IsNullOrWhiteSpace(input) ||
    //            !input.StartsWith("/"))
    //        {
    //            RemoveCommandSuggestionBubble(chat);
    //            return;
    //        }
    //
    //        List<ChatCommandSuggestions.CommandInfo> matches =
    //            ChatCommandSuggestions.GetMatches(input);
    //
    //        if (matches.Count == 0)
    //        {
    //            RemoveCommandSuggestionBubble(chat);
    //            return;
    //        }
    //
    //        string suggestionText = BuildCommandSuggestionText(matches);
    //
    //        bool same =
    //            CommandSuggestionBubble != null &&
    //            CommandSuggestionBubble.gameObject != null &&
    //            CommandSuggestionBubble.gameObject.activeSelf &&
    //            input == LastCommandSuggestionInput &&
    //            suggestionText == LastCommandSuggestionText;
    //
    //        if (!same)
    //        {
    //            LastCommandSuggestionInput = input;
    //            LastCommandSuggestionText = suggestionText;
    //
    //            CreateOrUpdateCommandSuggestionBubble(chat, suggestionText);
    //        }
    //
    //        ForceSuggestionBubbleLast(chat);
    //        InvokeVanillaAlignAllBubbles(chat);
    //    }

    //    private static string BuildCommandSuggestionText(List<ChatCommandSuggestions.CommandInfo> matches)
    //    {
    //        List<string> lines = new();
    //
    //        foreach (var match in matches)
    //        {
    //            string desc = match.Description ?? "";
    //
    //            if (desc.Length > 48)
    //                desc = desc.Substring(0, 45) + "...";
    //
    //            lines.Add($"<color=#66ffcc>{match.Command}</color> <color=#bdbdbd>({desc})</color>");
    //        }
    //
    //        return string.Join("\n", lines);
    //    }

    //    private static void CreateOrUpdateCommandSuggestionBubble(ChatController chat, string suggestionText)
    //    {
    //        if (chat == null ||
    //            chat.chatBubblePool == null ||
    //            chat.scroller == null ||
    //            chat.scroller.Inner == null ||
    //            PlayerControl.LocalPlayer == null)
    //        {
    //            return;
    //        }
    //
    //        try
    //        {
    //            if (CommandSuggestionBubble == null ||
    //                CommandSuggestionBubble.gameObject == null)
    //            {
    //                if (chat.chatBubblePool.NotInUse == 0)
    //                    chat.chatBubblePool.ReclaimOldest();
    //
    //                CommandSuggestionBubble = chat.chatBubblePool.Get<ChatBubble>();
    //            }
    //
    //            if (CommandSuggestionBubble == null)
    //                return;
    //
    //            CommandSuggestionBubble.gameObject.name = "BANMOD_CommandSuggestionBubble";
    //            CommandSuggestionBubble.gameObject.SetActive(true);
    //
    //            CommandSuggestionBubble.transform.SetParent(chat.scroller.Inner, false);
    //            CommandSuggestionBubble.transform.localScale = Vector3.one;
    //
    //            CommandSuggestionBubble.SetLeft();
    //
    //            if (PlayerControl.LocalPlayer.Data != null)
    //                CommandSuggestionBubble.SetCosmetics(PlayerControl.LocalPlayer.Data);
    //
    //            CommandSuggestionBubble.SetName(
    //                "Comandi",
    //                false,
    //                false,
    //                new Color(0.65f, 0.65f, 0.65f, 1f));
    //
    //            CommandSuggestionBubble.SetText(suggestionText);
    //            CommandSuggestionBubble.AlignChildren();
    //
    //            ForceSuggestionBubbleLast(chat);
    //            InvokeVanillaAlignAllBubbles(chat);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError("[BanMod] CreateOrUpdateCommandSuggestionBubble error: " + ex);
    //        }
    //    }

    //    private static void RemoveCommandSuggestionBubble(ChatController chat)
    //    {
    //        LastCommandSuggestionInput = "";
    //        LastCommandSuggestionText = "";
    //
    //        if (chat == null || chat.chatBubblePool == null)
    //        {
    //            CommandSuggestionBubble = null;
    //            return;
    //        }
    //
    //        try
    //        {
    //            if (CommandSuggestionBubble != null)
    //            {
    //                try
    //                {
    //                    chat.chatBubblePool.Reclaim(CommandSuggestionBubble);
    //                }
    //                catch
    //                {
    //                    if (CommandSuggestionBubble.gameObject != null)
    //                        CommandSuggestionBubble.gameObject.SetActive(false);
    //                }
    //
    //                CommandSuggestionBubble = null;
    //            }
    //
    //            foreach (var obj in chat.chatBubblePool.activeChildren.ToArray())
    //            {
    //                ChatBubble bubble = obj as ChatBubble;
    //
    //                if (bubble == null || bubble.gameObject == null)
    //                    continue;
    //
    //                if (bubble.gameObject.name == "BANMOD_CommandSuggestionBubble")
    //                {
    //                    try
    //                    {
    //                        chat.chatBubblePool.Reclaim(bubble);
    //                    }
    //                    catch
    //                    {
    //                        bubble.gameObject.SetActive(false);
    //                    }
    //                }
    //            }
    //
    //            InvokeVanillaAlignAllBubbles(chat);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError("[BanMod] RemoveCommandSuggestionBubble error: " + ex);
    //        }
    //    }

    //    private static void ForceSuggestionBubbleLast(ChatController chat)
    //    {
    //        if (chat == null ||
    //            chat.chatBubblePool == null ||
    //            CommandSuggestionBubble == null)
    //        {
    //            return;
    //        }
    //
    //        try
    //        {
    //            var activeChildren = chat.chatBubblePool.activeChildren;
    //
    //            if (activeChildren == null)
    //                return;
    //
    //            PoolableBehavior suggestionAsPoolable = CommandSuggestionBubble.Cast<PoolableBehavior>();
    //
    //            if (suggestionAsPoolable == null)
    //                return;
    //
    //            activeChildren.Remove(suggestionAsPoolable);
    //            activeChildren.Add(suggestionAsPoolable);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError("[BanMod] ForceSuggestionBubbleLast error: " + ex);
    //        }
    //    }

    //    private static void InvokeVanillaAlignAllBubbles(ChatController chat)
    //    {
    //        if (chat == null)
    //            return;
    //
    //        try
    //        {
    //            AlignAllBubblesMethod?.Invoke(chat, null);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError("[BanMod] InvokeVanillaAlignAllBubbles error: " + ex);
    //        }
    //    }

    private static void ApplyChatTheme(ChatController chat)
    {
        if (chat == null)
            return;

        if (BanMod.DarkTheme.Value)
        {
            if (chat.freeChatField != null)
            {
                chat.freeChatField.background.color = new Color(0.1f, 0.1f, 0.1f, 1f);

                if (chat.freeChatField.textArea != null)
                {
                    chat.freeChatField.textArea.compoText.Color(Color.white);
                    chat.freeChatField.textArea.outputText.color = Color.white;
                }
            }

            if (chat.quickChatField != null)
            {
                chat.quickChatField.background.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                chat.quickChatField.text.color = Color.white;
            }

            if (QuickChatIcon == null)
                QuickChatIcon = GameObject.Find("QuickChatIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                QuickChatIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkQuickChat.png", 100f);

            if (OpenBanMenuIcon == null)
                OpenBanMenuIcon = GameObject.Find("OpenBanMenuIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenBanMenuIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkReport.png", 100f);

            if (OpenKeyboardIcon == null)
                OpenKeyboardIcon = GameObject.Find("OpenKeyboardIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenKeyboardIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkKeyboard.png", 100f);

            if (GameStates.IsDead)
            {
                if (chat.freeChatField != null)
                    chat.freeChatField.background.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

                if (chat.quickChatField != null)
                    chat.quickChatField.background.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            }
        }
        else
        {
            if (chat.freeChatField != null)
            {
                chat.freeChatField.background.color = Color.white;

                if (chat.freeChatField.textArea != null)
                {
                    chat.freeChatField.textArea.compoText.Color(Color.white);
                    chat.freeChatField.textArea.outputText.color = Color.black;
                }
            }

            if (QuickChatIcon == null)
                QuickChatIcon = GameObject.Find("QuickChatIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                QuickChatIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.QuickChat.png", 100f);

            if (OpenBanMenuIcon == null)
                OpenBanMenuIcon = GameObject.Find("OpenBanMenuIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenBanMenuIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.Report.png", 100f);

            if (OpenKeyboardIcon == null)
                OpenKeyboardIcon = GameObject.Find("OpenKeyboardIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenKeyboardIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.Keyboard.png", 100f);

            if (GameStates.IsDead)
            {
                if (chat.freeChatField != null)
                    chat.freeChatField.background.color = new Color(1f, 1f, 1f, 0.5f);

                if (chat.quickChatField != null)
                    chat.quickChatField.background.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }

    private static void HandleClipboardAndHistory(ChatController chat)
    {
        if (chat == null || chat.freeChatField == null || chat.freeChatField.textArea == null)
            return;

        if (!chat.freeChatField.textArea.hasFocus)
            return;

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(chat.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            chat.freeChatField.textArea.SetText(chat.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(chat.freeChatField.textArea.text);
            chat.freeChatField.textArea.SetText("");
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.ChatHistory.Count - 1);
            chat.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection++;

            if (CurrentHistorySelection < ChatCommands.ChatHistory.Count)
                chat.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
            else
                chat.freeChatField.textArea.SetText("");
        }
    }

    public static void TryFlashChatNotifyDot()
    {
        try
        {
            if (Instance == null)
                return;

            ChatNotification notification = null;
            SpriteRenderer dot = null;

            try
            {
                if (ChatNotificationField != null)
                {
                    object raw = ChatNotificationField.GetValue(Instance);
                    notification = (raw as Il2CppObjectBase)?.TryCast<ChatNotification>();
                }
            }
            catch
            {
            }

            try
            {
                if (ChatNotifyDotField != null)
                {
                    object raw = ChatNotifyDotField.GetValue(Instance);
                    dot = (raw as Il2CppObjectBase)?.TryCast<SpriteRenderer>();
                }
            }
            catch
            {
            }

            notification?.Close();

            if (dot != null)
                dot.enabled = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[BanMod] TryFlashChatNotifyDot error: " + ex);
        }
    }

    public static void TrySetChatNotification(PlayerControl sourcePlayer, string text)
    {
        try
        {
            if (Instance == null || sourcePlayer == null)
                return;

            ChatNotification notification = null;

            if (ChatNotificationField != null)
            {
                object raw = ChatNotificationField.GetValue(Instance);
                notification = (raw as Il2CppObjectBase)?.TryCast<ChatNotification>();
            }

            notification?.SetUp(sourcePlayer, text);

            AudioClip messageSound = null;

            if (messageSound != null)
                SoundManager.Instance.PlaySound(messageSound, false, 1f, null).pitch = 0.5f + sourcePlayer.PlayerId / 15f;

            TryFlashChatNotifyDot();
        }
        catch
        {
        }
    }

    public static PassiveButton SafeGetPassiveButton(Component source)
    {
        if (source == null)
            return null;

        try
        {
            Component c = source.GetComponent("PassiveButton");

            if (c == null)
                return null;

            return (c as Il2CppObjectBase)?.TryCast<PassiveButton>();
        }
        catch
        {
            return null;
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
class ChatSendPatch
{
    static bool Prefix(ChatController __instance)
    {
        if (__instance == null)
            return false;

        string freeMessage = __instance.freeChatField?.textArea?.text;
        bool quickChatReady = __instance.quickChatMenu != null && __instance.quickChatMenu.CanSend;

        bool isModdedChat =
            ModdedOriginalChatManager.Enabled &&
            !quickChatReady &&
            !string.IsNullOrWhiteSpace(freeMessage);

        if (isModdedChat)
            return true;

        if (!string.IsNullOrEmpty(freeMessage) && freeMessage.Length > 120)
        {
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                HudManager.Instance.Notifier,
                $"Messaggio troppo lungo! Max 120 caratteri. Messaggio digitato: \"{freeMessage}\" (Lunghezza: {freeMessage.Length})");

            return false;
        }

        if (!quickChatReady && string.IsNullOrWhiteSpace(freeMessage))
            return false;

        if (__instance.timeSinceLastMessage <= 3.15f)
        {
            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    Translator.GetString("Waitasecond"));
            }

            return false;
        }

        if (!MessageBlocker.CanSendMessage())
        {
            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    Translator.GetString("Waitasecond"));
            }

            return false;
        }

        MessageBlocker.UpdateLastMessageTime();
        return true;
    }
}
[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
internal class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        if (__instance == null || __instance.textArea == null || __instance.charCountText == null)
            return;

        int length = __instance.textArea.text.Length;

        __instance.charCountText.SetText(
            length <= 0
                ? Translator.GetString("BANMOD")
                : $"{length}/{__instance.textArea.characterLimit}");

        __instance.charCountText.enableWordWrapping = false;

        if (length < (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? 80 : 100))
            __instance.charCountText.color = Color.cyan;
        else if (length < (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? 101 : 120))
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        else
            __instance.charCountText.color = Color.red;
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Start))]
public static class AllowPaste_TextBoxTMP_Start_Postfix
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (__instance == null)
            return;

        __instance.allowAllCharacters = true;
        __instance.AllowEmail = true;
        __instance.AllowSymbols = true;
    }
}
public static class ChatCopyData
{
    public static readonly Dictionary<ChatBubble, string> Messages =
        new Dictionary<ChatBubble, string>();
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
public static class ChatBubblePatch
{
    public static void Postfix(ChatBubble __instance, string chatText)
    {
        if (__instance == null)
            return;

        ChatCopyData.Messages[__instance] = chatText;
    }
}

[HarmonyPatch(typeof(ChatController), "Update")]
public static class ChatCopyPatch
{
    public static void Postfix()
    {
        bool ctrl =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        if (!ctrl || !Input.GetKeyDown(KeyCode.C))
            return;

        ChatBubble bubble = FindBubbleUnderMouse();

        if (bubble == null)
            return;

        if (ChatCopyData.Messages.TryGetValue(
            bubble,
            out string text))
        {
            GUIUtility.systemCopyBuffer = text;

            Debug.Log(
                $"[ChatCopy] Copied: {text}");
        }
    }

    private static ChatBubble FindBubbleUnderMouse()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return null;

        Vector3 mousePos =
            Input.mousePosition;

        ChatBubble nearestBubble = null;
        float nearestDistance =
            float.MaxValue;

        foreach (KeyValuePair<ChatBubble, string> pair
                 in ChatCopyData.Messages)
        {
            ChatBubble bubble = pair.Key;

            if (bubble == null)
                continue;

            if (bubble.TextArea == null)
                continue;

            Vector3 screenPos =
                cam.WorldToScreenPoint(
                    bubble.TextArea.transform.position);

            float distance =
                Vector2.Distance(
                    mousePos,
                    screenPos);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBubble = bubble;
            }
        }

        if (nearestDistance > 120f)
            return null;

        return nearestBubble;
    }
}
