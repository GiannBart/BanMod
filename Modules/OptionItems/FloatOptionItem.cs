//credits and licenses in the resources folder
using System;
using BanMod;

namespace BanMod
{
    public class FloatOptionItem : OptionItem
    {
        public FloatValueRule Rule;

        public FloatOptionItem(int id, string name, float defaultValue, OptionCategory category, bool isSingleValue, FloatValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), category, isSingleValue)
        {
            Rule = rule;
        }

        public static FloatOptionItem Create(int id, string name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new FloatOptionItem(id, name, defaultValue, category, isSingleValue, rule);
        }

        public static FloatOptionItem Create(string name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new FloatOptionItem(OptionItem.NextAutoId(), name, defaultValue, category, isSingleValue, rule);
        }

        public static FloatOptionItem Create(int id, Enum name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new FloatOptionItem(id, name.ToString(), defaultValue, category, isSingleValue, rule);
        }

        public static FloatOptionItem Create(Enum name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new FloatOptionItem(OptionItem.NextAutoId(), name.ToString(), defaultValue, category, isSingleValue, rule);
        }

        public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
        }
    }
}
