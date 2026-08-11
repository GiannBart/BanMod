//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BanMod;

public static class BanManager
{
    private const string DenyNameListPath = "./BAN_DATA/DENIED/DenyName.txt";
    private const string BanListPath = "./BAN_DATA/DENIED/BanList.txt";
    private const string BanModeratorListPath = "./BAN_DATA/DENIED/BanModeratorList.txt";
    public class BanEntry
    {
        public string FriendCode;
        public string HashedPuid;
        public string PlayerName;
        public string Reason;
    }

    public static void Initialize()
    {
        try
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");

            if (!File.Exists(BanListPath))
                File.Create(BanListPath).Close();

            if (!File.Exists(BanModeratorListPath))
                File.Create(BanModeratorListPath).Close();

            if (!File.Exists(DenyNameListPath))
                File.Create(DenyNameListPath).Close();

            Directory.CreateDirectory("BAN_DATA/ALLOWED");

        }
        catch (Exception) { }
    }

    public static IEnumerator WaitAndCheckAll(ClientData client)
    {
        if (client == null)
            yield break;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            yield break;

        int clientId = client.Id;
        string fallbackName = client.PlayerName ?? "";

        PlayerControl playerControl = null;
        int attempts = 0;

        while (playerControl == null && attempts < 30)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                yield break;

            playerControl = PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(p => p != null && p.OwnerId == clientId);

            if (playerControl == null)
            {
                attempts++;
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (playerControl == null)
        {
            BMLogger.Info("[BanMod] Impossibile trovare PlayerControl per il client: " + fallbackName);
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        if (GameData.Instance == null)
            yield break;

        if (playerControl == null || playerControl.Data == null || playerControl.Data.Disconnected)
            yield break;

        NetworkedPlayerInfo playerInfo = GameData.Instance.GetPlayerById(playerControl.PlayerId);

        if (playerInfo != null && playerInfo.PlayerLevel <= 1)
        {
            yield return new WaitForSeconds(0.5f);

            if (GameData.Instance == null)
                yield break;

            playerInfo = GameData.Instance.GetPlayerById(playerControl.PlayerId);

            if (playerInfo != null && playerInfo.PlayerLevel == 0)
            {
                yield return new WaitForSeconds(1.0f);

                if (GameData.Instance == null)
                    yield break;

                playerInfo = GameData.Instance.GetPlayerById(playerControl.PlayerId);
            }
        }

        if (playerInfo == null)
            yield break;

        if (AmongUsClient.Instance == null)
            yield break;

        ClientData liveClient = AmongUsClient.Instance.GetClient(clientId) ?? AmongUsClient.Instance.GetRecentClient(clientId);
        
        if (liveClient == null)
            liveClient = client;

        string realName = playerInfo.DefaultOutfit?.PlayerName ?? liveClient.PlayerName ?? fallbackName;

        {
            int colorId = playerInfo.DefaultOutfit?.ColorId ?? -1;

            if (colorId == 18 && !BanMod.IsProtected(liveClient))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    AmongUsClient.Instance.KickPlayer(clientId, false);

                yield break;
            }
        }

        if (Options.KickLevel.GetBool() && !BanMod.IsProtected(liveClient))
        {
            if (GameData.Instance == null)
                yield break;

            var pInfo = GameData.Instance.GetPlayerById(playerInfo.PlayerId);

            if (pInfo == null)
                yield break;

            if (pInfo.PlayerLevel == 0)
            {
                yield return new WaitForSeconds(3f);

                if (GameData.Instance == null)
                    yield break;

                pInfo = GameData.Instance.GetPlayerById(playerInfo.PlayerId);

                if (pInfo == null)
                    yield break;
            }

            int realLevel = (int)(pInfo.PlayerLevel + 1);
            int minLevel = Options.KickLevelLevel.GetInt();
            string action = Options.KickLevelAction.GetString();

            if (realLevel < minLevel)
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    yield break;

                if (action == "Ban")
                {
                    AmongUsClient.Instance.KickPlayer(clientId, true);
                }
                else if (action == "Kick")
                {
                    AmongUsClient.Instance.KickPlayer(clientId, false);
                }

                if (HudManager.Instance?.Notifier != null)
                {
                    NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                        HudManager.Instance.Notifier,
                        $"{realName} rimosso (LV {realLevel} < {minLevel})"
                    );
                }
            }
        }
    }
    public static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return string.Empty;
        string puid = player.ProductUserId;
        using SHA256 sha256 = SHA256.Create();
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();
        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
    }

    public static void AddBanPlayer(ClientData player, string reason = "ManualBan", bool fromModeratorCommand = false)
    {
        if (player == null)
            return;

        if (!AmongUsClient.Instance.AmHost || !BanMod.AddBanToList.Value)
            return;

        if (SilentPermanentFriendCodeBan.ShouldSuppressBanManagerWrite(player))
            return;

        if (BanMod.IsProtected(player))
            return;

        string friendCode = player.FriendCode;
        string hashedPuid = player.GetHashedPuid();

        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(hashedPuid))
            return;

        if (GetBanEntry(friendCode, hashedPuid) != null)
            return;

        if (hashedPuid == "e3b0cb855")
            hashedPuid = "";

        string realName = BanMod.GetRealPlayerName(player);

        string line = $"{friendCode},{hashedPuid},{realName},{reason}";
        string moderatorLine = $"{friendCode},{hashedPuid},{realName},ModeratorBan";

        if (fromModeratorCommand)
        {
            File.AppendAllText(BanModeratorListPath, moderatorLine + Environment.NewLine);

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    $"{realName} {Translator.GetString("PlayerinBanList")} (ModeratorBan)"
                );
            }
        }
        else
        {
            File.AppendAllText(BanListPath, line + Environment.NewLine);

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    $"{realName} {Translator.GetString("PlayerinBanList")} ({reason})"
                );
            }
        }

    }
    public static void AddBanPlayerFromOverload(ClientData player, string reason = "OVERLOAD_HACKER", bool fromModeratorCommand = false)
    {
        if (player == null)
            return;
        if (BanMod.IsProtected(player))
            return;
        if (SilentPermanentFriendCodeBan.ShouldSuppressBanManagerWrite(player))
            return;

        string friendCode = player.FriendCode;
        string hashedPuid = player.GetHashedPuid();

        if (hashedPuid == "e3b0cb855")
            hashedPuid = "";

        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(hashedPuid))
            return;

        try
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");

            if (!File.Exists(BanListPath))
                File.Create(BanListPath).Close();

            foreach (string line in File.ReadLines(BanListPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 2)
                    continue;

                string fc = parts[0];
                string puid = parts[1];

                if ((!string.IsNullOrEmpty(friendCode) && fc == friendCode) ||
                    (!string.IsNullOrEmpty(hashedPuid) && puid == hashedPuid))
                {
                    return;
                }
            }

            string realName = BanMod.GetRealPlayerName(player);

            string lineToAdd = $"{friendCode},{hashedPuid},{realName},{reason}";
            File.AppendAllText(BanListPath, lineToAdd + Environment.NewLine);

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    $"{realName} added to BanList ({reason})"
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BanMod] AddBanPlayerFromOverload error: {ex}");
        }
    }
    public static void CheckBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        if (BanMod.IsProtected(player)) return;

        string realName = BanMod.GetRealPlayerName(player);
        string friendcode = player?.FriendCode;

        if (Options.CheckFriendCode.GetBool() && friendcode?.Length < 10)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{realName} {Translator.GetString("PlayerCodeInvalid")}");
            return;
        }

        if (Options.CheckFriendCode.GetBool() && friendcode?.Count(c => c == '#') != 1)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{realName} {Translator.GetString("PlayerCodeInvalid")}");
            return;
        }

        if (Options.CheckFriendCode.GetBool() && friendcode?.Any(c => !char.IsLetterOrDigit(c) && c != '#') == true)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{realName} {Translator.GetString("PlayerCodeInvalid")}");
            return;
        }

        const string pattern = @"[\W\d]";
        if (Options.CheckFriendCode.GetBool() && Regex.IsMatch(friendcode[..friendcode.IndexOf("#", StringComparison.Ordinal)], pattern))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{realName} {Translator.GetString("PlayerCodeInvalid")}");
            return;
        }

        if (!Options.CheckBanList.GetBool()) return;

        var banEntry = GetBanEntry(player?.FriendCode, player?.GetHashedPuid());
        if (banEntry != null)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, $"{realName} {Translator.GetString("PlayerinBanList")} ({banEntry.Reason})");
        }
    }
    public static bool RemoveBanPlayerFromBanList(ClientData player)
    {
        if (player == null)
            return false;

        if (!AmongUsClient.Instance.AmHost)
            return false;

        string friendCode = player.FriendCode;
        string hashedPuid = player.GetHashedPuid();

        if (hashedPuid == "e3b0cb855")
            hashedPuid = "";

        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(hashedPuid))
            return false;

        try
        {
            if (!File.Exists(BanListPath))
                return false;

            List<string> lines = File.ReadAllLines(BanListPath).ToList();
            int originalCount = lines.Count;

            lines = lines.Where(line =>
            {
                if (string.IsNullOrWhiteSpace(line))
                    return false;

                string[] parts = line.Split(',');
                if (parts.Length < 2)
                    return true;

                string fc = parts[0];
                string puid = parts[1];

                bool match =
                    (!string.IsNullOrEmpty(friendCode) && fc == friendCode) ||
                    (!string.IsNullOrEmpty(hashedPuid) && puid == hashedPuid);

                return !match;
            }).ToList();

            if (lines.Count == originalCount)
                return false;

            File.WriteAllLines(BanListPath, lines);

            string realName = BanMod.GetRealPlayerName(player);

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(
                    HudManager.Instance.Notifier,
                    $"{realName} removed from BanList"
                );
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BanMod] RemoveBanPlayerFromBanList error: {ex}");
            return false;
        }
    }
    public static BanEntry GetBanEntry(string code, string hashedpuid)
    {
        if (!AmongUsClient.Instance.AmHost)
            return null;

        if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(hashedpuid))
            return null;

   
        if (!string.IsNullOrEmpty(code))
        {
            if (Utils.IsVip(code) || Utils.IsModerator(code))
                return null;
            if (code == "medialteam#6599")
                return null;
        }

        try
        {
            if (!File.Exists(BanListPath))
                return null;

            foreach (string line in File.ReadLines(BanListPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 2)
                    continue;

                string fc = parts[0];
                string puid = parts[1];
                string name = parts.Length >= 3 ? parts[2] : "";
                string reason = parts.Length >= 4 ? parts[3] : "Unknown";

                if ((!string.IsNullOrEmpty(code) && fc == code) ||
                    (!string.IsNullOrEmpty(hashedpuid) && puid == hashedpuid))
                {
                    return new BanEntry
                    {
                        FriendCode = fc,
                        HashedPuid = puid,
                        PlayerName = name,
                        Reason = reason
                    };
                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"[BanMod] GetBanEntry error: {ex}");
        }

        return null;
    }
   
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (BanMod.IsProtected(recentClient))
        {
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetDisabledColors();
            return;
        }
        if (BanManager.GetBanEntry(recentClient.FriendCode, recentClient.GetHashedPuid()) == null)
        {
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
        }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
public static class InnerNetClientKickPlayerPatch
{
    public static bool Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        try
        {
            if (__instance == null)
                return true;

            ClientData targetClient =
                __instance.GetClient(clientId) ??
                __instance.GetRecentClient(clientId);

            if (targetClient == null)
                return true;

            if (AllowedManager.IsModCreator(targetClient.FriendCode))
            {
                BMLogger.Info(
                    $"[BanMod] Tentativo di {(ban ? "ban" : "kick")} " +
                    $"bloccato per BanMod_Dev.");

                return false;
            }
        }
        catch (Exception ex)
        {
            BMLogger.Info(
                $"[BanMod] Errore protezione KickPlayer: {ex}");
        }

        return true;
    }
}

