//credits and licenses in the resources folder
using System;
using HarmonyLib;
using UnityEngine;

namespace BanMod
{
    public static class MenuRouter
    {
        public enum Panel
        {
            None,
            Host,
            Moderator,
            MsgMenu,
            PlayerTasks,
            SkinUI,
            VisualOptions,
            MusicPlayer,
            PlayerUI,
            Keybinds,
            Presets
        }

        [Flags]
        public enum RequiredRefs
        {
            None = 0,
            Client = 1 << 0,
            LocalPlayer = 1 << 1,
            PlayerData = 1 << 2,
            GameData = 1 << 3,
            Hud = 1 << 4,
            Ship = 1 << 5
        }

        public static Panel Current { get; private set; } = Panel.None;

        public static event Action<Panel> OnPanelChanged;

        private static bool _isBroadcasting;
        private static float _nextWarnTime;

        public static void Open(Panel panel)
        {
            if (panel == Panel.None)
            {
                SetCurrent(Panel.None, true);
                return;
            }

            RequiredRefs req = GetRequirements(panel);

            if (req != RequiredRefs.None && !AreRefsReady(req))
            {
                SetCurrent(Panel.None, true);
                WarnThrottled("[BanMod] Apertura menu '" + panel + "' annullata: riferimenti non validi.");
                return;
            }

            SetCurrent(panel, true);
        }

        public static void Toggle(Panel panel)
        {
            if (Current == panel)
                Open(Panel.None);
            else
                Open(panel);
        }

        public static void Tick()
        {
            if (Current == Panel.None)
                return;

            RequiredRefs req = GetRequirements(Current);

            if (req != RequiredRefs.None && !AreRefsReady(req))
            {
                WarnThrottled("[BanMod] Menu '" + Current + "' chiuso: riferimenti persi.");
                SetCurrent(Panel.None, true);
            }
        }

        public static bool IsGameContextReady()
        {
            return AreRefsReady(
                RequiredRefs.Client |
                RequiredRefs.LocalPlayer |
                RequiredRefs.PlayerData |
                RequiredRefs.GameData |
                RequiredRefs.Hud
            );
        }

        public static bool IsPanelProtected(Panel panel)
        {
            return GetRequirements(panel) != RequiredRefs.None;
        }

        public static RequiredRefs GetRequirements(Panel panel)
        {
            switch (panel)
            {
                // Menu che possono aprirsi anche dal main menu.
                case Panel.None:
                case Panel.MusicPlayer:
                case Panel.Keybinds:
                    return RequiredRefs.None;

                // Menu che richiedono riferimenti validi.
                case Panel.Host:
                case Panel.Moderator:
                case Panel.MsgMenu:
                case Panel.PlayerTasks:
                case Panel.SkinUI:
                case Panel.VisualOptions:
                case Panel.PlayerUI:
                case Panel.Presets:
                    return RequiredRefs.Client |
                           RequiredRefs.LocalPlayer |
                           RequiredRefs.PlayerData |
                           RequiredRefs.GameData |
                           RequiredRefs.Hud;

                // Ogni nuovo menu viene protetto di default.
                default:
                    return RequiredRefs.Client |
                           RequiredRefs.LocalPlayer |
                           RequiredRefs.PlayerData |
                           RequiredRefs.GameData |
                           RequiredRefs.Hud;
            }
        }

        public static bool AreRefsReady(RequiredRefs req)
        {
            try
            {
                if ((req & RequiredRefs.Client) != 0)
                {
                    if (AmongUsClient.Instance == null)
                        return false;
                }

                if ((req & RequiredRefs.LocalPlayer) != 0)
                {
                    if (PlayerControl.LocalPlayer == null)
                        return false;
                }

                if ((req & RequiredRefs.PlayerData) != 0)
                {
                    if (PlayerControl.LocalPlayer == null)
                        return false;

                    if (PlayerControl.LocalPlayer.Data == null)
                        return false;
                }

                if ((req & RequiredRefs.GameData) != 0)
                {
                    if (GameData.Instance == null)
                        return false;
                }

                if ((req & RequiredRefs.Hud) != 0)
                {
                    if (HudManager.Instance == null)
                        return false;
                }

                if ((req & RequiredRefs.Ship) != 0)
                {
                    if (ShipStatus.Instance == null)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetCurrent(Panel panel, bool forceBroadcast)
        {
            if (!forceBroadcast && Current == panel)
                return;

            Current = panel;
            SafeBroadcast(panel);
        }

        private static void SafeBroadcast(Panel panel)
        {
            if (_isBroadcasting)
                return;

            _isBroadcasting = true;

            try
            {
                Action<Panel> handlers = OnPanelChanged;

                if (handlers == null)
                    return;

                foreach (Action<Panel> callback in handlers.GetInvocationList())
                {
                    try
                    {
                        callback(panel);
                    }
                    catch (Exception ex)
                    {
                        WarnThrottled("[BanMod] Handler menu bloccato: " + ex.GetType().Name + " - " + ex.Message);
                    }
                }
            }
            finally
            {
                _isBroadcasting = false;
            }
        }

        public static bool ShouldSilenceHudException(Exception ex)
        {
            if (ex == null)
                return false;

            string text = ex.ToString();

            bool isNullRef =
                ex is NullReferenceException ||
                text.Contains("NullReferenceException") ||
                text.Contains("Object reference not set to an instance of an object");

            if (!isNullRef)
                return false;

            // Se il contesto non è pronto, chiudi ogni menu e blocca solo questa NullReference.
            if (!IsGameContextReady())
            {
                if (Current != Panel.None)
                    SetCurrent(Panel.None, true);

                return true;
            }

            // Se un menu protetto è aperto e causa una NullReference, chiudilo.
            if (IsPanelProtected(Current))
            {
                SetCurrent(Panel.None, true);
                return true;
            }

            return false;
        }

        private static void WarnThrottled(string msg)
        {
            try
            {
                if (Time.unscaledTime < _nextWarnTime)
                    return;

                _nextWarnTime = Time.unscaledTime + 2f;
                Debug.LogWarning(msg);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class MenuRouterHudStartGuardPatch
    {
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            if (MenuRouter.ShouldSilenceHudException(__exception))
                return null;

            return __exception;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class MenuRouterHudUpdateGuardPatch
    {
        public static void Prefix()
        {
            MenuRouter.Tick();
        }

        public static Exception Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            if (MenuRouter.ShouldSilenceHudException(__exception))
                return null;

            return __exception;
        }
    }
}