//credits and licenses in the resources folder
using AmongUs.Data;
using AmongUs.GameOptions;
using BanMod;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using static BanMod.BanMod;
using static BanMod.Translator;
using static BanMod.Utils;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static UnityEngine.GraphicsBuffer;
using GameStates = BanMod.GameStates;

namespace BanMod
{
    public static class Block
    {
        public static float ShieldEndTime = 0f;
        private static Coroutine shieldCoroutine;

        public static HashSet<byte> InitialProtectedPlayers = new HashSet<byte>();
        public static HashSet<byte> PlayersLostProtection = new HashSet<byte>();

        public static void StartShieldTimer(MonoBehaviour caller, float durationSeconds)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.isLobby) return;
            if (shieldCoroutine != null)
            {
                caller.StopCoroutine(shieldCoroutine);
                BanMod.ShieldedPlayers.Clear();
            }

            ShieldEndTime = Time.time + durationSeconds;
            InitialProtectedPlayers.Clear();
            PlayersLostProtection.Clear();

            shieldCoroutine = caller.StartCoroutine(ShieldCoroutine(durationSeconds));
        }

        private static IEnumerator ShieldCoroutine(float durationSeconds)
        {
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.IsGameOver)
                yield break;

            BanMod.ShieldedPlayers.Clear();

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data != null && !player.Data.IsDead)
                {
                    BanMod.ShieldedPlayers.Add(player.PlayerId);
                    InitialProtectedPlayers.Add(player.PlayerId);

                }
            }


            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.IsGameOver)
                {
                    BanMod.ShieldedPlayers.Clear();
                    shieldCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player?.Data != null && !player.Data.IsDead)
                {
                    if (BanMod.ShieldedPlayers.Contains(player.PlayerId))
                    {
                        BanMod.ShieldedPlayers.Remove(player.PlayerId);
                        player.RemoveProtection();
                        player.protectedByGuardianId = -1;
                        player.Data.MarkDirty();
                    }
                }
            }

            BanMod.ShieldedPlayers.Clear();
            InitialProtectedPlayers.Clear();
            PlayersLostProtection.Clear();

            if (HudManager.Instance?.Notifier != null)
            {
                NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, "KillBlock terminato.");
            }

            shieldCoroutine = null;
        }
    }
}


[HarmonyPatch(typeof(HudManager), nameof(HudManager.OnGameStart))]
public static class HudManagerInitializePatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.AmHost && (Protection))
        {
            if (!Protection) return;
            Block.StartShieldTimer(PlayerControl.LocalPlayer, 10);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, "KillBlock for 10S Added");
        }
    }

}


[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
public static class NumberOptionLimitPatch
{
    private static readonly HashSet<StringNames> LockedOptions = new() 
    {
        StringNames.GameNumImpostors,
        StringNames.GamePlayerSpeed,
        StringNames.GameKillCooldown,
        StringNames.ViperDissolveTime,
        StringNames.CapacityLabel,
        StringNames.ViperDissolveTime
    };

    public static void Postfix(NumberOption __instance)
    {
        if (__instance == null)
            return;

        if (GameStates.isHideNSeek)
            return;

        if (__instance.Title == StringNames.ViperDissolveTime)
        {
            __instance.ValidRange = new FloatRange(1f, 180f);
            __instance.Increment = 1f;
            return;
        }

        if (LockedOptions.Contains(__instance.Title))
            return; 

        __instance.ValidRange = new FloatRange(0f, 999f);

    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
public static class GameOptionsMenuInitializePatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        if (__instance == null)
            return;

        if (GameStates.isHideNSeek)
            return;

        foreach (var ob in __instance.Children)
        {
            var numOpt = ob.TryCast<NumberOption>();
            if (numOpt == null) continue;

            switch (ob.Title)
            {
                case StringNames.GameKillCooldown:
                    numOpt.ValidRange = new FloatRange(0.001f, 180f);
                    break;

                case StringNames.GameNumImpostors:
                    numOpt.ValidRange = new FloatRange(1f, 3f);
                    break;

                case StringNames.GamePlayerSpeed:
                    numOpt.ValidRange = new FloatRange(0.50f, 3f);
                    break;

                case StringNames.ViperDissolveTime:
                    numOpt.ValidRange = new FloatRange(1f, 180f);
                    numOpt.Increment = 1f;
                    break;

                case StringNames.CapacityLabel:
                    numOpt.ValidRange = new FloatRange(4f, 15f);
                    break;
            }
        }
    }
}



[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetCoolDown))]
public static class Patch_ActionButton_SetCoolDown
{
    static bool Prefix(ActionButton __instance, ref float timer, ref float maxTimer, ref bool __state)
    {
        __state = false;

        try
        {
            if (IsBanModDisabled)
                return true;

            if (__instance == null)
                return true;

            if (GameStates.isHideNSeek)
                return true;

            if (maxTimer > 0f)
                return true;

            __state = true;

            timer = 0f;
            maxTimer = 1f;

            return true;
        }
        catch
        {
            return true;
        }
    }

    static void Postfix(ActionButton __instance, bool __state)
    {
        try
        {
            if (!__state)
                return;

            if (IsBanModDisabled)
                return;

            if (__instance == null)
                return;

            __instance.SetCooldownFill(0.1f);
        }
        catch
        {
        }
    }
}