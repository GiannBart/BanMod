//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using GameCore;
using HarmonyLib;
using System;
using UnityEngine;

namespace BanMod
{
    public class StringOptionItem : OptionItem
    {
        public IntegerValueRule Rule;
        public string[] Selections;
        public bool ShouldTranslate = true;

        public Action OnValueChange;

        public StringOptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue, string[] selections, bool shouldTranslate = true, Action onValueChange = null)
        : base(id, name, defaultValue, category, isSingleValue)
        {
            Rule = (0, selections.Length - 1, 1);
            Selections = selections;
            ShouldTranslate = shouldTranslate;
            OnValueChange = onValueChange;
        }

        public static StringOptionItem Create(int id, string name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue, bool shouldTranslate = true, Action onValueChange = null)
        {
            return new StringOptionItem(id, name, defaultIndex, category, isSingleValue, selections, shouldTranslate, onValueChange);
        }

        public static StringOptionItem Create(string name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue, bool shouldTranslate = true, Action onValueChange = null)
        {
            return new StringOptionItem(OptionItem.NextAutoId(), name, defaultIndex, category, isSingleValue, selections, shouldTranslate, onValueChange);
        }

        public static StringOptionItem Create(int id, Enum name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue, bool shouldTranslate = true, Action onValueChange = null)
        {
            return new StringOptionItem(id, name.ToString(), defaultIndex, category, isSingleValue, selections, shouldTranslate, onValueChange);
        }

        public static StringOptionItem Create(Enum name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue, bool shouldTranslate = true, Action onValueChange = null)
        {
            return new StringOptionItem(OptionItem.NextAutoId(), name.ToString(), defaultIndex, category, isSingleValue, selections, shouldTranslate, onValueChange);
        }

        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);

        public override string GetString()
        {
            int index = Rule.GetValueByIndex(CurrentValue);
            if (index < 0 || index >= Selections.Length) return string.Empty;

            string val = Selections[index];
            return ShouldTranslate ? Translator.GetString(val) : val;
        }

        public int GetChance()
        {
            if (Selections.Length == 2) return CurrentValue * 100;

            var offset = 12 - Selections.Length;
            var index = CurrentValue + offset;
            var rate = index <= 1 ? index * 5 : (index - 1) * 10;
            return rate;
        }

        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);

            OnValueChange?.Invoke();

            if (this == Options.GameMode || this == Options.PresetSelection)
            {
                foreach (var op in AllOptions)
                    op.Refresh();

                SyncAllOptions();
            }
        }

        public override void SetValue(int afterValue, bool doSave, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(afterValue), doSave, doSync);
        }
    }
}
