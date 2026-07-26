//credits and licenses in the resources folder
using Hazel;
using System;
using UnityEngine;

namespace BanMod

{
    public static class HostRoleOptionsRpc
    {
        private const string LogTag = "HostRoleOptionsRpc";

        public static void SendToAll()
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;

                if (PlayerControl.LocalPlayer == null)
                    return;

                byte watcherId = Watcher.WatcherSelected ? Watcher.WatcherId : byte.MaxValue;
                byte exilerId = Exiler.ExilerSelected ? Exiler.ExilerId : byte.MaxValue;
                byte judgeId = Judge.JudgeSelected ? Judge.JudgeId : byte.MaxValue;
                byte profilerId = Profiler.ProfilerSelected ? Profiler.ProfilerId : byte.MaxValue;
                byte guesserId = Guesser.SpecialKillerSelected ? Guesser.SpecialKillerId : byte.MaxValue;
                byte jesterId = Jester.JesterSelected ? Jester.JesterId : byte.MaxValue;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.HostRoleOptionsUpdate,
                    SendOption.Reliable,
                    -1
                );

                writer.Write(Options.Watcher.GetBool());
                writer.Write(Options.ExilerExe.GetBool());
                writer.Write(Options.Judge.GetBool());
                writer.Write(Options.Profiler.GetBool());
                writer.Write(Options.Guess.GetBool());
                writer.Write(Options.Jester.GetBool());

                writer.Write(Options.PhantomGuess.GetBool());
                writer.Write(Options.ViperGuess.GetBool());
                writer.Write(Options.ShapeGuess.GetBool());
                writer.Write(Options.ImpostorGuess.GetBool());

                writer.Write(exilerId);
                writer.Write(judgeId);
                writer.Write(profilerId);
                writer.Write(watcherId);
                writer.Write(guesserId);
                writer.Write(jesterId); 

                AmongUsClient.Instance.FinishRpcImmediately(writer);

                HostRoleOptionsStatus.Update(
                    Options.Watcher.GetBool(),
                    Options.ExilerExe.GetBool(),
                    Options.Judge.GetBool(),
                    Options.Profiler.GetBool(),
                    Options.Guess.GetBool(),
                    Options.Jester.GetBool(),
                    Options.PhantomGuess.GetBool(),
                    Options.ViperGuess.GetBool(),
                    Options.ShapeGuess.GetBool(),
                    Options.ImpostorGuess.GetBool(),
                    watcherId,
                    exilerId,
                    judgeId,
                    profilerId,
                    guesserId,
                    jesterId
                );

                try
                {
                    RoleButtonRefresh.RefreshNow();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                BMLogger.Warn($"SendToAll failed: {ex}", LogTag);
            }
        }

        public static void Receive(PlayerControl senderObject, MessageReader reader)
        {
            try
            {
                if (senderObject == null || reader == null)
                    return;

                if (AmongUsClient.Instance == null)
                    return;

                if (!IsFromHost(senderObject))
                {
                    BMLogger.Warn(
                        $"HostRoleOptionsUpdate ignorato: sender non host | PlayerId={senderObject.PlayerId}",
                        LogTag
                    );
                    return;
                }

                bool watcherEnabled = reader.ReadBoolean();
                bool exilerEnabled = reader.ReadBoolean();
                bool judgeEnabled = reader.ReadBoolean();
                bool profilerEnabled = reader.ReadBoolean();
                bool guesserEnabled = reader.ReadBoolean();
                bool jesterEnabled = reader.ReadBoolean();

                bool phantomGuessEnabled = reader.ReadBoolean();
                bool viperGuessEnabled = reader.ReadBoolean();
                bool shapeGuessEnabled = reader.ReadBoolean();
                bool impostorGuessEnabled = reader.ReadBoolean();

                byte exilerId = reader.ReadByte();
                byte judgeId = reader.ReadByte();
                byte profilerId = reader.ReadByte();
                byte watcherId = reader.ReadByte();
                byte guesserId = reader.ReadByte();
                byte jesterId = reader.ReadByte();


                HostRoleOptionsStatus.Update(
                    watcherEnabled,
                    exilerEnabled,
                    judgeEnabled,
                    profilerEnabled,
                    guesserEnabled,
                    jesterEnabled,
                    phantomGuessEnabled,
                    viperGuessEnabled,
                    shapeGuessEnabled,
                    impostorGuessEnabled,
                    watcherId,
                    exilerId,
                    judgeId,
                    profilerId,
                    guesserId,
                    jesterId
                );

                BMLogger.Info(
                    $"HostRoleOptionsUpdate ricevuto | Watcher={watcherEnabled} WatcherId={watcherId} | " +
                    $"Exiler={exilerEnabled} ExilerId={exilerId} | " +
                    $"Judge={judgeEnabled} JudgeId={judgeId} | Profiler={profilerEnabled} ProfilerId={profilerId} | " +
                    $"Guesser={guesserEnabled} GuesserId={guesserId} | Jester={jesterEnabled} JesterId={jesterId}",
                    LogTag
                );

                try
                {
                    RoleButtonRefresh.RefreshNow();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                BMLogger.Warn($"Receive failed: {ex}", LogTag);
            }
        }

        private static bool IsFromHost(PlayerControl senderObject)
        {
            try
            {
                if (senderObject == null || AmongUsClient.Instance == null)
                    return false;

                if (senderObject.OwnerId == AmongUsClient.Instance.HostId)
                    return true;

                if (senderObject.Data != null && senderObject.Data.ClientId == AmongUsClient.Instance.HostId)
                    return true;
            }
            catch
            {
            }

            return false;
        }
    }
    public static class HostRoleOptionsStatus
    {
        public static bool ReceivedFromHost = false;

        public static bool WatcherEnabled = false;
        public static bool ExilerEnabled = false;
        public static bool GuesserEnabled = false;
        public static bool JesterEnabled = false;
        public static bool JudgeEnabled = false;
        public static bool ProfilerEnabled = false;

        public static bool PhantomGuessEnabled = false;
        public static bool ViperGuessEnabled = false;
        public static bool ShapeGuessEnabled = false;
        public static bool ImpostorGuessEnabled = false;

        public static byte WatcherId = byte.MaxValue;
        public static byte ExilerId = byte.MaxValue;
        public static byte JudgeId = byte.MaxValue;
        public static byte ProfilerId = byte.MaxValue;
        public static byte GuesserId = byte.MaxValue;
        public static byte JesterId = byte.MaxValue;

        public static void Reset()
        {
            ReceivedFromHost = false;

            WatcherEnabled = false;
            ExilerEnabled = false;
            JudgeEnabled = false;
            ProfilerEnabled = false;
            GuesserEnabled = false;
            JesterEnabled = false;

            PhantomGuessEnabled = false;
            ViperGuessEnabled = false;
            ShapeGuessEnabled = false;
            ImpostorGuessEnabled = false;

            WatcherId = byte.MaxValue;
            ExilerId = byte.MaxValue;
            ProfilerId = byte.MaxValue;
            JudgeId = byte.MaxValue;
            GuesserId = byte.MaxValue;
            JesterId = byte.MaxValue;
        }

        public static void Update(
            bool watcherEnabled,
            bool exilerEnabled,
            bool judgeEnabled,
            bool profilerEnabled,
            bool guesserEnabled,
            bool jesterEnabled,
            bool phantomGuessEnabled,
            bool viperGuessEnabled,
            bool shapeGuessEnabled,
            bool impostorGuessEnabled,
            byte watcherId,
            byte exilerId,
            byte judgeId,
            byte profilerId,
            byte guesserId,
            byte jesterId)
        {
            ReceivedFromHost = true;

            WatcherEnabled = watcherEnabled;
            ExilerEnabled = exilerEnabled;
            GuesserEnabled = guesserEnabled;
            JesterEnabled = jesterEnabled;
            JudgeEnabled = judgeEnabled;
            ProfilerEnabled = profilerEnabled;

            PhantomGuessEnabled = phantomGuessEnabled;
            ViperGuessEnabled = viperGuessEnabled;
            ShapeGuessEnabled = shapeGuessEnabled;
            ImpostorGuessEnabled = impostorGuessEnabled;

            WatcherId = watcherId;
            ExilerId = exilerId;
            GuesserId = guesserId;
            JesterId = jesterId;
            ProfilerId = profilerId;
            JudgeId = judgeId;

            Watcher.WatcherId = watcherId;
            Watcher.WatcherSelected = watcherId != byte.MaxValue && watcherId != 255;

            Exiler.ExilerId = exilerId;
            Exiler.ExilerSelected = exilerId != byte.MaxValue && exilerId != 255;
            
            Judge.JudgeId = judgeId;
            Judge.JudgeSelected = judgeId != byte.MaxValue && judgeId != 255;

            Profiler.ProfilerId = profilerId;
            Profiler.ProfilerSelected = profilerId != byte.MaxValue && profilerId != 255;

            Guesser.SpecialKillerId = guesserId;
            Guesser.SpecialKillerSelected = guesserId != byte.MaxValue && guesserId != 255;

            Jester.JesterId = jesterId;
            Jester.JesterSelected = jesterId != byte.MaxValue && jesterId != 255;

        }
    }
}