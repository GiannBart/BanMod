//using BepInEx.Unity.IL2CPP.Utils;
//using HarmonyLib;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace BanMod;

//internal static class RandomVentSpawnManager
//{
//    private static bool AlreadyTriggered;
//    private static bool Running;

//    public static void Reset()
//    {
//        AlreadyTriggered = false;
//        Running = false;
//    }

//    public static void TryStart()
//    {
//        try
//        {
//            if (AlreadyTriggered || Running)
//                return;

//            if (Options.RandomVentSpawn == null ||
//                !Options.RandomVentSpawn.GetBool())
//            {
//                return;
//            }

//            if (AmongUsClient.Instance == null ||
//                !AmongUsClient.Instance.AmHost)
//            {
//                return;
//            }

//            if (ShipStatus.Instance == null ||
//                HudManager.Instance == null)
//            {
//                return;
//            }

//            if (ShipStatus.Instance.AllVents == null ||
//                ShipStatus.Instance.AllVents.Length <= 0)
//            {
//                return;
//            }

//            VentilationSystem system = GetVentSystem();

//            if (system == null ||
//                system.PlayersInsideVents == null)
//            {
//                return;
//            }

//            AlreadyTriggered = true;
//            Running = true;

//            HudManager.Instance.StartCoroutine(
//                CoBootPlayersFromRandomVents()
//            );
//        }
//        catch (Exception ex)
//        {
//            Running = false;

//            Debug.LogError(
//                "[RandomVentSpawn] Start failed: " + ex
//            );
//        }
//    }

//    private static IEnumerator CoBootPlayersFromRandomVents()
//    {
//        try
//        {
//            yield return null;
//            yield return null;

//            VentilationSystem system = GetVentSystem();

//            if (system == null ||
//                system.PlayersInsideVents == null)
//            {
//                yield break;
//            }

//            List<PlayerControl> players =
//                GetValidPlayers();

//            List<Vent> vents =
//                GetRandomizedVents();

//            if (players.Count <= 0 ||
//                vents.Count <= 0)
//            {
//                yield break;
//            }

//            Dictionary<byte, List<PlayerControl>> playersByColor =
//                BuildPlayerGroupsByColor(players);

//            Dictionary<byte, Vent> ventByColor =
//                BuildVentAssignments(
//                    playersByColor,
//                    vents
//                );

//            for (int i = 0; i < players.Count; i++)
//            {
//                PlayerControl player = players[i];

//                if (player == null ||
//                    player.Data == null ||
//                    player.Data.Disconnected ||
//                    player.Data.IsDead)
//                {
//                    continue;
//                }

//                byte colorId =
//                    (byte)player.Data.DefaultOutfit.ColorId;

//                if (!ventByColor.TryGetValue(
//                        colorId,
//                        out Vent vent))
//                {
//                    vent = GetRandomVent(vents);
//                }

//                if (vent == null)
//                    continue;

//                string roomName =
//                    GetVentRoomKey(vent);

//                byte ventId =
//                    (byte)vent.Id;

//                system.PlayersInsideVents[player.PlayerId] =
//                    ventId;

//                Debug.Log(
//                    "[RandomVentSpawn] Booting " +
//                    $"{player.Data.PlayerName} " +
//                    $"(color {colorId}) " +
//                    $"from vent {vent.Id}, " +
//                    $"room {roomName}"
//                );

//                VentilationSystem.Update(
//                    VentilationSystem.Operation.BootImpostors,
//                    vent.Id
//                );

//                yield return new WaitForSeconds(
//                    UnityEngine.Random.Range(
//                        0.05f,
//                        0.15f
//                    )
//                );

//                try
//                {
//                    if (system.PlayersInsideVents.ContainsKey(
//                            player.PlayerId))
//                    {
//                        system.PlayersInsideVents.Remove(
//                            player.PlayerId
//                        );
//                    }
//                }
//                catch
//                {
//                }
//            }
//        }
//        finally
//        {
//            Running = false;
//        }
//    }

//    private static Dictionary<byte, List<PlayerControl>>
//        BuildPlayerGroupsByColor(
//            List<PlayerControl> players)
//    {
//        Dictionary<byte, List<PlayerControl>> result =
//            new Dictionary<byte, List<PlayerControl>>();

//        for (int i = 0;
//             i < players.Count;
//             i++)
//        {
//            PlayerControl player = players[i];

//            if (player == null ||
//                player.Data == null ||
//                player.Data.Disconnected ||
//                player.Data.IsDead)
//            {
//                continue;
//            }

//            byte colorId =
//                (byte)player.Data.DefaultOutfit.ColorId;

//            if (!result.TryGetValue(
//                    colorId,
//                    out List<PlayerControl> colorPlayers))
//            {
//                colorPlayers =
//                    new List<PlayerControl>();

//                result[colorId] =
//                    colorPlayers;
//            }

//            colorPlayers.Add(player);
//        }

//        return result;
//    }

//    private static Dictionary<byte, Vent>
//        BuildVentAssignments(
//            Dictionary<byte, List<PlayerControl>> playersByColor,
//            List<Vent> allVents)
//    {
//        Dictionary<byte, Vent> result =
//            new Dictionary<byte, Vent>();

//        List<Vent> assignedOtherColorVents =
//            new List<Vent>();

//        List<byte> duplicatedColors =
//            new List<byte>();

//        foreach (KeyValuePair<byte, List<PlayerControl>> entry
//                 in playersByColor)
//        {
//            if (entry.Value != null &&
//                entry.Value.Count > 1)
//            {
//                duplicatedColors.Add(entry.Key);
//                continue;
//            }

//            Vent randomVent =
//                GetRandomVent(allVents);

//            if (randomVent == null)
//                continue;

//            result[entry.Key] =
//                randomVent;

//            assignedOtherColorVents.Add(
//                randomVent
//            );
//        }

//        ShuffleColorIds(duplicatedColors);

//        for (int i = 0;
//             i < duplicatedColors.Count;
//             i++)
//        {
//            byte colorId =
//                duplicatedColors[i];

//            Vent sharedVent =
//                SelectFarthestVentFromOtherColors(
//                    allVents,
//                    assignedOtherColorVents
//                );

//            if (sharedVent == null)
//                continue;

//            result[colorId] =
//                sharedVent;

//            assignedOtherColorVents.Add(
//                sharedVent
//            );

//            int playerCount =
//                playersByColor[colorId].Count;

//            Debug.Log(
//                "[RandomVentSpawn] Shared vent " +
//                $"{sharedVent.Id} assigned to " +
//                $"{playerCount} players with color {colorId}"
//            );
//        }

//        return result;
//    }

//    private static Vent SelectFarthestVentFromOtherColors(
//        List<Vent> allVents,
//        List<Vent> otherColorVents)
//    {
//        if (allVents == null ||
//            allVents.Count == 0)
//        {
//            return null;
//        }

//        if (otherColorVents == null ||
//            otherColorVents.Count == 0)
//        {
//            return GetRandomVent(allVents);
//        }

//        Vent bestVent = null;
//        float bestMinimumDistance = -1f;

//        for (int i = 0;
//             i < allVents.Count;
//             i++)
//        {
//            Vent candidate =
//                allVents[i];

//            if (candidate == null)
//                continue;

//            float minimumDistance =
//                GetMinimumDistanceSquared(
//                    candidate,
//                    otherColorVents
//                );

//            if (minimumDistance >
//                bestMinimumDistance)
//            {
//                bestMinimumDistance =
//                    minimumDistance;

//                bestVent =
//                    candidate;
//            }
//        }

//        return bestVent ??
//            GetRandomVent(allVents);
//    }

//    private static Vent GetRandomVent(
//        List<Vent> allVents)
//    {
//        if (allVents == null ||
//            allVents.Count == 0)
//        {
//            return null;
//        }

//        return allVents[
//            UnityEngine.Random.Range(
//                0,
//                allVents.Count
//            )
//        ];
//    }

//    private static void ShuffleColorIds(
//        List<byte> colors)
//    {
//        for (int i = colors.Count - 1;
//             i > 0;
//             i--)
//        {
//            int j =
//                UnityEngine.Random.Range(
//                    0,
//                    i + 1
//                );

//            byte temp = colors[i];
//            colors[i] = colors[j];
//            colors[j] = temp;
//        }
//    }

//    private static float GetMinimumDistanceSquared(
//        Vent candidate,
//        List<Vent> assignedVents)
//    {
//        if (candidate == null)
//            return -1f;

//        Vector2 candidatePosition =
//            (Vector2)candidate.transform.position;

//        float minimumDistance =
//            float.MaxValue;

//        for (int i = 0;
//             i < assignedVents.Count;
//             i++)
//        {
//            Vent assignedVent =
//                assignedVents[i];

//            if (assignedVent == null)
//                continue;

//            Vector2 assignedPosition =
//                (Vector2)assignedVent.transform.position;

//            float distanceSquared =
//                (
//                    candidatePosition -
//                    assignedPosition
//                ).sqrMagnitude;

//            if (distanceSquared <
//                minimumDistance)
//            {
//                minimumDistance =
//                    distanceSquared;
//            }
//        }

//        return minimumDistance;
//    }

//    private static string GetVentRoomKey(
//     Vent vent)
//    {
//        if (vent == null)
//            return "Unknown";

//        try
//        {
//            if (BanMod.RoomZoneManagerInstance != null)
//            {
//                var room =
//                    BanMod.RoomZoneManagerInstance
//                        .GetCurrentRoom(
//                            (Vector2)vent.transform.position,
//                            Utils.GetCurrentMap()
//                        );

//                if (room != null &&
//                    !string.IsNullOrWhiteSpace(
//                        room.RoomName))
//                {
//                    return room.RoomName;
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogWarning(
//                "[RandomVentSpawn] Could not detect " +
//                $"room for vent {vent.Id}: {ex.Message}"
//            );
//        }

//        return "Vent_" + vent.Id;
//    }


//    private static List<PlayerControl> GetValidPlayers()
//    {
//        List<PlayerControl> result =
//            new List<PlayerControl>();

//        try
//        {
//            for (int i = 0;
//                 i < PlayerControl.AllPlayerControls.Count;
//                 i++)
//            {
//                PlayerControl player =
//                    PlayerControl.AllPlayerControls[i];

//                if (player == null ||
//                    player.Data == null)
//                {
//                    continue;
//                }

//                if (player.Data.Disconnected ||
//                    player.Data.IsDead)
//                {
//                    continue;
//                }

//                if (player.isDummy)
//                    continue;

//                result.Add(player);
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError(
//                "[RandomVentSpawn] Could not collect " +
//                "players: " + ex
//            );
//        }

//        result.Sort(
//            (a, b) =>
//                a.PlayerId.CompareTo(b.PlayerId)
//        );

//        return result;
//    }

//    private static List<Vent> GetRandomizedVents()
//    {
//        List<Vent> vents =
//            new List<Vent>();

//        try
//        {
//            if (ShipStatus.Instance == null ||
//                ShipStatus.Instance.AllVents == null)
//            {
//                return vents;
//            }

//            foreach (Vent vent
//                     in ShipStatus.Instance.AllVents)
//            {
//                if (vent == null)
//                    continue;

//                if (vent.gameObject == null ||
//                    !vent.gameObject.activeInHierarchy)
//                {
//                    continue;
//                }

//                vents.Add(vent);
//            }

//            /*
//             * Fisher-Yates shuffle.
//             *
//             * Serve anche per rendere casuale la scelta
//             * quando due vent hanno la stessa distanza.
//             */
//            for (int i = vents.Count - 1;
//                 i > 0;
//                 i--)
//            {
//                int j =
//                    UnityEngine.Random.Range(
//                        0,
//                        i + 1
//                    );

//                Vent temp =
//                    vents[i];

//                vents[i] =
//                    vents[j];

//                vents[j] =
//                    temp;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError(
//                "[RandomVentSpawn] Could not collect " +
//                "vents: " + ex
//            );
//        }

//        return vents;
//    }

//    private static VentilationSystem GetVentSystem()
//    {
//        try
//        {
//            if (ShipStatus.Instance == null ||
//                ShipStatus.Instance.Systems == null)
//            {
//                return null;
//            }

//            ISystemType systemType;

//            if (!ShipStatus.Instance.Systems.TryGetValue(
//                    SystemTypes.Ventilation,
//                    out systemType))
//            {
//                return null;
//            }

//            return systemType?
//                .TryCast<VentilationSystem>();
//        }
//        catch
//        {
//            return null;
//        }
//    }
//}

//[HarmonyPatch(
//    typeof(GameStartManager),
//    nameof(GameStartManager.Start)
//)]
//internal static class RandomVentSpawnResetPatch
//{
//    private static void Postfix()
//    {
//        RandomVentSpawnManager.Reset();
//    }
//}

//[HarmonyPatch(
//    typeof(IntroCutscene),
//    "OnDestroy"
//)]
//internal static class RandomVentSpawnIntroEndPatch
//{
//    private static void Postfix()
//    {
//        RandomVentSpawnManager.TryStart();
//    }
//}
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

            if (ShipStatus.Instance.AllVents == null ||
                ShipStatus.Instance.AllVents.Length <= 0)
            {
                return;
            }

            VentilationSystem system = GetVentSystem();

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
        }
        catch (Exception ex)
        {
            Running = false;

            Debug.LogError(
                "[RandomVentSpawn] Start failed: " + ex
            );
        }
    }
    public static void TryStartAfterMeeting()
    {
        try
        {
            GameModeType gameMode = Options.GameMode.Selected;
            if (gameMode != GameModeType.FFA)
                return;

            if (Options.RandomVentSpawnffa == null ||
                !Options.RandomVentSpawnffa.GetBool())
            {
                return;
            }

            if (Running)
                return;

            AlreadyTriggered = false;

            TryStart();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[RandomVentSpawn] After meeting failed: " + ex
            );
        }
    }
    private static IEnumerator CoBootPlayersFromRandomVents()
    {
        VentilationSystem system = null;

        List<byte> queuedPlayerIds =
            new List<byte>();

        try
        {
            yield return null;
            yield return null;

            system = GetVentSystem();

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
                BuildPlayerGroupsByColor(players);

            Dictionary<byte, Vent> ventByColor =
                BuildVentAssignments(
                    playersByColor,
                    vents
                );

            List<int> ventsToBoot =
                new List<int>();

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

                byte colorId =
                    (byte)player.Data.DefaultOutfit.ColorId;

                if (!ventByColor.TryGetValue(
                        colorId,
                        out Vent vent))
                {
                    vent = GetRandomVent(vents);
                }

                if (vent == null)
                    continue;

                string roomName =
                    GetVentRoomKey(vent);

                byte ventId =
                    (byte)vent.Id;

                system.PlayersInsideVents[player.PlayerId] =
                    ventId;

                queuedPlayerIds.Add(
                    player.PlayerId
                );

                if (!ventsToBoot.Contains(vent.Id))
                {
                    ventsToBoot.Add(vent.Id);
                }

                Debug.Log(
                    "[RandomVentSpawn] Queued " +
                    $"{player.Data.PlayerName} " +
                    $"(color {colorId}) " +
                    $"from vent {vent.Id}, " +
                    $"room {roomName}"
                );
            }

            /*
             * Tutti i player sono già presenti nella tabella del
             * VentilationSystem. Le operazioni vengono quindi inviate
             * consecutivamente, senza yield: per il gioco avvengono
             * tutte nello stesso frame.
             */
            for (int i = 0; i < ventsToBoot.Count; i++)
            {
                VentilationSystem.Update(
                    VentilationSystem.Operation.BootImpostors,
                    ventsToBoot[i]
                );
            }

            // Lascia un frame al sistema per elaborare tutte le espulsioni.
            yield return null;
        }
        finally
        {
            if (system != null &&
                system.PlayersInsideVents != null)
            {
                for (int i = 0; i < queuedPlayerIds.Count; i++)
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

            Running = false;
        }
    }

    private static Dictionary<byte, List<PlayerControl>>
        BuildPlayerGroupsByColor(
            List<PlayerControl> players)
    {
        Dictionary<byte, List<PlayerControl>> result =
            new Dictionary<byte, List<PlayerControl>>();

        for (int i = 0;
             i < players.Count;
             i++)
        {
            PlayerControl player = players[i];

            if (player == null ||
                player.Data == null ||
                player.Data.Disconnected ||
                player.Data.IsDead)
            {
                continue;
            }

            byte colorId =
                (byte)player.Data.DefaultOutfit.ColorId;

            if (!result.TryGetValue(
                    colorId,
                    out List<PlayerControl> colorPlayers))
            {
                colorPlayers =
                    new List<PlayerControl>();

                result[colorId] =
                    colorPlayers;
            }

            colorPlayers.Add(player);
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
                duplicatedColors.Add(entry.Key);
                continue;
            }

            singleColors.Add(entry.Key);
        }

        ShuffleColorIds(singleColors);
        ShuffleColorIds(duplicatedColors);

        AssignColorsToVents(
            singleColors,
            playersByColor,
            allVents,
            assignedVents,
            result
        );

        /*
         * I colori duplicati vengono assegnati dopo gli altri:
         * tutti i player dello stesso colore ricevono la stessa vent,
         * scelta il più lontano possibile dalle vent già occupate.
         */
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
        for (int i = 0; i < colors.Count; i++)
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
                playersByColor[colorId].Count;

            Debug.Log(
                "[RandomVentSpawn] Vent " +
                $"{selectedVent.Id} assigned to color " +
                $"{colorId} ({playerCount} player(s))"
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

        Vent bestVent = null;
        float bestMinimumDistance = -1f;

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
        for (int i = 0; i < allVents.Count; i++)
        {
            Vent vent = allVents[i];

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

        for (int i = 0; i < assignedVents.Count; i++)
        {
            Vent assignedVent = assignedVents[i];

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

        for (int i = 0; i < allVents.Count; i++)
        {
            Vent vent = allVents[i];

            if (vent != null &&
                !IsVentAssigned(
                    vent,
                    assignedVents
                ))
            {
                freeVents.Add(vent);
            }
        }

        if (freeVents.Count > 0)
        {
            return GetRandomVent(freeVents);
        }

        return GetRandomVent(allVents);
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
        for (int i = colors.Count - 1;
             i > 0;
             i--)
        {
            int j =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            byte temp = colors[i];
            colors[i] = colors[j];
            colors[j] = temp;
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
                (Vector2)assignedVent.transform.position;

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
                $"room for vent {vent.Id}: {ex.Message}"
            );
        }

        return "Vent_" + vent.Id;
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

                result.Add(player);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[RandomVentSpawn] Could not collect " +
                "players: " + ex
            );
        }

        result.Sort(
            (a, b) =>
                a.PlayerId.CompareTo(b.PlayerId)
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

                vents.Add(vent);
            }

            /*
             * Fisher-Yates shuffle.
             *
             * Serve anche per rendere casuale la scelta
             * quando due vent hanno la stessa distanza.
             */
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
                "[RandomVentSpawn] Could not collect " +
                "vents: " + ex
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