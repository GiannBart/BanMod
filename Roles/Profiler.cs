using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static BanMod.Utils;

namespace BanMod
{
    public enum ProfilerHintStrength
    {
        Mild = 0,
        Medium = 1,
        Strong = 2,
        TaskBased = 3
    }

    public static class Profiler
    {
        public static byte ProfilerId = byte.MaxValue;
        public static bool ProfilerSelected = false;

        private static byte LastImpostorId = byte.MaxValue;
        private static string LastHintKey = string.Empty;

        private static readonly Dictionary<byte, HashSet<string>> UsedHintKeysByImpostor =
            new Dictionary<byte, HashSet<string>>();

        private static readonly HashSet<string> ExhaustedCategoriesNotified =
            new HashSet<string>();

        private static int MeetingSequence = 0;
        private static int LastSentMeetingSequence = -1;
        private static int LastCapturedMeetingHudId = -1;
        private static bool HintCoroutineRunning = false;

        private static readonly Dictionary<byte, ImpostorSnapshot> MeetingSnapshots =
            new Dictionary<byte, ImpostorSnapshot>();

        private sealed class ImpostorSnapshot
        {
            public byte PlayerId;
            public string PlayerName = string.Empty;
            public int Level;
            public bool HasPet;
            public bool HasHat;
            public int ColorId = -1;
            public string RoomName = string.Empty;
            public Vector2 Position;
        }

        private sealed class HintCandidate
        {
            public string Key = string.Empty;
            public string Text = string.Empty;
        }

        private static string Tr(string key, params object[] args)
        {
            string format = Translator.GetString(key);

            if (args == null || args.Length == 0)
                return format;

            return string.Format(format, args);
        }

        // Usa cifre Unicode a larghezza intera. Visivamente sono un font diverso
        // e non vengono trattate come le normali cifre ASCII dalla chat.
        private static string NumberText(int value)
        {
            return ToAlternateDigits(
                value.ToString(CultureInfo.InvariantCulture));
        }

        private static string ToAlternateDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            char[] chars = value.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                switch (chars[i])
                {
                    case '0': chars[i] = '０'; break;
                    case '1': chars[i] = '１'; break;
                    case '2': chars[i] = '２'; break;
                    case '3': chars[i] = '３'; break;
                    case '4': chars[i] = '４'; break;
                    case '5': chars[i] = '５'; break;
                    case '6': chars[i] = '６'; break;
                    case '7': chars[i] = '７'; break;
                    case '8': chars[i] = '８'; break;
                    case '9': chars[i] = '９'; break;
                }
            }

            return new string(chars);
        }

        private static bool IsTranslatedRoom(
            string roomName,
            params string[] translationKeys)
        {
            if (string.IsNullOrWhiteSpace(roomName) ||
                translationKeys == null)
            {
                return false;
            }

            foreach (string key in translationKeys)
            {
                if (string.Equals(
                        roomName,
                        Tr(key),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        public static void SendProfilerMessage()
        {
            if (ProfilerId == byte.MaxValue)
                return;

            var allPlayers = BanMod.AllPlayerControls;
            var ProfilerPlayer =
                allPlayers.FirstOrDefault(p => p.PlayerId == ProfilerId);

            if (ProfilerPlayer == null ||
                ProfilerPlayer.Data == null ||
                ProfilerPlayer.Data.IsDead)
            {
                return;
            }

            string msg = string.Format(Translator.GetString("ProfilerInfo"));

            if (AmongUsClient.Instance.AmHost &&
                PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, ProfilerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, ProfilerId);
                MessageBlocker.UpdateLastMessageTime();
            }
        }
        public static ProfilerHintStrength ConfiguredStrength
        {
            get
            {
                int value = Mathf.Clamp(Options.ProfilerHintMode.GetInt(), 0, 3);
                return (ProfilerHintStrength)value;
            }
        }

        public static void OnStart()
        {
            ResetProfiler();

            if (!Options.Profiler.GetBool())
                return;

            // Solo l'host sceglie il ruolo e invia gli indizi privati.
            if (!AmongUsClient.Instance.AmHost)
                return;

            SelectProfiler();

            if (!ProfilerSelected)
                BMLogger.Info("[Profiler] Profiler non assegnato.");
        }

        public static void SelectProfiler()
        {
            if (!AmongUsClient.Instance.AmHost ||
                !Options.Profiler.GetBool())
            {
                return;
            }

            var candidates = BanMod.AllPlayerControls
                .Where(p => p != null
                            && p.Data != null
                            && !p.Data.IsDead
                            && !p.Data.Disconnected
                            && p.PlayerId != Judge.JudgeId
                            && p.PlayerId != Guesser.SpecialKillerId
                            && p.PlayerId != Jester.JesterId
                            && p.PlayerId != Watcher.WatcherId
                            && p.PlayerId != Exiler.ExilerId
                            && !Scientist(p)
                            && !Engineer(p)
                            && !Tracker(p)
                            && !Detective(p)
                            && !Judge(p)
                            && !IsImpostorAligned(p))
                .ToList();

            if (candidates.Count == 0)
            {
                ProfilerId = byte.MaxValue;
                ProfilerSelected = false;

                BMLogger.Info(
                    "[Profiler] Nessun candidato valido per Profiler trovato.");

                return;
            }

            PlayerControl selected =
                candidates[UnityEngine.Random.Range(0, candidates.Count)];

            ProfilerId = selected.PlayerId;
            ProfilerSelected = true;

            BMLogger.Info(
                $"[Profiler] Profiler assegnato al PlayerId {ProfilerId}.");
        }

        public static void ResetProfiler()
        {
            ProfilerId = byte.MaxValue;
            ProfilerSelected = false;

            LastImpostorId = byte.MaxValue;
            LastHintKey = string.Empty;

            MeetingSequence = 0;
            LastSentMeetingSequence = -1;
            LastCapturedMeetingHudId = -1;
            HintCoroutineRunning = false;

            MeetingSnapshots.Clear();
            UsedHintKeysByImpostor.Clear();
            ExhaustedCategoriesNotified.Clear();
        }

        internal static void CaptureMeetingSnapshot(MeetingHud meetingHud)
        {
            if (!AmongUsClient.Instance.AmHost ||
                !Options.Profiler.GetBool() ||
                !ProfilerSelected ||
                meetingHud == null)
            {
                return;
            }

            int meetingHudId = meetingHud.GetInstanceID();

            // ServerStart e Start possono arrivare molto vicini:
            // la stessa riunione deve essere acquisita una sola volta.
            if (LastCapturedMeetingHudId == meetingHudId)
                return;

            LastCapturedMeetingHudId = meetingHudId;
            MeetingSequence++;
            MeetingSnapshots.Clear();

            List<PlayerControl> impostors = GetAliveImpostors();

            foreach (PlayerControl impostor in impostors)
            {
                MeetingSnapshots[impostor.PlayerId] =
                    CreateSnapshot(impostor);
            }

            BMLogger.Info(
                $"[Profiler] Snapshot meeting {MeetingSequence}: " +
                $"{MeetingSnapshots.Count} impostori vivi.");
        }

        internal static void ScheduleMeetingHint(MeetingHud meetingHud)
        {
            if (!AmongUsClient.Instance.AmHost ||
                !Options.Profiler.GetBool() ||
                !ProfilerSelected ||
                meetingHud == null ||
                HintCoroutineRunning)
            {
                return;
            }

            HintCoroutineRunning = true;
            meetingHud.StartCoroutine(CoSendMeetingHint(meetingHud));
        }

        private static IEnumerator CoSendMeetingHint(MeetingHud meetingHud)
        {
            // Attende che la chat del meeting sia visibile.
            yield return new WaitForSeconds(1.4f);

            HintCoroutineRunning = false;

            if (meetingHud == null ||
                MeetingHud.Instance == null ||
                !AmongUsClient.Instance.AmHost)
            {
                yield break;
            }

            // Se Start è arrivato prima di ServerStart, acquisisce qui.
            CaptureMeetingSnapshot(meetingHud);

            if (MeetingSequence == LastSentMeetingSequence)
                yield break;

            LastSentMeetingSequence = MeetingSequence;
            SendMeetingHint();
        }

        public static void SendMeetingHint()
        {
            if (!AmongUsClient.Instance.AmHost ||
                !Options.Profiler.GetBool() ||
                !ProfilerSelected ||
                ProfilerId == byte.MaxValue)
            {
                return;
            }

            PlayerControl profilerPlayer =
                BanMod.AllPlayerControls.FirstOrDefault(
                    p => p != null && p.PlayerId == ProfilerId);

            if (profilerPlayer == null ||
                profilerPlayer.Data == null ||
                profilerPlayer.Data.IsDead ||
                profilerPlayer.Data.Disconnected)
            {
                return;
            }

            List<PlayerControl> impostors = GetAliveImpostors();

            if (impostors.Count == 0)
            {
                SendPrivateMessage(
                    Tr("ProfilerNoAliveImpostors"));

                return;
            }

            PlayerControl target = SelectNextImpostor(impostors);

            if (target == null)
                return;

            if (!MeetingSnapshots.TryGetValue(
                    target.PlayerId,
                    out ImpostorSnapshot snapshot))
            {
                snapshot = CreateSnapshot(target);
            }

            float taskPercent = GetTaskCompletionPercent(profilerPlayer);
            ProfilerHintStrength strength =
                ResolveStrength(taskPercent);

            LastImpostorId = target.PlayerId;

            HintCandidate hint =
                BuildHint(
                    snapshot,
                    strength,
                    impostors.Count,
                    target.PlayerId);

            if (hint == null || string.IsNullOrWhiteSpace(hint.Text))
            {
                SendHintsExhaustedMessage(
                    target.PlayerId,
                    strength,
                    taskPercent);

                return;
            }

            HashSet<string> usedKeys =
                GetUsedHintKeys(target.PlayerId);

            usedKeys.Add(hint.Key);
            LastHintKey = hint.Key;

            // Se in precedenza la categoria risultava terminata, ma è
            // comparso un nuovo indizio valido, consente una futura notifica.
            ExhaustedCategoriesNotified.Remove(
                GetExhaustedCategoryKey(
                    target.PlayerId,
                    strength));

            string strengthName = GetStrengthName(strength);
            string taskLine = ConfiguredStrength == ProfilerHintStrength.TaskBased
                ? Tr(
                    "ProfilerTaskProgress",
                    NumberText(Mathf.FloorToInt(taskPercent)))
                : string.Empty;

            string message = Tr(
                "ProfilerHintMessage",
                strengthName,
                hint.Text,
                taskLine);

            SendPrivateMessage(message);

            BMLogger.Info(
                $"[Profiler] Meeting {MeetingSequence}, " +
                $"target {target.PlayerId}, forza {strength}, " +
                $"indizio {hint.Key}. " +
                $"Usati per questo impostore: {usedKeys.Count}.");
        }

        private static void SendPrivateMessage(string message)
        {
            if (PlayerControl.LocalPlayer != null &&
                PlayerControl.LocalPlayer.Data != null &&
                PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(message, ProfilerId);
            }
            else
            {
                Utils.SendMessage(message, ProfilerId);
            }

            MessageBlocker.UpdateLastMessageTime();
        }

        private static PlayerControl SelectNextImpostor(
            List<PlayerControl> impostors)
        {
            if (impostors == null || impostors.Count == 0)
                return null;

            List<PlayerControl> ordered = impostors
                .OrderBy(p => p.PlayerId)
                .ToList();

            if (ordered.Count == 1)
                return ordered[0];

            int previousIndex =
                ordered.FindIndex(p => p.PlayerId == LastImpostorId);

            if (previousIndex < 0)
            {
                return ordered[
                    UnityEngine.Random.Range(0, ordered.Count)];
            }

            return ordered[(previousIndex + 1) % ordered.Count];
        }

        private static ProfilerHintStrength ResolveStrength(
            float taskPercent)
        {
            ProfilerHintStrength configured = ConfiguredStrength;

            if (configured != ProfilerHintStrength.TaskBased)
                return configured;

            if (taskPercent < 50f)
                return ProfilerHintStrength.Mild;

            if (taskPercent < 80f)
                return ProfilerHintStrength.Medium;

            return ProfilerHintStrength.Strong;
        }

        private static HintCandidate BuildHint(
            ImpostorSnapshot snapshot,
            ProfilerHintStrength strength,
            int impostorCount,
            byte impostorId)
        {
            string subject = impostorCount > 1
                ? Tr("ProfilerSubjectMultiple")
                : Tr("ProfilerSubjectSingle");

            List<HintCandidate> candidates;

            switch (strength)
            {
                case ProfilerHintStrength.Medium:
                    candidates = BuildMediumHints(snapshot, subject);
                    break;

                case ProfilerHintStrength.Strong:
                    candidates = BuildStrongHints(snapshot, subject);
                    break;

                default:
                    candidates = BuildMildHints(snapshot, subject);
                    break;
            }

            HashSet<string> usedKeys =
                GetUsedHintKeys(impostorId);

            candidates = candidates
                .Where(h =>
                    h != null &&
                    !string.IsNullOrWhiteSpace(h.Key) &&
                    !string.IsNullOrWhiteSpace(h.Text) &&
                    !usedKeys.Contains(h.Key))
                .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates[
                UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static HashSet<string> GetUsedHintKeys(
            byte impostorId)
        {
            if (!UsedHintKeysByImpostor.TryGetValue(
                    impostorId,
                    out HashSet<string> usedKeys))
            {
                usedKeys = new HashSet<string>();
                UsedHintKeysByImpostor[impostorId] = usedKeys;
            }

            return usedKeys;
        }

        private static string GetExhaustedCategoryKey(
            byte impostorId,
            ProfilerHintStrength strength)
        {
            return $"{impostorId}:{(int)strength}";
        }

        private static void SendHintsExhaustedMessage(
            byte impostorId,
            ProfilerHintStrength strength,
            float taskPercent)
        {
            string exhaustedKey =
                GetExhaustedCategoryKey(
                    impostorId,
                    strength);

            // Non ripete a ogni meeting lo stesso avviso di categoria finita.
            if (!ExhaustedCategoriesNotified.Add(exhaustedKey))
                return;

            string message;

            if (ConfiguredStrength == ProfilerHintStrength.TaskBased)
            {
                switch (strength)
                {
                    case ProfilerHintStrength.Mild:
                        message =
                            Tr("ProfilerHintsExhaustedTaskMild");
                        break;

                    case ProfilerHintStrength.Medium:
                        message =
                            Tr("ProfilerHintsExhaustedTaskMedium");
                        break;

                    default:
                        message =
                            Tr("ProfilerHintsExhaustedTaskStrong");
                        break;
                }

                message += Tr(
                    "ProfilerTaskProgress",
                    NumberText(Mathf.FloorToInt(taskPercent)));
            }
            else
            {
                message = Tr(
                    "ProfilerHintsExhaustedFixed",
                    GetStrengthName(strength));
            }

            SendPrivateMessage(message);

            BMLogger.Info(
                $"[Profiler] Indizi esauriti | " +
                $"target {impostorId}, forza {strength}.");
        }

        private static List<HintCandidate> BuildMildHints(
            ImpostorSnapshot snapshot,
            string subject)
        {
            var hints = new List<HintCandidate>();
            string name = snapshot.PlayerName ?? string.Empty;

            char[] vowels = name
                .ToLowerInvariant()
                .Where(c => "aeiou".Contains(c))
                .Distinct()
                .ToArray();

            foreach (char vowel in vowels)
            {
                hints.Add(new HintCandidate
                {
                    Key = $"mild-letter-{vowel}",
                    Text = Tr(
                        "ProfilerMildLetter",
                        subject,
                        vowel)
                });
            }

            if (name.Length > 0)
            {
                hints.Add(new HintCandidate
                {
                    Key = "mild-name-parity",
                    Text = name.Length % 2 == 0
                        ? Tr("ProfilerMildNameEven", subject)
                        : Tr("ProfilerMildNameOdd", subject)
                });

                char first = char.ToUpperInvariant(name[0]);

                if (char.IsLetter(first))
                {
                    hints.Add(new HintCandidate
                    {
                        Key = "mild-name-half",
                        Text = first <= 'M'
                            ? Tr("ProfilerMildNameAM", subject)
                            : Tr("ProfilerMildNameNZ", subject)
                    });
                }

                bool containsNumber = name.Any(char.IsDigit);

                hints.Add(new HintCandidate
                {
                    Key = "mild-name-number",
                    Text = containsNumber
                        ? Tr("ProfilerMildNameHasNumber", subject)
                        : Tr("ProfilerMildNameNoNumber", subject)
                });
            }

            int broadLevel = GetBroadLevelThreshold(snapshot.Level);

            if (snapshot.Level >= broadLevel)
            {
                hints.Add(new HintCandidate
                {
                    Key = "mild-level-above",
                    Text = Tr(
                        "ProfilerMildLevelAtLeast",
                        subject,
                        NumberText(broadLevel))
                });
            }
            else
            {
                hints.Add(new HintCandidate
                {
                    Key = "mild-level-below",
                    Text = Tr(
                        "ProfilerMildLevelBelow",
                        subject,
                        NumberText(broadLevel))
                });
            }

            string mapSide = GetMapSide(snapshot.Position);

            hints.Add(new HintCandidate
            {
                Key = "mild-map-side",
                Text = Tr(
                    "ProfilerMapSideHint",
                    subject,
                    mapSide)
            });

            return hints;
        }

        private static List<HintCandidate> BuildMediumHints(
            ImpostorSnapshot snapshot,
            string subject)
        {
            var hints = new List<HintCandidate>
            {
                new HintCandidate
                {
                    Key = "medium-pet",
                    Text = snapshot.HasPet
                        ? Tr("ProfilerMediumHasPet", subject)
                        : Tr("ProfilerMediumNoPet", subject)
                },
                new HintCandidate
                {
                    Key = "medium-hat",
                    Text = snapshot.HasHat
                        ? Tr("ProfilerMediumHasHat", subject)
                        : Tr("ProfilerMediumNoHat", subject)
                }
            };

            int lower = Mathf.Max(0, (snapshot.Level / 25) * 25);
            int upper = lower + 24;

            hints.Add(new HintCandidate
            {
                Key = "medium-level-range",
                Text = Tr(
                    "ProfilerLevelRange",
                    subject,
                    NumberText(lower),
                    NumberText(upper))
            });

            string name = snapshot.PlayerName ?? string.Empty;

            if (name.Length > 0 && char.IsLetter(name[0]))
            {
                bool startsWithVowel =
                    "aeiou".Contains(
                        char.ToLowerInvariant(name[0]));

                hints.Add(new HintCandidate
                {
                    Key = "medium-first-type",
                    Text = startsWithVowel
                        ? Tr("ProfilerMediumNameVowel", subject)
                        : Tr("ProfilerMediumNameConsonant", subject)
                });
            }

            if (!string.IsNullOrWhiteSpace(snapshot.RoomName))
            {
                hints.Add(new HintCandidate
                {
                    Key = "medium-room-group",
                    Text = Tr(
                        "ProfilerRoomGroupHint",
                        subject,
                        GetRoomGroup(snapshot.RoomName))
                });
            }

            return hints;
        }

        private static List<HintCandidate> BuildStrongHints(
            ImpostorSnapshot snapshot,
            string subject)
        {
            var hints = new List<HintCandidate>();
            string name = snapshot.PlayerName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(snapshot.RoomName))
            {
                hints.Add(new HintCandidate
                {
                    Key = "strong-room",
                    Text = Tr(
                        "ProfilerStrongRoom",
                        subject,
                        snapshot.RoomName)
                });
            }
            else
            {
                hints.Add(new HintCandidate
                {
                    Key = "strong-map-side",
                    Text = Tr(
                        "ProfilerMapSideHint",
                        subject,
                        GetMapSide(snapshot.Position))
                });
            }

            if (name.Length > 0)
            {
                char first = char.ToUpperInvariant(name[0]);

                if (char.IsLetterOrDigit(first))
                {
                    string firstText = char.IsDigit(first)
                        ? ToAlternateDigits(first.ToString())
                        : first.ToString();

                    hints.Add(new HintCandidate
                    {
                        Key = "strong-first-letter",
                        Text = Tr(
                            "ProfilerStrongFirstCharacter",
                            subject,
                            firstText)
                    });
                }

                hints.Add(new HintCandidate
                {
                    Key = "strong-name-length",
                    Text = Tr(
                        "ProfilerStrongNameLength",
                        subject,
                        NumberText(name.Length))
                });
            }

            int lower = Mathf.Max(0, (snapshot.Level / 10) * 10);
            int upper = lower + 9;

            hints.Add(new HintCandidate
            {
                Key = "strong-level-range",
                Text = Tr(
                    "ProfilerLevelRange",
                    subject,
                    NumberText(lower),
                    NumberText(upper))
            });

            string colorGroup = GetColorGroup(snapshot.ColorId);

            if (!string.IsNullOrWhiteSpace(colorGroup))
            {
                hints.Add(new HintCandidate
                {
                    Key = "strong-color-group",
                    Text = Tr(
                        "ProfilerStrongColorGroup",
                        subject,
                        colorGroup)
                });
            }

            hints.Add(new HintCandidate
            {
                Key = "strong-combined-cosmetic",
                Text = snapshot.HasPet
                    ? (snapshot.HasHat
                        ? Tr("ProfilerHasPetHasHat", subject)
                        : Tr("ProfilerHasPetNoHat", subject))
                    : (snapshot.HasHat
                        ? Tr("ProfilerNoPetHasHat", subject)
                        : Tr("ProfilerNoPetNoHat", subject))
            });

            return hints;
        }

        private static int GetBroadLevelThreshold(int level)
        {
            if (level >= 100)
                return 100;

            if (level >= 75)
                return 75;

            if (level >= 50)
                return 50;

            if (level >= 25)
                return 25;

            return 10;
        }

        private static string GetMapSide(Vector2 position)
        {
            if (position.x < -3f)
                return Tr("ProfilerMapSideLeft");

            if (position.x > 3f)
                return Tr("ProfilerMapSideRight");

            return Tr("ProfilerMapSideCenter");
        }

        private static string GetRoomGroup(string roomName)
        {
            if (IsTranslatedRoom(
                    roomName,
                    "ProfilerRoomCafeteria",
                    "ProfilerRoomAdmin",
                    "ProfilerRoomOffice"))
            {
                return Tr("ProfilerRoomGroupCentral");
            }

            if (IsTranslatedRoom(
                    roomName,
                    "ProfilerRoomLowerEngine",
                    "ProfilerRoomUpperEngine",
                    "ProfilerRoomEngineRoom",
                    "ProfilerRoomReactor",
                    "ProfilerRoomElectrical"))
            {
                return Tr("ProfilerRoomGroupTechnical");
            }

            if (IsTranslatedRoom(
                    roomName,
                    "ProfilerRoomMedBay",
                    "ProfilerRoomMedical",
                    "ProfilerRoomLaboratory",
                    "ProfilerRoomSpecimenRoom"))
            {
                return Tr("ProfilerRoomGroupMedical");
            }

            if (IsTranslatedRoom(
                    roomName,
                    "ProfilerRoomNavigation",
                    "ProfilerRoomWeapons",
                    "ProfilerRoomShields"))
            {
                return Tr("ProfilerRoomGroupExternal");
            }

            return roomName;
        }

        private static string GetColorGroup(int colorId)
        {
            switch (colorId)
            {
                case 0:
                case 3:
                case 4:
                case 5:
                case 12:
                case 13:
                case 14:
                case 17:
                    return Tr("ProfilerColorWarm");

                case 1:
                case 2:
                case 8:
                case 10:
                case 11:
                    return Tr("ProfilerColorCool");

                case 6:
                case 9:
                case 15:
                case 16:
                    return Tr("ProfilerColorDarkNeutral");

                case 7:
                    return Tr("ProfilerColorLight");

                default:
                    return string.Empty;
            }
        }

        private static string GetStrengthName(
            ProfilerHintStrength strength)
        {
            switch (strength)
            {
                case ProfilerHintStrength.Medium:
                    return Tr("ProfilerStrengthMedium");

                case ProfilerHintStrength.Strong:
                    return Tr("ProfilerStrengthStrong");

                default:
                    return Tr("ProfilerStrengthMild");
            }
        }

        private static float GetTaskCompletionPercent(
            PlayerControl player)
        {
            if (player == null || player.Data == null)
                return 0f;

            object tasksObject = ReadMember(
                player.Data,
                "Tasks",
                "tasks");

            if (!(tasksObject is IEnumerable enumerable))
                return 0f;

            int total = 0;
            int completed = 0;

            foreach (object task in enumerable)
            {
                if (task == null)
                    continue;

                total++;

                object completeValue =
                    ReadMember(task, "Complete", "complete");

                if (ConvertToBool(completeValue))
                    completed++;
            }

            if (total <= 0)
                return 0f;

            return completed * 100f / total;
        }

        private static List<PlayerControl> GetAliveImpostors()
        {
            return BanMod.AllPlayerControls
                .Where(p => p != null
                            && p.Data != null
                            && !p.Data.IsDead
                            && !p.Data.Disconnected
                            && IsImpostorAligned(p))
                .OrderBy(p => p.PlayerId)
                .ToList();
        }

        private static bool IsImpostorAligned(
            PlayerControl player)
        {
            if (player == null || player.Data == null)
                return false;

            try
            {
                if (BanMod.forceImpostor &&
                    BanMod.forcedImpostorIds.Contains(player.PlayerId))
                {
                    return true;
                }
            }
            catch
            {
                // Ignora se la modalità forced impostor non è inizializzata.
            }

            try
            {
                if (Impostor(player) ||
                    Cobra(player) ||
                    Shapeshifter(player) ||
                    Phantom(player))
                {
                    return true;
                }
            }
            catch
            {
                // Fallback tramite Role.IsImpostor.
            }

            object role = ReadMember(player.Data, "Role", "role");
            object isImpostor =
                ReadMember(role, "IsImpostor", "isImpostor");

            return ConvertToBool(isImpostor);
        }

        private static ImpostorSnapshot CreateSnapshot(
            PlayerControl player)
        {
            object data = player?.Data;
            object outfit = ReadMember(
                data,
                "DefaultOutfit",
                "defaultOutfit");

            return new ImpostorSnapshot
            {
                PlayerId = player.PlayerId,
                PlayerName = player.Data?.PlayerName ?? string.Empty,
                Level = ConvertToInt(
                    ReadMember(data, "PlayerLevel", "playerLevel")),
                HasPet = HasCosmetic(
                    ReadMember(outfit, "PetId", "petId")),
                HasHat = HasCosmetic(
                    ReadMember(outfit, "HatId", "hatId")),
                ColorId = ConvertToInt(
                    ReadMember(outfit, "ColorId", "colorId"),
                    -1),
                Position = player != null
                    ? (Vector2)player.transform.position
                    : Vector2.zero,
                RoomName = GetPlayerRoomName(player)
            };
        }

        private static bool HasCosmetic(object cosmeticId)
        {
            string value = cosmeticId?.ToString();

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string lower = value.ToLowerInvariant();

            return !lower.Contains("empty") &&
                   !lower.Contains("none") &&
                   lower != "0";
        }

        private static string GetPlayerRoomName(
            PlayerControl player)
        {
            if (player == null || ShipStatus.Instance == null)
                return string.Empty;

            Vector2 position = player.transform.position;

            object roomsObject = ReadMember(
                ShipStatus.Instance,
                "AllRooms",
                "allRooms");

            if (!(roomsObject is IEnumerable rooms))
                return string.Empty;

            foreach (object room in rooms)
            {
                if (room == null)
                    continue;

                object areaObject = ReadMember(
                    room,
                    "roomArea",
                    "RoomArea",
                    "roomCollider",
                    "RoomCollider");

                Collider2D collider = areaObject as Collider2D;

                if (collider == null &&
                    areaObject is Component areaComponent)
                {
                    collider =
                        areaComponent.GetComponent<Collider2D>();
                }

                if (collider == null &&
                    room is Component roomComponent)
                {
                    collider =
                        roomComponent.GetComponent<Collider2D>();
                }

                if (collider == null ||
                    !collider.OverlapPoint(position))
                {
                    continue;
                }

                object roomId = ReadMember(
                    room,
                    "RoomId",
                    "roomId");

                string rawName =
                    roomId?.ToString() ?? room.ToString();

                return TranslateRoomName(rawName);
            }

            return string.Empty;
        }

        private static string TranslateRoomName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            switch (rawName.Replace(" ", string.Empty)
                           .ToLowerInvariant())
            {
                case "cafeteria":
                    return Tr("ProfilerRoomCafeteria");
                case "weapons":
                    return Tr("ProfilerRoomWeapons");
                case "navigation":
                    return Tr("ProfilerRoomNavigation");
                case "o2":
                case "lifesupp":
                    return Tr("ProfilerRoomO2");
                case "shields":
                    return Tr("ProfilerRoomShields");
                case "communications":
                case "comms":
                    return Tr("ProfilerRoomCommunications");
                case "storage":
                    return Tr("ProfilerRoomStorage");
                case "admin":
                    return Tr("ProfilerRoomAdmin");
                case "electrical":
                    return Tr("ProfilerRoomElectrical");
                case "lowerengine":
                    return Tr("ProfilerRoomLowerEngine");
                case "upperengine":
                    return Tr("ProfilerRoomUpperEngine");
                case "security":
                    return Tr("ProfilerRoomSecurity");
                case "reactor":
                    return Tr("ProfilerRoomReactor");
                case "medbay":
                    return Tr("ProfilerRoomMedBay");
                case "laboratory":
                    return Tr("ProfilerRoomLaboratory");
                case "specimenroom":
                    return Tr("ProfilerRoomSpecimenRoom");
                case "office":
                    return Tr("ProfilerRoomOffice");
                case "greenhouse":
                    return Tr("ProfilerRoomGreenhouse");
                case "launchpad":
                    return Tr("ProfilerRoomLaunchpad");
                case "balcony":
                    return Tr("ProfilerRoomBalcony");
                case "records":
                    return Tr("ProfilerRoomRecords");
                case "vaultroom":
                case "vault":
                    return Tr("ProfilerRoomVault");
                case "brig":
                    return Tr("ProfilerRoomBrig");
                case "engine":
                    return Tr("ProfilerRoomEngineRoom");
                case "mainhall":
                    return Tr("ProfilerRoomMainHall");
                case "showers":
                    return Tr("ProfilerRoomShowers");
                case "meetingroom":
                    return Tr("ProfilerRoomMeetingRoom");
                case "kitchen":
                    return Tr("ProfilerRoomKitchen");
                case "cockpit":
                    return Tr("ProfilerRoomCockpit");
                case "armory":
                    return Tr("ProfilerRoomArmory");
                case "viewingdeck":
                    return Tr("ProfilerRoomViewingDeck");
                case "medical":
                    return Tr("ProfilerRoomMedical");
                case "securityroom":
                    return Tr("ProfilerRoomSecurity");
                default:
                    return rawName;
            }
        }

        private static object ReadMember(
            object source,
            params string[] names)
        {
            if (source == null || names == null)
                return null;

            Type type = source.GetType();
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.IgnoreCase;

            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(name, flags);

                    if (property != null)
                        return property.GetValue(source);
                }
                catch
                {
                    // Prova il campo.
                }

                try
                {
                    FieldInfo field =
                        type.GetField(name, flags);

                    if (field != null)
                        return field.GetValue(source);
                }
                catch
                {
                    // Prova il nome successivo.
                }
            }

            return null;
        }

        private static int ConvertToInt(
            object value,
            int fallback = 0)
        {
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ConvertToBool(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
    public static class ProfilerMeetingCapturePatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            Profiler.CaptureMeetingSnapshot(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), "Start")]
    public static class ProfilerMeetingMessagePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            Profiler.ScheduleMeetingHint(__instance);
        }
    }
}
