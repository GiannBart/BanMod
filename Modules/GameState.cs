//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using InnerNet;

namespace BanMod;

public static class GameStates
{
    public static bool AlreadyDied;

    public static bool IsInTask => InGame && MeetingHud.Instance == null;
    public static bool IsMeeting => InGame && MeetingHud.Instance != null;
    public static bool IsVoting =>
        IsMeeting &&
        MeetingHud.Instance != null &&
        (MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted);

    public static bool isShip => ShipStatus.Instance != null;

    public static bool Started =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;

    public static bool InGame =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;

    public static bool isLobby =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined &&
        !isFreePlay;

    public static bool isOnlineGame =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;

    public static bool isLocalGame =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;

    public static bool isFreePlay =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;

    public static bool isPlayer => PlayerControl.LocalPlayer != null;

    public static bool isHost =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.AmHost;

    public static bool isMeetingVoting =>
        IsMeeting &&
        MeetingHud.Instance != null &&
        (MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted);

    public static bool isMeetingProceeding =>
        IsMeeting &&
        MeetingHud.Instance != null &&
        MeetingHud.Instance.state is MeetingHud.VoteStates.Proceeding;

    public static bool isNormalGame =>
        GameOptionsManager.Instance != null &&
        GameOptionsManager.Instance.CurrentGameOptions != null &&
        GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;

    public static bool isHideNSeek =>
        GameOptionsManager.Instance != null &&
        GameOptionsManager.Instance.CurrentGameOptions != null &&
        GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;

    public static bool IsInGameplay => InGame && MeetingHud.Instance == null;
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsCanMove => PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.CanMove;
    public static bool IsDead => PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead;
}