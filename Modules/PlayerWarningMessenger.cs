//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class PlayerWarningMessenger
    {
        private static readonly HashSet<string> SentWarnings = new();

        public static void ResetAll()
        {
            SentWarnings.Clear();
        }

        public static void ClearForPlayer(byte playerId)
        {
            string prefix = playerId + ":";
            foreach (string key in new List<string>(SentWarnings))
            {
                if (key.StartsWith(prefix))
                    SentWarnings.Remove(key);
            }
        }

        public static void ClearForPlayer(byte playerId, string warningKey)
        {
            SentWarnings.Remove($"{playerId}:{warningKey}");
        }

        public static void SendOnce(PlayerControl target, string warningKey, string translationKey)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (target == null || target.Data == null || target.Data.Disconnected) return;

            if (string.IsNullOrWhiteSpace(warningKey)) warningKey = "generic";
            string sentKey = $"{target.PlayerId}:{warningKey}";
            if (SentWarnings.Contains(sentKey)) return;

            string message = GetString(translationKey);
            if (string.IsNullOrWhiteSpace(message)) message = translationKey;
            if (string.IsNullOrWhiteSpace(message)) return;

            SentWarnings.Add(sentKey);

            try
            {
                Utils.SendMessage(message.Trim(), target.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
                BMLogger.LogInfo($"[PlayerWarningMessenger] Avviso inviato a {target.Data.PlayerName} | Key={warningKey}");
            }
            catch (Exception ex)
            {
                BMLogger.LogWarning($"[PlayerWarningMessenger] Errore invio avviso a {target.PlayerId}: {ex}");
            }
        }
    }
}
