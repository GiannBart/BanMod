using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod;

public static class RoomRush
{
    public const int GameModeIndex = 7;
    private const int MinRoundSeconds = 6;
    private const int MaxRoundSeconds = 30;
    private const float NextRoundDelay = 1f;

    private const float InitialStartDelay = 10f;

    private static readonly HashSet<byte> ReachedRoom = new();
    private static readonly Dictionary<byte, string> OriginalNames = new();
    private static readonly Dictionary<byte, string> LastPrivateNames = new();

    private static PlainShipRoom TargetRoom;
    private static PlainShipRoom PreviousTargetRoom;

    private static float RoundEndAt;
    private static float NextRoundAt = -1f;
    private static float InitialStartAt = -1f;

    private static bool Running;
    private static bool Finished;

    public static bool IsEnabled =>
        Options.GameMode != null &&
        Options.GameMode.GetValue(GameModeType.RoomRush);

    public static string WinnerName { get; private set; } = "";
    public static void FixedUpdate()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
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
                        "[RoomRush] Gameplay pronto. RoomRush partirà tra 10 secondi."
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

        if (TargetRoom == null)
        {
            StartNewRound();
            return;
        }

        List<PlayerControl> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            Finish(alivePlayers.FirstOrDefault());
            return;
        }

        ReachedRoom.RemoveWhere(
            id => !alivePlayers.Any(pc => pc.PlayerId == id)
        );


        foreach (PlayerControl player in alivePlayers)
        {
            if (ReachedRoom.Contains(player.PlayerId))
                continue;

            if (player.inVent)
                continue;

            if (!IsInsideTargetRoom(player))
                continue;

            ReachedRoom.Add(player.PlayerId);

            try
            {
                BMLogger.LogInfo(
                    $"[RoomRush] {GetOriginalName(player)} ha raggiunto " +
                    $"{GetRoomName(TargetRoom.RoomId)}"
                );
            }
            catch
            {
            }
        }

        int reachedAlive = alivePlayers.Count(
            pc => ReachedRoom.Contains(pc.PlayerId)
        );


        if (reachedAlive == alivePlayers.Count)
        {
            QueueNextRound();
            return;
        }


        if (reachedAlive == alivePlayers.Count - 1)
        {
            PlayerControl lastPlayer = alivePlayers.FirstOrDefault(
                pc => !ReachedRoom.Contains(pc.PlayerId)
            );

            if (lastPlayer != null)
            {
                try
                {
                    BMLogger.LogInfo(
                        $"[RoomRush] Ultimo fuori stanza: " +
                        $"{GetOriginalName(lastPlayer)}"
                    );
                }
                catch
                {
                }

                EliminatePlayer(lastPlayer);

                if (alivePlayers.Count - 1 <= 1)
                {
                    PlayerControl winner = alivePlayers.FirstOrDefault(
                        pc => pc.PlayerId != lastPlayer.PlayerId
                    );

                    Finish(winner);
                }
                else
                {
                    QueueNextRound();
                }

                return;
            }
        }


        if (Time.realtimeSinceStartup >= RoundEndAt)
        {
            PlayerControl[] outsidePlayers = alivePlayers
                .Where(pc => !ReachedRoom.Contains(pc.PlayerId))
                .ToArray();

            foreach (PlayerControl player in outsidePlayers)
                EliminatePlayer(player);

            int survivors = alivePlayers.Count - outsidePlayers.Length;

            if (survivors <= 1)
            {
                PlayerControl winner = alivePlayers.FirstOrDefault(
                    pc => ReachedRoom.Contains(pc.PlayerId)
                );

                Finish(winner);
            }
            else
            {
                QueueNextRound();
            }

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

        TargetRoom = null;
        PreviousTargetRoom = null;

        RoundEndAt = 0f;
        NextRoundAt = -1f;
        InitialStartAt = -1f;

        ReachedRoom.Clear();
        LastPrivateNames.Clear();

        CacheOriginalNames();

        try
        {
            BMLogger.LogInfo("[RoomRush] Avvio RoomRush");
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

        List<PlayerControl> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            Finish(alivePlayers.FirstOrDefault());
            return;
        }

        List<PlainShipRoom> validRooms = GetValidRooms();

        if (validRooms.Count == 0)
        {
            try
            {
                BMLogger.LogError("[RoomRush] Nessuna stanza valida trovata.");
            }
            catch
            {
            }

            Finish();
            return;
        }

        PlainShipRoom oldTarget = TargetRoom;

        List<PlainShipRoom> choices = validRooms;

        if (oldTarget != null && validRooms.Count > 1)
        {
            choices = validRooms
                .Where(room => room.RoomId != oldTarget.RoomId)
                .ToList();
        }

        PreviousTargetRoom = oldTarget;
        TargetRoom = choices[UnityEngine.Random.Range(0, choices.Count)];

        ReachedRoom.Clear();

        int seconds = CalculateRoundTime(
            PreviousTargetRoom,
            TargetRoom,
            alivePlayers
        );

        RoundEndAt = Time.realtimeSinceStartup + seconds;
        NextRoundAt = -1f;

        LastPrivateNames.Clear();

        try
        {
            BMLogger.LogInfo(
                $"[RoomRush] Nuovo round -> " +
                $"{GetRoomName(TargetRoom.RoomId)} | {seconds}s"
            );
        }
        catch
        {
        }

        UpdatePrivateNames(alivePlayers, force: true);
    }

    private static void QueueNextRound()
    {
        if (Finished)
            return;

        if (NextRoundAt >= 0f)
            return;

        NextRoundAt = Time.realtimeSinceStartup + NextRoundDelay;
    }

    private static int CalculateRoundTime(
        PlainShipRoom previous,
        PlainShipRoom target,
        List<PlayerControl> alivePlayers)
    {
        if (target == null || target.roomArea == null)
            return 15;

        Vector2 targetPosition = target.roomArea.bounds.center;
        float distance;

        if (previous != null && previous.roomArea != null)
        {
            Vector2 previousPosition = previous.roomArea.bounds.center;

            distance = Vector2.Distance(
                previousPosition,
                targetPosition
            );
        }
        else
        {
            distance = 0f;

            foreach (PlayerControl player in alivePlayers)
            {
                if (player == null)
                    continue;

                float d = Vector2.Distance(
                    player.transform.position,
                    targetPosition
                );

                if (d > distance)
                    distance = d;
            }
        }

        float calculated =
            4.5f + (distance * 0.85f);

        return Mathf.Clamp(
            Mathf.CeilToInt(calculated),
            MinRoundSeconds,
            MaxRoundSeconds
        );
    }


    private static List<PlainShipRoom> GetValidRooms()
    {
        var rooms = new List<PlainShipRoom>();

        if (ShipStatus.Instance == null ||
            ShipStatus.Instance.AllRooms == null)
            return rooms;

        var usedRoomIds = new HashSet<SystemTypes>();

        foreach (PlainShipRoom room in ShipStatus.Instance.AllRooms)
        {
            if (room == null || room.roomArea == null)
                continue;

            string id = room.RoomId.ToString();

            if (id.Equals("Hallway", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("Outside", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!usedRoomIds.Add(room.RoomId))
                continue;

            rooms.Add(room);
        }

        return rooms;
    }

    private static bool IsInsideTargetRoom(PlayerControl player)
    {
        if (player == null ||
            player.Collider == null ||
            TargetRoom == null ||
            TargetRoom.roomArea == null)
            return false;

        try
        {
            return player.Collider.IsTouching(TargetRoom.roomArea);
        }
        catch
        {
            return false;
        }
    }


    private static readonly string[] DirectionArrows =
    {
        "→", // 0°
        "↗", // 45°
        "↑", // 90°
        "↖", // 135°
        "←", // 180°
        "↙", // 225°
        "↓", // 270°
        "↘"  // 315°
    };

    private static string GetArrow(PlayerControl player)
    {
        if (player == null ||
            TargetRoom == null ||
            TargetRoom.roomArea == null)
            return "";

        Vector2 playerPosition = player.transform.position;
        Vector2 targetPosition = TargetRoom.roomArea.bounds.center;

        Vector2 direction = targetPosition - playerPosition;

        if (direction.sqrMagnitude < 1.5f * 1.5f)
            return "•";

        float angle = Mathf.Atan2(
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

        foreach (PlayerControl player in BanMod.AllPlayerControls)
        {
            if (player == null || player.Data == null)
                continue;

            string realName = GetAuthoritativeOriginalName(player);
            OriginalNames[player.PlayerId] = realName;
        }
    }

    private static string GetOriginalName(PlayerControl player)
    {
        if (player == null)
            return "";

        if (OriginalNames.TryGetValue(
                player.PlayerId,
                out string cachedName))
        {
            return cachedName;
        }

        string originalName =
            GetAuthoritativeOriginalName(player);

        OriginalNames[player.PlayerId] = originalName;

        return originalName;
    }

    private static string GetAuthoritativeOriginalName(
        PlayerControl player)
    {
        if (player == null)
            return "";

        try
        {
            NetworkedPlayerInfo info =
                GameData.Instance?.GetPlayerById(player.PlayerId);

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
            if (!string.IsNullOrWhiteSpace(player.Data?.PlayerName))
                return player.Data.PlayerName;
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
        if (TargetRoom == null)
            return;

        int seconds = Mathf.Max(
            0,
            Mathf.CeilToInt(
                RoundEndAt - Time.realtimeSinceStartup
            )
        );

        string roomName =
            GetRoomName(TargetRoom.RoomId);

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null || player.Data == null)
                continue;

            bool reached =
                ReachedRoom.Contains(player.PlayerId);

            string originalName =
                GetOriginalName(player);

            string information;

            if (reached)
            {
                information =
                    $"<size=75%><color=#55FF55>✓ {roomName}</color></size>\n" +
                    $"<size=100%><color=#FFFFFF>TIME: {seconds}s</color></size>";
            }
            else
            {
                string arrow = GetArrow(player);

                information =
                    $"<size=75%><color=#FFD84A>{roomName}</color></size>  " +
                    $"<size=120%><color=#FFD84A>{arrow}</color></size>\n" +
                    $"<size=100%><color=#FFFFFF>TIME: {seconds}s</color></size>";
            }

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

        int clientId = GetClientId(player);

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

            AmongUsClient.Instance.FinishRpcImmediately(writer);

            LastPrivateNames[player.PlayerId] = name;
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[RoomRush] SetName privato fallito per " +
                    $"{player.PlayerId}: {ex.Message}"
                );
            }
            catch
            {
            }
        }
    }

    private static int GetClientId(PlayerControl player)
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


    private static void EliminatePlayer(PlayerControl player)
    {
        if (player == null ||
            player.Data == null ||
            player.Data.IsDead ||
            player.Data.Disconnected)
            return;

        try
        {
            player.RpcMurderPlayer(player, true);
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[RoomRush] Impossibile eliminare " +
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


    private static string GetRoomName(SystemTypes room)
    {
        string raw = room.ToString();

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

    private static string SplitCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder();
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


    private static void Finish(PlayerControl winner = null)
    {
        if (Finished)
            return;

        Finished = true;
        NextRoundAt = -1f;

        if (winner == null)
        {
            List<PlayerControl> alivePlayers = GetAlivePlayers();

            if (alivePlayers.Count == 1)
                winner = alivePlayers[0];
        }

        WinnerName = winner != null
            ? GetOriginalName(winner)
            : "";

        try
        {
            BMLogger.LogInfo(
                string.IsNullOrEmpty(WinnerName)
                    ? "[RoomRush] Partita terminata senza vincitore identificato."
                    : $"[RoomRush] Vincitore salvato per il sommario: {WinnerName}"
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

            BMLogger.LogInfo(
                "[RoomRush] RpcEndGame inviato."
            );
        }
        catch (Exception ex)
        {
            try
            {
                BMLogger.LogError(
                    $"[RoomRush] Errore RpcEndGame: {ex.Message}"
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

        TargetRoom = null;
        PreviousTargetRoom = null;

        RoundEndAt = 0f;
        NextRoundAt = -1f;
        InitialStartAt = -1f;

        ReachedRoom.Clear();
        OriginalNames.Clear();
        LastPrivateNames.Clear();
    }

    private static void RestorePrivateNames()
    {
        try
        {
            foreach (PlayerControl player in BanMod.AllPlayerControls)
            {
                if (player == null || player.Data == null)
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
internal static class RoomRushFixedUpdatePatch
{
    private static void Postfix(PlayerControl __instance)
    {
        if (__instance == null ||
            PlayerControl.LocalPlayer == null)
            return;

        if (__instance != PlayerControl.LocalPlayer)
            return;

        RoomRush.FixedUpdate();
    }
}
