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

            if (Options.RandomVentSpawn == null ||
                !Options.RandomVentSpawn.GetBool())
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

    private static IEnumerator CoBootPlayersFromRandomVents()
    {
        try
        {
            yield return null;
            yield return null;

            VentilationSystem system = GetVentSystem();

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

            Dictionary<byte, Vent> ventsByColor =
                new Dictionary<byte, Vent>();

            int nextVentIndex = 0;

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

                Vent vent;

                if (!ventsByColor.TryGetValue(
                        colorId,
                        out vent))
                {
                    vent =
                        vents[nextVentIndex % vents.Count];

                    ventsByColor[colorId] = vent;
                    nextVentIndex++;
                }

                if (vent == null)
                    continue;

                byte ventId =
                    (byte)vent.Id;


                system.PlayersInsideVents[player.PlayerId] =
                    ventId;

                Debug.Log(
                    "[RandomVentSpawn] Booting " +
                    $"{player.Data.PlayerName} " +
                    $"from vent {vent.Id}"
                );

                VentilationSystem.Update(
                    VentilationSystem.Operation.BootImpostors,
                    vent.Id
                );

                yield return new WaitForSeconds(
                    UnityEngine.Random.Range(
                        0.05f,
                        0.15f
                    )
                );

                try
                {
                    if (system.PlayersInsideVents.ContainsKey(
                        player.PlayerId))
                    {
                        system.PlayersInsideVents.Remove(
                            player.PlayerId
                        );
                    }
                }
                catch
                {
                }
            }
        }
        finally
        {
            Running = false;
        }
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
        catch
        {
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

            for (int i = vents.Count - 1;
                 i > 0;
                 i--)
            {
                int j =
                    UnityEngine.Random.Range(
                        0,
                        i + 1
                    );

                Vent temp = vents[i];
                vents[i] = vents[j];
                vents[j] = temp;
            }
        }
        catch
        {
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
