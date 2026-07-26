//credits and licenses in the resources folder
using AmongUs.Data;
using BanMod.Modules.CustomHats;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod.Modules.CustomHats
{
    public static class CustomHatSync
    {
        public const byte RpcId = 240;

        private static readonly Dictionary<byte, string> CustomHatByPlayerId = new Dictionary<byte, string>();
        private static readonly Dictionary<byte, int> CustomHatClientByPlayerId = new Dictionary<byte, int>();
        private static readonly Dictionary<byte, string> CustomHatFriendCodeByPlayerId = new Dictionary<byte, string>();
        private static readonly Dictionary<string, string> LastKnownHatByName = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> LastKnownHatByColor = new Dictionary<int, string>();
        private static readonly Dictionary<byte, byte> ShapeshiftTargetByPlayerId = new Dictionary<byte, byte>();
        private static string lastLocalHatId = "";
        private static float lastBroadcastTime = -999f;
        private static string GetFriendCode(NetworkedPlayerInfo playerInfo)
        {
            try
            {
                if (playerInfo != null && !string.IsNullOrEmpty(playerInfo.FriendCode))
                {
                    return playerInfo.FriendCode;
                }
            }
            catch
            {
            }
            return string.Empty;
        }
        public static void UpdateLocalFromCustomization()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null)
                    return;

                string hatId = GetCurrentCustomizationHatId();

                if (!IsCustomHatId(hatId))
                    hatId = "";

                byte playerId = PlayerControl.LocalPlayer.PlayerId;
                NetworkedPlayerInfo localInfo = PlayerControl.LocalPlayer.Data;
                SetHat(playerId, hatId, GetClientId(localInfo), GetFriendCode(localInfo));

                bool changed = hatId != lastLocalHatId;
                bool periodic = !string.IsNullOrEmpty(hatId) && Time.time - lastBroadcastTime > 5f;

                if (!changed && !periodic)
                    return;

                lastLocalHatId = hatId;
                lastBroadcastTime = Time.time;
                SendRpc(playerId, hatId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.UpdateLocalFromCustomization failed: " + ex);
            }
        }

        public static void RefreshKnownPlayers()
        {
            try
            {
                if (GameData.Instance == null || GameData.Instance.AllPlayers == null)
                    return;

                foreach (NetworkedPlayerInfo info in GameData.Instance.AllPlayers)
                {
                    if (info == null || info.Disconnected)
                        continue;

                    if (TryResolveRealHatId(info, out string hatId))
                        CacheKnownPlayerHat(info, hatId);
                    else
                        CacheKnownPlayerHat(info, string.Empty);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.RefreshKnownPlayers failed: " + ex);
            }
        }

        public static bool TryResolveHatId(NetworkedPlayerInfo playerInfo, out string hatId)
        {
            hatId = "";

            try
            {
                if (playerInfo == null)
                    return false;

                if (ShapeshiftTargetByPlayerId.TryGetValue(playerInfo.PlayerId, out byte targetPlayerId) &&
                    targetPlayerId != playerInfo.PlayerId)
                {
                    NetworkedPlayerInfo targetInfo = FindPlayerInfo(targetPlayerId);
                    if (targetInfo == null)
                        return false;

                    return TryResolveRealHatId(targetInfo, out hatId);
                }

                return TryResolveRealHatId(playerInfo, out hatId);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.TryResolveHatId failed: " + ex);
            }

            return false;
        }

        public static bool TryResolveRealHatId(NetworkedPlayerInfo playerInfo, out string hatId)
        {
            hatId = "";

            if (playerInfo == null)
                return false;

            if (CustomHatByPlayerId.TryGetValue(playerInfo.PlayerId, out string syncedHatId) && IsCustomHatId(syncedHatId))
            {
                int currentClientId = GetClientId(playerInfo);
                string currentFriendCode = GetFriendCode(playerInfo);

                bool clientIdMismatch = CustomHatClientByPlayerId.TryGetValue(playerInfo.PlayerId, out int cachedClientId) &&
                                        currentClientId >= 0 &&
                                        cachedClientId >= 0 &&
                                        cachedClientId != currentClientId;

                bool friendCodeMismatch = false;
                if (CustomHatFriendCodeByPlayerId.TryGetValue(playerInfo.PlayerId, out string cachedFriendCode))
                {
                    friendCodeMismatch = (cachedFriendCode ?? "") != (currentFriendCode ?? "");
                }

                if (clientIdMismatch || friendCodeMismatch)
                {
                    SetHat(playerInfo.PlayerId, "", -1, "");
                    return false;
                }

                hatId = syncedHatId;
                CacheKnownPlayerHat(playerInfo, hatId);
                return true;
            }

            if (PlayerControl.LocalPlayer != null &&
                playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                string localHatId = GetCurrentCustomizationHatId();

                if (IsCustomHatId(localHatId))
                {
                    hatId = localHatId;
                    SetHat(playerInfo.PlayerId, localHatId, GetClientId(playerInfo), GetFriendCode(playerInfo));
                    CacheKnownPlayerHat(playerInfo, hatId);
                    return true;
                }
            }

            return false;
        }

        public static void SetShapeshiftTarget(PlayerControl shapeshifter, PlayerControl target)
        {
            try
            {
                if (shapeshifter == null)
                    return;

                byte shapeshifterId = shapeshifter.PlayerId;

                if (target == null || target.PlayerId == shapeshifterId)
                {
                    if (ShapeshiftTargetByPlayerId.ContainsKey(shapeshifterId))
                        ShapeshiftTargetByPlayerId.Remove(shapeshifterId);
                    return;
                }

                ShapeshiftTargetByPlayerId[shapeshifterId] = target.PlayerId;
            }
            catch
            {
            }
        }

        public static bool IsRenderingShapeshiftTarget(NetworkedPlayerInfo playerInfo)
        {
            try
            {
                if (playerInfo == null)
                    return false;

                return ShapeshiftTargetByPlayerId.TryGetValue(playerInfo.PlayerId, out byte targetPlayerId) &&
                       targetPlayerId != playerInfo.PlayerId;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryResolveDisplayedColorId(NetworkedPlayerInfo playerInfo, out int colorId)
        {
            colorId = 0;

            try
            {
                if (playerInfo == null)
                    return false;

                NetworkedPlayerInfo displayInfo = playerInfo;

                if (ShapeshiftTargetByPlayerId.TryGetValue(playerInfo.PlayerId, out byte targetPlayerId) &&
                    targetPlayerId != playerInfo.PlayerId)
                {
                    NetworkedPlayerInfo targetInfo = FindPlayerInfo(targetPlayerId);
                    if (targetInfo != null)
                        displayInfo = targetInfo;
                }

                if (displayInfo.DefaultOutfit != null)
                {
                    colorId = displayInfo.DefaultOutfit.ColorId;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool TryResolveCachedRealHatId(string playerName, int colorId, bool isYou, out string hatId)
        {
            hatId = string.Empty;

            try
            {
                if (isYou)
                {
                    string localHatId = GetCurrentCustomizationHatId();
                    if (IsCustomHatId(localHatId))
                    {
                        hatId = localHatId;
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(playerName) &&
                    LastKnownHatByName.TryGetValue(playerName, out string byName) &&
                    IsCustomHatId(byName))
                {
                    hatId = byName;
                    return true;
                }

                if (LastKnownHatByColor.TryGetValue(colorId, out string byColor) && IsCustomHatId(byColor))
                {
                    hatId = byColor;
                    return true;
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.TryResolveCachedRealHatId failed: " + ex);
            }

            return false;
        }

        private static void CacheKnownPlayerHat(NetworkedPlayerInfo playerInfo, string hatId)
        {
            try
            {
                if (playerInfo == null)
                    return;

                string name = playerInfo.PlayerName;
                int colorId = -1;

                try
                {
                    if (playerInfo.DefaultOutfit != null)
                        colorId = playerInfo.DefaultOutfit.ColorId;
                }
                catch
                {
                }

                if (!IsCustomHatId(hatId))
                {
                    if (!string.IsNullOrEmpty(name) && LastKnownHatByName.ContainsKey(name))
                        LastKnownHatByName.Remove(name);
                    if (colorId >= 0 && LastKnownHatByColor.ContainsKey(colorId))
                        LastKnownHatByColor.Remove(colorId);
                    return;
                }

                if (!string.IsNullOrEmpty(name))
                    LastKnownHatByName[name] = hatId;

                if (colorId >= 0)
                    LastKnownHatByColor[colorId] = hatId;
            }
            catch
            {
            }
        }

        public static bool TryGetHat(byte playerId, out string hatId)
        {
            if (CustomHatByPlayerId.TryGetValue(playerId, out hatId) && IsCustomHatId(hatId))
                return true;

            hatId = "";
            return false;
        }

        public static void ReceiveRpc(MessageReader reader, PlayerControl sourcePlayer = null)
        {
            try
            {
                if (reader == null)
                    return;

                byte playerId = reader.ReadByte();
                string hatId = reader.ReadString();

                if (!IsCustomHatId(hatId))
                    hatId = "";

                if (sourcePlayer != null && sourcePlayer.PlayerId != playerId)
                    return;

                NetworkedPlayerInfo info = FindPlayerInfo(playerId);
                string friendCode = GetFriendCode(info);
                SetHat(playerId, hatId, GetClientId(sourcePlayer), friendCode);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.ReceiveRpc failed: " + ex);
            }
        }

        private static void SetHat(byte playerId, string hatId, int clientId, string friendCode)
        {
            if (string.IsNullOrEmpty(hatId))
            {
                if (CustomHatByPlayerId.ContainsKey(playerId)) CustomHatByPlayerId.Remove(playerId);
                if (CustomHatClientByPlayerId.ContainsKey(playerId)) CustomHatClientByPlayerId.Remove(playerId);
                if (CustomHatFriendCodeByPlayerId.ContainsKey(playerId)) CustomHatFriendCodeByPlayerId.Remove(playerId);

                return;
            }

            CustomHatByPlayerId[playerId] = hatId;
            CustomHatClientByPlayerId[playerId] = clientId;
            CustomHatFriendCodeByPlayerId[playerId] = friendCode;

            NetworkedPlayerInfo info = FindPlayerInfo(playerId);
            if (info != null)
                CacheKnownPlayerHat(info, hatId);
        }
        private static void SendRpc(byte playerId, string hatId)
        {
            try
            {
                if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null)
                    return;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    RpcId,
                    SendOption.Reliable,
                    -1
                );

                writer.Write(playerId);
                writer.Write(hatId ?? string.Empty);

                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CustomHatSync.SendRpc failed: " + ex);
            }
        }


        private static NetworkedPlayerInfo FindPlayerInfo(byte playerId)
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null)
                    return null;

                for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
                {
                    PlayerControl player = PlayerControl.AllPlayerControls[i];
                    if (player != null && player.PlayerId == playerId)
                        return player.Data;
                }
            }
            catch
            {
            }

            return null;
        }

        private static int GetClientId(PlayerControl player)
        {
            try
            {
                if (player != null && player.Data != null)
                    return player.Data.ClientId;
            }
            catch
            {
            }

            return -1;
        }

        private static int GetClientId(NetworkedPlayerInfo playerInfo)
        {
            try
            {
                if (playerInfo != null)
                    return playerInfo.ClientId;
            }
            catch
            {
            }

            return -1;
        }

        private static string GetCurrentCustomizationHatId()
        {
            try
            {
                if (DataManager.Player != null &&
                    DataManager.Player.Customization != null &&
                    !string.IsNullOrEmpty(DataManager.Player.Customization.Hat))
                {
                    return DataManager.Player.Customization.Hat;
                }
            }
            catch
            {
            }

            return "";
        }

        private static bool IsCustomHatId(string hatId)
        {
            if (string.IsNullOrEmpty(hatId) || hatId == "hat_NoHat" || hatId == "missing")
                return false;

            return CustomHatManager.TryGetViewData(hatId, out _);
        }
        public static void ResetSessionCaches()
        {
            ShapeshiftTargetByPlayerId.Clear();

            LastKnownHatByName.Clear();
            LastKnownHatByColor.Clear();
        }
    }
}
[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
internal static class LobbyResetPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        CustomHatSync.ResetSessionCaches();
    }
}