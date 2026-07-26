//credits and licenses in the resources folder
using BanMod;
using System;

namespace BanMod
{
    public class BooleanOptionItem : OptionItem
    {
        public const string TEXT_true = "ColoredOn";
        public const string TEXT_false = "ColoredOff";

        public BooleanOptionItem(int id, string name, bool defaultValue, OptionCategory category, bool isSingleValue)
        : base(id, name, defaultValue ? 1 : 0, category, isSingleValue)
        {
        }

        public static BooleanOptionItem Create(int id, string name, bool defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new BooleanOptionItem(id, name, defaultValue, category, isSingleValue);
        }

        public static BooleanOptionItem Create(string name, bool defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new BooleanOptionItem(OptionItem.NextAutoId(), name, defaultValue, category, isSingleValue);
        }

        public static BooleanOptionItem Create(int id, Enum name, bool defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new BooleanOptionItem(id, name.ToString(), defaultValue, category, isSingleValue);
        }

        public static BooleanOptionItem Create(Enum name, bool defaultValue, OptionCategory category, bool isSingleValue)
        {
            return new BooleanOptionItem(OptionItem.NextAutoId(), name.ToString(), defaultValue, category, isSingleValue);
        }

        public override string GetString()
        {
            return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
        }

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(value % 2 == 0 ? 0 : 1, doSync);
        }
    }
}
