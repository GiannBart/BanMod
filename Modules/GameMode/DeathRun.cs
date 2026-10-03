using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod;
public static class DeathRun
{
    private const int RouteLength = 4;
    private const float HoldSeconds = 1f;
    private const float HoldCompleteDisplaySeconds = 0.20f;
    private const float NextRoundDelay = 1f;
    private const float InitialStartDelay = 10f;

    private static readonly Dictionary<byte, int> PlayerStep = new();
    private static readonly Dictionary<byte, float> HoldStartedAt = new();
    private static readonly Dictionary<byte, float> HoldCompleteDisplayUntil = new();
    private static readonly HashSet<byte> CompletedRoute = new();

    private static readonly Dictionary<byte, string> OriginalNames = new();
    private static readonly Dictionary<byte, string> LastPrivateNames = new();

    private static readonly List<PlainShipRoom> Route = new();

    private static float NextRoundAt = -1f;
    private static float InitialStartAt = -1f;

    private static bool Running;
    private static bool Finished;

    public static bool IsEnabled =>
        Options.GameMode != null &&
        Options.GameMode.GetValue(GameModeType.DeathRun);

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
                        "[DeathRun] Gameplay pronto. DeathRun partirà tra 10 secondi."
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

        List<PlayerControl> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            Finish(alivePlayers.FirstOrDefault());
            return;
        }

        if (Route.Count == 0)
        {
            StartNewRound();
            return;
        }

        CleanupRuntimePlayerState(alivePlayers);


        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null ||
                player.Data == null ||
                CompletedRoute.Contains(player.PlayerId))
                continue;

            if (!PlayerStep.TryGetValue(
                    player.PlayerId,
                    out int step))
            {
                step = 0;
                PlayerStep[player.PlayerId] = 0;
            }

            if (step < 0)
            {
                step = 0;
                PlayerStep[player.PlayerId] = 0;
            }

            if (step >= Route.Count)
            {
                CompletedRoute.Add(player.PlayerId);
                HoldStartedAt.Remove(player.PlayerId);
                continue;
            }

            if (player.inVent)
            {
                HoldStartedAt.Remove(player.PlayerId);
                continue;
            }

            PlainShipRoom target =
                Route[step];

            if (!IsInsideRoom(player, target))
            {
                HoldStartedAt.Remove(player.PlayerId);
                continue;
            }

            if (!HoldStartedAt.TryGetValue(
                    player.PlayerId,
                    out float holdStart))
            {
                HoldStartedAt[player.PlayerId] =
                    Time.realtimeSinceStartup;

                continue;
            }

            float heldFor =
                Time.realtimeSinceStartup - holdStart;

            if (heldFor < HoldSeconds)
                continue;

            SendHoldCompletePrivateName(
                player,
                step,
                target
            );

            HoldCompleteDisplayUntil[player.PlayerId] =
                Time.realtimeSinceStartup +
                HoldCompleteDisplaySeconds;

            HoldStartedAt.Remove(player.PlayerId);

            step++;
            PlayerStep[player.PlayerId] = step;

            try
            {
                BMLogger.LogInfo(
                    $"[DeathRun] {GetOriginalName(player)} ha completato " +
                    $"step {step}/{Route.Count}."
                );
            }
            catch
            {
            }

            if (step >= Route.Count)
            {
                CompletedRoute.Add(player.PlayerId);

                try
                {
                    BMLogger.LogInfo(
                        $"[DeathRun] {GetOriginalName(player)} ha completato il percorso."
                    );
                }
                catch
                {
                }
            }
        }

        int completedAlive =
            CompletedRoute.Count(id =>
                alivePlayers.Any(
                    pc => pc.PlayerId == id
                )
            );

        if (completedAlive == alivePlayers.Count)
        {
            QueueNextRound();
            return;
        }

        if (completedAlive == alivePlayers.Count - 1)
        {
            PlayerControl lastPlayer =
                alivePlayers.FirstOrDefault(
                    pc => !CompletedRoute.Contains(
                        pc.PlayerId
                    )
                );

            if (lastPlayer != null)
            {
                try
                {
                    BMLogger.LogInfo(
                        $"[DeathRun] Ultimo a completare: " +
                        $"{GetOriginalName(lastPlayer)}"
                    );
                }
                catch
                {
                }

                EliminatePlayer(lastPlayer);

                int survivors =
                    alivePlayers.Count - 1;

                if (survivors <= 1)
                {
                    PlayerControl winner =
                        alivePlayers.FirstOrDefault(
                            pc =>
                                pc.PlayerId !=
                                lastPlayer.PlayerId
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

        NextRoundAt = -1f;
        InitialStartAt = -1f;

        Route.Clear();
        PlayerStep.Clear();
        HoldStartedAt.Clear();
        HoldCompleteDisplayUntil.Clear();
        CompletedRoute.Clear();
        LastPrivateNames.Clear();

        CacheOriginalNames();

        try
        {
            BMLogger.LogInfo("[DeathRun] Avvio DeathRun");
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

        if (validRooms.Count < 2)
        {
            try
            {
                BMLogger.LogError(
                    "[DeathRun] Non ci sono abbastanza stanze valide."
                );
            }
            catch
            {
            }

            Finish();
            return;
        }

        BuildRoute(validRooms);

        if (Route.Count == 0)
        {
            Finish();
            return;
        }

        PlayerStep.Clear();
        HoldStartedAt.Clear();
        HoldCompleteDisplayUntil.Clear();
        CompletedRoute.Clear();

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null)
                continue;

            PlayerStep[player.PlayerId] = 0;
        }

        NextRoundAt = -1f;
        LastPrivateNames.Clear();

        try
        {
            string routeText = string.Join(
                " -> ",
                Route.Select(
                    room => GetRoomName(room.RoomId)
                )
            );

            BMLogger.LogInfo(
                $"[DeathRun] Nuovo percorso: {routeText} | " +
                $"hold {HoldSeconds:0.0}s per stanza"
            );
        }
        catch
        {
        }

        UpdatePrivateNames(alivePlayers, force: true);
    }

    private static void BuildRoute(
        List<PlainShipRoom> validRooms)
    {
        Route.Clear();

        var pool = new List<PlainShipRoom>(validRooms);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            PlainShipRoom temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }

        int count =
            Mathf.Min(RouteLength, pool.Count);

        for (int i = 0; i < count; i++)
            Route.Add(pool[i]);
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

    private static PlainShipRoom GetCurrentTarget(
        PlayerControl player)
    {
        if (player == null ||
            Route.Count == 0 ||
            CompletedRoute.Contains(player.PlayerId))
            return null;

        if (!PlayerStep.TryGetValue(
                player.PlayerId,
                out int step))
        {
            step = 0;
        }

        if (step < 0 || step >= Route.Count)
            return null;

        return Route[step];
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
        PlainShipRoom targetRoom)
    {
        if (player == null ||
            targetRoom == null ||
            targetRoom.roomArea == null)
            return "";

        Vector2 direction =
            (Vector2)targetRoom.roomArea.bounds.center -
            (Vector2)player.transform.position;

        if (direction.sqrMagnitude <
            1.5f * 1.5f)
        {
            return "•";
        }

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

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
        if (Route.Count == 0)
            return;

        foreach (PlayerControl player in alivePlayers)
        {
            if (player == null ||
                player.Data == null)
                continue;

            if (HoldCompleteDisplayUntil.TryGetValue(
                    player.PlayerId,
                    out float displayUntil))
            {
                if (Time.realtimeSinceStartup < displayUntil)
                    continue;

                HoldCompleteDisplayUntil.Remove(
                    player.PlayerId
                );
            }

            string originalName =
                GetOriginalName(player);

            string information;

            if (CompletedRoute.Contains(player.PlayerId))
            {
                information =
                    $"<size=75%><color=#55FF55>✓ ROUTE COMPLETE</color></size>\n" +
                    $"<size=80%><color=#FFFFFF>WAITING FOR THE LAST PLAYER...</color></size>";
            }
            else
            {
                if (!PlayerStep.TryGetValue(
                        player.PlayerId,
                        out int step))
                {
                    step = 0;
                }

                PlainShipRoom target =
                    GetCurrentTarget(player);

                if (target == null)
                    continue;

                string roomName =
                    GetRoomName(target.RoomId);

                string arrow =
                    GetArrow(player, target);

                int displayStep =
                    Mathf.Clamp(
                        step + 1,
                        1,
                        Route.Count
                    );

                information =
                    $"<size=70%><color=#66CCFF>STEP {displayStep}/{Route.Count}</color></size> " +
                    $"<size=75%><color=#FFD84A>{roomName}</color></size> " +
                    $"<size=120%><color=#FFD84A>{arrow}</color></size>\n" +
                    $"<size=90%><color=#FF5555>HOLD: 0%</color></size>";
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

    private static void SendHoldCompletePrivateName(
        PlayerControl player,
        int step,
        PlainShipRoom target)
    {
        if (player == null ||
            player.Data == null ||
            target == null)
            return;

        string originalName =
            GetOriginalName(player);

        string roomName =
            GetRoomName(target.RoomId);

        string arrow =
            GetArrow(player, target);

        int displayStep =
            Mathf.Clamp(
                step + 1,
                1,
                Route.Count
            );

        string information =
            $"<size=70%><color=#66CCFF>STEP {displayStep}/{Route.Count}</color></size> " +
            $"<size=75%><color=#FFD84A>{roomName}</color></size> " +
            $"<size=120%><color=#FFD84A>{arrow}</color></size>\n" +
            $"<size=90%><color=#55FF55>HOLD: 100%</color></size>";

        string privateName =
            $"<size=100%>{originalName}</size>\n" +
            information;

        SendPrivateSetNameToSelf(
            player,
            privateName,
            force: true
        );
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
                    $"[DeathRun] SetName privato fallito per " +
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
                    $"[DeathRun] Impossibile eliminare " +
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
                alivePlayers.Select(pc => pc.PlayerId)
            );

        foreach (byte id in
                 PlayerStep.Keys.ToArray())
        {
            if (!aliveIds.Contains(id))
                PlayerStep.Remove(id);
        }

        foreach (byte id in
                 HoldStartedAt.Keys.ToArray())
        {
            if (!aliveIds.Contains(id))
                HoldStartedAt.Remove(id);
        }

        foreach (byte id in
                 HoldCompleteDisplayUntil.Keys.ToArray())
        {
            if (!aliveIds.Contains(id))
                HoldCompleteDisplayUntil.Remove(id);
        }

        CompletedRoute.RemoveWhere(
            id => !aliveIds.Contains(id)
        );
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
                    ? "[DeathRun] Nessun vincitore identificato."
                    : $"[DeathRun] Vincitore: {WinnerName}"
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
                    $"[DeathRun] Errore RpcEndGame: {ex.Message}"
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

        NextRoundAt = -1f;
        InitialStartAt = -1f;

        Route.Clear();
        PlayerStep.Clear();
        HoldStartedAt.Clear();
        HoldCompleteDisplayUntil.Clear();
        CompletedRoute.Clear();

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
internal static class DeathRunFixedUpdatePatch
{
    private static void Postfix(
        PlayerControl __instance)
    {
        if (__instance == null ||
            PlayerControl.LocalPlayer == null)
            return;

        if (__instance != PlayerControl.LocalPlayer)
            return;

        DeathRun.FixedUpdate();
    }
}
