//credits and licenses in the resources folder
namespace BanMod
{
    public static class RoleOptionSyncHelper
    {
        public static bool IsHost()
        {
            return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        }

        public static bool CanReadHostOptions()
        {
            return IsHost() || HostRoleOptionsStatus.ReceivedFromHost;
        }

        public static bool IsExilerEnabled()
        {
            return IsHost()
                ? Options.ExilerExe.GetBool()
                : HostRoleOptionsStatus.ExilerEnabled;
        }

        public static byte GetExilerId()
        {
            return IsHost()
                ? Exiler.ExilerId
                : HostRoleOptionsStatus.ExilerId;
        }
        public static bool IsJudgeEnabled()
        {
            return IsHost()
                ? Options.Judge.GetBool()
                : HostRoleOptionsStatus.JudgeEnabled;
        }
        public static byte GetJudgeId()
        {
            return IsHost()
                ? Judge.JudgeId
                : HostRoleOptionsStatus.JudgeId;
        }

        public static bool IsProfilerEnabled()
        {
            return IsHost()
                ? Options.Profiler.GetBool()
                : HostRoleOptionsStatus.ProfilerEnabled;
        }
        public static byte GetProfilerId()
        {
            return IsHost()
                ? Profiler.ProfilerId
                : HostRoleOptionsStatus.ProfilerId;
        }
        public static bool IsGuesserEnabled()
        {
            return IsHost()
                ? Options.Guess.GetBool()
                : HostRoleOptionsStatus.GuesserEnabled;
        }

        public static byte GetGuesserId()
        {
            return IsHost()
                ? Guesser.SpecialKillerId
                : HostRoleOptionsStatus.GuesserId;
        }

        public static bool IsImpGuesserEnabledFor(PlayerControl player)
        {
            if (player == null)
                return false;

            if (IsHost())
            {
                return
                    (Utils.Phantom(player) && Options.PhantomGuess.GetBool()) ||
                    (Utils.Shapeshifter(player) && Options.ShapeGuess.GetBool()) ||
                    (Utils.Cobra(player) && Options.ViperGuess.GetBool()) ||
                    (Utils.Impostor(player) && Options.ImpostorGuess.GetBool());
            }

            return
                (Utils.Phantom(player) && HostRoleOptionsStatus.PhantomGuessEnabled) ||
                (Utils.Shapeshifter(player) && HostRoleOptionsStatus.ShapeGuessEnabled) ||
                (Utils.Cobra(player) && HostRoleOptionsStatus.ViperGuessEnabled) ||
                (Utils.Impostor(player) && HostRoleOptionsStatus.ImpostorGuessEnabled);
        }
    }
}