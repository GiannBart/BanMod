using BanMod;

namespace BanMod;

public static class FfaExternalSummary
{
    public static string WinnerName =>
        FfaExternalBridge.GetWinnerName();

    public static string ModeName =>
        FfaExternalBridge.GetModeName();

    public static string Summary =>
        FfaExternalBridge.GetSummary();
}
