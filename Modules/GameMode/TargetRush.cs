using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod;

public static class TargetRush
{
    private const int InitialRoundSeconds = 20;
    private const int RoundTimeDecrease = 2;
    private const int MinimumRoundSeconds = 8;
    private const int MaximumGroupsPerRound = 4;

    private const float NextRoundDelay = 1f;
    private const float InitialStartDelay = 10f;

    private static readonly Dictionary<byte, PlainShipRoom> PlayerTargets = new();
    private static readonly Dictionary<SystemTypes, int> RoomRequirements = new();

    private static readonly Dictionary<byte, string> OriginalNames = new();
    private static readonly Dictionary<byte, string> LastPrivateNames = new();

    private static float RoundEndAt;
    private static float NextRoundAt = -1f;
    private static float InitialStartAt = -1f;

    private static int RoundNumber;

    private static bool Running;
    private static bool Finished;

    public static bool IsEnabled =>
        Options.GameMode != null &&
        Options.GameMode.GetValue(GameModeType.TargetRush);

    public static string WinnerName { get; private set; } = "";

    public static void FixedUpdate()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        if (!AmongUsClient.Instance.IsGameStarted || !IsEnabled)
        {
            if (Running)
                Reset();

            return;
        }

        if (!IsGameplayReady())
        {
            if (!Running)
                InitialStartAt = -1f;

            return;
        }

        if (!Running)
        {
            if (InitialStartAt < 0f)
            {
                InitialStartAt =
                    Time.realtimeSinceStartup + InitialStartDelay;

                try
                {
                    BMLogger.LogInfo(
                        "[TargetRush] Gameplay pronto. TargetRush partirà tra 10 secondi."
                    );
                }
                catch
                {
                }

                return;
            }

            if (Time.realtimeSinceStartup < InitialStartAt)
                return;

            InitialStartAt = -1f;

            StartGame();
            return;
        }

        if (Finished)
            return;

        if (NextRoundAt >= 0f)
        {
            if (Time.realtimeSinceStartup >= NextRoundAt)
            {
                NextRoundAt = -1f;
                StartNewRound();
            }

            return;
        }

        List<PlayerControl> alivePlayers =
            GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            Finish(alivePlayers.FirstOrDefault());
            return;
        }

        if (PlayerTargets.Count == 0 ||
            RoomRequirements.Count == 0)
        {
            StartNewRound();
            return;
        }

        CleanupRuntimePlayerState(alivePlayers);

        if (Time.realtimeSinceStartup >= RoundEndAt)
        {
            ResolveRound(alivePlayers);
            return;
        }

        UpdatePrivateNames(alivePlayers);
    }

    private static bool IsGameplayReady()
    {
        try
        {
            if (ShipStatus.Instance == null)
                return false;

            if (PlayerControl.LocalPlayer == null ||
                PlayerControl.LocalPlayer.Data == null)
                return false;

            if (HudManager.Instance == null)
                return false;

            if (MeetingHud.Instance != null ||
                ExileController.Instance != null)
                return false;

            if (!PlayerControl.LocalPlayer.moveable)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void StartGame()
    {
        WinnerName = "";

        Running = true;
        Finished = false;

        RoundEndAt = 0f;
        NextRoundAt = -1f;
        InitialStartAt = -1f;

        RoundNumber = 0;

        PlayerTargets.Clear();
        RoomRequirements.Clear();
        LastPrivateNames.Clear();

        CacheOriginalNames();

        try
        {
            BMLogger.LogInfo("[TargetRush] Avvio TargetRush");
        }
        catch
        {
        }

        StartNewRound();
    }

    private static void StartNewRound()
    {
        if (!Running || Finished)
            return;

        List<PlayerControl> alivePlayers =
            GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            Finish(alivePlayers.FirstOrDefault());
            return;
        }

        List<PlainShipRoom> validRooms =
            GetValidRooms();

        if (validRooms.Count == 0)
        {
            try
            {
                BMLogger.LogError(
                    "[TargetRush] Nessuna stanza valida trovata."
                );
            }
            catch
            {
            }

            Finish();
            return;
        }

        AssignGroups(
            alivePlayers,
            validRooms
        );

        int seconds =
            Mathf.Max(
                MinimumRoundSeconds,
                InitialRoundSeconds -
                (RoundNumber * RoundTimeDecrease)
            );

        RoundNumber++;

        RoundEndAt =
            Time.realtimeSinceStartup + seconds;

        NextRoundAt = -1f;
        LastPrivateNames.Clear();

        try
        {
            string requirements =
                string.Join(
                    ", ",
                    RoomRequirements.Select(
                        pair =>
                            $"{GetRoomName(pair.Key)}={pair.Value}"
                    )
                );

            BMLogger.LogInfo(
                $"[TargetRush] Round {RoundNumber} | {seconds}s | " +
                requirements
            );
        }
        catch
        {
        }

        UpdatePrivateNames(
            alivePlayers,
            force: true
        );
    }

    private static void AssignGroups(
        List<PlayerControl> alivePlayers,
        List<PlainShipRoom> validRooms)
    {
        PlayerTargets.Clear();
        RoomRequirements.Clear();

        var players =
            new List<PlayerControl>(alivePlayers);

        var rooms =
            new List<PlainShipRoom>(validRooms);

        Shuffle(players);
        Shuffle(rooms);

        int aliveCount =
            players.Count;

        int groupCount =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    aliveCount / 4f
                ),
                1,
                Mathf.Min(
                    MaximumGroupsPerRound,
                    rooms.Count
                )
            );

        int baseSize =
            aliveCount / groupCount;

        int remainder =
            aliveCount % groupCount;

        int playerIndex = 0;

        for (int i = 0; i < groupCount; i++)
        {
            int required =
                baseSize +
                (i < remainder ? 1 : 0);

            PlainShipRoom room =
                rooms[i];

            RoomRequirements[room.RoomId] =
                required;

            for (int n = 0;
                 n < required &&
                 playerIndex < players.Count;
                 n++)
            {
                PlayerControl player =
                    players[playerIndex++];

                if (player == null)
                    continue;

                PlayerTargets[player.PlayerId] =
                    room;
            }
        }
    }

    private static void ResolveRound(
        List<PlayerControl> alivePlayers)
    {
        Dictionary<SystemTypes, int> roomCounts =
            GetRoomCounts(alivePlayers);

        var survivors =
            new List<PlayerControl>();

        var failed =
            new List<PlayerControl>();

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null ||
                player.Data == null)
                continue;

            if (IsPlayerSafeAtTimerEnd(
                    player,
                    roomCounts))
            {
                survivors.Add(player);
            }
            else
            {
                failed.Add(player);
            }
        }

        foreach (PlayerControl player in failed)
            EliminatePlayer(player);

        try
        {
            BMLogger.LogInfo(
                $"[TargetRush] Fine round | " +
                $"safe={survivors.Count} | eliminati={failed.Count}"
            );
        }
        catch
        {
        }

        if (survivors.Count <= 1)
        {
            Finish(
                survivors.FirstOrDefault()
            );

            return;
        }

        QueueNextRound();
    }

    private static bool IsPlayerSafeAtTimerEnd(
        PlayerControl player,
        Dictionary<SystemTypes, int> roomCounts)
    {
        if (player == null ||
            player.Data == null ||
            player.Data.IsDead ||
            player.Data.Disconnected ||
            player.inVent)
        {
            return false;
        }

        if (!PlayerTargets.TryGetValue(
                player.PlayerId,
                out PlainShipRoom target) ||
            target == null)
        {
            return false;
        }

        if (!IsInsideRoom(
                player,
                target))
        {
            return false;
        }

        if (!RoomRequirements.TryGetValue(
                target.RoomId,
                out int required))
        {
            return false;
        }

        int actual =
            roomCounts.TryGetValue(
                target.RoomId,
                out int count)
                ? count
                : 0;

        return actual == required;
    }

    private static Dictionary<SystemTypes, int> GetRoomCounts(
        List<PlayerControl> alivePlayers)
    {
        var counts =
            new Dictionary<SystemTypes, int>();

        foreach (SystemTypes roomId in
                 RoomRequirements.Keys)
        {
            counts[roomId] = 0;
        }

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected ||
                player.inVent)
            {
                continue;
            }

            foreach (SystemTypes roomId in
                     RoomRequirements.Keys.ToArray())
            {
                PlainShipRoom room =
                    FindRoom(roomId);

                if (room != null &&
                    IsInsideRoom(
                        player,
                        room))
                {
                    counts[roomId]++;
                    break;
                }
            }
        }

        return counts;
    }

    private static PlainShipRoom FindRoom(
        SystemTypes roomId)
    {
        if (ShipStatus.Instance == null ||
            ShipStatus.Instance.AllRooms == null)
            return null;

        foreach (PlainShipRoom room in
                 ShipStatus.Instance.AllRooms)
        {
            if (room != null &&
                room.RoomId == roomId &&
                room.roomArea != null)
            {
                return room;
            }
        }

        return null;
    }

    private static void Shuffle<T>(
        List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static void QueueNextRound()
    {
        if (Finished)
            return;

        if (NextRoundAt >= 0f)
            return;

        NextRoundAt =
            Time.realtimeSinceStartup + NextRoundDelay;
    }

    private static List<PlainShipRoom> GetValidRooms()
    {
        var rooms =
            new List<PlainShipRoom>();

        if (ShipStatus.Instance == null ||
            ShipStatus.Instance.AllRooms == null)
            return rooms;

        var usedIds =
            new HashSet<SystemTypes>();

        foreach (PlainShipRoom room in
                 ShipStatus.Instance.AllRooms)
        {
            if (room == null ||
                room.roomArea == null)
                continue;

            string id =
                room.RoomId.ToString();

            if (id.Equals(
                    "Hallway",
                    StringComparison.OrdinalIgnoreCase) ||
                id.Equals(
                    "Outside",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!usedIds.Add(room.RoomId))
                continue;

            rooms.Add(room);
        }

        return rooms;
    }

    private static bool IsInsideRoom(
        PlayerControl player,
        PlainShipRoom room)
    {
        if (player == null ||
            player.Collider == null ||
            room == null ||
            room.roomArea == null)
            return false;

        try
        {
            return player.Collider.IsTouching(
                room.roomArea
            );
        }
        catch
        {
            return false;
        }
    }

    private static readonly string[] DirectionArrows =
    {
        "→",
        "↗",
        "↑",
        "↖",
        "←",
        "↙",
        "↓",
        "↘"
    };

    private static string GetArrow(
        PlayerControl player,
        PlainShipRoom target)
    {
        if (player == null ||
            target == null ||
            target.roomArea == null)
            return "";

        Vector2 direction =
            (Vector2)target.roomArea.bounds.center -
            (Vector2)player.transform.position;

        if (direction.sqrMagnitude <
            1.5f * 1.5f)
        {
            return "•";
        }

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        int index =
            Mathf.RoundToInt(angle / 45f) % 8;

        return DirectionArrows[index];
    }

    private static void CacheOriginalNames()
    {
        OriginalNames.Clear();

        foreach (PlayerControl player in
                 BanMod.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null)
                continue;

            OriginalNames[player.PlayerId] =
                GetAuthoritativeOriginalName(player);
        }
    }

    private static string GetOriginalName(
        PlayerControl player)
    {
        if (player == null)
            return "";

        if (OriginalNames.TryGetValue(
                player.PlayerId,
                out string cached))
        {
            return cached;
        }

        string name =
            GetAuthoritativeOriginalName(player);

        OriginalNames[player.PlayerId] = name;

        return name;
    }

    private static string GetAuthoritativeOriginalName(
        PlayerControl player)
    {
        if (player == null)
            return "";

        try
        {
            NetworkedPlayerInfo info =
                GameData.Instance?.GetPlayerById(
                    player.PlayerId
                );

            string defaultName =
                info?.DefaultOutfit?.PlayerName;

            if (!string.IsNullOrWhiteSpace(defaultName))
                return defaultName;
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(
                    player.Data?.PlayerName))
            {
                return player.Data.PlayerName;
            }
        }
        catch
        {
        }

        return $"Player {player.PlayerId}";
    }

    private static void UpdatePrivateNames(
        List<PlayerControl> alivePlayers,
        bool force = false)
    {
        int seconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    RoundEndAt -
                    Time.realtimeSinceStartup
                )
            );

        Dictionary<SystemTypes, int> roomCounts =
            GetRoomCounts(alivePlayers);

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null ||
                player.Data == null)
                continue;

            if (!PlayerTargets.TryGetValue(
                    player.PlayerId,
                    out PlainShipRoom target) ||
                target == null)
            {
                continue;
            }

            if (!RoomRequirements.TryGetValue(
                    target.RoomId,
                    out int required))
            {
                continue;
            }

            int actual =
                roomCounts.TryGetValue(
                    target.RoomId,
                    out int count)
                    ? count
                    : 0;

            string originalName =
                GetOriginalName(player);

            string roomName =
                GetRoomName(target.RoomId);

            string arrow =
                GetArrow(player, target);

            string countColor =
                actual == required
                    ? "#55FF55"
                    : actual > required
                        ? "#FF5555"
                        : "#FFD84A";

            string information =
                $"<size=70%><color=#66CCFF>ROUND {RoundNumber}</color></size> " +
                $"<size=75%><color=#FFD84A>{roomName}</color></size> " +
                $"<size=120%><color=#FFD84A>{arrow}</color></size>\n" +
                $"<size=90%><color={countColor}>{actual}/{required}</color></size> " +
                $"<size=100%><color=#FFFFFF>TIME: {seconds}s</color></size>";

            string privateName =
                $"<size=100%>{originalName}</size>\n" +
                information;

            SendPrivateSetNameToSelf(
                player,
                privateName,
                force
            );
        }
    }

    private static void SendPrivateSetNameToSelf(
        PlayerControl player,
        string name,
        bool force = false)
    {
        if (player == null ||
            player.Data == null ||
            string.IsNullOrEmpty(name))
            return;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        if (!force &&
            LastPrivateNames.TryGetValue(
                player.PlayerId,
                out string previous) &&
            previous == name)
        {
            return;
        }

        int clientId =
            GetClientId(player);

        if (clientId < 0)
            return;

        try
        {
            MessageWriter writer =
                AmongUsClient.Instance.StartRpcImmediately(
                    player.NetId,
                    (byte)RpcCalls.SetName,
                    SendOption.Reliable,
                    clientId
                );

            writer.Write(player.Data.NetId);
            writer.Write(name);
            writer.Write(false);

            AmongUsClient.Instance
                .FinishRpcImmediately(writer);

            LastPrivateNames[player.PlayerId] =
                name;
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[TargetRush] SetName privato fallito per " +
                    $"{player.PlayerId}: {ex.Message}"
                );
            }
            catch
            {
            }
        }
    }

    private static int GetClientId(
        PlayerControl player)
    {
        if (player == null ||
            AmongUsClient.Instance == null ||
            AmongUsClient.Instance.allClients == null)
            return -1;

        try
        {
            foreach (ClientData client in
                     AmongUsClient.Instance.allClients)
            {
                if (client == null ||
                    client.Character == null)
                    continue;

                if (client.Character.PlayerId ==
                    player.PlayerId)
                {
                    return client.Id;
                }
            }

            if (player.AmOwner)
                return AmongUsClient.Instance.ClientId;
        }
        catch
        {
        }

        return -1;
    }

    private static void EliminatePlayer(
        PlayerControl player)
    {
        if (player == null ||
            player.Data == null ||
            player.Data.IsDead ||
            player.Data.Disconnected)
            return;

        try
        {
            player.RpcMurderPlayer(
                player,
                true
            );
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[TargetRush] Impossibile eliminare " +
                    $"{GetOriginalName(player)}: {ex.Message}"
                );
            }
            catch
            {
            }
        }
    }

    private static List<PlayerControl> GetAlivePlayers()
    {
        try
        {
            return BanMod.AllAlivePlayerControls
                .Where(player =>
                    player != null &&
                    player.Data != null &&
                    !player.Data.IsDead &&
                    !player.Data.Disconnected)
                .ToList();
        }
        catch
        {
            return new List<PlayerControl>();
        }
    }

    private static void CleanupRuntimePlayerState(
        List<PlayerControl> alivePlayers)
    {
        var aliveIds =
            new HashSet<byte>(
                alivePlayers.Select(
                    pc => pc.PlayerId
                )
            );

        foreach (byte id in
                 PlayerTargets.Keys.ToArray())
        {
            if (!aliveIds.Contains(id))
                PlayerTargets.Remove(id);
        }
    }

    private static string GetRoomName(
        SystemTypes room)
    {
        string raw =
            room.ToString();

        return raw switch
        {
            "Nav" => "Navigation",
            "LifeSupp" => "O2",
            "Comms" => "Communications",
            "MedBay" => "MedBay",
            "LowerEngine" => "Lower Engine",
            "UpperEngine" => "Upper Engine",
            "LockerRoom" => "Locker Room",
            "BoilerRoom" => "Boiler Room",
            "MainHall" => "Main Hall",
            "GapRoom" => "Gap Room",
            "VaultRoom" => "Vault",
            "ViewingDeck" => "Viewing Deck",
            "CargoBay" => "Cargo Bay",
            "MeetingRoom" => "Meeting Room",
            "HallOfPortraits" => "Portrait Hall",
            "SleepingQuarters" => "Sleeping Quarters",
            "FishingDock" => "Fishing Dock",
            "MiningPit" => "Mining Pit",
            "RecRoom" => "Rec Room",
            _ => SplitCamelCase(raw)
        };
    }

    private static string SplitCamelCase(
        string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result =
            new System.Text.StringBuilder();

        result.Append(text[0]);

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) &&
                !char.IsUpper(text[i - 1]))
            {
                result.Append(' ');
            }

            result.Append(text[i]);
        }

        return result.ToString();
    }

    private static void Finish(
        PlayerControl winner = null)
    {
        if (Finished)
            return;

        Finished = true;
        NextRoundAt = -1f;

        if (winner == null)
        {
            List<PlayerControl> alive =
                GetAlivePlayers();

            if (alive.Count == 1)
                winner = alive[0];
        }

        WinnerName =
            winner != null
                ? GetOriginalName(winner)
                : "";

        try
        {
            BMLogger.LogInfo(
                string.IsNullOrEmpty(WinnerName)
                    ? "[TargetRush] Nessun vincitore identificato."
                    : $"[TargetRush] Vincitore: {WinnerName}"
            );
        }
        catch
        {
        }

        RestorePrivateNames();

        try
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RpcEndGame(
                    GameOverReason.CrewmatesByTask,
                    false
                );
            }
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[TargetRush] Errore RpcEndGame: {ex.Message}"
                );
            }
            catch
            {
            }
        }
    }

    public static void Reset()
    {
        if (Running)
            RestorePrivateNames();

        Running = false;
        Finished = false;

        RoundEndAt = 0f;
        NextRoundAt = -1f;
        InitialStartAt = -1f;

        RoundNumber = 0;

        PlayerTargets.Clear();
        RoomRequirements.Clear();

        OriginalNames.Clear();
        LastPrivateNames.Clear();

    }

    private static void RestorePrivateNames()
    {
        try
        {
            foreach (PlayerControl player in
                     BanMod.AllPlayerControls)
            {
                if (player == null ||
                    player.Data == null)
                    continue;

                if (!OriginalNames.TryGetValue(
                        player.PlayerId,
                        out string originalName))
                {
                    continue;
                }

                SendPrivateSetNameToSelf(
                    player,
                    originalName,
                    force: true
                );
            }
        }
        catch
        {
        }

        LastPrivateNames.Clear();
    }
}

[HarmonyPatch(
    typeof(PlayerControl),
    nameof(PlayerControl.FixedUpdate)
)]
internal static class TargetRushFixedUpdatePatch
{
    private static void Postfix(
        PlayerControl __instance)
    {
        if (__instance == null ||
            PlayerControl.LocalPlayer == null)
            return;

        if (__instance != PlayerControl.LocalPlayer)
            return;

        TargetRush.FixedUpdate();
    }
}
