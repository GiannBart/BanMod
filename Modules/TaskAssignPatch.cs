//credits and licenses in the resources folder
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using static NetworkedPlayerInfo;
using Random = UnityEngine.Random;

namespace BanMod
{
    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    internal static class RpcSetTasksPatch
    {
        private static Il2CppSystem.Collections.Generic.List<byte> generatedCommonTasks;
        private static Il2CppSystem.Collections.Generic.List<byte> generatedSharedTasks;
        public static class TaskMaxState
        {
            public static int SubmitScanCount;

            public static void Reset()
            {
                SubmitScanCount = 0;
            }
        }
        public static bool Prefix(NetworkedPlayerInfo __instance, ref Il2CppStructArray<byte> taskTypeIds)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            PlayerControl pc = __instance.Object;
            if (pc == null) return true;

            bool shareAllTasks = Options.SharedAllTasks != null && Options.SharedAllTasks.GetBool();

            if (shareAllTasks)
            {
                if (generatedSharedTasks == null)
                {
                    generatedSharedTasks = GenerateSharedTaskList();
                }

                if (
                    (Watcher.WatcherSelected && pc.PlayerId == Watcher.WatcherId) ||
                    ((BanMod.GM.Value || ForcedRoleSystem.GM) && pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                )
                {
                    if (generatedSharedTasks != null && generatedSharedTasks.Count > 0)
                    {
                        taskTypeIds = new Il2CppStructArray<byte>(1);
                        taskTypeIds[0] = generatedSharedTasks[0];

                        BMLogger.LogInfo($"[TaskPatch] {pc.Data.PlayerName}: assegnata solo la prima shared/common task. TaskId={generatedSharedTasks[0]}");
                        return true;
                    }

                    taskTypeIds = new Il2CppStructArray<byte>(1);
                    BMLogger.LogWarning($"[TaskPatch] {pc.Data.PlayerName}: nessuna shared/common task disponibile, assegnate 0 task.");
                    return true;
                }

                taskTypeIds = (Il2CppStructArray<byte>)generatedSharedTasks.ToArray();
                return true;
            }

            if (generatedCommonTasks == null)
            {
                var commonTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();

                foreach (var task in ShipStatus.Instance.CommonTasks)
                    commonTasks.Add(task);

                commonTasks = FilterTasks(commonTasks);
                Shuffle(commonTasks);

                generatedCommonTasks = new Il2CppSystem.Collections.Generic.List<byte>();

                var usedCommonTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();
                int startCommon = 0;

                AddValidTasks(
                    ref startCommon,
                    BanMod.NormalOptions.NumCommonTasks,
                    generatedCommonTasks,
                    usedCommonTypes,
                    commonTasks,
                    "Common"
                );
            }

            if (
                (Watcher.WatcherSelected && pc.PlayerId == Watcher.WatcherId) ||
                ((BanMod.GM.Value || ForcedRoleSystem.GM) && pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            )
            {
                if (generatedCommonTasks != null && generatedCommonTasks.Count > 0)
                {
                    taskTypeIds = new Il2CppStructArray<byte>(1);
                    taskTypeIds[0] = generatedCommonTasks[0];

                    BMLogger.LogInfo($"[TaskPatch] {pc.Data.PlayerName}: assegnata solo la common task condivisa. TaskId={generatedCommonTasks[0]}");
                    return true;
                }

                taskTypeIds = new Il2CppStructArray<byte>(0);
                BMLogger.LogWarning($"[TaskPatch] {pc.Data.PlayerName}: nessuna common task disponibile, assegnate 0 task.");
                return true;
            }

            var tasksList = new Il2CppSystem.Collections.Generic.List<byte>();

            foreach (var taskIndex in generatedCommonTasks)
            {
                tasksList.Add(taskIndex);
            }

            var longTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var task in ShipStatus.Instance.LongTasks)
                longTasks.Add(task);

            longTasks = FilterTasks(longTasks);
            Shuffle(longTasks);

            var usedTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();
            int startLong = 0;
            AddValidTasks(ref startLong, BanMod.NormalOptions.NumLongTasks, tasksList, usedTypes, longTasks, "Long");

            var shortTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var task in ShipStatus.Instance.ShortTasks)
                shortTasks.Add(task);

            shortTasks = FilterTasks(shortTasks);
            Shuffle(shortTasks);

            int startShort = 0;
            AddValidTasks(ref startShort, BanMod.NormalOptions.NumShortTasks, tasksList, usedTypes, shortTasks, "Short");

            BMLogger.LogInfo($"[Debug] CommonTasks (shared): {generatedCommonTasks.Count}, Long: {BanMod.NormalOptions.NumLongTasks}, Short: {BanMod.NormalOptions.NumShortTasks}");

            taskTypeIds = (Il2CppStructArray<byte>)tasksList.ToArray();

            return true;
        }
        private static Il2CppSystem.Collections.Generic.List<byte> GenerateSharedTaskList()
        {
            var result = new Il2CppSystem.Collections.Generic.List<byte>();
            var usedTypes = new Il2CppSystem.Collections.Generic.HashSet<TaskTypes>();

            var common = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var t in ShipStatus.Instance.CommonTasks)
                common.Add(t);

            common = FilterTasks(common);
            common = RemoveSubmitScan(common);
            Shuffle(common);

            int start = 0;
            AddValidTasks(ref start, BanMod.NormalOptions.NumCommonTasks, result, usedTypes, common, "Common");

            var longTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var t in ShipStatus.Instance.LongTasks)
                longTasks.Add(t);

            longTasks = FilterTasks(longTasks);
            longTasks = RemoveSubmitScan(longTasks);
            Shuffle(longTasks);

            start = 0;
            AddValidTasks(ref start, BanMod.NormalOptions.NumLongTasks, result, usedTypes, longTasks, "Long");

            var shortTasks = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var t in ShipStatus.Instance.ShortTasks)
                shortTasks.Add(t);

            shortTasks = FilterTasks(shortTasks);
            shortTasks = RemoveSubmitScan(shortTasks);
            Shuffle(shortTasks);

            start = 0;
            AddValidTasks(ref start, BanMod.NormalOptions.NumShortTasks, result, usedTypes, shortTasks, "Short");

            BMLogger.LogInfo($"[TaskPatch] Generated SHARED task list ({result.Count} tasks)");

            return result;
        }
        private static Il2CppSystem.Collections.Generic.List<NormalPlayerTask> FilterTasks(Il2CppSystem.Collections.Generic.List<NormalPlayerTask> original)
        {
            var filtered = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();
            foreach (var task in original)
            {
                if (AddTasksFromListPatch.DisableTasksSettings.TryGetValue(task.TaskType, out var opt) && opt != null && opt.GetBool())
                {
                    BMLogger.LogInfo($"[DisableTasks] Rimosso task {task.TaskType}");
                    continue;
                }
                filtered.Add(task);
            }
            return filtered;
        }
        private static Il2CppSystem.Collections.Generic.List<NormalPlayerTask> RemoveSubmitScan(Il2CppSystem.Collections.Generic.List<NormalPlayerTask> original)
        {
            var filtered = new Il2CppSystem.Collections.Generic.List<NormalPlayerTask>();

            foreach (var task in original)
            {
                if (task.TaskType == TaskTypes.SubmitScan)
                    continue;

                filtered.Add(task);
            }

            return filtered;
        }
        private static void AddValidTasks(
            ref int startIndex,
            int count,
            Il2CppSystem.Collections.Generic.List<byte> tasksList,
            Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTypes,
            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> taskPool,
            string taskCategory)
        {
            BMLogger.LogInfo($"[TaskPatch] Aggiungo {count} {taskCategory} tasks da un pool di {taskPool.Count} disponibili.");
            int added = 0;

            if (count <= 0)
            {
                BMLogger.LogInfo($"[TaskPatch] count = 0 per categoria {taskCategory}, salto.");
                return;
            }

            foreach (var task in taskPool)
            {
                if (added >= count) break;

                if (usedTypes.Contains(task.TaskType)) continue;

                NormalPlayerTask realTask = ShipStatus.Instance.GetTaskById((byte)task.Index);
                if (task.TaskType == TaskTypes.SubmitScan)
                {
                    int max = Options.SubmitScanMax.GetInt();

                    if (max == -1)
                    {
                        int playerCount = GameData.Instance != null
                            ? GameData.Instance.PlayerCount
                            : 10;

                        int vanillaMax = playerCount >= 11 ? 4 : 3;

                        if (TaskMaxState.SubmitScanCount >= vanillaMax)
                            continue;
                    }
                    else
                    {
                        if (TaskMaxState.SubmitScanCount >= max)
                            continue;
                    }
                }
                if (realTask == null)
                {
                    BMLogger.LogWarning($"[TaskPatch] Task con Index {task.Index} non trovato in ShipStatus, skipping.");
                    continue;
                }

                tasksList.Add((byte)task.Index);
                usedTypes.Add(task.TaskType);
                startIndex++;
                added++;
                if (task.TaskType == TaskTypes.SubmitScan)
                {
                    TaskMaxState.SubmitScanCount++;
                }
            }

            if (added < count)
            {
                BMLogger.LogWarning($"[TaskPatch] Non abbastanza {taskCategory} tasks disponibili dopo il filtro. Richiesti {count}, ottenuti {added}.");
            }
        }

        private static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int j = Random.Range(i, n);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        public static void ResetGeneratedCommonTasks()
        {
            generatedCommonTasks = null;
            generatedSharedTasks = null;
            TaskMaxState.Reset();
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    internal static class AddTasksFromListPatch
    {
        public static System.Collections.Generic.Dictionary<TaskTypes, OptionItem> DisableTasksSettings = new System.Collections.Generic.Dictionary<TaskTypes, OptionItem>();

        public static void Prefix([HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (DisableTasksSettings.Count == 0)
            {
                DisableTasksSettings = new System.Collections.Generic.Dictionary<TaskTypes, OptionItem>
                {
                    [TaskTypes.SubmitScan] = Options.DisableSubmitScan,
                    [TaskTypes.PrimeShields] = Options.DisablePrimeShields,
                    [TaskTypes.FuelEngines] = Options.DisableFuelEngines,
                    [TaskTypes.ChartCourse] = Options.DisableChartCourse,
                    [TaskTypes.StartReactor] = Options.DisableStartReactor,
                    [TaskTypes.SwipeCard] = Options.DisableSwipeCard,
                    [TaskTypes.ClearAsteroids] = Options.DisableClearAsteroids,
                    [TaskTypes.UploadData] = Options.DisableUploadData,
                    [TaskTypes.InspectSample] = Options.DisableInspectSample,
                    [TaskTypes.EmptyChute] = Options.DisableEmptyChute,
                    [TaskTypes.EmptyGarbage] = Options.DisableEmptyGarbage,
                    [TaskTypes.AlignEngineOutput] = Options.DisableAlignEngineOutput,
                    [TaskTypes.FixWiring] = Options.DisableFixWiring,
                    [TaskTypes.CalibrateDistributor] = Options.DisableCalibrateDistributor,
                    [TaskTypes.DivertPower] = Options.DisableDivertPower,
                    [TaskTypes.UnlockManifolds] = Options.DisableUnlockManifolds,
                    [TaskTypes.CleanO2Filter] = Options.DisableCleanO2Filter,
                    [TaskTypes.StabilizeSteering] = Options.DisableStabilizeSteering,
                    [TaskTypes.AssembleArtifact] = Options.DisableAssembleArtifact,
                    [TaskTypes.SortSamples] = Options.DisableSortSamples,
                    [TaskTypes.MeasureWeather] = Options.DisableMeasureWeather,
                    [TaskTypes.EnterIdCode] = Options.DisableEnterIdCode,
                    [TaskTypes.BuyBeverage] = Options.DisableBuyBeverage,
                    [TaskTypes.ProcessData] = Options.DisableProcessData,
                    [TaskTypes.RunDiagnostics] = Options.DisableRunDiagnostics,
                    [TaskTypes.WaterPlants] = Options.DisableWaterPlants,
                    [TaskTypes.MonitorOxygen] = Options.DisableMonitorTree,
                    [TaskTypes.StoreArtifacts] = Options.DisableStoreArtifacts,
                    [TaskTypes.FillCanisters] = Options.DisableFillCanisters,
                    [TaskTypes.FixWeatherNode] = Options.DisableActivateWeatherNodes,
                    [TaskTypes.InsertKeys] = Options.DisableInsertKeys,
                    [TaskTypes.ScanBoardingPass] = Options.DisableScanBoardingPass,
                    [TaskTypes.OpenWaterways] = Options.DisableOpenWaterways,
                    [TaskTypes.ReplaceWaterJug] = Options.DisableReplaceWaterJug,
                    [TaskTypes.RepairDrill] = Options.DisableRepairDrill,
                    [TaskTypes.AlignTelescope] = Options.DisableAlignTelescope,
                    [TaskTypes.RecordTemperature] = Options.DisableRecordTemperature,
                    [TaskTypes.RebootWifi] = Options.DisableRebootWifi,
                    [TaskTypes.PolishRuby] = Options.DisablePolishRuby,
                    [TaskTypes.ResetBreakers] = Options.DisableResetBreaker,
                    [TaskTypes.Decontaminate] = Options.DisableDecontaminate,
                    [TaskTypes.MakeBurger] = Options.DisableMakeBurger,
                    [TaskTypes.UnlockSafe] = Options.DisableUnlockSafe,
                    [TaskTypes.SortRecords] = Options.DisableSortRecords,
                    [TaskTypes.PutAwayPistols] = Options.DisablePutAwayPistols,
                    [TaskTypes.FixShower] = Options.DisableFixShower,
                    [TaskTypes.CleanToilet] = Options.DisableCleanToilet,
                    [TaskTypes.DressMannequin] = Options.DisableDressMannequin,
                    [TaskTypes.PickUpTowels] = Options.DisablePickUpTowels,
                    [TaskTypes.RewindTapes] = Options.DisableRewindTapes,
                    [TaskTypes.StartFans] = Options.DisableStartFans,
                    [TaskTypes.DevelopPhotos] = Options.DisableDevelopPhotos,
                    [TaskTypes.PutAwayRifles] = Options.DisablePutAwayRifles,
                    [TaskTypes.VentCleaning] = Options.DisableCleanVent,
                    [TaskTypes.BuildSandcastle] = Options.DisableBuildSandcastle,
                    [TaskTypes.CatchFish] = Options.DisableCatchFish,
                    [TaskTypes.CollectShells] = Options.DisableCollectShells,
                    [TaskTypes.LiftWeights] = Options.DisableLiftWeights,
                    [TaskTypes.RoastMarshmallow] = Options.DisableRoastMarshmallow,
                    [TaskTypes.TestFrisbee] = Options.DisableThrowFisbee,
                    [TaskTypes.CollectSamples] = Options.DisableCollectSamples,
                    [TaskTypes.CollectVegetables] = Options.DisableCollectVegetables,
                    [TaskTypes.HoistSupplies] = Options.DisableHoistSupplies,
                    [TaskTypes.MineOres] = Options.DisableMineOres,
                    [TaskTypes.PolishGem] = Options.DisablePolishGem,
                    [TaskTypes.ReplaceParts] = Options.DisableReplaceParts,
                    [TaskTypes.HelpCritter] = Options.DisableHelpCritter,
                    [TaskTypes.CrankGenerator] = Options.DisableCrankGenerator,
                    [TaskTypes.FixAntenna] = Options.DisableFixAntenna,
                    [TaskTypes.TuneRadio] = Options.DisableFindSignal,
                    [TaskTypes.ExtractFuel] = Options.DisableExtractFuel,
                    [TaskTypes.MonitorMushroom] = Options.DisableMonitorMushroom,
                    [TaskTypes.PlayVideogame] = Options.DisablePlayVideoGame,
                };
            }
        }
    }
}
