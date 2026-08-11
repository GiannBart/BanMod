//credits and licenses in the resources folder
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Linq;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

public static class Engineer
{
    public static void SendEngineerMessage()
    {
        var engineerPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.Data.RoleType == RoleTypes.Engineer);
        if (engineerPlayer == null || engineerPlayer.Data.IsDead) return;
        if (Jester.IsJester(engineerPlayer.PlayerId)) return;
        byte engineerId = engineerPlayer.PlayerId;

        string msg = string.Format(GetString("EngineerInfo"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, engineerId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, engineerId);
            MessageBlocker.UpdateLastMessageTime();

        }

    }

    public static void SendEngineerMessageTest()
    {
        string msg = string.Format(GetString("EngineerInfo"));
        {
            Utils.SendMessage(msg, 255);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
}
[HarmonyPatch(typeof(EngineerRole), "FixedUpdate")]
public static class EngineerRole_FixedUpdate_Patch
{
    private static readonly Dictionary<byte, int> VentFixCounts = new();
    public static string message1;
    public static void Postfix(EngineerRole __instance)
    {
        if (BanMod.IsBanModDisabled) return;
        if (__instance?.Player == null) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (Jester.IsJester(__instance.Player.PlayerId)) return;
        if (!Options.EngineerFixer.GetBool()) return;


        if (!__instance.Player.inVent) return;

        if (!PlayerTask.AllTasksCompleted(__instance.Player)) return;

        var playerId = __instance.Player.PlayerId;
        var shipStatus = ShipStatus.Instance;
        if (shipStatus == null) return;

        if (!VentFixCounts.ContainsKey(playerId))
            VentFixCounts[playerId] = 0;

        int maxFixes = Options.VentTimes.GetInt();
        if (VentFixCounts[playerId] >= maxFixes) return;

        if (FixSabotages(shipStatus))
        {
            VentFixCounts[playerId]++;
        }
        else
        {
            return;
        }

        int remainingFixes = maxFixes - VentFixCounts[playerId];
        message1 = GetString("VentRemain") + $"{VentFixCounts[playerId]} / {maxFixes}";

        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(message1, playerId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(message1, playerId);
            MessageBlocker.UpdateLastMessageTime();
        }
    }

    private static bool FixSabotages(ShipStatus shipStatus)
    {
        bool fixedAny = false;

        void Fix(SystemTypes type, int value)
        {
            shipStatus.RpcUpdateSystem(type, (byte)value);
            fixedAny = true;
        }

        if (AnySabotageIsActive())
        {
            Fix(SystemTypes.Reactor, 16);
            Fix(SystemTypes.Reactor, 16 | 0);
            Fix(SystemTypes.Reactor, 16 | 1);
            Fix(SystemTypes.Laboratory, 16);
            Fix(SystemTypes.HeliSabotage, 16 | 0);
            Fix(SystemTypes.HeliSabotage, 16 | 1);
            Fix(SystemTypes.LifeSupp, 16);
            Fix(SystemTypes.Comms, 16 | 0);
            Fix(SystemTypes.Comms, 16 | 1);

            if (shipStatus.Systems.TryGetValue(SystemTypes.Electrical, out var system))
            {
                var elecSys = system.Cast<SwitchSystem>();
                for (var i = 0; i < 5; i++)
                {
                    int switchMask = 1 << (i & 0x1F);
                    if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                    {
                        shipStatus.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                        fixedAny = true;
                    }
                }
            }
        }

        return fixedAny;
    }
}
