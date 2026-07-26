//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using HarmonyLib;
using System;
using UnityEngine;
using static BanMod.Utils;

namespace BanMod
{
    public static class HostAfkManager
    {
        private static bool _isHostAfk = false;
        private static float _afkTimer = 0f;
        private static float _totalAfkSeconds = 0f;
        private static Vector2 _savedPos = Vector2.zero;
        private static bool _isOutside = false;

        public static bool IsHostAfk
        {
            get => _isHostAfk;
            set
            {
                if (_isHostAfk == value) return;
                _isHostAfk = value;

                _afkTimer = 0f;
                _totalAfkSeconds = 0f;

                if (_isHostAfk)
                {
                    SendAfkNotification();
                }
                else
                {
                    ResetHostStatus();
                }
            }
        }

        public static void SendAfkNotification()
        {
            string msg = GameStates.isLobby ? Translator.GetString("HostAfkLobby") : Translator.GetString("HostAfkGame");
            Utils.SendMessage(msg);
            MessageBlocker.UpdateLastMessageTime();
        }

        private static void ResetHostStatus()
        {
            if (GameStates.IsInTask && PlayerControl.LocalPlayer != null)
            {
                Vector2 target = (_savedPos != Vector2.zero) ? _savedPos : new Vector2(-0.2f, 1.3f);
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target);
            }
            _savedPos = Vector2.zero;
            _isOutside = false;
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class HostAfkLogicPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (BanMod.IsBanModDisabled) return;
                if (BanMod.IsBanModDisabled) return;
                if (!AmongUsClient.Instance.AmHost || __instance != PlayerControl.LocalPlayer) return;
                if (IsHostAfk)
                {
                    if (GameStates.isLobby || !AmongUsClient.Instance.IsGameStarted)
                    {
                        _afkTimer = 0f;
                        _totalAfkSeconds = 0f;
                        return;
                    }

                    _afkTimer += Time.deltaTime;
                    _totalAfkSeconds += Time.deltaTime;

                    if (GameStates.IsInTask)
                    {
                        if (!_isOutside)
                        {
                            _savedPos = __instance.GetTruePosition();
                            _isOutside = true;
                        }
                        __instance.NetTransform.RpcSnapTo(new Vector2(100f, 100f));

                        if (_afkTimer >= 300f)
                        {
                            Utils.KillPlayer(__instance);
                            IsHostAfk = false; 
                        }
                    }
                }
            }

            public static string GetAfkTimerString(PlayerControl player)
            {
                if (!_isHostAfk || player == null || player != PlayerControl.LocalPlayer) return "";

                if (GameStates.isLobby || !AmongUsClient.Instance.IsGameStarted)
                {
                    return " <color=#ff0000>[HOST IS AFK]</color>";
                }

                int remaining = Mathf.Max(0, 300 - (int)_afkTimer);
                int total = (int)_totalAfkSeconds;

                return $" <color=#ff0000>[AFK for {total}s]</color>\n<color=#ffff00>[Suicide in {remaining}s]</color>\n";
            }
        }
    }
}