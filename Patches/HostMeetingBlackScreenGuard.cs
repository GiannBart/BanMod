//using HarmonyLib;
//using UnityEngine;

//namespace BanMod
//{
//    public static class HostSafePlayerCounts
//    {
//        public static bool Enabled { get; set; } = true;

//        internal static bool IsProtectedMode()
//        {
//            if (!Enabled)
//                return false;

//            if (AmongUsClient.Instance == null ||
//                !AmongUsClient.Instance.AmHost)
//                return false;

//            try
//            {
//                return Options.GameMode.Selected == GameModeType.FFA;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        internal static int CountRealAlivePlayers()
//        {
//            int alive = 0;

//            try
//            {
//                if (GameData.Instance == null)
//                    return 0;

//                for (int i = 0; i < GameData.Instance.PlayerCount; i++)
//                {
//                    NetworkedPlayerInfo player = GameData.Instance.AllPlayers[i];

//                    if (player == null)
//                        continue;

//                    if (player.Disconnected)
//                        continue;

//                    if (player.IsDead)
//                        continue;

//                    alive++;
//                }
//            }
//            catch
//            {
//            }

//            return alive;
//        }
//    }

//    [HarmonyPatch(typeof(LogicGameFlow), "GetPlayerCounts")]
//    internal static class HostSafePlayerCounts_GetPlayerCountsPatch
//    {
//        [HarmonyPostfix]
//        [HarmonyPriority(Priority.Last)]
//        private static void Postfix(
//            ref Il2CppSystem.ValueTuple<int, int, int> __result)
//        {
//            if (!HostSafePlayerCounts.IsProtectedMode())
//                return;

//            int originalHumans = __result.Item1;
//            int originalImpostors = __result.Item2;
//            int originalImpostorCount = __result.Item3;

//            int alivePlayers = HostSafePlayerCounts.CountRealAlivePlayers();

//            if (alivePlayers <= 0)
//                return;

//            bool unsafeCounts = originalHumans <= originalImpostors;

//            if (!unsafeCounts)
//                return;

//            int safeHumans;
//            int safeImpostors;
//            int safeImpostorCount;

//            if (alivePlayers <= 2)
//            {
//                safeHumans = 2;
//                safeImpostors = 1;
//            }
//            else
//            {
//                safeImpostors = (alivePlayers - 1) / 2;

//                if (safeImpostors < 1)
//                    safeImpostors = 1;

//                safeHumans = alivePlayers - safeImpostors;

//                if (safeHumans <= safeImpostors)
//                {
//                    safeImpostors = 1;
//                    safeHumans = alivePlayers - 1;
//                }
//            }

//            safeImpostorCount = safeImpostors;

//            Debug.Log(
//                "[HostSafePlayerCounts FFA] realAlive=" + alivePlayers +
//                " original=(" +
//                originalHumans + "," +
//                originalImpostors + "," +
//                originalImpostorCount + ")" +
//                " -> safe=(" +
//                safeHumans + "," +
//                safeImpostors + "," +
//                safeImpostorCount + ")"
//            );

//            __result = new Il2CppSystem.ValueTuple<int, int, int>(
//                safeHumans,
//                safeImpostors,
//                safeImpostorCount
//            );
//        }
//    }
//}
using BanMod;
using HarmonyLib;
using UnityEngine;

namespace BanMod
{
    public static class HostSafePlayerCounts
    {
        public static bool Enabled { get; set; } = true;

        internal static bool IsFfaMode()
        {
            if (!Enabled)
                return false;

            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost)
                return false;

            try
            {
                return Options.GameMode.Selected == GameModeType.FFA;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsZeroImpostorMode()
        {
            if (!Enabled)
                return false;

            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost)
                return false;

            try
            {
                GameModeType mode = Options.GameMode.Selected;

                return mode == GameModeType.TaskRun ||
                       mode == GameModeType.ZombieMode ||
                       mode == GameModeType.HotPotato;
            }
            catch
            {
                return false;
            }
        }

        internal static int CountRealAlivePlayers()
        {
            int alive = 0;

            try
            {
                if (GameData.Instance == null)
                    return 0;

                for (int i = 0; i < GameData.Instance.PlayerCount; i++)
                {
                    NetworkedPlayerInfo player =
                        GameData.Instance.AllPlayers[i];

                    if (player == null)
                        continue;

                    if (player.Disconnected)
                        continue;

                    if (player.IsDead)
                        continue;

                    alive++;
                }
            }
            catch
            {
            }

            return alive;
        }
    }

    [HarmonyPatch(typeof(LogicGameFlow), "GetPlayerCounts")]
    internal static class HostSafePlayerCounts_GetPlayerCountsPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            ref Il2CppSystem.ValueTuple<int, int, int> __result)
        {
            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost)
            {
                return;
            }

            int originalHumans =
                __result.Item1;

            int originalImpostors =
                __result.Item2;

            int originalImpostorCount =
                __result.Item3;

            int alivePlayers =
                HostSafePlayerCounts.CountRealAlivePlayers();

            if (alivePlayers <= 0)
                return;

            if (HostSafePlayerCounts.IsFfaMode())
            {
                bool unsafeCounts =
                    originalHumans <= originalImpostors;

                if (!unsafeCounts)
                    return;

                int safeHumans;
                int safeImpostors;
                int safeImpostorCount;

                if (alivePlayers <= 2)
                {
                    safeHumans = 2;
                    safeImpostors = 1;
                }
                else
                {
                    safeImpostors =
                        (alivePlayers - 1) / 2;

                    if (safeImpostors < 1)
                        safeImpostors = 1;

                    safeHumans =
                        alivePlayers - safeImpostors;

                    if (safeHumans <= safeImpostors)
                    {
                        safeImpostors = 1;
                        safeHumans = alivePlayers - 1;
                    }
                }

                safeImpostorCount =
                    safeImpostors;

                Debug.Log(
                    "[HostSafePlayerCounts FFA] realAlive=" +
                    alivePlayers +
                    " original=(" +
                    originalHumans + "," +
                    originalImpostors + "," +
                    originalImpostorCount + ")" +
                    " -> safe=(" +
                    safeHumans + "," +
                    safeImpostors + "," +
                    safeImpostorCount + ")"
                );

                __result =
                    new Il2CppSystem.ValueTuple<int, int, int>(
                        safeHumans,
                        safeImpostors,
                        safeImpostorCount
                    );

                return;
            }

            if (HostSafePlayerCounts.IsZeroImpostorMode())
            {
                bool zeroRealImpostors =
                    originalImpostors <= 0 ||
                    originalImpostorCount <= 0;

                if (!zeroRealImpostors)
                    return;

                const int safeHumans = 2;
                const int safeImpostors = 1;
                const int safeImpostorCount = 1;

                Debug.Log(
                    "[HostSafePlayerCounts ZERO-IMP] " +
                    "original=(" +
                    originalHumans + "," +
                    originalImpostors + "," +
                    originalImpostorCount + ")" +
                    " -> safe=(2,1,1)"
                );

                __result =
                    new Il2CppSystem.ValueTuple<int, int, int>(
                        safeHumans,
                        safeImpostors,
                        safeImpostorCount
                    );

                return;
            }
        }
    }
}
[HarmonyPatch(
    typeof(LogicGameFlowNormal),
    nameof(LogicGameFlowNormal.IsGameOverDueToDeath)
)]
internal static class HostSafePlayerCounts_IsGameOverDueToDeathPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ref bool __result)
    {
        if (!HostSafePlayerCounts.Enabled)
            return true;

        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        try
        {
            GameModeType mode =
                Options.GameMode.Selected;

            if (mode == GameModeType.TaskRun ||
                mode == GameModeType.ZombieMode ||
                mode == GameModeType.HotPotato)
            {
                __result = false;
                return false;
            }
        }
        catch
        {
        }

        return true;
    }
}