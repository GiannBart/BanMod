//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Rewired.UI.ControlMapper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

public static class ImpostorGuesser
{
    public static byte PhantomPlayerId = 255;
    public static byte ShapePlayerId = 255;
    public static byte ViperPlayerId = 255;
    public static byte impostorPlayerId = 255;

    public static void SendPhantomPlayerMessage()
    {
        if (!Options.PhantomGuess.GetBool())
            return;

        var allPlayers = BanMod.AllPlayerControls;

        var phantomPlayer = allPlayers
            .FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.IsDead &&
                Phantom(p));

        if (phantomPlayer == null)
            return;

        PhantomPlayerId = phantomPlayer.PlayerId;

        string msg = string.Format(GetString("GuessImmortalInfo"));

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomPlayerId);
        }
        else
        {
            Utils.SendMessage(msg, PhantomPlayerId);
        }

        MessageBlocker.UpdateLastMessageTime();

        try
        {
            RoleButtonRefresh.RefreshNow();
        }
        catch
        {
        }
    }

    public static void SendShapePlayerMessage()
    {
        if (!Options.ShapeGuess.GetBool())
            return;

        var allPlayers = BanMod.AllPlayerControls;

        var shapePlayer = allPlayers
            .FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.IsDead &&
                Shapeshifter(p));

        if (shapePlayer == null)
            return;

        ShapePlayerId = shapePlayer.PlayerId;

        string msg = string.Format(GetString("GuessImmortalInfo"));

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, ShapePlayerId);
        }
        else
        {
            Utils.SendMessage(msg, ShapePlayerId);
        }

        MessageBlocker.UpdateLastMessageTime();

        try
        {
            RoleButtonRefresh.RefreshNow();
        }
        catch
        {
        }
    }

    public static void SendViperPlayerMessage()
    {
        if (!Options.ViperGuess.GetBool())
            return;

        var allPlayers = BanMod.AllPlayerControls;

        var viperPlayer = allPlayers
            .FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.IsDead &&
                Cobra(p));

        if (viperPlayer == null)
            return;

        ViperPlayerId = viperPlayer.PlayerId;

        string msg = string.Format(GetString("GuessImmortalInfo"));

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, ViperPlayerId);
        }
        else
        {
            Utils.SendMessage(msg, ViperPlayerId);
        }

        MessageBlocker.UpdateLastMessageTime();

        try
        {
            RoleButtonRefresh.RefreshNow();
        }
        catch
        {
        }
    }

    public static void SendImpostorPlayerMessage()
    {
        if (!Options.ImpostorGuess.GetBool())
            return;

        var allPlayers = BanMod.AllPlayerControls;

        var impostorPlayer = allPlayers
            .FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.IsDead &&
                Impostor(p));

        if (impostorPlayer == null)
            return;

        impostorPlayerId = impostorPlayer.PlayerId;

        string msg = string.Format(GetString("GuessImmortalInfo"));

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost &&
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, impostorPlayerId);
        }
        else
        {
            Utils.SendMessage(msg, impostorPlayerId);
        }

        MessageBlocker.UpdateLastMessageTime();

        try
        {
            RoleButtonRefresh.RefreshNow();
        }
        catch
        {
        }
    }

    public static bool IsAnyImpGuesser(byte playerId)
    {
        if (playerId == byte.MaxValue || playerId == 255)
            return false;

        if (Options.PhantomGuess.GetBool() && playerId == PhantomPlayerId)
            return true;

        if (Options.ShapeGuess.GetBool() && playerId == ShapePlayerId)
            return true;

        if (Options.ViperGuess.GetBool() && playerId == ViperPlayerId)
            return true;

        if (Options.ImpostorGuess.GetBool() && playerId == impostorPlayerId)
            return true;

        return false;
    }

    public static void Reset()
    {
        PhantomPlayerId = 255;
        ShapePlayerId = 255;
        ViperPlayerId = 255;
        impostorPlayerId = 255;
    }
}