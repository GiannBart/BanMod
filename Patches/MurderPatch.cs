//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Linq;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
internal static class CmdCheckMurder
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (__instance == PlayerControl.LocalPlayer && MurderPlayerCombinedPatch.isBlocked)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
internal static class MurderPlayerCombinedPatch
{
    public static readonly Dictionary<byte, int> misfireCount = new();
    public static bool isBlocked = false;
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags)
    {
        GameModeType gameMode = (GameModeType)Options.GameMode.GetValue();

        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (target == null || target.Data == null)
        {
            return;
        }

        bool failedProtected =
    resultFlags.HasFlag(MurderResultFlags.FailedProtected) ||
    (resultFlags.HasFlag(MurderResultFlags.DecisionByHost) &&
     target.protectedByGuardianId > -1 &&
     !resultFlags.HasFlag(MurderResultFlags.Succeeded));
        bool succeeded = resultFlags.HasFlag(MurderResultFlags.Succeeded);

        if (gameMode == GameModeType.RunOrDeath && IntroCutscene.Instance == null)
        {
            if (succeeded)
            {
                NoisemakerRunManager.PendingKillPosition = target.transform.position;
                NoisemakerRunManager.PendingRaceTrigger = true;
                BMLogger.Info("[BanMod] Trigger corsa impostato su TRUE");
            }
        }
        if (gameMode == GameModeType.SnS && !GameStates.isHideNSeek && IntroCutscene.Instance == null)
        {
            if (succeeded)
            {
                byte playerId = __instance.Data.PlayerId;

                if (target.Data.PlayerId != __instance.shapeshiftTargetPlayerId)
                {
                    if (!misfireCount.ContainsKey(playerId))
                        misfireCount[playerId] = 0;

                    misfireCount[playerId]++;
                    int currentMisfires = misfireCount[playerId];
                    float maxAllowed = Options.MisfiresToSuicide.GetFloat();

                    if (currentMisfires < maxAllowed)
                    {
                        float penaltyTime = Options.CantKillTime.GetFloat();

                        if (__instance == PlayerControl.LocalPlayer)
                        {
                            isBlocked = true;
                            LateTask.New(() =>
                            {
                                isBlocked = false;
                            }, (int)penaltyTime, "SNSResetRole");
                        }
                        else
                        {
                            __instance.RpcSetRole(RoleTypes.Crewmate);
                            __instance.isNew = true;
                            LateTask.New(() =>
                            {
                                __instance.isNew = false;
                                if (!__instance.Data.IsDead) __instance.RpcSetRole(RoleTypes.Shapeshifter, false);
                            }, (int)penaltyTime, "SNSResetRole");
                        }
                    }
                    else
                    {
                        __instance.RpcSetRole(RoleTypes.ImpostorGhost);
                    }
                }
            }
        }
        if (failedProtected)
        {
            PreviousMatchPopupTracker.RegisterEffectiveProtectionSave(target);

            FirstMeetingProtectionManager
                .ReapplyAfterProtectedKill(target);
        }
        if (succeeded)
        {
            Watcher.OnPlayerDied(target.PlayerId);

            if (BanMod.FirstDeadFriendCode == null)
            {
                var friendCode = target.Data.FriendCode;
                if (!string.IsNullOrEmpty(friendCode))
                {
                    BanMod.FirstDeadFriendCode = friendCode;
                }
            }

            BanMod.hasKilled = true;
            bool ignoreForStats = PreviousMatchPopupTracker.ConsumeIgnoredMirrorKill(target);

            if (!ignoreForStats)
            {
                PreviousMatchPopupTracker.RegisterRealKill(__instance, target);
                KillTracker.RegisterKill(__instance.PlayerId);
            }

            if (!BanMod.playerDeathTimes.ContainsKey(target.PlayerId))
            {
                BanMod.playerDeathTimes[target.PlayerId] = Time.time;
            }
        }
    }
}
