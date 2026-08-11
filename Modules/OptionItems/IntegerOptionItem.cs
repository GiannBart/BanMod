//credits and licenses in the resources folder
using System;
using BanMod;

namespace BanMod
{
    public class IntegerOptionItem : OptionItem
    {
        public IntegerValueRule Rule;

        public IntegerOptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue, IntegerValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), category, isSingleValue)
        {
            Rule = rule;
        }

        public static IntegerOptionItem Create(int id, string name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new IntegerOptionItem(id, name, defaultValue, category, isSingleValue, rule);
        }

        public static IntegerOptionItem Create(string name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new IntegerOptionItem(OptionItem.NextAutoId(), name, defaultValue, category, isSingleValue, rule);
        }

        public static IntegerOptionItem Create(int id, Enum name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new IntegerOptionItem(id, name.ToString(), defaultValue, category, isSingleValue, rule);
        }

        public static IntegerOptionItem Create(Enum name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new IntegerOptionItem(OptionItem.NextAutoId(), name.ToString(), defaultValue, category, isSingleValue, rule);
        }

        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
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
