//credits and licenses in the resources folder
using BanMod;
using System;
using System.Collections.Generic;
using System.Linq;
using static BanMod.Utils;
using static BanMod.Translator;
using static BanMod.ExtendedPlayerControl;

namespace BanMod;
public static class ImpostorManager
{
    public static List<PlayerControl> ImpostorsList = new List<PlayerControl>();

    public static void DetectImpostors()
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        var alive = BanMod.AllAlivePlayerControls;
        if (alive == null || alive.Count == 0)
            return;

        var impostors = alive
            .Where(p => p.Data?.Role?.TeamType == RoleTeamTypes.Impostor)
            .ToList();

        ImpostorsList.Clear();
        ImpostorsList.AddRange(impostors);
    }

    public static List<PlayerControl> GetImpostorsList()
    {
        return ImpostorsList;
    }
    public static void ImpostorNameSender()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var impostors = ImpostorManager.GetImpostorsList()
            .Where(p => IsPlayerActive((byte)p.PlayerId))
            .ToList();

        if (impostors.Count == 1)
        {
            var impostor = impostors[0];
            string name = impostor.name;
            string msg = GetString("OnlyImpostor");
            string msg1 = $"{GetString("OnlyImpostor")}";
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, (byte)impostor.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg1, (byte)impostor.PlayerId);
                MessageBlocker.UpdateLastMessageTime();

            }
        }
        else if (impostors.Count == 2)
        {
            var i1 = impostors[0];
            var i2 = impostors[1];

            string name1 = i1.name;
            string name2 = i2.name;

            string msgToI1 = $"{GetString("ImpostorAlly")} {name2}";
            string msgToI2 = $"{GetString("ImpostorAlly")} {name1}";
            string msgToI11 = $"{GetString("ImpostorAlly")} {name2}";
            string msgToI22 = $"{GetString("ImpostorAlly")} {name1}";
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msgToI1, (byte)i1.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msgToI11, (byte)i1.PlayerId);
                MessageBlocker.UpdateLastMessageTime();

            }
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msgToI2, (byte)i2.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msgToI22, (byte)i2.PlayerId);
                MessageBlocker.UpdateLastMessageTime();

            }
        }
    }
    public static void ImpostorNameSenderTest()
    {



        string name1 = PlayerControl.LocalPlayer.Data.PlayerName;
        string name2 = PlayerControl.LocalPlayer.Data.PlayerName;

        string msgToI1 = $"{GetString("ImpostorAlly")} {name2}";
        string msgToI2 = $"{GetString("ImpostorAlly")} {name1}";
        string msgToI11 = $"{GetString("ImpostorAlly")} {name2}";
        string msgToI22 = $"{GetString("ImpostorAlly")} {name1}";

        Utils.SendMessage(msgToI11, 255);
        MessageBlocker.UpdateLastMessageTime();
        Utils.SendMessage(msgToI22, 255);
        MessageBlocker.UpdateLastMessageTime();

    }
}
