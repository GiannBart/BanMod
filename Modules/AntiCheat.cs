//credits and licenses in the resources folder
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BanMod;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using Internal.Threading.Tasks.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
using static BanMod.AntiCheat;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public class AntiCheat
    {
        private const string LogTag = "AntiCheat";

        private static readonly Dictionary<byte, int> taskRpcCount = new();
        private static readonly Dictionary<byte, DateTime> taskRpcLastTime = new();

        private static string GetPlayerName(PlayerControl player)
            => player?.Data?.PlayerName ?? "Unknown";

        private static string GetPlayerLabel(PlayerControl player)
            => player == null ? "null-player" : $"{GetPlayerName(player)} [pid={player.PlayerId}]";

        private static void LogDebug(string text) => BMLogger.LogDebug(text, LogTag);
        private static void LogInfo(string text) => BMLogger.Info(text, LogTag);
        private static void LogWarn(string text) => BMLogger.Warn(text, LogTag);
        private static void LogError(string text) => BMLogger.Error(text, LogTag);

        private static void ShowNotification(string message)
        {
            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, message);
            }
            else
            {
                LogWarn($"Notifier not available. Message not shown: {message}");
            }
        }

        public static void ResetMatchState()
        {
            taskRpcCount.Clear();
            taskRpcLastTime.Clear();
            VentAntiCheat.warnedPlayersVent.Clear();
            BanMod.hasSentHackWarning = false;
            LogInfo("AntiCheat state reset at match end.");
        }

        public static void GestisciRilevamentoCheat(
            PlayerControl player,
            string uiMessage,
            string reason,
            SystemTypes? systemType = null,
            byte? amount = null,
            bool addCheater = true,
            bool applyPunishment = true)
        {
            string details = $"Reason={reason} | Player={GetPlayerLabel(player)}";
            if (systemType.HasValue) details += $" | System={systemType.Value}";
            if (amount.HasValue) details += $" | Amount={amount.Value}";

            LogWarn($"Cheat detected. {details}");

            ShowNotification(uiMessage);
            SendGlobalHackWarning();

            ClientData client = ExtendedPlayerControl.GetClient(player);
            if (client == null)
            {
                LogWarn($"Null ClientData, cannot register or apply punishments. {details}");
                return;
            }

            if (addCheater)
            {
                CheaterManager.AddPlayer(client, reason);
                LogInfo($"Player added to cheater list: {GetPlayerLabel(player)} | ClientId={client.Id}");
            }

            if (!applyPunishment)
            {
                LogDebug($"Automatic punishment not applied for {GetPlayerLabel(player)}.");
                return;
            }

            int action = Options.ActionCheater.GetValue();

            if (action == 0)
            {
                LogDebug($"Punishment set to 0 (Warning Only) for {GetPlayerLabel(player)}. No kick or ban executed.");
            }
            else if (action == 1)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                LogWarn($"Kick executed for {GetPlayerLabel(player)} | ClientId={client.Id}");
            }
            else if (action == 2)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                LogWarn($"Ban executed for {GetPlayerLabel(player)} | ClientId={client.Id}");
            }
            else
            {
                LogDebug($"No automatic punishment configured for {GetPlayerLabel(player)}. Action={action}");
            }
        }

        public static class VentAntiCheat
        {
            private static readonly Dictionary<byte, int> ventRemovalsThisTick = new();
            private static float lastCheckTime = 0;

            public static readonly HashSet<byte> warnedPlayersVent = new();

            public static void RegisterVentBoot(PlayerControl player)
            {
                if (player == null || !AmongUsClient.Instance.AmHost) return;
                if (player == PlayerControl.LocalPlayer) return;
                if (Time.time - lastCheckTime > Time.fixedDeltaTime)
                {
                    ventRemovalsThisTick.Clear();
                    lastCheckTime = Time.time;
                    BMLogger.LogDebug("New vent check window started.", LogTag);
                }

                byte pid = player.PlayerId;
                if (!ventRemovalsThisTick.ContainsKey(pid))
                    ventRemovalsThisTick[pid] = 0;

                ventRemovalsThisTick[pid]++;
                BMLogger.LogDebug($"Vent boot registered for {GetPlayerLabel(player)} | TickCount={ventRemovalsThisTick[pid]}", LogTag);

                if (ventRemovalsThisTick[pid] > 3)
                {
                    if (!warnedPlayersVent.Contains(pid) && Options.KickVentCheat.GetBool() && Options.EnableAntiCheat.GetBool())
                    {
                        if (!AmongUsClient.Instance.AmHost) return;

                        warnedPlayersVent.Add(pid);
                        string msg = $"{GetPlayerName(player)}{GetAuto("VentCheat")}";

                        GestisciRilevamentoCheat(
                            player,
                            msg,
                            "Detected too many vent boots in the same tick",
                            SystemTypes.Ventilation,
                            null,
                            addCheater: true,
                            applyPunishment: false
                        );
                    }
                }
            }
        }

        public static void SendGlobalHackWarning(ulong friendCode = 0)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                LogDebug("Global warning not sent: client is not host.");
                return;
            }

            if (BanMod.hasSentHackWarning)
            {
                LogDebug("Global warning not sent: already sent in this match.");
                return;
            }

            if (!Options.SentWarning.GetBool())
            {
                LogDebug("Global warning not sent: option disabled.");
                return;
            }

            BanMod.hasSentHackWarning = true;
            string msg = GetAuto("PrincipalMessage");

            if (MessageBlocker.CanSendMessage())
            {
                Utils.SendMessage(msg);
                MessageBlocker.UpdateLastMessageTime();
                LogInfo("Global hack warning sent in chat.");
            }
            else
            {
                LogDebug("Global warning blocked by MessageBlocker cooldown.");
            }
        }

        public static void PlayerControlReceiveRpc(PlayerControl pc, byte callId, MessageReader reader)
        {
            if (callId == 218 || callId == 219) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (pc == null || reader == null) return;
            if (pc == PlayerControl.LocalPlayer) return;

            try
            {
                var rpc = (RpcCalls)callId;

                switch (rpc)
                {
                    case RpcCalls.CompleteTask:
                        DateTime nowTask = DateTime.UtcNow;
                        byte taskId = pc.PlayerId;

                        if (!taskRpcLastTime.ContainsKey(taskId))
                        {
                            taskRpcCount[taskId] = 0;
                            taskRpcLastTime[taskId] = nowTask;
                            LogDebug($"Initialized CompleteTask monitoring for {GetPlayerLabel(pc)}.");
                        }

                        double delta = (nowTask - taskRpcLastTime[taskId]).TotalSeconds;

                        if (delta < 1d)
                        {
                            taskRpcCount[taskId]++;
                            LogDebug($"CompleteTask RPC received from {GetPlayerLabel(pc)} | Interval={delta:F3}s | Count={taskRpcCount[taskId]}");

                            if (taskRpcCount[taskId] > 2 && Options.CompleteTaskCheat.GetBool() && Options.EnableAntiCheat.GetBool())
                            {
                                string msg = $"{GetPlayerName(pc)}{GetAuto("TaskCheat")}";

                                GestisciRilevamentoCheat(
                                    pc,
                                    msg,
                                    "CompleteTask RPC spam in too short an interval"
                                );

                                taskRpcCount[taskId] = 0;
                                LogDebug($"CompleteTask counter reset for {GetPlayerLabel(pc)} after detection.");
                            }
                        }
                        else
                        {
                            taskRpcCount[taskId] = 1;
                            LogDebug($"CompleteTask counter reset for {GetPlayerLabel(pc)} | Interval={delta:F3}s");
                        }

                        taskRpcLastTime[taskId] = nowTask;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling RPC {callId} for {GetPlayerLabel(pc)}: {ex}");
                ShowNotification($"AntiCheat Error: {ex.Message}");
                SendGlobalHackWarning();
            }
        }
    }
}

[HarmonyPatch]
public static class ShipStatus_UpdateSystem_Patch
{
    private const string LogTag = "AntiCheat";

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(ShipStatus), "UpdateSystem", new Type[] {
            typeof(SystemTypes),
            typeof(PlayerControl),
            typeof(Hazel.MessageReader)
        });
    }

    public static void Prefix(
        ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] Hazel.MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost || player == null || reader == null) return;
        if (MeetingHud.Instance != null) return;

        SystemTypes[] monitoredSystems = new SystemTypes[]
        {
            SystemTypes.Electrical,
            SystemTypes.LifeSupp,
            SystemTypes.Comms,
            SystemTypes.Doors,
            SystemTypes.Sabotage,
            SystemTypes.Laboratory,
            SystemTypes.HeliSabotage,
            SystemTypes.MushroomMixupSabotage,
            SystemTypes.Reactor,
            SystemTypes.Ventilation
        };

        if (Array.IndexOf(monitoredSystems, systemType) == -1) return;

        string playerName = player.Data?.PlayerName ?? "Unknown";
        bool isAuthorized = Utils.Impostor(player) || Utils.Phantom(player) || Utils.Shapeshifter(player) || Utils.ImpostorTeam(player);

        if (systemType == SystemTypes.Ventilation)
        {
            BMLogger.LogDebug($"Received UpdateSystem Ventilation from {playerName} [pid={player.PlayerId}]", LogTag);
            AntiCheat.VentAntiCheat.RegisterVentBoot(player);
            return;
        }

        int originalPos = reader.Position;
        byte amount = reader.ReadByte();
        reader.Position = originalPos;

        BMLogger.LogDebug($"UpdateSystem intercepted | Player={playerName} [pid={player.PlayerId}] | System={systemType} | Amount={amount} | Authorized={isAuthorized}", LogTag);

        if ((amount == 128) && !isAuthorized && Options.SabotageCheat2.GetBool() && Options.EnableAntiCheat.GetBool())
        {
            if (!AmongUsClient.Instance.AmHost) return;
            StartFixAfterDelay(playerName, player, systemType, amount, "Sabotage sent by unauthorized player");
        }

        if (systemType == SystemTypes.Sabotage)
        {
            if (((amount == 3) || (amount == 7) || (amount == 14) || (amount == 8) || (amount == 21) || (amount == 58) || (amount == 57))
                && !isAuthorized
                && Options.SabotageCheat2.GetBool()
                && Options.EnableAntiCheat.GetBool())
            {
                if (!AmongUsClient.Instance.AmHost) return;
                StartFixAfterDelay(playerName, player, systemType, amount, "Sabotage trigger sent by unauthorized player");
            }
        }
        else if ((amount == 69) && !isAuthorized && Options.SabotageCheat2.GetBool() && Options.EnableAntiCheat.GetBool())
        {
            if (!AmongUsClient.Instance.AmHost) return;

            string msg = $"{playerName}{GetAuto("SabotageCheat")} UnfixLight";

            AntiCheat.GestisciRilevamentoCheat(
                player,
                msg,
                "Unauthorized UnfixLight attempt",
                systemType,
                amount
            );
        }
        else if (systemType == SystemTypes.Electrical &&
                 amount <= 4 &&
                 Options.SabotageCheat2.GetBool() &&
                 Options.EnableAntiCheat.GetBool() &&
                 !IsNearElectricalPanel(player))
        {
            string msg = $"{playerName} {GetAuto("AutoFixCheat")}";

            AntiCheat.GestisciRilevamentoCheat(
                player,
                msg,
                "Lights fixed from invalid position",
                systemType,
                amount
            );
        }
        else if (systemType == SystemTypes.LifeSupp &&
                 amount == 16 &&
                 Options.SabotageCheat2.GetBool() &&
                 Options.EnableAntiCheat.GetBool() &&
                 !IsNearOxygenPanel(player))
        {
            string msg = $"{playerName} {GetAuto("OxygenFixCheat")}";

            AntiCheat.GestisciRilevamentoCheat(
                player,
                msg,
                "Oxygen fixed from invalid position",
                systemType,
                amount
            );
        }
        else if ((systemType == SystemTypes.Reactor || systemType == SystemTypes.Laboratory || systemType == SystemTypes.HeliSabotage) &&
                 amount == 16 &&
                 Options.SabotageCheat2.GetBool() &&
                 Options.EnableAntiCheat.GetBool() &&
                 !IsNearReactorPanel(player))
        {
            string msg = $"{playerName} {GetAuto("ReactorFixCheat")}";

            AntiCheat.GestisciRilevamentoCheat(
                player,
                msg,
                "Reactor/Laboratory/Helicopter fixed from invalid position",
                systemType,
                amount
            );
        }
        else if (systemType == SystemTypes.Comms &&
                 amount == 16 &&
                 Options.SabotageCheat2.GetBool() &&
                 Options.EnableAntiCheat.GetBool() &&
                 !IsNearCommsPanel(player))
        {
            string msg = $"{playerName} {GetAuto("CommsFixCheat")}";

            AntiCheat.GestisciRilevamentoCheat(
                player,
                msg,
                "Comms fixed from invalid position",
                systemType,
                amount
            );
        }
    }

    private static void StartFixAfterDelay(string playerName, PlayerControl player, SystemTypes systemType, byte amount, string reason)
    {
        if (ShipStatus.Instance == null)
        {
            BMLogger.Warn($"Cannot schedule sabotage fix: ShipStatus.Instance is null | Player={playerName} | System={systemType} | Amount={amount}", LogTag);
            return;
        }

        BMLogger.Warn($"Sabotage fix scheduled | Reason={reason} | Player={playerName} [pid={player.PlayerId}] | System={systemType} | Amount={amount}", LogTag);
        ShipStatus.Instance.StartCoroutine(FixSabotagesCoroutine(playerName, player, systemType, amount));
    }

    private static IEnumerator FixSabotagesCoroutine(string playerName, PlayerControl player, SystemTypes systemType, byte amount)
    {
        yield return new WaitForSeconds(0.1f);

        if (ShipStatus.Instance == null)
        {
            BMLogger.Warn($"Sabotage fix cancelled: ShipStatus.Instance is null at execution time | Player={playerName} | System={systemType}", LogTag);
            yield break;
        }

        string sabotageName;
        switch (systemType)
        {
            case SystemTypes.Reactor:
                sabotageName = " Reactor";
                break;
            case SystemTypes.LifeSupp:
                sabotageName = " Oxygen";
                break;
            case SystemTypes.Comms:
                sabotageName = " Comms";
                break;
            case SystemTypes.Electrical:
                sabotageName = " Lights";
                break;
            case SystemTypes.Laboratory:
                sabotageName = " Laboratory";
                break;
            case SystemTypes.HeliSabotage:
                sabotageName = " Helicopter";
                break;
            default:
                sabotageName = " Sabotage";
                break;
        }

        string msg = $"{playerName} {GetAuto("SabotageCheat")}{sabotageName}";

        AntiCheat.GestisciRilevamentoCheat(
            player,
            msg,
            $"Forced fix of sabotage{sabotageName}",
            systemType,
            amount
        );

        BMLogger.Info($"Starting automatic sabotage correction for {playerName} | Origin system={systemType} | Amount={amount}", LogTag);

        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16 | 0);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, 16 | 1);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 0);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 1);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16 | 0);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16 | 1);

        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Electrical, out var system))
        {
            var elecSys = system.Cast<SwitchSystem>();
            for (var i = 0; i < 5; i++)
            {
                int switchMask = 1 << (i & 0x1F);
                if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                {
                    BMLogger.LogDebug($"Automatic light switch correction index={i}", LogTag);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                }
            }
        }

        BMLogger.Info($"Automatic sabotage correction completed for {playerName}.", LogTag);
    }

    static bool IsNearElectricalPanel(PlayerControl player)
    {
        Vector2 pos = player.GetTruePosition();
        MapNames mapId = GetCurrentMap();
        if ((int)mapId == -1) return false;

        const float defaultElectricalRange = 3.2f;
        const float airshipElectricalRange = 3.4f;

        if (mapId == MapNames.Airship)
        {
            Vector2[] airshipPanels = new Vector2[]
            {
            new Vector2(-12.82f, -11.27f),
            new Vector2(30.66f, 2.08f),
            new Vector2(13.97f, 6.35f)
            };

            return airshipPanels.Any(p => Vector2.Distance(pos, p) < airshipElectricalRange);
        }

        return Vector2.Distance(pos, GetElectricalPanelPos(mapId)) < defaultElectricalRange;
    }

    static bool IsNearOxygenPanel(PlayerControl player)
    {
        Vector2 pos = player.GetTruePosition();
        MapNames mapId = GetCurrentMap();
        if ((int)mapId == -1) return false;

        Vector2[] panels = mapId switch
        {
            MapNames.Skeld => new Vector2[]
            {
                new Vector2(6.52f, -6.61f),
                new Vector2(6.81f, -3.07f)
            },
            MapNames.MiraHQ => new Vector2[]
            {
                new Vector2(17.57f, 24.22f),
                new Vector2(4.03f, -0.63f)
            },
            _ => Array.Empty<Vector2>()
        };
        return panels.Any(p => Vector2.Distance(pos, p) < 2f);
    }

    static bool IsNearReactorPanel(PlayerControl player)
    {
        Vector2 pos = player.GetTruePosition();
        MapNames mapId = GetCurrentMap();
        if ((int)mapId == -1) return false;

        Vector2[] panels = mapId switch
        {
            MapNames.Skeld => new Vector2[]
            {
                new Vector2(-21.28f, -1.69f),
                new Vector2(-21.28f, -8.62f)
            },
            MapNames.MiraHQ => new Vector2[]
            {
                new Vector2(4.67f, 14.47f),
                new Vector2(0.24f, 14.49f)
            },
            MapNames.Polus => new Vector2[]
            {
                new Vector2(24.40f, -3.05f),
                new Vector2(4.43f, -3.91f)
            },
            MapNames.Airship => new Vector2[]
            {
                new Vector2(3.89f, 9.76f),
                new Vector2(11.49f, 6.35f)
            },
            MapNames.Fungle => new Vector2[]
            {
                new Vector2(20.95f, -6.01f),
                new Vector2(23.98f, -7.67f)
            },
            _ => Array.Empty<Vector2>()
        };
        return panels.Any(p => Vector2.Distance(pos, p) < 2f);
    }

    static bool IsNearCommsPanel(PlayerControl player)
    {
        Vector2 pos = player.GetTruePosition();
        MapNames mapId = GetCurrentMap();
        if ((int)mapId == -1) return false;

        Vector2[] panels = mapId switch
        {
            MapNames.Skeld => new Vector2[]
            {
                new Vector2(4.27f, -16.39f)
            },
            MapNames.MiraHQ => new Vector2[]
            {
                new Vector2(15.11f, 5.17f),
                new Vector2(13.79f, 18.93f),
                new Vector2(13.68f, 19.97f),
                new Vector2(14.46f, 19.24f)
            },
            MapNames.Polus => new Vector2[]
            {
                new Vector2(13.86f, -15.30f)
            },
            MapNames.Airship => new Vector2[]
            {
                new Vector2(-13.98f, 2.17f)
            },
            MapNames.Fungle => new Vector2[]
            {
                new Vector2(24.57f, 13.76f),
                new Vector2(8.25f, 0.43f)
            },
            _ => Array.Empty<Vector2>()
        };
        return panels.Any(p => Vector2.Distance(pos, p) < 2f);
    }

    static Vector2 GetElectricalPanelPos(MapNames mapId)
    {
        return mapId switch
        {
            MapNames.Skeld => new Vector2(-10.05f, -10.24f),
            MapNames.MiraHQ => new Vector2(14.74f, 21.08f),
            MapNames.Polus => new Vector2(9.60f, -11.44f),
            _ => Vector2.zero
        };
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
public static class GameEndPatchBM_AntiCheat
{
    private static void Postfix()
    {
        if (BanMod.BanMod.IsBanModDisabled) return;
        if (!AmongUsClient.Instance.AmHost) return;
        AntiCheat.ResetMatchState();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
public static class CloseDoorsOfTypePatch
{
    private const string LogTag = "AntiCheat";

    private static int closeDoorsCount = 0;
    private static float firstCallTime = 0f;
    private static bool warningSent = false;

    public static void Prefix(ShipStatus __instance, SystemTypes room)
    {
        if (BanMod.BanMod.IsBanModDisabled) return;
        if (!AmongUsClient.Instance.AmHost) return;

        float now = Time.time;

        if (firstCallTime == 0f || now - firstCallTime > 1f)
        {
            closeDoorsCount = 0;
            firstCallTime = now;
            warningSent = false;
            BMLogger.LogDebug("Door check window reset.", LogTag);
        }

        closeDoorsCount++;
        BMLogger.LogDebug($"CloseDoorsOfType intercepted | Room={room} | Count={closeDoorsCount}", LogTag);

        if (closeDoorsCount > 4 && !warningSent && (Options.ClooseDoorsCheat.GetBool() && Options.EnableAntiCheat.GetBool()))
        {
            if (!AmongUsClient.Instance.AmHost) return;

            string msg = GetAuto("AnonimousCheat");
            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, msg);
            }
            else
            {
                BMLogger.Warn($"Notifier not available during door spam report. Message={msg}", LogTag);
            }

            warningSent = true;
            BMLogger.Warn($"Possible door close spam detected | Count={closeDoorsCount} | Room={room}", LogTag);
        }
    }
}
