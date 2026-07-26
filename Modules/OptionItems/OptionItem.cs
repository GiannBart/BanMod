//credits and licenses in the resources folder
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using UnityEngine;

namespace BanMod
{
    public enum MainTab
    {
        Settings,
        Ban,
        Modded,
        Other
    }

    public static class OptionCategoryExtensions
    {
        public static MainTab GetDefaultMainTab(this OptionCategory category)
        {
            switch (category)
            {
                // HOST / impostazioni generali
                case OptionCategory.GameMode:
                case OptionCategory.GameModeHnS:
                case OptionCategory.SNS:
                case OptionCategory.FFA:
                case OptionCategory.General:
                case OptionCategory.Host:
                case OptionCategory.Name:
                case OptionCategory.Visual:
                    return MainTab.Settings;

                // KICK/BAN / controlli
                case OptionCategory.Afk:
                case OptionCategory.Blocklist:
                case OptionCategory.Wordlist:
                case OptionCategory.Spamlist:
                case OptionCategory.Cam:
                case OptionCategory.CamTask:
                case OptionCategory.Follow:
                case OptionCategory.Cheat:
                    return MainTab.Ban;

                // GENERALI / gameplay e task
                case OptionCategory.GeneralModded:
                case OptionCategory.Game:
                case OptionCategory.Task:
                case OptionCategory.Common:
                case OptionCategory.Short:
                case OptionCategory.Long:
                case OptionCategory.SabotageOption:
                case OptionCategory.Sabotage:
                    return MainTab.Modded;

                // RUOLI
                case OptionCategory.Seeker:
                case OptionCategory.Role:
                case OptionCategory.Impostor:
                case OptionCategory.Engineer:
                case OptionCategory.Watcher:
                case OptionCategory.Tracker:
                case OptionCategory.Scientist:
                case OptionCategory.Guesser:
                case OptionCategory.Jester:
                case OptionCategory.Exiler:
                case OptionCategory.Profiler:
                case OptionCategory.Judge:
                case OptionCategory.Phantom:
                case OptionCategory.Shapeshifter:
                case OptionCategory.Immortal:
                case OptionCategory.PhantomModded:
                case OptionCategory.Sheriff:
                case OptionCategory.ShapeshifterModded:
                    case OptionCategory.EngineerModded:
                    case OptionCategory.CustomRoles:
                        return MainTab.Other;

                    default:
                        return MainTab.Settings;
                    }
        }

        public static string GetHeaderKey(this OptionCategory category)
        {
            switch (category)
            {
                case OptionCategory.Host: return "TabGroup.Host";
                case OptionCategory.General: return "TabGroup.Gen";
                case OptionCategory.Name: return "TabGroup.Name";
                case OptionCategory.Visual: return "TabGroup.Visual";
                case OptionCategory.Afk: return "TabGroup.Afk";
                case OptionCategory.Blocklist: return "TabGroup.Block";
                case OptionCategory.Wordlist: return "TabGroup.Word";
                case OptionCategory.Spamlist: return "TabGroup.Spam";
                case OptionCategory.Cam: return "TabGroup.Cam";
                case OptionCategory.CamTask: return "TabGroup.CamTask";
                case OptionCategory.Follow: return "TabGroup.Follow";
                case OptionCategory.Cheat: return "TabGroup.Cheat";
                case OptionCategory.Game: return "TabGroup.Game";
                case OptionCategory.Common: return "TabGroup.Common";
                case OptionCategory.Short: return "TabGroup.Short";
                case OptionCategory.Long: return "TabGroup.Long";
                case OptionCategory.Sabotage: return "TabGroup.Sabotage";
                case OptionCategory.SabotageOption: return "TabGroup.SabotageOption";
                case OptionCategory.Role: return "TabGroup.ModdedRole";
                case OptionCategory.Guesser: return "TabGroup.guesser";
                case OptionCategory.Jester: return "TabGroup.Jester";
                case OptionCategory.Impostor: return "TabGroup.Impostor";
                case OptionCategory.Watcher: return "TabGroup.Watcher";
                case OptionCategory.Engineer: return "TabGroup.Engineer";
                case OptionCategory.Profiler: return "TabGroup.Profiler";
                case OptionCategory.Judge: return "TabGroup.Judge";
                case OptionCategory.Tracker: return "TabGroup.Tracker";
                case OptionCategory.Scientist: return "TabGroup.Scientist";
                case OptionCategory.Sheriff: return "TabGroup.Sheriff";
                case OptionCategory.Exiler: return "TabGroup.Exiler";
                case OptionCategory.Immortal: return "TabGroup.Immortal";
                case OptionCategory.Seeker: return "Seeker";
                case OptionCategory.SNS: return "TabGroup.SNS";
                case OptionCategory.FFA: return "FFA";
                case OptionCategory.GameMode: return "TabGroup.GameMode";
                case OptionCategory.GameModeHnS: return "TabGroup.GameMode";
                case OptionCategory.Phantom: return "TabGroup.Phantom";
                case OptionCategory.Task: return "TabGroup.Task";
                case OptionCategory.Shapeshifter: return "TabGroup.Shape";
                case OptionCategory.GeneralModded: return "TabGroup.Modded";
                case OptionCategory.PhantomModded: return "TabGroup.PhantomModded";
                case OptionCategory.ShapeshifterModded: return "TabGroup.ShapeshifterModded";
                case OptionCategory.EngineerModded: return "TabGroup.EngineerModded";
                case OptionCategory.CustomRoles: return "TabGroup.CustomRoles";
                default: return category.ToString();
            }
        }
    }

    public abstract class OptionItem
    {
        #region static
        public static IReadOnlyList<OptionItem> AllOptions => _allOptions;
        private static List<OptionItem> _allOptions = new(1024);

        private static Dictionary<OptionCategory, List<OptionItem>> _categorizedOptions = new Dictionary<OptionCategory, List<OptionItem>>();
        private static int _nextAutoId = 990000;

        public static int NextAutoId()
        {
            while (_fastOptions.ContainsKey(_nextAutoId))
                _nextAutoId++;
            return _nextAutoId++;
        }

        public static IReadOnlyList<OptionItem> GetOptions(OptionCategory category)
            => _categorizedOptions.GetValueOrDefault(category, new List<OptionItem>());

        public static IReadOnlyList<OptionItem> SeekerOptions => GetOptions(OptionCategory.Seeker);
        public static IReadOnlyList<OptionItem> SNSOptions => GetOptions(OptionCategory.SNS);
        public static IReadOnlyList<OptionItem> GameModeOptions => GetOptions(OptionCategory.GameMode);
        public static IReadOnlyList<OptionItem> RoleOptions => GetOptions(OptionCategory.Role);
        public static IReadOnlyList<OptionItem> CheatOptions => GetOptions(OptionCategory.Cheat);
        public static IReadOnlyList<OptionItem> BlocklistOptions => GetOptions(OptionCategory.Blocklist);
        public static IReadOnlyList<OptionItem> AfkOptions => GetOptions(OptionCategory.Afk);
        public static IReadOnlyList<OptionItem> CamOptions => GetOptions(OptionCategory.Cam);
        public static IReadOnlyList<OptionItem> CamTaskOptions => GetOptions(OptionCategory.CamTask);
        public static IReadOnlyList<OptionItem> WordlistOptions => GetOptions(OptionCategory.Wordlist);
        public static IReadOnlyList<OptionItem> SpamlistOptions => GetOptions(OptionCategory.Spamlist);
        public static IReadOnlyList<OptionItem> PhantomOptions => GetOptions(OptionCategory.Phantom);
        public static IReadOnlyList<OptionItem> ShapeshifterOptions => GetOptions(OptionCategory.Shapeshifter);
        public static IReadOnlyList<OptionItem> PhantomModdedOptions => GetOptions(OptionCategory.PhantomModded);
        public static IReadOnlyList<OptionItem> ShapeshifterModdedOptions => GetOptions(OptionCategory.ShapeshifterModded);
        public static IReadOnlyList<OptionItem> ImpostorOptions => GetOptions(OptionCategory.Impostor);
        public static IReadOnlyList<OptionItem> EngineerOptions => GetOptions(OptionCategory.Engineer);
        public static IReadOnlyList<OptionItem> EngineerModdedOptions => GetOptions(OptionCategory.EngineerModded);
        public static IReadOnlyList<OptionItem> ImmortalOptions => GetOptions(OptionCategory.Immortal);
        public static IReadOnlyList<OptionItem> ScientistOptions => GetOptions(OptionCategory.Scientist);
        public static IReadOnlyList<OptionItem> ExilerOptions => GetOptions(OptionCategory.Exiler);
        public static IReadOnlyList<OptionItem> WatcherOptions => GetOptions(OptionCategory.Watcher);
        public static IReadOnlyList<OptionItem> HostOptions => GetOptions(OptionCategory.Host);
        public static IReadOnlyList<OptionItem> GeneralModdedOptions => GetOptions(OptionCategory.GeneralModded);
        public static IReadOnlyList<OptionItem> GeneralOptions => GetOptions(OptionCategory.General);
        public static IReadOnlyList<OptionItem> SabotageOptions => GetOptions(OptionCategory.Sabotage);
        public static IReadOnlyList<OptionItem> GuesserOptions => GetOptions(OptionCategory.Guesser);
        public static IReadOnlyList<OptionItem> JesterOptions => GetOptions(OptionCategory.Jester);
        public static IReadOnlyList<OptionItem> VisualOptions => GetOptions(OptionCategory.Visual);
        public static IReadOnlyList<OptionItem> TrackerOptions => GetOptions(OptionCategory.Tracker);
        public static IReadOnlyList<OptionItem> NameOptions => GetOptions(OptionCategory.Name);
        public static IReadOnlyList<OptionItem> CommonOptions => GetOptions(OptionCategory.Common);
        public static IReadOnlyList<OptionItem> ShortOptions => GetOptions(OptionCategory.Short);
        public static IReadOnlyList<OptionItem> LongOptions => GetOptions(OptionCategory.Long);
        public static IReadOnlyList<OptionItem> FollowOptions => GetOptions(OptionCategory.Follow);
        public static IReadOnlyList<OptionItem> GameOptions => GetOptions(OptionCategory.Game);
        public static IReadOnlyList<OptionItem> CustomRolesOptions => GetOptions(OptionCategory.CustomRoles);

        public static IReadOnlyDictionary<int, OptionItem> FastOptions => _fastOptions;
        private static Dictionary<int, OptionItem> _fastOptions = new(1024);
        public static int CurrentPreset { get; set; }
#if DEBUG
        public static bool IdDuplicated { get; private set; } = false;
#endif
        #endregion

        public int Id { get; }
        public string Name { get; }
        public int DefaultValue { get; }
        public OptionCategory Category { get; }
        public MainTab MainTab { get; private set; }

        public bool IsSingleValue { get; }
        public bool IsExternallyEnabled { get; private set; } = true;

        public Color NameColor { get; protected set; }
        public OptionFormat ValueFormat { get; protected set; }
        public bool IsHeader { get; protected set; }
        public bool IsHidden { get; protected set; }
        public Dictionary<string, string> ReplacementDictionary
        {
            get => _replacementDictionary;
            set
            {
                if (value == null) _replacementDictionary?.Clear();
                else _replacementDictionary = value;
            }
        }
        private Dictionary<string, string> _replacementDictionary;

        public int[] AllValues { get; private set; } = new int[NumPresets];
        public int CurrentValue
        {
            get => GetValue();
            set => SetValue(value);
        }
        public int SingleValue { get; private set; }

        public OptionItem Parent { get; private set; }
        public static object ApplyDenyNameList { get; internal set; }

        public List<OptionItem> Children;
        public StringOption OptionBehaviour;

        public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;

        public void SetEnabled(bool enabled)
        {
            IsExternallyEnabled = enabled;
            RefreshVisibilityRecursive();
        }

        public bool IsVisibleByParent()
        {
            return !IsHidden && IsExternallyEnabled && (Parent == null || Parent.GetBool());
        }

        public void RefreshVisibilityRecursive()
        {
            if (OptionBehaviour != null)
                OptionBehaviour.gameObject.SetActive(GameSettingMenuPatch.ShouldShowOption(this));

            RefreshChildrenVisibility();
        }

        public void RefreshChildrenVisibility()
        {
            if (Children == null)
                return;

            foreach (OptionItem child in Children)
            {
                if (child == null)
                    continue;

                if (child.OptionBehaviour != null)
                    child.OptionBehaviour.gameObject.SetActive(GameSettingMenuPatch.ShouldShowOption(child));

                child.RefreshChildrenVisibility();
            }
        }

        public OptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            Id = id;
            Name = name;
            DefaultValue = defaultValue;
            Category = category;
            MainTab = category.GetDefaultMainTab();
            IsSingleValue = isSingleValue;

            NameColor = Color.white;
            ValueFormat = OptionFormat.None;
            IsHeader = false;
            IsHidden = false;

            Children = new();

            if (Id == PresetId)
            {
                SingleValue = DefaultValue;
                CurrentPreset = SingleValue;
            }
            else if (IsSingleValue)
            {
                SingleValue = DefaultValue;
            }
            else
            {
                for (int i = 0; i < NumPresets; i++)
                    AllValues[i] = DefaultValue;
            }

            if (_fastOptions.TryAdd(id, this))
            {
                _allOptions.Add(this);
                if (!_categorizedOptions.ContainsKey(category))
                    _categorizedOptions[category] = new List<OptionItem>();
                _categorizedOptions[category].Add(this);
            }
            else
            {
#if DEBUG
                IdDuplicated = true;
#endif
                BMLogger.Error($"ID duplicato rilevato: {id}", "OptionItem");
            }
        }

        public OptionItem Do(Action<OptionItem> action)
        {
            action(this);
            return this;
        }

        public OptionItem SetColor(Color value) => Do(i => i.NameColor = value);
        public OptionItem SetValueFormat(OptionFormat value) => Do(i => i.ValueFormat = value);
        public OptionItem SetHeader(bool value) => Do(i => i.IsHeader = value);
        public OptionItem SetMainTab(MainTab value) => Do(i =>
        {
            i.MainTab = value;
            i.RefreshVisibilityRecursive();
        });
        public OptionItem SetHidden(bool value) => Do(i =>
        {
            i.IsHidden = value;
            i.RefreshVisibilityRecursive();
        });

        public OptionItem SetParent(OptionItem parent) => Do(i =>
        {
            if (i.Parent == parent)
                return;

            if (i.Parent != null)
                i.Parent.Children.Remove(i);

            i.Parent = parent;

            if (parent != null)
                parent.SetChild(i);

            i.RefreshVisibilityRecursive();
        });

        public OptionItem SetChild(OptionItem child) => Do(i =>
        {
            if (child != null && !i.Children.Contains(child))
                i.Children.Add(child);
        });

        public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
            => Do(i => UpdateValueEvent += handler);

        public OptionItem AddReplacement((string key, string value) kvp)
            => Do(i =>
            {
                ReplacementDictionary ??= new();
                ReplacementDictionary.Add(kvp.key, kvp.value);
            });
        public OptionItem RemoveReplacement(string key)
            => Do(i => ReplacementDictionary?.Remove(key));

        public virtual string GetName(bool disableColor = false)
        {
            return disableColor ?
                Translator.GetString(Name, ReplacementDictionary) :
                Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary));
        }

        public virtual bool GetBool() => CurrentValue != 0 && (Parent == null || Parent.GetBool());
        public virtual int GetInt() => CurrentValue;
        public virtual float GetFloat() => CurrentValue;
        public virtual string GetString()
        {
            return ApplyFormat(CurrentValue.ToString());
        }
        public virtual int GetValue() => IsSingleValue ? SingleValue : AllValues[CurrentPreset];

        public string ApplyFormat(string value)
        {
            if (ValueFormat == OptionFormat.None) return value;
            return string.Format(Translator.GetString("Format." + ValueFormat), value);
        }

        public virtual void Refresh()
        {
            if (OptionBehaviour is not StringOption opt || opt == null)
            {
                RefreshVisibilityRecursive();
                return;
            }

            if (opt.TitleText != null) opt.TitleText.text = GetName();
            if (opt.ValueText != null) opt.ValueText.text = GetString();
            opt.oldValue = opt.Value = CurrentValue;

            RefreshVisibilityRecursive();
        }

        public virtual void SetValue(int afterValue, bool doSave, bool doSync = true)
        {
            int beforeValue = CurrentValue;
            if (IsSingleValue)
                SingleValue = afterValue;
            else
                AllValues[CurrentPreset] = afterValue;

            CallUpdateValueEvent(beforeValue, afterValue);
            Refresh();
            RefreshChildrenVisibility();
            if (doSync)
                SyncAllOptions();
            OptionSaver.Save();
        }

        public virtual void SetValue(int afterValue, bool doSync = true)
        {
            SetValue(afterValue, true, doSync);
        }

        public void SetAllValues(int[] values)
        {
            AllValues = values;
        }

        public static OptionItem operator ++(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue + 1));
        public static OptionItem operator --(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue - 1));

        public static void SwitchPreset(int newPreset)
        {
            CurrentPreset = Math.Clamp(newPreset, 0, NumPresets - 1);

            foreach (var op in AllOptions)
                op.Refresh();

            SyncAllOptions();
        }

        public static void SyncAllOptions()
        {
            GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();
            if (BanMod.AllPlayerControls.Count() <= 0
                || AmongUsClient.Instance.AmHost == false
                || PlayerControl.LocalPlayer == null) return;

            if (GameManager.Instance.IsHideAndSeek() || gameMode == GameModeType.SnS || gameMode == GameModeType.TaskRun)
            {
                BanMod.DisableRole = true;
                BanMod.DisableAllRoles();
            }
            else if (gameMode == GameModeType.BanMod)
            {
                BanMod.DisableRole = false;
                BanMod.EnableAllRoles();
            }
        }

        private void CallUpdateValueEvent(int beforeValue, int currentValue)
        {
            if (UpdateValueEvent == null) return;
            try
            {
                UpdateValueEvent(this, new UpdateValueEventArgs(beforeValue, currentValue));
            }
            catch (Exception ex)
            {
                BMLogger.Error($"[{Name}] Eccezione durante la chiamata di UpdateValueEvent", "OptionItem.UpdateValueEvent");
                BMLogger.Exception(ex, "OptionItem.UpdateValueEvent");
            }
        }

        public class UpdateValueEventArgs : EventArgs
        {
            public int CurrentValue { get; set; }
            public int BeforeValue { get; set; }
            public UpdateValueEventArgs(int beforeValue, int currentValue)
            {
                CurrentValue = currentValue;
                BeforeValue = beforeValue;
            }
        }

        public const int NumPresets = 5;
        public const int PresetId = 0;
    }

    public enum OptionCategory
    {
        Seeker,
        GameMode,
        GameModeHnS,
        Sheriff,
        SNS,
        FFA,
        Host,
        Cheat,
        Role,
        Blocklist,
        Afk,
        Cam,
        CamTask,
        Wordlist,
        Spamlist,
        Phantom,
        Shapeshifter,
        Jester,
        Watcher,
        Profiler,
        Judge,
        PhantomModded,
        ShapeshifterModded,
        EngineerModded,
        Impostor,
        Guesser,
        Engineer,
        Immortal,
        Scientist,
        Tracker,
        Exiler,
        GeneralModded,
        General,
        SabotageOption,
        Sabotage,
        Name,
        Common,
        Short,
        Long,
        Follow,
        Game,
        Task,
        CustomRoles,
        Visual
    }

    public enum OptionFormat
    {
        None,
        Players,
        Seconds,
        Percent,
        Times,
        Multiplier,
        Votes,
        Pieces,
    }
}
