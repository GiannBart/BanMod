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
using UnityEngine.ProBuilder;
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
        if (AmongUsClient.Instance.AmHost && (Options.Protection10Sec.GetBool()))
        {
            if (!Options.Protection10Sec.GetBool()) return;
            Block.StartShieldTimer(PlayerControl.LocalPlayer, 10);
            NotificationPopper_AddInfoMessagePatch.AddInfoMessage(HudManager.Instance.Notifier, "KillBlock for 10S Added");
        }
    }

}


