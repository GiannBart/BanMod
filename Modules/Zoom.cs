//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BanMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class Zoom
    {
        private static bool ResetButtons;

        public static void Postfix()
        {
            try
            {
                if (Camera.main == null || HudManager.Instance == null) return;

                if (HudManager.Instance.Chat?.IsOpenOrOpening == true) return;

                bool canZoom = (BanMod.EnableZoom.Value || PlayerMouseController.zoomkey) &&
                               (GameStates.isLobby || GameStates.IsMeeting || PlayerControl.LocalPlayer.Data.IsDead);

                if (canZoom)
                {
                    float scroll = Input.mouseScrollDelta.y;

                    if (scroll > 0f) 
                    {
                        if (Camera.main.orthographicSize > 3.0f)
                        {
                            SetZoomSize(times: false);
                        }
                    }
                    else if (scroll < 0f) 
                    {
                        if (Camera.main.orthographicSize < 18.0f)
                        {
                            SetZoomSize(times: true);
                        }
                    }

                    Flag.NewFlag("Zoom");
                }
                else
                {
                    Flag.Run(() => {
                        SetZoomSize(reset: true);
                    }, "Zoom");
                }

                if (ResetButtons && Mathf.Approximately(Camera.main.orthographicSize, 3.0f))
                {
                    RefreshUI();
                    ResetButtons = false;
                }
            }
            catch { }
        }

        public static void SetZoomSize(bool times = false, bool reset = false)
        {
            if (Camera.main == null || HudManager.Instance == null) return;

            if (reset)
            {
                Camera.main.orthographicSize = 3.0f;
                HudManager.Instance.UICamera.orthographicSize = 3.0f;

                if (HudManager.Instance.Chat)
                    HudManager.Instance.Chat.transform.localScale = Vector3.one;

                if (GameStates.IsMeeting && MeetingHud.Instance)
                    MeetingHud.Instance.transform.localScale = Vector3.one;

                RefreshUI();
                ResetButtons = false;
            }
            else
            {
                float sizeMultiplier = times ? 1.5f : 0.6666667f;

                Camera.main.orthographicSize *= sizeMultiplier;
                HudManager.Instance.UICamera.orthographicSize *= sizeMultiplier;

                if (Camera.main.orthographicSize < 3.0f)
                {
                    Camera.main.orthographicSize = 3.0f;
                    HudManager.Instance.UICamera.orthographicSize = 3.0f;
                }

                ResetButtons = true;
            }

            UpdateShadows();
        }

        private static void RefreshUI()
        {
            ResolutionManager.ResolutionChanged?.Invoke(
                (float)Screen.width / Screen.height,
                Screen.width,
                Screen.height,
                Screen.fullScreen
            );
        }

        private static void UpdateShadows()
        {
            if (DestroyableSingleton<HudManager>.Instance?.ShadowQuad != null)
            {
                bool isStandard = Mathf.Approximately(Camera.main.orthographicSize, 3.0f);
                DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(isStandard && PlayerControl.LocalPlayer.IsAlive());
            }
        }

        public static void OnFixedUpdate()
        {
            UpdateShadows();
        }
    }

    public static class Flag
    {
        private static readonly List<string> OneTimeList = new List<string>();
        private static readonly List<string> FirstRunList = new List<string>();

        public static void Run(Action action, string type, bool firstrun = false)
        {
            if (OneTimeList.Contains(type) || (firstrun && !FirstRunList.Contains(type)))
            {
                if (!FirstRunList.Contains(type)) FirstRunList.Add(type);
                OneTimeList.Remove(type);
                action();
            }
        }

        public static void NewFlag(string type)
        {
            if (!OneTimeList.Contains(type)) OneTimeList.Add(type);
        }
    }
}