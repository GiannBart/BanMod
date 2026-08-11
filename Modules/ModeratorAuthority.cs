// credits and licenses in the resources folder
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod
{
    public enum ModeratorAction : byte
    {
        TogglePublicPrivate = 1,
        StartGame = 2,
        InstantStart = 3,
        CallMeeting = 4,
        EndMeeting = 5,
        EndGame = 6,
        Kick = 7,
        Ban = 8,
        DestroyLobby = 9,
        SpawnLobby = 10,
        ChangeBody = 11,
        RandomFreeColor = 12,
        ToggleLobbyObject = 13
    }

    /// <summary>
    /// Authority for host-like moderator actions.
    ///
    /// The client-side moderator check is only for UI/shortcut visibility.
    /// Security is enforced again on the HOST using the sender PlayerControl
    /// received by PlayerControl.HandleRpc.
    /// </summary>
    public static class ModeratorAuthority
    {
        private const string LogTag = "ModeratorAuthority";

        public static bool CanUseLocal
        {
            get
            {
                try
                {
                    if (AmongUsClient.Instance == null ||
                        PlayerControl.LocalPlayer?.Data == null)
                        return false;

                    if (AmongUsClient.Instance.AmHost)
                        return true;

                    PlayerControl local = PlayerControl.LocalPlayer;

                    string friendCode = local.FriendCode;

                    if (string.IsNullOrWhiteSpace(friendCode))
                    {
                        int clientId = local.GetClientId();

                        if (clientId >= 0)
                        {
                            ClientData client =
                                AmongUsClient.Instance.GetClient(clientId);

                            friendCode = client?.FriendCode;
                        }
                    }

                    return AllowedManager.IsModerator(friendCode);
                }
                catch (Exception ex)
                {
                    BMLogger.Warn(
                        $"CanUseLocal error: {ex}",
                        LogTag
                    );

                    return false;
                }
            }
        }

        public static void Request(
            ModeratorAction action,
            byte targetPlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer?.Data == null)
                return;

            // Convenience only. The real permission check is Receive() on the host.
            if (!CanUseLocal)
                return;

            if (AmongUsClient.Instance.AmHost)
            {
                ExecuteHost(PlayerControl.LocalPlayer, action, targetPlayerId);
                return;
            }

            var writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ModeratorAction,
                SendOption.Reliable,
                AmongUsClient.Instance.HostId
            );

            writer.Write((byte)action);
            writer.Write(targetPlayerId);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        /// <summary>
        /// Called by the host's CustomRPC dispatcher.
        /// sender MUST be the PlayerControl (__instance) that emitted the RPC.
        /// </summary>
        public static void Receive(PlayerControl sender, MessageReader reader)
        {
            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                sender?.Data == null ||
                reader == null)
            {
                return;
            }

            int senderClientId = sender.GetClientId();
            if (senderClientId < 0)
                return;


            ClientData senderClient = AmongUsClient.Instance.GetClient(senderClientId);
            if (senderClient == null)
                return;

            string friendCode = sender.Data.FriendCode;
            if (string.IsNullOrWhiteSpace(friendCode))
                friendCode = senderClient.FriendCode;

            // THIS is the authoritative permission check.
            // Nothing sent in the RPC can grant moderator status.
            if (string.IsNullOrWhiteSpace(friendCode) ||
                !AllowedManager.IsModerator(friendCode))
            {
                BMLogger.Warn(
                    $"Moderator RPC rejected: unauthorized sender | " +
                    $"Player={sender.Data.PlayerName} | " +
                    $"ClientId={senderClientId} | " +
                    $"FriendCode={friendCode}",
                    LogTag
                );

                return;
            }

            ModeratorAction action = (ModeratorAction)reader.ReadByte();
            byte targetPlayerId = reader.ReadByte();

            if (!Enum.IsDefined(typeof(ModeratorAction), action))
            {
                BMLogger.Warn(
                    $"Moderator RPC rejected: unknown action {(byte)action} | Player={sender.Data.PlayerName}",
                    LogTag);
                return;
            }

            ExecuteHost(sender, action, targetPlayerId);
        }

        private static PlayerControl GetTarget(byte playerId)
        {
            if (playerId == byte.MaxValue)
                return null;

            return BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null &&
                p.Data != null &&
                !p.Data.Disconnected &&
                p.PlayerId == playerId);
        }

        private static bool TryGetTargetClient(
            byte targetPlayerId,
            out PlayerControl target,
            out ClientData targetClient)
        {
            target = GetTarget(targetPlayerId);
            targetClient = null;

            if (target?.Data == null)
                return false;

            targetClient = AmongUsClient.Instance.GetClient(target.OwnerId);
            return targetClient != null;
        }

        private static bool IsProtectedTarget(ClientData client)
        {
            if (client == null)
                return true;

            if (AllowedManager.IsModCreator(client.FriendCode))
                return true;

            if (BanMod.IsProtected(client))
                return true;

            return false;
        }

        private static void ExecuteHost(
            PlayerControl moderator,
            ModeratorAction action,
            byte targetPlayerId)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            switch (action)
            {
                case ModeratorAction.TogglePublicPrivate:
                    {
                        if (!GameStates.isLobby)
                            return;

                        var manager = DestroyableSingleton<GameStartManager>.Instance;

                        if (manager == null)
                            return;

                        manager.MakePublic();
                        break;
                    }

                case ModeratorAction.StartGame:
                {
                    if (!GameStates.isLobby)
                        return;

                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    manager?.BeginGame();
                    break;
                }

                case ModeratorAction.InstantStart:
                {
                    if (!GameStates.isLobby)
                        return;

                    var manager = UnityEngine.Object.FindObjectOfType<GameStartManager>();
                    manager?.BeginGame();
                    break;
                }

                case ModeratorAction.CallMeeting:
                {
                    if (GameStates.isLobby || moderator?.Data == null || moderator.Data.IsDead)
                        return;

                    // Same behavior already used by the host-side vanilla moderator command.
                    moderator.CmdReportDeadBody(null);
                    break;
                }

                case ModeratorAction.EndMeeting:
                {
                    if (MeetingHud.Instance == null)
                        return;

                    PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
                    break;
                }

                case ModeratorAction.EndGame:
                {
                    if (!GameStates.IsInGameplay || GameManager.Instance == null)
                        return;

                    GameManager.Instance.RpcEndGame(
                        GameOverReason.CrewmatesByTask,
                        false);
                    break;
                }

                case ModeratorAction.Kick:
                {
                    if (!TryGetTargetClient(targetPlayerId, out PlayerControl target, out ClientData client))
                        return;

                    if (target.AmOwner || IsProtectedTarget(client))
                        return;

                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    break;
                }

                case ModeratorAction.Ban:
                {
                    if (!TryGetTargetClient(targetPlayerId, out PlayerControl target, out ClientData client))
                        return;

                    if (target.AmOwner || IsProtectedTarget(client))
                        return;

                    BanManager.AddBanPlayer(client, "Moderator", false);
                    AmongUsClient.Instance.KickPlayer(client.Id, true);
                    break;
                }

                case ModeratorAction.DestroyLobby:
                {
                    if (!GameStates.isLobby)
                        return;

                    Utils.DestroyMap();
                    break;
                }

                case ModeratorAction.SpawnLobby:
                {
                    if (!GameStates.isLobby)
                        return;

                    Utils.SpawnLobby();
                    break;
                }

                case ModeratorAction.ToggleLobbyObject:
                {
                    if (!GameStates.isLobby)
                        return;

                    if (LobbyBehaviour.Instance == null)
                        Utils.SpawnLobby();
                    else
                        Utils.DestroyMap();

                    break;
                }

                case ModeratorAction.ChangeBody:
                {
                    PlayerControl target = GetTarget(targetPlayerId);
                    if (target?.Data == null)
                        return;

                    PlayerBodyTypes nextBody = Utils.GetNextBodyType(target);
                    float scale = Mathf.Clamp(target.transform.localScale.x, 0.25f, 2.0f);

                    // Host applies the authoritative visual state.
                    target.transform.localScale = new Vector3(scale, scale, 1f);
                    Utils.SetPlayerBodyType(target, nextBody);

                    // Then host broadcasts the resulting state.
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.SyncPlayerVisual,
                        SendOption.Reliable,
                        -1
                    );

                    writer.Write(target.PlayerId);
                    writer.Write(scale);
                    writer.Write((byte)nextBody);
                    writer.Write(false);

                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                }

                case ModeratorAction.RandomFreeColor:
                {
                    PlayerControl target = GetTarget(targetPlayerId);
                    if (target?.Data == null)
                        return;

                    List<byte> usedColors = new();
                    foreach (var info in GameData.Instance.AllPlayers)
                    {
                        if (info?.DefaultOutfit != null)
                            usedColors.Add((byte)info.DefaultOutfit.ColorId);
                    }

                    List<byte> freeColors = Enumerable
                        .Range(0, Palette.PlayerColors.Length)
                        .Select(i => (byte)i)
                        .Where(c => !usedColors.Contains(c))
                        .ToList();

                    if (freeColors.Count == 0)
                        freeColors.Add(0);

                    byte color = freeColors[new System.Random().Next(freeColors.Count)];
                    target.RpcSetColor(color);
                    break;
                }
            }
        }
    }
}
