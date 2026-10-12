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

                bool exilerEnabled = Options.ExilerExe.GetBool();
                bool guesserEnabled = Options.Guess.GetBool();
                bool jesterEnabled = Options.Jester.GetBool();

                bool phantomGuessEnabled = Options.PhantomGuess.GetBool();
                bool viperGuessEnabled = Options.ViperGuess.GetBool();
                bool shapeGuessEnabled = Options.ShapeGuess.GetBool();
                bool impostorGuessEnabled = Options.ImpostorGuess.GetBool();

                byte exilerId =
                    Exiler.ExilerSelected ? Exiler.ExilerId : byte.MaxValue;

                byte guesserId =
                    Guesser.SpecialKillerSelected ? Guesser.SpecialKillerId : byte.MaxValue;

                byte jesterId =
                    Jester.JesterSelected ? Jester.JesterId : byte.MaxValue;

                // NUOVO TRASPORTO SERVER: nessun CustomRPC.
                BanModSyncApi.SendHostRoleOptionsUpdate(
                    exilerEnabled,
                    guesserEnabled,
                    jesterEnabled,
                    phantomGuessEnabled,
                    viperGuessEnabled,
                    shapeGuessEnabled,
                    impostorGuessEnabled,
                    exilerId,
                    guesserId,
                    jesterId);

                HostRoleOptionsStatus.Update(
                    exilerEnabled,
                    guesserEnabled,
                    jesterEnabled,
                    phantomGuessEnabled,
                    viperGuessEnabled,
                    shapeGuessEnabled,
                    impostorGuessEnabled,
                    exilerId,
                    guesserId,
                    jesterId);

                try { RoleButtonRefresh.RefreshNow(); } catch { }
            }
            catch (Exception ex)
            {
                BMLogger.Warn($"SendToAll failed: {ex}", LogTag);
            }
        }

        // Receiver del nuovo canale server.
        public static void ReceiveServer(
            bool exilerEnabled,
            bool guesserEnabled,
            bool jesterEnabled,
            bool phantomGuessEnabled,
            bool viperGuessEnabled,
            bool shapeGuessEnabled,
            bool impostorGuessEnabled,
            byte exilerId,
            byte guesserId,
            byte jesterId)
        {
            try
            {
                HostRoleOptionsStatus.Update(
                    exilerEnabled,
                    guesserEnabled,
                    jesterEnabled,
                    phantomGuessEnabled,
                    viperGuessEnabled,
                    shapeGuessEnabled,
                    impostorGuessEnabled,
                    exilerId,
                    guesserId,
                    jesterId);

                BMLogger.Info(
                    $"HostRoleOptionsUpdate ricevuto via server | " +
                    $"Exiler={exilerEnabled} ExilerId={exilerId} | " +
                    $"Guesser={guesserEnabled} GuesserId={guesserId} | " +
                    $"Jester={jesterEnabled} JesterId={jesterId}",
                    LogTag);

                try { RoleButtonRefresh.RefreshNow(); } catch { }
            }
            catch (Exception ex)
            {
                BMLogger.Warn($"ReceiveServer failed: {ex}", LogTag);
            }
        }

        // Legacy receiver: può restare per compatibilità con vecchie versioni.
        // Non viene usato dai nuovi sender.
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
                        LogTag);
                    return;
                }

                bool exilerEnabled = reader.ReadBoolean();
                bool guesserEnabled = reader.ReadBoolean();
                bool jesterEnabled = reader.ReadBoolean();

                bool phantomGuessEnabled = reader.ReadBoolean();
                bool viperGuessEnabled = reader.ReadBoolean();
                bool shapeGuessEnabled = reader.ReadBoolean();
                bool impostorGuessEnabled = reader.ReadBoolean();

                byte exilerId = reader.ReadByte();
                byte guesserId = reader.ReadByte();
                byte jesterId = reader.ReadByte();

                ReceiveServer(
                    exilerEnabled,
                    guesserEnabled,
                    jesterEnabled,
                    phantomGuessEnabled,
                    viperGuessEnabled,
                    shapeGuessEnabled,
                    impostorGuessEnabled,
                    exilerId,
                    guesserId,
                    jesterId);
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

                if (senderObject.Data != null &&
                    senderObject.Data.ClientId == AmongUsClient.Instance.HostId)
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

        public static bool ExilerEnabled = false;
        public static bool GuesserEnabled = false;
        public static bool JesterEnabled = false;

        public static bool PhantomGuessEnabled = false;
        public static bool ViperGuessEnabled = false;
        public static bool ShapeGuessEnabled = false;
        public static bool ImpostorGuessEnabled = false;

        public static byte ExilerId = byte.MaxValue;
        public static byte GuesserId = byte.MaxValue;
        public static byte JesterId = byte.MaxValue;

        public static void Reset()
        {
            ReceivedFromHost = false;

            ExilerEnabled = false;
            GuesserEnabled = false;
            JesterEnabled = false;

            PhantomGuessEnabled = false;
            ViperGuessEnabled = false;
            ShapeGuessEnabled = false;
            ImpostorGuessEnabled = false;

            ExilerId = byte.MaxValue;
            GuesserId = byte.MaxValue;
            JesterId = byte.MaxValue;
        }

        public static void Update(
            bool exilerEnabled,
            bool guesserEnabled,
            bool jesterEnabled,
            bool phantomGuessEnabled,
            bool viperGuessEnabled,
            bool shapeGuessEnabled,
            bool impostorGuessEnabled,
            byte exilerId,
            byte guesserId,
            byte jesterId)
        {
            ReceivedFromHost = true;

            ExilerEnabled = exilerEnabled;
            GuesserEnabled = guesserEnabled;
            JesterEnabled = jesterEnabled;

            PhantomGuessEnabled = phantomGuessEnabled;
            ViperGuessEnabled = viperGuessEnabled;
            ShapeGuessEnabled = shapeGuessEnabled;
            ImpostorGuessEnabled = impostorGuessEnabled;

            ExilerId = exilerId;
            GuesserId = guesserId;
            JesterId = jesterId;

            Exiler.ExilerId = exilerId;
            Exiler.ExilerSelected = exilerId != byte.MaxValue && exilerId != 255;

            Guesser.SpecialKillerId = guesserId;
            Guesser.SpecialKillerSelected = guesserId != byte.MaxValue && guesserId != 255;

            Jester.JesterId = jesterId;
            Jester.JesterSelected = jesterId != byte.MaxValue && jesterId != 255;
        }
    }
}
