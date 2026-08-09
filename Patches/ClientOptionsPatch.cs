using HarmonyLib;
using Rewired.Utils.Platforms.Windows;
using UnityEngine;

namespace BanMod;

//锟斤拷源锟斤拷https://github.com/tukasa0001/TownOfHost/pull/1265
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem ShowFPS;
    private static ClientOptionItem GM;
    private static ClientOptionItem DarkTheme;
    private static ClientOptionItem DisableLobbyMusic;
    private static ClientOptionItem AktiveLobby;
    private static ClientOptionItem SeeRoleMeeting;
    private static ClientOptionItem EnableZoom;
    private static ClientOptionItem AktiveChat;
    private static ClientOptionItem ChatOffIfImpostor;
    private static ClientOptionItem Resize_Player;
    private static ClientOptionItem Teleport;
    private static ClientOptionItem NoGameEnd;
    private static ClientOptionItem AddBanToList;
    private static ClientOptionItem ExcludeFriends;
    private static ClientOptionItem VoteLockEnabled;
    private static ClientOptionItem SwitchVanilla;

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null) return;

        if (ShowFPS == null || ShowFPS.ToggleButton == null)
        {
            ShowFPS = ClientOptionItem.Create("ShowFPS", BanMod.ShowFPS, __instance);
        }
        if (GM == null || GM.ToggleButton == null)
        {
            GM = ClientOptionItem.Create("GM", BanMod.GM, __instance);
        }
        if (DarkTheme == null || DarkTheme.ToggleButton == null)
        {
            DarkTheme = ClientOptionItem.Create("DarkTheme", BanMod.DarkTheme, __instance);
        }
        if (DisableLobbyMusic == null || DisableLobbyMusic.ToggleButton == null)
        {
            DisableLobbyMusic = ClientOptionItem.Create("DisableLobbyMusic", BanMod.DisableLobbyMusic, __instance);
        }
        if (AktiveLobby == null || AktiveLobby.ToggleButton == null)
        {
            AktiveLobby = ClientOptionItem.Create("ActiveLobbyDecorations", BanMod.AktiveLobby, __instance);
        }
        if (SeeRoleMeeting == null || SeeRoleMeeting.ToggleButton == null)
        {
            SeeRoleMeeting = ClientOptionItem.Create("SeeRoleMeeting", BanMod.SeeRoleMeeting, __instance);
        }
        if (EnableZoom == null || EnableZoom.ToggleButton == null)
        {
            EnableZoom = ClientOptionItem.Create("EnableZoom", BanMod.EnableZoom, __instance);
        }
        if (AktiveChat == null || AktiveChat.ToggleButton == null)
        {
            AktiveChat = ClientOptionItem.Create("Vis_EnableChat", BanMod.AktiveChat, __instance);
        }
        if (ChatOffIfImpostor == null || ChatOffIfImpostor.ToggleButton == null)
        {
            ChatOffIfImpostor = ClientOptionItem.Create("ChatOffIfImpostor", BanMod.ChatOffIfImpostor, __instance);
        }
        if (Resize_Player == null || Resize_Player.ToggleButton == null)
        {
            Resize_Player = ClientOptionItem.Create("Resize_Player", BanMod.Resize_Player, __instance);
        }
        if (Teleport == null || Teleport.ToggleButton == null)
        {
            Teleport = ClientOptionItem.Create("Teleport", BanMod.Teleport, __instance);
        }
        if (NoGameEnd == null || NoGameEnd.ToggleButton == null)
        {
            NoGameEnd = ClientOptionItem.Create("NoGameEnd", BanMod.NoGameEnd, __instance);
        }
        if (AddBanToList == null || AddBanToList.ToggleButton == null)
        {
            AddBanToList = ClientOptionItem.Create("AddBanToList", BanMod.AddBanToList, __instance);
        }
        if (ExcludeFriends == null || ExcludeFriends.ToggleButton == null)
        {
            ExcludeFriends = ClientOptionItem.Create("ExcludeFriends", BanMod.ExcludeFriends, __instance);
        }
        if (VoteLockEnabled == null || VoteLockEnabled.ToggleButton == null)
        {
            VoteLockEnabled = ClientOptionItem.Create("Opt_VoteLockEnabled", BanMod.VoteLockEnabled, __instance);
        }
        if (SwitchVanilla == null || SwitchVanilla.ToggleButton == null)
        {
            SwitchVanilla = ClientOptionItem.Create("SwitchVanilla", BanMod.SwitchVanilla, __instance, SwitchVanillaButtonToggle);
            static void SwitchVanillaButtonToggle()
            {
                Harmony.UnpatchAll();
                BanMod.Instance.Unload();
            }
        }
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}
