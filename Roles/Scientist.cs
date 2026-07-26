//credits and licenses in the resources folder
using HarmonyLib;
using Il2CppSystem.Linq;
using Il2CppSystem.Runtime.Remoting.Messaging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.ChatCommands;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

public static class Scientist
{
    public static void OnMeetingStarted()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Options.ScientistTime.GetBool()) return;

        MeetingStartTime = Time.time;
        float now = MeetingStartTime;

        foreach (var player in BanMod.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsDead) continue;

            if (Scientist(player))
            {
                foreach (var kvp in BanMod.playerDeathTimes)
                {
                    byte deadId = kvp.Key;
                    float deathTime = kvp.Value;

                    PlayerControl deadPlayer = GetPlayerById(deadId);
                    if (deadPlayer == null) continue;

                    int secondsAgo = Mathf.FloorToInt(now - deathTime);
                    string deadName = deadPlayer.name;
                    string messages = $"{deadName}: {ToFullWidthNumbers(secondsAgo.ToString())}s";
                    string title = $"{GetString("ScientistPlayerDiedTime")}";

                    if (!player.Data.IsDead)
                    {
                        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            Utils.RequestProxyMessage(messages, player.PlayerId);
                            MessageBlocker.UpdateLastMessageTime();
                        }
                        else
                        {
                            Utils.SendMessage(messages, player.PlayerId);
                            MessageBlocker.UpdateLastMessageTime();
                        }
                    }
                }
            }
        }
    }
    public static void ScientistCommandHost()
    {
        float now = MeetingStartTime; 
        List<string> messages = new List<string>();

        foreach (var kvp in BanMod.playerDeathTimes)
        {
            byte deadId = kvp.Key;
            float deathTime = kvp.Value;

            PlayerControl deadPlayer = Utils.GetPlayerById(deadId);
            if (deadPlayer == null) continue;

            int secondsAgo = Mathf.FloorToInt(now - deathTime);
            string deadName = deadPlayer.name;
            string line = $"{deadName}: {secondsAgo}s";
            messages.Add(line);
        }

        if (messages.Count > 0) 
        {
            string finalMessage = string.Join("\n", messages);
            ShowChat(finalMessage);
        }
        else
        {
            ShowChat(Translator.GetString("no_deaths_recorded")); 
        }
    }

    public static void ScientistCommand(PlayerControl targetPlayer)
    {
        if (targetPlayer == null || !Scientist(targetPlayer)) return;

        if (MeetingStartTime == 0f)
        {
            Utils.SendMessage(Translator.GetString("ScientistCommandNotReady"), targetPlayer.PlayerId);
            MessageBlocker.UpdateLastMessageTime();
            return;
        }

        float now = MeetingStartTime; 

        List<string> messages = new List<string>();

        foreach (var kvp in BanMod.playerDeathTimes)
        {
            byte deadId = kvp.Key;
            float deathTime = kvp.Value;

            PlayerControl deadPlayer = GetPlayerById(deadId);
            if (deadPlayer == null) continue;

            int secondsAgo = Mathf.FloorToInt(now - deathTime);
            string deadName = deadPlayer.name;
            string secondsInWords = Utils.NumberToWords(secondsAgo);
            string line = $"{deadName} - {secondsAgo} ({secondsInWords}) {Translator.GetString("seconds_suffix")}";
            messages.Add(line);
        }

        if (messages.Count > 0)
        {
            string finalMessage = string.Join("\n", messages);
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(finalMessage, targetPlayer.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(finalMessage, targetPlayer.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
        }
        else
        {
            Utils.SendMessage(Translator.GetString("ScientistNoDeathsYet"), targetPlayer.PlayerId);
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendScientistMessage()
    {
        if (!Options.ScientistTime.GetBool()) return;

        PlayerControl scientist = BanMod.AllPlayerControls
            .FirstOrDefault(p => p != null && p.Data != null && Scientist(p));

        if (scientist == null) return;

        string msg1 = string.Format(GetString("ScientistInfo"));
        byte scientistPlayerId = scientist.PlayerId;

        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg1, scientistPlayerId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg1, scientistPlayerId);
            MessageBlocker.UpdateLastMessageTime();

        }

    }
    public static void SendScientistMessageTest()
    {

        string msg1 = string.Format(GetString("ScientistInfo"));

        {
            Utils.SendMessage(msg1, 255);
            MessageBlocker.UpdateLastMessageTime();

        }

    }
}