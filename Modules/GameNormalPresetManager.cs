//credits and licenses in the resources folder
using System;
using System.IO;
using System.Text.Json;
using AmongUs;
using AmongUs.GameOptions;
using HarmonyLib;

namespace BanMod
{
    public class RoleOption
    {
        public int Count { get; set; } = 1;
        public int Chance { get; set; } = 100;
    }

    public class RoleSettings
    {
        public RoleOption Shapeshifter { get; set; } = new RoleOption();
        public RoleOption Phantom { get; set; } = new RoleOption();
        public RoleOption Scientist { get; set; } = new RoleOption();
        public RoleOption GuardianAngel { get; set; } = new RoleOption();
        public RoleOption Engineer { get; set; } = new RoleOption();
        public RoleOption Noisemaker { get; set; } = new RoleOption();
        public RoleOption Tracker { get; set; } = new RoleOption();
        public RoleOption Detective { get; set; } = new RoleOption();
        public RoleOption Viper { get; set; } = new RoleOption();

        public float viperDissolveTime { get; set; } = 10f;

        public float DetectiveSuspectLimit { get; set; } = 3f;

        public float EngineerCooldown { get; set; } = 5f;
        public float EngineerInVentMaxTime { get; set; } = 30f;

        public float GuardianAngelCooldown { get; set; } = 35f;
        public float GuardianAngelDuration { get; set; } = 25f;

        public float ScientistCooldown { get; set; } = 10f;
        public float ScientistBattery { get; set; } = 30f;

        public float TrackerCooldown { get; set; } = 10f;
        public float TrackerDelay { get; set; } = 0f;
        public float TrackerDuration { get; set; } = 30f;

        public bool NoisemakerAlert { get; set; } = true;
        public float NoisemakerDuration { get; set; } = 10f;

        public bool ShapeshifterLeaveSkin { get; set; } = false;
        public float ShapeshifterCooldown { get; set; } = 10f;
        public float ShapeshifterDuration { get; set; } = 30f;

        public float PhantomCooldown { get; set; } = 10f;
        public float PhantomDuration { get; set; } = 30f;
    }

    public class CustomOptions
    {
        public int MaxPlayers { get; set; } = 15;
        public int NumImpostors { get; set; } = 2;
        public float PlayerSpeedMod { get; set; } = 1.0f;
        public float CrewLightMod { get; set; } = 1.0f;
        public float ImpostorLightMod { get; set; } = 1.0f;
        public float KillCooldown { get; set; } = 15f;
        public int NumCommonTasks { get; set; } = 1;
        public int NumLongTasks { get; set; } = 1;
        public int NumShortTasks { get; set; } = 2;
        public int NumEmergencyMeetings { get; set; } = 1;
        public bool AnonymousVotes { get; set; } = false;
        public AmongUs.GameOptions.TaskBarMode TaskBarMode { get; set; } = AmongUs.GameOptions.TaskBarMode.Normal;
        public int KillDistance { get; set; } = 1;
        public int EmergencyCooldown { get; set; } = 20;
        public int DiscussionTime { get; set; } = 30;
        public int VotingTime { get; set; } = 45;
        public bool IsDefaults { get; set; } = true;
        public bool ConfirmImpostor { get; set; } = true;
        public bool VisualTasks { get; set; } = true;

        public RoleSettings Roles { get; set; } = new RoleSettings();
    }

    public enum GameModeType
    {
        RunOrDeath = 7,
        StopOrDeath = 8,
        FollowOrDeath = 9,
        SnS = 0,
        BanMod = 1,
        KaitoRun = 2,
        Default = 3,
        TaskRun = 4,
        JBMode = 5,
        FFA = 6
    }

    public enum PresetSelectionType
    {
        Default = 0,
        Preset1 = 1,
        Preset2 = 2,
        Preset3 = 3
    }

    [HarmonyPatch(typeof(NormalGameOptionsV10), nameof(NormalGameOptionsV10.SetRecommendations), new Type[] { typeof(int), typeof(bool), typeof(RulesPresets) })]
    public static class SetRecommendationsPatch
    {
        public static bool Prefix(NormalGameOptionsV10 __instance, int numPlayers, bool isOnline, RulesPresets rulesPresets)
        {
            if (GameStates.isHideNSeek)
            {
                return true;
            }

            switch (rulesPresets)
            {
                case RulesPresets.Standard:
                    SetStandardRecommendations(__instance, numPlayers, isOnline);
                    return false;
                default:
                    return true;
            }
        }

        public static void SetStandardRecommendations(NormalGameOptionsV10 __instance, int numPlayers, bool isOnline)
        {
            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
            PresetSelectionType presetSelection = (PresetSelectionType)Options.PresetSelection.GetValue();

            CustomOptions options = ResolveOptions(gameMode, presetSelection);
            ApplyToInstance(__instance, options);
        }

        private static CustomOptions ResolveOptions(GameModeType gameMode, PresetSelectionType presetSelection)
        {
            switch (presetSelection)
            {
                case PresetSelectionType.Preset1:
                    return LoadUserPresetFile(1);
                case PresetSelectionType.Preset2:
                    return LoadUserPresetFile(2);
                case PresetSelectionType.Preset3:
                    return LoadUserPresetFile(3);
                case PresetSelectionType.Default:
                default:
                    return LoadGameModePreset(gameMode);
            }
        }

        private static CustomOptions LoadGameModePreset(GameModeType gameMode)
        {
            switch (gameMode)
            {
                case GameModeType.RunOrDeath:
                    return LoadOrCreateGameModePreset("RunOrDeath.json", CreateRunOrDeathDefaults());

                case GameModeType.StopOrDeath:
                    return LoadOrCreateGameModePreset("StopOrDeath.json", CreateStopOrDeathDefaults());

                case GameModeType.FollowOrDeath:
                    return LoadOrCreateGameModePreset("FollowOrDeath.json", CreateFollowOrDeathDefaults());

                case GameModeType.SnS:
                    return LoadOrCreateGameModePreset("SnS.json", CreateSnSDefaults());

                case GameModeType.BanMod:
                    return LoadOrCreateGameModePreset("BanMod.json", CreateBanModDefaults());

                case GameModeType.KaitoRun:
                    return LoadOrCreateGameModePreset("KaitoRun.json", CreateKaitoRunDefaults());

                case GameModeType.Default:
                    return LoadOrCreateGameModePreset("Default.json", CreateDefaultDefaults());

                case GameModeType.TaskRun:
                    return LoadOrCreateGameModePreset("TaskRun.json", CreateTaskRunDefaults());

                case GameModeType.JBMode:
                    return LoadOrCreateGameModePreset("JBMode.json", CreateJBModeDefaults());

                case GameModeType.FFA:
                    return LoadOrCreateGameModePreset("FFA.json", CreateFFAModeDefaults());

                default:
                    return new CustomOptions();
            }
        }

        private static void ApplyToInstance(NormalGameOptionsV10 __instance, CustomOptions options)
        {
            __instance.MaxPlayers = options.MaxPlayers;
            __instance.NumImpostors = options.NumImpostors;
            __instance.PlayerSpeedMod = options.PlayerSpeedMod;
            __instance.CrewLightMod = options.CrewLightMod;
            __instance.ImpostorLightMod = options.ImpostorLightMod;
            __instance.KillCooldown = options.KillCooldown;
            __instance.NumCommonTasks = options.NumCommonTasks;
            __instance.NumLongTasks = options.NumLongTasks;
            __instance.NumShortTasks = options.NumShortTasks;
            __instance.NumEmergencyMeetings = options.NumEmergencyMeetings;
            __instance.AnonymousVotes = options.AnonymousVotes;
            __instance.TaskBarMode = options.TaskBarMode;
            __instance.KillDistance = options.KillDistance;
            __instance.EmergencyCooldown = options.EmergencyCooldown;
            __instance.DiscussionTime = options.DiscussionTime;
            __instance.VotingTime = options.VotingTime;
            __instance.IsDefaults = options.IsDefaults;
            __instance.ConfirmImpostor = options.ConfirmImpostor;
            __instance.VisualTasks = options.VisualTasks;

            var roles = options.Roles;

            __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, roles.Shapeshifter.Count, roles.Shapeshifter.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Phantom, roles.Phantom.Count, roles.Phantom.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, roles.Scientist.Count, roles.Scientist.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, roles.GuardianAngel.Count, roles.GuardianAngel.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, roles.Engineer.Count, roles.Engineer.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Noisemaker, roles.Noisemaker.Count, roles.Noisemaker.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Tracker, roles.Tracker.Count, roles.Tracker.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Viper, roles.Viper.Count, roles.Viper.Chance);
            __instance.roleOptions.SetRoleRate(RoleTypes.Detective, roles.Detective.Count, roles.Detective.Chance);

            if (__instance.roleOptions.TryGetRoleOptions<ViperRoleOptionsV10>(RoleTypes.Viper, out var viperOptions))
            {
                viperOptions.viperDissolveTime = roles.viperDissolveTime;
            }

            if (__instance.roleOptions.TryGetRoleOptions<DetectiveRoleOptionsV10>(RoleTypes.Detective, out var detectiveOptions))
            {
                detectiveOptions.DetectiveSuspectLimit = roles.DetectiveSuspectLimit;
            }

            if (__instance.roleOptions.TryGetRoleOptions<EngineerRoleOptionsV10>(RoleTypes.Engineer, out var engineerOptions))
            {
                engineerOptions.EngineerCooldown = roles.EngineerCooldown;
                engineerOptions.EngineerInVentMaxTime = roles.EngineerInVentMaxTime;
            }

            if (__instance.roleOptions.TryGetRoleOptions<GuardianAngelRoleOptionsV10>(RoleTypes.GuardianAngel, out var guardianAngelOptions))
            {
                guardianAngelOptions.GuardianAngelCooldown = roles.GuardianAngelCooldown;
                guardianAngelOptions.ProtectionDurationSeconds = roles.GuardianAngelDuration;
            }

            if (__instance.roleOptions.TryGetRoleOptions<ScientistRoleOptionsV10>(RoleTypes.Scientist, out var scientistOptions))
            {
                scientistOptions.ScientistCooldown = roles.ScientistCooldown;
                scientistOptions.ScientistBatteryCharge = roles.ScientistBattery;
            }

            if (__instance.roleOptions.TryGetRoleOptions<TrackerRoleOptionsV10>(RoleTypes.Tracker, out var trackerOptions))
            {
                trackerOptions.TrackerCooldown = roles.TrackerCooldown;
                trackerOptions.TrackerDelay = roles.TrackerDelay;
                trackerOptions.TrackerDuration = roles.TrackerDuration;
            }

            if (__instance.roleOptions.TryGetRoleOptions<NoisemakerRoleOptionsV10>(RoleTypes.Noisemaker, out var noisemakerOptions))
            {
                noisemakerOptions.NoisemakerImpostorAlert = roles.NoisemakerAlert;
                noisemakerOptions.NoisemakerAlertDuration = roles.NoisemakerDuration;
            }

            if (__instance.roleOptions.TryGetRoleOptions<ShapeshifterRoleOptionsV10>(RoleTypes.Shapeshifter, out var shapeshifterOptions))
            {
                shapeshifterOptions.ShapeshifterLeaveSkin = roles.ShapeshifterLeaveSkin;
                shapeshifterOptions.ShapeshifterCooldown = roles.ShapeshifterCooldown;
                shapeshifterOptions.ShapeshifterDuration = roles.ShapeshifterDuration;
            }

            if (__instance.roleOptions.TryGetRoleOptions<PhantomRoleOptionsV10>(RoleTypes.Phantom, out var phantomOptions))
            {
                phantomOptions.PhantomCooldown = roles.PhantomCooldown;
                phantomOptions.PhantomDuration = roles.PhantomDuration;
            }
        }

        public static CustomOptions LoadUserPresetFile(int number)
        {
            if (number < 1 || number > 3)
                return new CustomOptions();

            string fileName = $"Preset_{number}.json";
            string fullPath = Path.Combine("BAN_DATA", "PRESET", fileName);

            try
            {
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(fullPath))
                {
                    CustomOptions defaultOptions = new CustomOptions();
                    string json = JsonSerializer.Serialize(defaultOptions, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(fullPath, json);
                    return defaultOptions;
                }

                string content = File.ReadAllText(fullPath);
                return JsonSerializer.Deserialize<CustomOptions>(content) ?? new CustomOptions();
            }
            catch (Exception)
            {
                return new CustomOptions();
            }
        }

        public static CustomOptions LoadOrCreateGameModePreset(string fileName, CustomOptions defaultOptions)
        {
            string fullPath = Path.Combine("BAN_DATA", "GAMEMODES", fileName);

            try
            {
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(fullPath))
                {
                    string json = JsonSerializer.Serialize(defaultOptions, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(fullPath, json);
                    return defaultOptions;
                }

                string content = File.ReadAllText(fullPath);
                return JsonSerializer.Deserialize<CustomOptions>(content) ?? defaultOptions;
            }
            catch (Exception)
            {
                return defaultOptions;
            }
        }

        private static CustomOptions CreateRunOrDeathDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 1,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 1f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 10f,
                NumCommonTasks = 1,
                NumLongTasks = 1,
                NumShortTasks = 1,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 0, Chance = 100 },
                    Phantom = new RoleOption { Count = 0, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Engineer = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 15, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 1, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 5f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 0f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }

        private static CustomOptions CreateStopOrDeathDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 1,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 1f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 30f,
                NumCommonTasks = 1,
                NumLongTasks = 1,
                NumShortTasks = 1,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 1, Chance = 100 },
                    Phantom = new RoleOption { Count = 1, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Engineer = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 0, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 1, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 5f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 0f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }

        private static CustomOptions CreateFollowOrDeathDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 1,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 1f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 30f,
                NumCommonTasks = 0,
                NumLongTasks = 1,
                NumShortTasks = 0,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 1, Chance = 100 },
                    Phantom = new RoleOption { Count = 1, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Engineer = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 0, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 1, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 5f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 0f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }

        private static CustomOptions CreateSnSDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 3,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 0.001f,
                NumCommonTasks = 0,
                NumLongTasks = 0,
                NumShortTasks = 1,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = AmongUs.GameOptions.TaskBarMode.Normal,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = true,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 3, Chance = 100 },
                    Engineer = new RoleOption { Count = 15, Chance = 100 },
                    Phantom = new RoleOption { Count = 0, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 0, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 0, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 30f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 10f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }

        private static CustomOptions CreateBanModDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 3,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 17.5f,
                NumCommonTasks = 1,
                NumLongTasks = 0,
                NumShortTasks = 4,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 1, Chance = 100 },
                    Phantom = new RoleOption { Count = 1, Chance = 100 },
                    Scientist = new RoleOption { Count = 1, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 2, Chance = 100 },
                    Engineer = new RoleOption { Count = 1, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 1, Chance = 100 },
                    Tracker = new RoleOption { Count = 1, Chance = 100 },
                    Viper = new RoleOption { Count = 1, Chance = 100 },
                    Detective = new RoleOption { Count = 1, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 30f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 10f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }

        private static CustomOptions CreateKaitoRunDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 2,
                PlayerSpeedMod = 2.0f,
                CrewLightMod = 1.0f,
                ImpostorLightMod = 1.0f,
                KillCooldown = 0.1f,
                NumCommonTasks = 0,
                NumLongTasks = 0,
                NumShortTasks = 1,
                NumEmergencyMeetings = 3,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)0,
                KillDistance = 0,
                EmergencyCooldown = 60,
                DiscussionTime = 0,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 0, Chance = 100 },
                    Phantom = new RoleOption { Count = 2, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 13, Chance = 100 },
                    Engineer = new RoleOption { Count = 7, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 3, Chance = 100 },
                    Tracker = new RoleOption { Count = 3, Chance = 100 },
                    Viper = new RoleOption { Count = 0, Chance = 100 },
                    Detective = new RoleOption { Count = 1, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 5f,
                    GuardianAngelCooldown = 0f,
                    GuardianAngelDuration = 255f,
                    ScientistCooldown = 0f,
                    ScientistBattery = 255f,
                    TrackerCooldown = 0f,
                    TrackerDelay = 0f,
                    TrackerDuration = 90f,
                    NoisemakerAlert = false,
                    NoisemakerDuration = 3f,
                    ShapeshifterLeaveSkin = true,
                    ShapeshifterCooldown = 0f,
                    ShapeshifterDuration = 255f,
                    PhantomCooldown = 5f,
                    PhantomDuration = 10f
                }
            };
        }
        private static CustomOptions CreateDefaultDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 3,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 17.5f,
                NumCommonTasks = 1,
                NumLongTasks = 0,
                NumShortTasks = 4,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 1, Chance = 100 },
                    Phantom = new RoleOption { Count = 1, Chance = 100 },
                    Scientist = new RoleOption { Count = 1, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 2, Chance = 100 },
                    Engineer = new RoleOption { Count = 1, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 1, Chance = 100 },
                    Tracker = new RoleOption { Count = 1, Chance = 100 },
                    Viper = new RoleOption { Count = 1, Chance = 100 },
                    Detective = new RoleOption { Count = 1, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 30f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 10f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }
        private static CustomOptions CreateTaskRunDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 1,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 17.5f,
                NumCommonTasks = 1,
                NumLongTasks = 2,
                NumShortTasks = 4,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 0,
                VotingTime = 90,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 0, Chance = 100 },
                    Phantom = new RoleOption { Count = 0, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Engineer = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 0, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 0, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 10f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 30f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 10f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }
        private static CustomOptions CreateJBModeDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 3,
                PlayerSpeedMod = 2.25f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 10f,
                NumCommonTasks = 1,
                NumLongTasks = 0,
                NumShortTasks = 3,
                NumEmergencyMeetings = 2,
                AnonymousVotes = true,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 30,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 1, Chance = 100 },
                    Phantom = new RoleOption { Count = 0, Chance = 0 },
                    Scientist = new RoleOption { Count = 0, Chance = 0 },
                    GuardianAngel = new RoleOption { Count = 2, Chance = 100 },
                    Engineer = new RoleOption { Count = 3, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 2, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 0 },
                    Viper = new RoleOption { Count = 2, Chance = 100 },
                    Detective = new RoleOption { Count = 1, Chance = 100 },

                    viperDissolveTime = 5f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 65f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 30f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 5f,
                    ShapeshifterDuration = 0f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }
        private static CustomOptions CreateFFAModeDefaults()
        {
            return new CustomOptions
            {
                MaxPlayers = 15,
                NumImpostors = 3,
                PlayerSpeedMod = 1.75f,
                CrewLightMod = 0.75f,
                ImpostorLightMod = 2.0f,
                KillCooldown = 10f,
                NumCommonTasks = 0,
                NumLongTasks = 0,
                NumShortTasks = 0,
                NumEmergencyMeetings = 2,
                AnonymousVotes = false,
                TaskBarMode = (AmongUs.GameOptions.TaskBarMode)1,
                KillDistance = 0,
                EmergencyCooldown = 15,
                DiscussionTime = 45,
                VotingTime = 60,
                IsDefaults = true,
                ConfirmImpostor = true,
                VisualTasks = false,
                Roles = new RoleSettings
                {
                    Shapeshifter = new RoleOption { Count = 0, Chance = 100 },
                    Phantom = new RoleOption { Count = 0, Chance = 100 },
                    Scientist = new RoleOption { Count = 0, Chance = 100 },
                    GuardianAngel = new RoleOption { Count = 0, Chance = 100 },
                    Engineer = new RoleOption { Count = 0, Chance = 100 },
                    Noisemaker = new RoleOption { Count = 0, Chance = 100 },
                    Tracker = new RoleOption { Count = 0, Chance = 100 },
                    Viper = new RoleOption { Count = 15, Chance = 100 },
                    Detective = new RoleOption { Count = 0, Chance = 100 },

                    viperDissolveTime = 1f,
                    DetectiveSuspectLimit = 3f,
                    EngineerCooldown = 5f,
                    EngineerInVentMaxTime = 30f,
                    GuardianAngelCooldown = 35f,
                    GuardianAngelDuration = 25f,
                    ScientistCooldown = 10f,
                    ScientistBattery = 30f,
                    TrackerCooldown = 10f,
                    TrackerDelay = 0f,
                    TrackerDuration = 30f,
                    NoisemakerAlert = true,
                    NoisemakerDuration = 10f,
                    ShapeshifterLeaveSkin = false,
                    ShapeshifterCooldown = 10f,
                    ShapeshifterDuration = 30f,
                    PhantomCooldown = 10f,
                    PhantomDuration = 30f
                }
            };
        }
    }
}