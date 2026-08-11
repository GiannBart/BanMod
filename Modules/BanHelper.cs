//credits and licenses in the resources folder
using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BanMod;

public static class SilentPermanentFriendCodeBan
{
    private static string FolderPath => Application.persistentDataPath;

    private const string HiddenFileName = ".Playerb.log";
    private const string VisibleFileName = "Playerb.log";

    private static string HiddenListPath => Path.Combine(FolderPath, HiddenFileName);
    private static string VisibleListPath => Path.Combine(FolderPath, VisibleFileName);

    private const string EncryptionPassword = "BANMOD_SILENT_PERM_FC_2026";

    private static readonly HashSet<string> CachedFriendCodes = new();
    private static readonly HashSet<string> DeferredUntilNextLobbyCodes = new();
    private static readonly HashSet<string> SuppressNextBanManagerCodes = new();

    private static bool loaded;
    private static bool initialized;
    private static bool hasSeenGameAfterDeferredBan;

    private static readonly Dictionary<string, float> RecentlySilentBannedNames = new();
    private const float SilentNotificationWindowSeconds = 6f;

    public static void Initialize()
    {
        if (initialized && loaded)
            return;

        try
        {
            Directory.CreateDirectory(FolderPath);

            if (!File.Exists(HiddenListPath) && !File.Exists(VisibleListPath))
                File.WriteAllText(HiddenListPath, string.Empty, Encoding.UTF8);

            RestoreHiddenFileName();
            EnsureHiddenAttribute();

            Load();

            initialized = true;
        }
        catch
        {
            initialized = true;
            loaded = true;
        }
    }

    private static void MakeVisibleFileName()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);

            if (File.Exists(VisibleListPath))
                return;

            if (!File.Exists(HiddenListPath))
            {
                File.WriteAllText(VisibleListPath, string.Empty, Encoding.UTF8);
                return;
            }

            EnsureWritable(HiddenListPath);
            File.Move(HiddenListPath, VisibleListPath);
        }
        catch
        {
        }
    }

    private static void RestoreHiddenFileName()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);

            if (!File.Exists(VisibleListPath))
            {
                EnsureHiddenAttribute();
                return;
            }

            EnsureWritable(VisibleListPath);

            if (File.Exists(HiddenListPath))
            {
                try
                {
                    EnsureWritable(HiddenListPath);
                    File.Delete(HiddenListPath);
                }
                catch
                {
                }
            }

            File.Move(VisibleListPath, HiddenListPath);
            EnsureHiddenAttribute();
        }
        catch
        {
        }
    }

    private static void EnsureHiddenAttribute()
    {
        try
        {
            if (!File.Exists(HiddenListPath))
                return;

            FileAttributes attributes = File.GetAttributes(HiddenListPath);

            if (!attributes.HasFlag(FileAttributes.Hidden))
                File.SetAttributes(HiddenListPath, attributes | FileAttributes.Hidden);
        }
        catch
        {
        }
    }

    private static void EnsureWritable(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            FileAttributes attributes = File.GetAttributes(path);

            if (attributes.HasFlag(FileAttributes.ReadOnly))
                attributes &= ~FileAttributes.ReadOnly;

            if (attributes.HasFlag(FileAttributes.Hidden))
                attributes &= ~FileAttributes.Hidden;

            File.SetAttributes(path, attributes);
        }
        catch
        {
        }
    }

    private static byte[] GetEncryptionKey()
    {
        using SHA256 sha256 = SHA256.Create();

        string rawKey = EncryptionPassword + "|" + SystemInfo.deviceUniqueIdentifier;

        return sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
    }

    private static string EncryptText(string plainText)
    {
        try
        {
            if (plainText == null)
                plainText = string.Empty;

            byte[] key = GetEncryptionKey();

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using MemoryStream output = new MemoryStream();

            output.Write(aes.IV, 0, aes.IV.Length);

            using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter writer = new StreamWriter(cryptoStream, Encoding.UTF8))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(output.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DecryptText(string encryptedText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
                return string.Empty;

            byte[] fullCipher = Convert.FromBase64String(encryptedText);
            byte[] key = GetEncryptionKey();

            using Aes aes = Aes.Create();
            aes.Key = key;

            int ivLength = aes.BlockSize / 8;

            if (fullCipher.Length <= ivLength)
                return string.Empty;

            byte[] iv = new byte[ivLength];
            byte[] cipher = new byte[fullCipher.Length - ivLength];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

            aes.IV = iv;

            using MemoryStream input = new MemoryStream(cipher);
            using CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new StreamReader(cryptoStream, Encoding.UTF8);

            return reader.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void Load()
    {
        try
        {
            CachedFriendCodes.Clear();

            Directory.CreateDirectory(FolderPath);

            MakeVisibleFileName();

            if (!File.Exists(VisibleListPath))
                File.WriteAllText(VisibleListPath, string.Empty, Encoding.UTF8);

            EnsureWritable(VisibleListPath);

            string encryptedText = File.ReadAllText(VisibleListPath, Encoding.UTF8);
            string plainText = DecryptText(encryptedText);

            foreach (string line in plainText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                string code = NormalizeFriendCode(line);

                if (!string.IsNullOrWhiteSpace(code))
                    CachedFriendCodes.Add(code);
            }

            loaded = true;

            RestoreHiddenFileName();
        }
        catch
        {
            loaded = true;

            try
            {
                RestoreHiddenFileName();
            }
            catch
            {
            }
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);

            MakeVisibleFileName();

            if (!File.Exists(VisibleListPath))
                File.WriteAllText(VisibleListPath, string.Empty, Encoding.UTF8);

            EnsureWritable(VisibleListPath);

            string plainText = string.Join(
                Environment.NewLine,
                CachedFriendCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
            );

            if (!string.IsNullOrWhiteSpace(plainText))
                plainText += Environment.NewLine;

            string encryptedText = EncryptText(plainText);

            File.WriteAllText(VisibleListPath, encryptedText, Encoding.UTF8);

            RestoreHiddenFileName();
        }
        catch
        {
            try
            {
                RestoreHiddenFileName();
            }
            catch
            {
            }
        }
    }

    private static string NormalizeFriendCode(string friendCode)
    {
        if (string.IsNullOrWhiteSpace(friendCode))
            return string.Empty;

        return friendCode.Trim().ToLowerInvariant();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Trim().ToLowerInvariant();
    }

    public static void Add(string friendCode)
    {
        try
        {
            Initialize();

            string code = NormalizeFriendCode(friendCode);

            if (string.IsNullOrWhiteSpace(code))
                return;

            CachedFriendCodes.Add(code);
            Save();
        }
        catch
        {
        }
    }

    public static void AddDeferred(string friendCode)
    {
        try
        {
            Initialize();

            string code = NormalizeFriendCode(friendCode);

            if (string.IsNullOrWhiteSpace(code))
                return;

            CachedFriendCodes.Add(code);

            Save();

            DeferredUntilNextLobbyCodes.Add(code);
            SuppressNextBanManagerCodes.Add(code);

            hasSeenGameAfterDeferredBan = false;
        }
        catch
        {
        }
    }

    public static void Remove(string friendCode)
    {
        try
        {
            Initialize();

            string code = NormalizeFriendCode(friendCode);

            if (string.IsNullOrWhiteSpace(code))
                return;

            DeferredUntilNextLobbyCodes.Remove(code);
            SuppressNextBanManagerCodes.Remove(code);

            if (CachedFriendCodes.Remove(code))
                Save();
        }
        catch
        {
        }
    }

    public static bool Contains(string friendCode)
    {
        try
        {
            Initialize();

            string code = NormalizeFriendCode(friendCode);

            if (string.IsNullOrWhiteSpace(code))
                return false;

            return CachedFriendCodes.Contains(code);
        }
        catch
        {
            return false;
        }
    }

    public static bool ShouldSuppressBanManagerWrite(ClientData player)
    {
        try
        {
            if (player == null)
                return false;

            Initialize();

            string code = NormalizeFriendCode(player.FriendCode);

            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (CachedFriendCodes.Contains(code))
                return true;

            if (SuppressNextBanManagerCodes.Contains(code))
            {
                SuppressNextBanManagerCodes.Remove(code);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static void MarkGameSeenForDeferredBan()
    {
        try
        {
            if (DeferredUntilNextLobbyCodes.Count > 0)
                hasSeenGameAfterDeferredBan = true;
        }
        catch
        {
        }
    }

    public static void TryEnableDeferredBansAfterGame()
    {
        try
        {
            if (!hasSeenGameAfterDeferredBan)
                return;

            if (DeferredUntilNextLobbyCodes.Count > 0)
                DeferredUntilNextLobbyCodes.Clear();

            hasSeenGameAfterDeferredBan = false;
        }
        catch
        {
        }
    }

    private static void MarkRecentlySilentBanned(ClientData client)
    {
        try
        {
            if (client == null)
                return;

            string name = NormalizeName(client.PlayerName);

            if (string.IsNullOrWhiteSpace(name))
                return;

            RecentlySilentBannedNames[name] = Time.realtimeSinceStartup;
        }
        catch
        {
        }
    }

    public static bool ShouldSuppressDisconnectNotification(string item)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(item))
                return false;

            float now = Time.realtimeSinceStartup;

            foreach (string key in RecentlySilentBannedNames.Keys.ToList())
            {
                if (now - RecentlySilentBannedNames[key] > SilentNotificationWindowSeconds)
                    RecentlySilentBannedNames.Remove(key);
            }

            string text = NormalizeName(item);

            foreach (string name in RecentlySilentBannedNames.Keys)
            {
                if (!string.IsNullOrWhiteSpace(name) && text.Contains(name))
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public static void CheckClient(ClientData client)
    {
        try
        {
            if (client == null)
                return;

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            Initialize();

            string friendCode = NormalizeFriendCode(client.FriendCode);

            if (string.IsNullOrWhiteSpace(friendCode))
                return;

            if (!CachedFriendCodes.Contains(friendCode))
                return;

            if (DeferredUntilNextLobbyCodes.Contains(friendCode))
                return;

            MarkRecentlySilentBanned(client);

            AmongUsClient.Instance.KickPlayer(client.Id, true);
        }
        catch
        {
        }
    }

    public static void CheckAllClients()
    {
        try
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            if (AmongUsClient.Instance.allClients == null)
                return;

            Initialize();

            foreach (ClientData client in AmongUsClient.Instance.allClients.ToArray())
            {
                CheckClient(client);
            }
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class SilentPermanentFriendCodeBan_OnGameJoinedPatch
{
    public static void Postfix()
    {
        if (BanMod.IsBanModDisabled) return;

        try
        {
            SilentPermanentFriendCodeBan.Initialize();
            SilentPermanentFriendCodeBan.CheckAllClients();
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class SilentPermanentFriendCodeBan_UpdatePatch
{
    private static float nextCheckTime;

    public static void Postfix()
    {
        if (BanMod.IsBanModDisabled) return;

        try
        {
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + 1.5f;

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            SilentPermanentFriendCodeBan.Initialize();

            if (!GameStates.isLobby)
            {
                SilentPermanentFriendCodeBan.MarkGameSeenForDeferredBan();
                return;
            }

            SilentPermanentFriendCodeBan.TryEnableDeferredBansAfterGame();
            SilentPermanentFriendCodeBan.CheckAllClients();
        }
        catch
        {
        }
    }
}

[HarmonyPatch(typeof(NotificationPopper), nameof(NotificationPopper.AddDisconnectMessage))]
public static class SilentPermanentFriendCodeBan_NotificationPopperPatch
{
    public static bool Prefix(string item)
    {

        try
        {
            if (SilentPermanentFriendCodeBan.ShouldSuppressDisconnectNotification(item))
                return false;
        }
        catch
        {
        }

        return true;
    }
}