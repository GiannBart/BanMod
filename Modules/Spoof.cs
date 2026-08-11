//credits and licenses in the resources folder
using AmongUs.Data;
using InnerNet;
using System.Linq;

namespace BanMod;
public static class Spoof
{
    public static uint parsedLevel;

    public static void spoofLevel()
    {
        if (!string.IsNullOrEmpty(BanMod.spoofLevel.Value) &&
            uint.TryParse(BanMod.spoofLevel.Value, out parsedLevel) &&
            parsedLevel != DataManager.Player.Stats.Level)
        {

            DataManager.Player.stats.level = parsedLevel - 1;
            DataManager.Player.Save();
        }
    }

    public static void spoofPlatform(PlatformSpecificData platformSpecificData)
    {
        Platforms? platformType;

        if (Utils.stringToPlatformType(BanMod.spoofPlatform.Value, out platformType))
        {
            if (BanMod.IsBanModDisabled) return;
            platformSpecificData.Platform = (Platforms)platformType;
        }
    }
}
