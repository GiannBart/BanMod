using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod;

internal static class RandomVentSpawnManager
{
    private static bool AlreadyTriggered;
    private static bool Running;

    public static void Reset()
    {
        AlreadyTriggered = false;
        Running = false;
    }

    public static void TryStart()
    {
        try
        {
            if (AlreadyTriggered || Running)
                return;

            bool randomVentSpawnEnabled =
                Options.RandomVentSpawn != null &&
                Options.RandomVentSpawn.GetBool();

            bool randomVentSpawnFfaEnabled =
                Options.RandomVentSpawnffa != null &&
                Options.RandomVentSpawnffa.GetBool() &&
                Options.GameMode.GetValue(GameModeType.FFA);

            if (!randomVentSpawnEnabled &&
                !randomVentSpawnFfaEnabled)
            {
                return;
            }

            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost)
            {
                return;
            }

            if (ShipStatus.Instance == null ||
                HudManager.Instance == null)
            {
                return;
            }


            if (BanModServerSelection.IsVanilla)
            {
                if (ShipStatus.Instance.AllVents == null ||
                    ShipStatus.Instance.AllVents.Length <= 0)
                {
                    return;
                }

                VentilationSystem system =
                    GetVentSystem();

                if (system == null ||
                    system.PlayersInsideVents == null)
                {
                    return;
                }

                AlreadyTriggered = true;
                Running = true;

                HudManager.Instance.StartCoroutine(
                    CoBootPlayersFromRandomVents()
                );

                return;
            }


            if (BanModServerSelection.IsModded25)
            {
                if (ShipStatus.Instance.AllRooms == null)
                    return;

                AlreadyTriggered = true;
                Running = true;

                HudManager.Instance.StartCoroutine(
                    CoTeleportPlayersToRandomMapPositions()
                );

                return;
            }
        }
        catch (Exception ex)
        {
            Running = false;

            Debug.LogError(
                "[RandomSpawn] Start failed: " + ex
            );
        }
    }

    private static IEnumerator CoTeleportPlayersToRandomMapPositions()
    {
        try
        {
            yield return null;
            yield return null;

            List<PlayerControl> players =
                GetValidPlayers();

            if (players == null ||
                players.Count == 0)
            {
                yield break;
            }

            bool isFfa =
                Options.GameMode.GetValue(
                    GameModeType.FFA
                );


            if (!isFfa)
            {
                TeleportPlayersFarApart(
                    players
                );
            }



            else
            {
                TeleportFfaPlayersByColor(
                    players
                );
            }

            yield return null;
        }
        finally
        {
            Running = false;
        }
    }


    private static void TeleportPlayersFarApart(List<PlayerControl> players)
    {
        if (players == null || players.Count == 0)
            return;

        ShufflePlayers(players);

        List<Vector2> assignedPositions =
            new List<Vector2>();

        for (int i = 0; i < players.Count; i++)
        {
            PlayerControl player = players[i];

            if (player == null ||
                player.Data == null ||
                player.Data.Disconnected ||
                player.Data.IsDead)
            {
                continue;
            }

            if (!TryGetFarthestRandomMapPosition(
                    assignedPositions,
                    out Vector2 spawnPosition,
                    out PlainShipRoom selectedRoom))
            {
                Debug.LogWarning(
                    $"[RandomSpawn] Nessuna posizione sicura trovata per " +
                    $"{player.Data.PlayerName}"
                );

                continue;
            }

            assignedPositions.Add(spawnPosition);

            Debug.Log(
                $"[RandomSpawn] NON-FFA: " +
                $"{player.Data.PlayerName} -> " +
                $"{spawnPosition}"
            );

            player.NetTransform.RpcSnapTo(spawnPosition);
        }
    }

    private static void TeleportFfaPlayersByColor(
    List<PlayerControl> players)
    {
        if (players == null || players.Count == 0)
            return;

        Dictionary<byte, List<PlayerControl>> playersByColor =
            BuildPlayerGroupsByColor(players);

        if (playersByColor == null ||
            playersByColor.Count == 0)
        {
            return;
        }

        List<byte> colors =
            new List<byte>(playersByColor.Keys);

        ShuffleColorIds(colors);

        List<Vector2> assignedTeamCenters =
            new List<Vector2>();


        for (int i = 0; i < colors.Count; i++)
        {
            byte colorId = colors[i];

            if (!playersByColor.TryGetValue(
                    colorId,
                    out List<PlayerControl> team) ||
                team == null ||
                team.Count == 0)
            {
                continue;
            }

            if (!TryGetFarthestRandomMapPosition(
                    assignedTeamCenters,
                    out Vector2 teamCenter,
                    out PlainShipRoom teamRoom))
            {
                Debug.LogWarning(
                    $"[RandomSpawn] Nessuna posizione sicura " +
                    $"per team colore {colorId}"
                );

                continue;
            }


            assignedTeamCenters.Add(teamCenter);

            ShufflePlayers(team);


            List<Vector2> usedTeamPositions =
                new List<Vector2>();


            for (int p = 0; p < team.Count; p++)
            {
                PlayerControl player = team[p];

                if (player == null ||
                    player.Data == null ||
                    player.Data.Disconnected ||
                    player.Data.IsDead)
                {
                    continue;
                }


                Vector2 finalPosition;

                if (usedTeamPositions.Count == 0)
                {
                    finalPosition =
                        teamCenter;
                }


                else
                {
                    if (!TryGetNearbyPointInRoom(
                            teamRoom,
                            teamCenter,
                            usedTeamPositions,
                            out finalPosition))
                    {
                        bool foundFallback =
                            false;


                        for (int attempt = 0;
                             attempt < 50;
                             attempt++)
                        {
                            if (!TryGetRandomPointInRoom(
                                    teamRoom,
                                    out Vector2 fallback))
                            {
                                continue;
                            }


                            bool tooClose =
                                false;


                            for (int u = 0;
                                 u < usedTeamPositions.Count;
                                 u++)
                            {
                                if ((fallback -
                                     usedTeamPositions[u])
                                    .sqrMagnitude <
                                    0.50f * 0.50f)
                                {
                                    tooClose =
                                        true;

                                    break;
                                }
                            }


                            if (tooClose)
                                continue;


                            finalPosition =
                                fallback;

                            foundFallback =
                                true;

                            break;
                        }


                        if (!foundFallback)
                        {
                            Debug.LogWarning(
                                $"[RandomSpawn] Nessuno spazio sicuro " +
                                $"per {player.Data.PlayerName} " +
                                $"nel team {colorId}"
                            );

                            continue;
                        }
                    }
                }


                usedTeamPositions.Add(
                    finalPosition
                );


                Debug.Log(
                    $"[RandomSpawn] FFA: " +
                    $"{player.Data.PlayerName} " +
                    $"(Color {colorId}) -> " +
                    $"{finalPosition}"
                );


                player.NetTransform.RpcSnapTo(
                    finalPosition
                );
            }
        }
    }


    private static bool TryGetFarthestRandomMapPosition(
        List<Vector2> assignedPositions,
        out Vector2 result,
        out PlainShipRoom selectedRoom)
    {
        result =
            Vector2.zero;

        selectedRoom =
            null;


        List<PlainShipRoom> validRooms =
            GetValidRooms();

        if (validRooms == null ||
            validRooms.Count == 0)
        {
            return false;
        }


        if (assignedPositions == null ||
            assignedPositions.Count == 0)
        {
            ShuffleRooms(
                validRooms
            );

            for (int i = 0;
                 i < validRooms.Count;
                 i++)
            {
                PlainShipRoom room =
                    validRooms[i];

                if (TryGetRandomPointInRoom(
                        room,
                        out Vector2 point))
                {
                    result =
                        point;

                    selectedRoom =
                        room;

                    return true;
                }
            }

            return false;
        }


        Vector2 bestPosition =
            Vector2.zero;

        PlainShipRoom bestRoom =
            null;

        float bestMinimumDistance =
            -1f;

        bool found =
            false;


        const int samplesPerRoom =
            10;


        for (int roomIndex = 0;
             roomIndex < validRooms.Count;
             roomIndex++)
        {
            PlainShipRoom room =
                validRooms[roomIndex];

            for (int sample = 0;
                 sample < samplesPerRoom;
                 sample++)
            {
                if (!TryGetRandomPointInRoom(
                        room,
                        out Vector2 candidate))
                {
                    continue;
                }

                float minimumDistance =
                    GetMinimumDistanceSquared(
                        candidate,
                        assignedPositions
                    );

                if (minimumDistance >
                    bestMinimumDistance)
                {
                    bestMinimumDistance =
                        minimumDistance;

                    bestPosition =
                        candidate;

                    bestRoom =
                        room;

                    found =
                        true;
                }
            }
        }


        if (!found)
            return false;


        result =
            bestPosition;

        selectedRoom =
            bestRoom;

        return true;
    }

    private static bool TryGetNearbyPointInRoom(
        PlainShipRoom room,
        Vector2 center,
        List<Vector2> usedTeamPositions,
        out Vector2 position)
    {
        position =
            Vector2.zero;

        if (room == null ||
            room.roomArea == null)
        {
            return false;
        }


        const float minRadius =
            0.45f;

        const float maxRadius =
            1.25f;


        const float minimumMemberDistance =
            0.50f;


        const int maxAttempts =
            100;


        for (int attempt = 0;
             attempt < maxAttempts;
             attempt++)
        {
            float angle =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

            float radius =
                UnityEngine.Random.Range(
                    minRadius,
                    maxRadius
                );

            Vector2 candidate =
                center +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;


            if (!IsSpawnPointSafe(
                    room,
                    candidate))
            {
                continue;
            }


            bool tooClose =
                false;

            if (usedTeamPositions != null)
            {
                for (int i = 0;
                     i < usedTeamPositions.Count;
                     i++)
                {
                    float distanceSquared =
                        (
                            candidate -
                            usedTeamPositions[i]
                        ).sqrMagnitude;

                    if (distanceSquared <
                        minimumMemberDistance *
                        minimumMemberDistance)
                    {
                        tooClose =
                            true;

                        break;
                    }
                }
            }

            if (tooClose)
                continue;


            position =
                candidate;

            return true;
        }


        return false;
    }


    private static List<PlainShipRoom> GetValidRooms()
    {
        List<PlainShipRoom> result =
            new List<PlainShipRoom>();


        if (ShipStatus.Instance == null ||
            ShipStatus.Instance.AllRooms == null)
        {
            return result;
        }


        foreach (PlainShipRoom room
                 in ShipStatus.Instance.AllRooms)
        {
            if (room == null)
                continue;

            if (room.roomArea == null)
                continue;

            if (!room.roomArea.enabled)
                continue;

            if (room.gameObject == null ||
                !room.gameObject.activeInHierarchy)
            {
                continue;
            }

            result.Add(
                room
            );
        }


        return result;
    }


    private static bool TryGetRandomPointInRoom(
        PlainShipRoom room,
        out Vector2 position)
    {
        position =
            Vector2.zero;


        if (room == null ||
            room.roomArea == null)
        {
            return false;
        }


        Collider2D area =
            room.roomArea;

        Bounds bounds =
            area.bounds;

        float usableX =
            bounds.extents.x * 0.70f;

        float usableY =
            bounds.extents.y * 0.70f;

        const int maxAttempts =
            120;


        for (int attempt = 0;
             attempt < maxAttempts;
             attempt++)
        {
            Vector2 candidate =
                new Vector2(
                    UnityEngine.Random.Range(
                        bounds.center.x - usableX,
                        bounds.center.x + usableX
                    ),
                    UnityEngine.Random.Range(
                        bounds.center.y - usableY,
                        bounds.center.y + usableY
                    )
                );


            if (!area.OverlapPoint(candidate))
                continue;

            if (!IsSpawnPointSafe(
                    room,
                    candidate))
            {
                continue;
            }


            position =
                candidate;

            return true;
        }

        return false;
    }

    private static bool IsSpawnPointSafe(
        PlainShipRoom room,
        Vector2 position)
    {
        if (room == null ||
            room.roomArea == null)
        {
            return false;
        }


        if (!room.roomArea.OverlapPoint(position))
            return false;


        const float safetyRadius =
            0.48f;


        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                position,
                safetyRadius
            );


        if (colliders == null ||
            colliders.Length == 0)
        {
            return true;
        }


        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D collider =
                colliders[i];


            if (collider == null)
                continue;

            if (!collider.enabled)
                continue;

            if (collider == room.roomArea)
                continue;


            GameObject obj =
                collider.gameObject;

            if (obj == null)
                continue;



            if (!collider.isTrigger)
            {
                return false;
            }



            if (IsDangerousSpawnObject(
                    obj))
            {
                return false;
            }
        }


        return true;
    }

    private static bool IsDangerousSpawnObject(
        GameObject obj)
    {
        if (obj == null)
            return false;


        Transform current =
            obj.transform;


        while (current != null)
        {
            GameObject currentObject =
                current.gameObject;

            if (currentObject == null)
                break;


            string objectName =
                currentObject.name
                    .ToLowerInvariant();


            if (objectName.Contains("console") ||
                objectName.Contains("task") ||
                objectName.Contains("vent") ||
                objectName.Contains("ladder") ||
                objectName.Contains("door") ||
                objectName.Contains("platform") ||
                objectName.Contains("panel") ||
                objectName.Contains("terminal") ||
                objectName.Contains("switch") ||
                objectName.Contains("button") ||
                objectName.Contains("wall"))
            {
                return true;
            }


            current =
                current.parent;
        }


        return false;
    }

    private static float GetMinimumDistanceSquared(
        Vector2 candidate,
        List<Vector2> assignedPositions)
    {
        if (assignedPositions == null ||
            assignedPositions.Count == 0)
        {
            return float.MaxValue;
        }


        float minimumDistance =
            float.MaxValue;


        for (int i = 0;
             i < assignedPositions.Count;
             i++)
        {
            float distanceSquared =
                (
                    candidate -
                    assignedPositions[i]
                ).sqrMagnitude;


            if (distanceSquared <
                minimumDistance)
            {
                minimumDistance =
                    distanceSquared;
            }
        }


        return minimumDistance;
    }


    private static void ShufflePlayers(
        List<PlayerControl> players)
    {
        if (players == null)
            return;


        for (int i = players.Count - 1;
             i > 0;
             i--)
        {
            int j =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );


            PlayerControl temp =
                players[i];

            players[i] =
                players[j];

            players[j] =
                temp;
        }
    }


    private static void ShuffleRooms(
        List<PlainShipRoom> rooms)
    {
        if (rooms == null)
            return;


        for (int i = rooms.Count - 1;
             i > 0;
             i--)
        {
            int j =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );


            PlainShipRoom temp =
                rooms[i];

            rooms[i] =
                rooms[j];

            rooms[j] =
                temp;
        }
    }

    public static void TryStartAfterMeeting()
    {
        try
        {
            GameModeType gameMode =
                Options.GameMode.Selected;


            if (gameMode != GameModeType.FFA)
                return;


            if (Options.RandomVentSpawnffa == null ||
                !Options.RandomVentSpawnffa.GetBool())
            {
                return;
            }


            if (Running)
                return;


            AlreadyTriggered =
                false;


            TryStart();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[RandomVentSpawn] After meeting failed: " +
                ex
            );
        }
    }


    private static IEnumerator CoBootPlayersFromRandomVents()
    {
        VentilationSystem system =
            null;


        List<byte> queuedPlayerIds =
            new List<byte>();


        try
        {
            yield return null;
            yield return null;


            system =
                GetVentSystem();


            if (system == null ||
                system.PlayersInsideVents == null)
            {
                yield break;
            }


            List<PlayerControl> players =
                GetValidPlayers();


            List<Vent> vents =
                GetRandomizedVents();


            if (players.Count <= 0 ||
                vents.Count <= 0)
            {
                yield break;
            }


            Dictionary<byte, List<PlayerControl>> playersByColor =
                BuildPlayerGroupsByColor(
                    players
                );


            Dictionary<byte, Vent> ventByColor =
                BuildVentAssignments(
                    playersByColor,
                    vents
                );


            List<int> ventsToBoot =
                new List<int>();


            for (int i = 0;
                 i < players.Count;
                 i++)
            {
                PlayerControl player =
                    players[i];


                if (player == null ||
                    player.Data == null ||
                    player.Data.Disconnected ||
                    player.Data.IsDead)
                {
                    continue;
                }


                byte colorId =
                    (byte)player.Data
                        .DefaultOutfit
                        .ColorId;


                if (!ventByColor.TryGetValue(
                        colorId,
                        out Vent vent))
                {
                    vent =
                        GetRandomVent(
                            vents
                        );
                }


                if (vent == null)
                    continue;


                string roomName =
                    GetVentRoomKey(
                        vent
                    );


                byte ventId =
                    (byte)vent.Id;


                system.PlayersInsideVents[
                    player.PlayerId
                ] = ventId;


                queuedPlayerIds.Add(
                    player.PlayerId
                );


                if (!ventsToBoot.Contains(
                        vent.Id))
                {
                    ventsToBoot.Add(
                        vent.Id
                    );
                }


                Debug.Log(
                    "[RandomVentSpawn] Queued " +
                    $"{player.Data.PlayerName} " +
                    $"(color {colorId}) " +
                    $"from vent {vent.Id}, " +
                    $"room {roomName}"
                );
            }

            for (int i = 0;
                 i < ventsToBoot.Count;
                 i++)
            {
                VentilationSystem.Update(
                    VentilationSystem.Operation.BootImpostors,
                    ventsToBoot[i]
                );
            }


            yield return null;
        }
        finally
        {
            if (system != null &&
                system.PlayersInsideVents != null)
            {
                for (int i = 0;
                     i < queuedPlayerIds.Count;
                     i++)
                {
                    try
                    {
                        if (system.PlayersInsideVents.ContainsKey(
                                queuedPlayerIds[i]))
                        {
                            system.PlayersInsideVents.Remove(
                                queuedPlayerIds[i]
                            );
                        }
                    }
                    catch
                    {
                    }
                }
            }


            Running =
                false;
        }
    }

    private static Dictionary<byte, List<PlayerControl>>
        BuildPlayerGroupsByColor(
            List<PlayerControl> players)
    {
        Dictionary<byte, List<PlayerControl>> result =
            new Dictionary<byte, List<PlayerControl>>();


        if (players == null)
            return result;


        for (int i = 0;
             i < players.Count;
             i++)
        {
            PlayerControl player =
                players[i];


            if (player == null ||
                player.Data == null ||
                player.Data.Disconnected ||
                player.Data.IsDead)
            {
                continue;
            }


            byte colorId =
                (byte)player.Data
                    .DefaultOutfit
                    .ColorId;


            if (!result.TryGetValue(
                    colorId,
                    out List<PlayerControl> colorPlayers))
            {
                colorPlayers =
                    new List<PlayerControl>();


                result[colorId] =
                    colorPlayers;
            }


            colorPlayers.Add(
                player
            );
        }


        return result;
    }


    private static Dictionary<byte, Vent>
        BuildVentAssignments(
            Dictionary<byte, List<PlayerControl>> playersByColor,
            List<Vent> allVents)
    {
        Dictionary<byte, Vent> result =
            new Dictionary<byte, Vent>();


        List<Vent> assignedVents =
            new List<Vent>();


        List<byte> singleColors =
            new List<byte>();


        List<byte> duplicatedColors =
            new List<byte>();


        foreach (KeyValuePair<byte, List<PlayerControl>> entry
                 in playersByColor)
        {
            if (entry.Value != null &&
                entry.Value.Count > 1)
            {
                duplicatedColors.Add(
                    entry.Key
                );

                continue;
            }


            singleColors.Add(
                entry.Key
            );
        }


        ShuffleColorIds(
            singleColors
        );


        ShuffleColorIds(
            duplicatedColors
        );


        AssignColorsToVents(
            singleColors,
            playersByColor,
            allVents,
            assignedVents,
            result
        );


        AssignColorsToVents(
            duplicatedColors,
            playersByColor,
            allVents,
            assignedVents,
            result
        );


        return result;
    }


    private static void AssignColorsToVents(
        List<byte> colors,
        Dictionary<byte, List<PlayerControl>> playersByColor,
        List<Vent> allVents,
        List<Vent> assignedVents,
        Dictionary<byte, Vent> result)
    {
        for (int i = 0;
             i < colors.Count;
             i++)
        {
            byte colorId =
                colors[i];


            Vent selectedVent =
                SelectFarthestAvailableVent(
                    allVents,
                    assignedVents
                );


            if (selectedVent == null)
                continue;


            result[colorId] =
                selectedVent;


            assignedVents.Add(
                selectedVent
            );


            int playerCount =
                playersByColor[
                    colorId
                ].Count;


            Debug.Log(
                "[RandomVentSpawn] Vent " +
                $"{selectedVent.Id} assigned " +
                $"to color {colorId} " +
                $"({playerCount} player(s))"
            );
        }
    }


    private static Vent SelectFarthestAvailableVent(
        List<Vent> allVents,
        List<Vent> assignedVents)
    {
        if (allVents == null ||
            allVents.Count == 0)
        {
            return null;
        }


        bool hasFreeVent =
            HasFreeVent(
                allVents,
                assignedVents
            );


        if (assignedVents == null ||
            assignedVents.Count == 0)
        {
            return GetRandomFreeVent(
                allVents,
                assignedVents
            );
        }


        Vent bestVent =
            null;


        float bestMinimumDistance =
            -1f;


        for (int i = 0;
             i < allVents.Count;
             i++)
        {
            Vent candidate =
                allVents[i];


            if (candidate == null)
                continue;


            if (hasFreeVent &&
                IsVentAssigned(
                    candidate,
                    assignedVents
                ))
            {
                continue;
            }


            float minimumDistance =
                GetMinimumDistanceSquared(
                    candidate,
                    assignedVents
                );


            if (minimumDistance >
                bestMinimumDistance)
            {
                bestMinimumDistance =
                    minimumDistance;


                bestVent =
                    candidate;
            }
        }


        return bestVent ??
               GetRandomFreeVent(
                   allVents,
                   assignedVents
               );
    }

    private static bool HasFreeVent(
        List<Vent> allVents,
        List<Vent> assignedVents)
    {
        for (int i = 0;
             i < allVents.Count;
             i++)
        {
            Vent vent =
                allVents[i];


            if (vent != null &&
                !IsVentAssigned(
                    vent,
                    assignedVents
                ))
            {
                return true;
            }
        }


        return false;
    }


    private static bool IsVentAssigned(
        Vent vent,
        List<Vent> assignedVents)
    {
        if (vent == null ||
            assignedVents == null)
        {
            return false;
        }


        for (int i = 0;
             i < assignedVents.Count;
             i++)
        {
            Vent assignedVent =
                assignedVents[i];


            if (assignedVent != null &&
                assignedVent.Id == vent.Id)
            {
                return true;
            }
        }


        return false;
    }


    private static Vent GetRandomFreeVent(
        List<Vent> allVents,
        List<Vent> assignedVents)
    {
        List<Vent> freeVents =
            new List<Vent>();


        for (int i = 0;
             i < allVents.Count;
             i++)
        {
            Vent vent =
                allVents[i];


            if (vent != null &&
                !IsVentAssigned(
                    vent,
                    assignedVents
                ))
            {
                freeVents.Add(
                    vent
                );
            }
        }


        if (freeVents.Count > 0)
        {
            return GetRandomVent(
                freeVents
            );
        }


        return GetRandomVent(
            allVents
        );
    }

    private static Vent GetRandomVent(
        List<Vent> allVents)
    {
        if (allVents == null ||
            allVents.Count == 0)
        {
            return null;
        }


        return allVents[
            UnityEngine.Random.Range(
                0,
                allVents.Count
            )
        ];
    }


    private static void ShuffleColorIds(
        List<byte> colors)
    {
        if (colors == null)
            return;


        for (int i = colors.Count - 1;
             i > 0;
             i--)
        {
            int j =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );


            byte temp =
                colors[i];


            colors[i] =
                colors[j];


            colors[j] =
                temp;
        }
    }

    private static float GetMinimumDistanceSquared(
        Vent candidate,
        List<Vent> assignedVents)
    {
        if (candidate == null)
            return -1f;


        Vector2 candidatePosition =
            (Vector2)candidate.transform.position;


        float minimumDistance =
            float.MaxValue;


        for (int i = 0;
             i < assignedVents.Count;
             i++)
        {
            Vent assignedVent =
                assignedVents[i];


            if (assignedVent == null)
                continue;


            Vector2 assignedPosition =
                (Vector2)assignedVent
                    .transform
                    .position;


            float distanceSquared =
                (
                    candidatePosition -
                    assignedPosition
                ).sqrMagnitude;


            if (distanceSquared <
                minimumDistance)
            {
                minimumDistance =
                    distanceSquared;
            }
        }


        return minimumDistance;
    }

    private static string GetVentRoomKey(
        Vent vent)
    {
        if (vent == null)
            return "Unknown";


        try
        {
            if (BanMod.RoomZoneManagerInstance != null)
            {
                var room =
                    BanMod.RoomZoneManagerInstance
                        .GetCurrentRoom(
                            (Vector2)vent.transform.position,
                            Utils.GetCurrentMap()
                        );


                if (room != null &&
                    !string.IsNullOrWhiteSpace(
                        room.RoomName))
                {
                    return room.RoomName;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[RandomVentSpawn] Could not detect " +
                $"room for vent {vent.Id}: " +
                ex.Message
            );
        }


        return "Vent_" +
               vent.Id;
    }

    private static List<PlayerControl> GetValidPlayers()
    {
        List<PlayerControl> result =
            new List<PlayerControl>();


        try
        {
            for (int i = 0;
                 i < PlayerControl.AllPlayerControls.Count;
                 i++)
            {
                PlayerControl player =
                    PlayerControl.AllPlayerControls[i];


                if (player == null ||
                    player.Data == null)
                {
                    continue;
                }


                if (player.Data.Disconnected ||
                    player.Data.IsDead)
                {
                    continue;
                }


                if (player.isDummy)
                    continue;


                result.Add(
                    player
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[RandomSpawn] Could not collect players: " +
                ex
            );
        }


        result.Sort(
            (a, b) =>
                a.PlayerId.CompareTo(
                    b.PlayerId
                )
        );


        return result;
    }

    private static List<Vent> GetRandomizedVents()
    {
        List<Vent> vents =
            new List<Vent>();


        try
        {
            if (ShipStatus.Instance == null ||
                ShipStatus.Instance.AllVents == null)
            {
                return vents;
            }


            foreach (Vent vent
                     in ShipStatus.Instance.AllVents)
            {
                if (vent == null)
                    continue;


                if (vent.gameObject == null ||
                    !vent.gameObject.activeInHierarchy)
                {
                    continue;
                }


                vents.Add(
                    vent
                );
            }


            for (int i = vents.Count - 1;
                 i > 0;
                 i--)
            {
                int j =
                    UnityEngine.Random.Range(
                        0,
                        i + 1
                    );


                Vent temp =
                    vents[i];


                vents[i] =
                    vents[j];


                vents[j] =
                    temp;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[RandomVentSpawn] Could not collect vents: " +
                ex
            );
        }


        return vents;
    }

    private static VentilationSystem GetVentSystem()
    {
        try
        {
            if (ShipStatus.Instance == null ||
                ShipStatus.Instance.Systems == null)
            {
                return null;
            }


            ISystemType systemType;


            if (!ShipStatus.Instance.Systems.TryGetValue(
                    SystemTypes.Ventilation,
                    out systemType))
            {
                return null;
            }


            return systemType?
                .TryCast<VentilationSystem>();
        }
        catch
        {
            return null;
        }
    }
}

[HarmonyPatch(
    typeof(GameStartManager),
    nameof(GameStartManager.Start)
)]
internal static class RandomVentSpawnResetPatch
{
    private static void Postfix()
    {
        RandomVentSpawnManager.Reset();
    }
}



[HarmonyPatch(
    typeof(IntroCutscene),
    "OnDestroy"
)]
internal static class RandomVentSpawnIntroEndPatch
{
    private static void Postfix()
    {
        RandomVentSpawnManager.TryStart();
    }
}


[HarmonyPatch(
    typeof(ExileController),
    nameof(ExileController.WrapUp)
)]
internal static class RandomVentSpawnAfterMeetingPatch
{
    private static void Postfix()
    {
        RandomVentSpawnManager.TryStartAfterMeeting();
    }
}