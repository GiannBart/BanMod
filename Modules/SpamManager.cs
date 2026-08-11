//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using Assets.CoreScripts;
using HarmonyLib;
using UnityEngine;
using static InnerNet.ClientData;
using static ChatController;
using static FilterPopUp.FilterInfoUI;
using AmongUs.Data;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class SpamManager
    {
        private static readonly string SPAMSTART_FILE_PATH = "./BAN_DATA/DENIED/SpamStart.txt";
        private static readonly string BANEDWORDS_FILE_PATH = "./BAN_DATA/DENIED/BanWords.txt";

        internal static string msg1;
        internal static string msg2;

        public static List<string> SpamStart = new();
        public static List<string> BanWords = new();

        public static void Initialize()
        {
            CreateIfNotExists();
            SpamStart = ReadNonEmptyLines(SPAMSTART_FILE_PATH).ToList();
            BanWords = ReadNonEmptyLines(BANEDWORDS_FILE_PATH).ToList();
        }

        private static void CreateIfNotExists()
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");

            if (!File.Exists(SPAMSTART_FILE_PATH))
            {
                try
                {
                    if (File.Exists("./SpamStart.txt")) File.Move("./SpamStart.txt", SPAMSTART_FILE_PATH);
                    else
                    {
                        string fileName = GetLanguageFileName();
                        BMLogger.Warn($"Creating new SpamStart file: {fileName}", "SpamManager");
                        File.WriteAllText(SPAMSTART_FILE_PATH, GetResourcesTxt($"BanMod.Resources.SpamStart.{fileName}.txt"));
                    }
                }
                catch (Exception ex)
                {
                    BMLogger.Exception(ex, "SpamManager");
                }
            }

            if (!File.Exists(BANEDWORDS_FILE_PATH))
            {
                try
                {
                    if (File.Exists("./BanWords.txt")) File.Move("./BanWords.txt", BANEDWORDS_FILE_PATH);
                    else if (File.Exists("./BanWords.txt")) File.Move("./BanWords.txt", BANEDWORDS_FILE_PATH);
                    else
                    {
                        string fileName = GetLanguageFileName();
                        BMLogger.Warn($"Creating new BanWords file: {fileName}", "SpamManager");
                        File.WriteAllText(BANEDWORDS_FILE_PATH, GetResourcesTxt($"BanMod.Resources.BanWord.{fileName}.txt"));
                    }
                }
                catch (Exception ex)
                {
                    BMLogger.Exception(ex, "SpamManager");
                }
            }
        }

        private static string GetLanguageFileName()
        {
            var name = CultureInfo.CurrentCulture.Name.Split("-");
            return name.Length >= 2 ? name[0] switch
            {
                "zh" => "SChinese",
                "ru" => "Russian",
                "it" => "Italian",
                _ => "English"
            } : "English";
        }

        private static string GetResourcesTxt(string path)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            using StreamReader reader = new(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static IEnumerable<string> ReadNonEmptyLines(string path)
        {
            if (!File.Exists(path)) return Enumerable.Empty<string>();
            return File.ReadAllLines(path, Encoding.UTF8)
                       .Select(l => l.Trim())
                       .Where(l => l.Length > 1);
        }

        private static string Colorize(string text, string colorHex) => $"<color={colorHex}>{text}</color>";

        public static bool CheckWord(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                string lowerText = text.ToLowerInvariant();

                foreach (var pattern in BanWords)
                {
                    string lowerPattern = pattern.ToLowerInvariant().Trim();

                    string patternRegex = $@"\b{Regex.Escape(lowerPattern)}\b";
                    if (Regex.IsMatch(lowerText, patternRegex, RegexOptions.CultureInvariant))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Exception(ex, "SpamManager");
                return true;
            }
        }


        public static bool CheckStart(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                string lowerText = text.ToLowerInvariant().Trim();

                foreach (var pattern in SpamStart)
                {
                    string lowerPattern = pattern.ToLowerInvariant().Trim();

                    string patternRegex = $@"\b{Regex.Escape(lowerPattern)}\b";
                    if (Regex.IsMatch(lowerText, patternRegex, RegexOptions.CultureInvariant))
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                BMLogger.Exception(ex, "SpamManager");
                return true;
            }
        }




        public static bool CheckStart(PlayerControl player, string text)
        {
            bool kick = false;
            string playername = player.Data.PlayerName;

            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId ||
                (BanMod.ExcludeFriends.Value && Utils.IsVip(player.FriendCode) ||
                Utils.IsModerator(player.FriendCode)))
                return false;
            if (!CheckStart(text))
                return false;
            if (!AmongUsClient.Instance.AmHost) return false;
            if (Options.AutoKickStart.GetBool() && CheckStart(text) && GameStates.isLobby)
            {
                var clientId = player.GetClientId();
                BanMod.SayStartTimes.TryAdd(clientId, 0);
                BanMod.SayStartTimes[clientId]++;

                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, playername + GetString("SayStart"));
                msg1 = GetString("SpamWarning") +
                       $"{BanMod.SayStartTimes[clientId]} / {Options.AutoKickStartTimes.GetInt()})";
                if (Options.SendAutoKickStartMsg.GetBool())
                {
                    Utils.SendMessage(msg1, player.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                if (BanMod.SayStartTimes[clientId] > Options.AutoKickStartTimes.GetInt())
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, playername + GetString("KickSayStart"));
                    kick = true;
                }
            }
            int action = Options.AutoKickStartAction.GetValue();
            var client = AmongUsClient.Instance.GetClient(player.GetClientId());
            if (action == 1) 
            {
                BanManager.AddBanPlayer(client, "CheckSpam");
            }
            if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), action == 1);
            return true;
        }

        public static bool CheckWord(PlayerControl player, string text)
        {
            bool kick = false;
            string playername = player.Data.PlayerName;

            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId ||
                (BanMod.ExcludeFriends.Value && Utils.IsVip(player.FriendCode) ||
                Utils.IsModerator(player.FriendCode)))
                return false;
            if (!CheckWord(text))
                return false;
            if (!AmongUsClient.Instance.AmHost) return false;
            if (Options.AutoKickStopWords.GetBool() && CheckWord(text))
            {
                var clientId = player.GetClientId();
                BanMod.SayBanwordsTimes.TryAdd(clientId, 0);
                BanMod.SayBanwordsTimes[clientId]++;

                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, playername + GetString("SayBanWord"));
                msg2 = GetString("WordWarning") +
                       $"{BanMod.SayBanwordsTimes[clientId]} / {Options.AutoKickStopWordsTimes.GetInt()})";
                if (Options.SendAutoKickStopWordsMsg.GetBool())
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg2, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg2, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                if (BanMod.SayBanwordsTimes[clientId] > Options.AutoKickStopWordsTimes.GetInt())
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, playername + GetString("KickSayBanWord"));
                    kick = true;
                }
            }

            int action = Options.AutoKickStopWordsAction.GetValue();
            if (kick)
            {
                var client = AmongUsClient.Instance.GetClient(player.GetClientId());
                if (action == 1) 
                {
                    BanManager.AddBanPlayer(client, "CheckWord");
                }

                AmongUsClient.Instance.KickPlayer(player.GetClientId(), action == 1);
            }
            return true;
        }
    }
}
