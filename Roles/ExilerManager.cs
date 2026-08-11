//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using Rewired.UI.ControlMapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class ExilerManager
    {
        public static GameObject ExilerUI = null;

        private const string ButtonName = "BANMOD_ExilerButton";

        public static IEnumerator WaitForButtonsAndCreate(MeetingHud __instance)
        {
            while (__instance == null)
                yield return null;

            while (__instance.playerStates == null || __instance.playerStates.Count == 0)
                yield return null;

            while (__instance.playerStates.All(pva => pva == null || pva.Buttons == null))
                yield return null;

            yield return null;

            CreateExilerButton(__instance);
        }

        public static void CreateExilerButton(MeetingHud __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                if (ChatCommands.ComandoExeUsed)
                    return;

                PlayerControl local = PlayerControl.LocalPlayer;

                if (local == null || local.Data == null)
                    return;

                if (local.Data.IsDead)
                    return;

                if (!RoleOptionSyncHelper.CanReadHostOptions())
                    return;

                if (!RoleOptionSyncHelper.IsExilerEnabled())
                    return;

                byte exilerId = RoleOptionSyncHelper.GetExilerId();

                if (exilerId == byte.MaxValue || exilerId == 255)
                    return;

                if (local.PlayerId != exilerId)
                    return;

                foreach (var pva in __instance.playerStates.ToArray())
                {
                    try
                    {
                        if (pva == null || pva.transform == null || pva.Buttons == null)
                            continue;

                        if (pva.transform.Find(ButtonName) != null)
                            continue;

                        byte targetId = pva.TargetPlayerId;

                        PlayerControl pc = Utils.GetPlayerById(targetId);

                        if (pc == null || pc.Data == null || !pc.IsAlive())
                            continue;

                        Transform cancelButtonTransform = pva.Buttons.transform.Find("CancelButton");

                        if (cancelButtonTransform == null || cancelButtonTransform.gameObject == null)
                            continue;

                        GameObject template = cancelButtonTransform.gameObject;
                        GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);

                        targetBox.name = ButtonName;
                        targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
                        targetBox.transform.localScale = template.transform.localScale;
                        targetBox.transform.localRotation = template.transform.localRotation;
                        targetBox.SetActive(true);

                        Sprite sprite = Utils.LoadSprite("BanMod.Resources.image.TargetIconExile.png", 100f);

                        SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();

                        if (renderer != null)
                        {
                            renderer.sprite = sprite;
                            renderer.enabled = true;
                        }

                        PassiveButton button = targetBox.GetComponent<PassiveButton>();

                        if (button == null)
                            continue;

                        button.OnClick.RemoveAllListeners();
                        button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                        {
                            try
                            {
                                if (PlayerControl.LocalPlayer == null ||
                                    PlayerControl.LocalPlayer.Data == null ||
                                    PlayerControl.LocalPlayer.Data.IsDead ||
                                    ExilerUI != null)
                                {
                                    return;
                                }

                                ExilerOnClick(targetId, __instance);
                            }
                            catch
                            {
                            }
                        }));
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static void ExilerOnClick(byte playerId, MeetingHud __instance)
        {
            if (!RoleOptionSyncHelper.CanReadHostOptions())
                return;

            if (!RoleOptionSyncHelper.IsExilerEnabled())
                return;

            PlayerControl local = PlayerControl.LocalPlayer;

            if (local == null || local.Data == null || local.Data.IsDead)
                return;

            byte exilerId = RoleOptionSyncHelper.GetExilerId();

            if (local.PlayerId != exilerId)
                return;

            bool isHost = RoleOptionSyncHelper.IsHost();
            InternalShowConfirmation(playerId, __instance, isHost);
        }

        private static void InternalShowConfirmation(byte playerId, MeetingHud __instance, bool isHost)
        {
            if (!RoleOptionSyncHelper.CanReadHostOptions())
                return;

            if (!RoleOptionSyncHelper.IsExilerEnabled())
                return;

            PlayerControl local = PlayerControl.LocalPlayer;

            if (local == null || local.Data == null || local.Data.IsDead)
                return;

            if (local.PlayerId != RoleOptionSyncHelper.GetExilerId())
                return;

            PlayerControl pc = Utils.GetPlayerById(playerId);

            if (pc == null || pc.Data == null || !pc.IsAlive() || ExilerUI != null)
                return;

            __instance.playerStates.ToList().ForEach(x =>
            {
                if (x != null && x.gameObject != null)
                    x.gameObject.SetActive(false);
            });

            if (__instance.SkipVoteButton != null && __instance.SkipVoteButton.gameObject != null)
                __instance.SkipVoteButton.gameObject.SetActive(false);

            Transform closeMeetingButton = __instance.transform.Find("CloseMeetingButton");

            if (closeMeetingButton != null)
                closeMeetingButton.gameObject.SetActive(false);

            GameObject canvasGO = new GameObject("ExilerUICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            ExilerUI = canvasGO;

            GameObject container = new GameObject("ExilerUIContainer");
            container.transform.SetParent(canvasGO.transform, false);

            GameObject confirmTextObj = new GameObject("ConfirmText");
            confirmTextObj.transform.SetParent(container.transform, false);

            TextMeshProUGUI tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetString("ConfirmExeMessage")} {pc.name}?";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.color = Color.red;

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(400, 100);
            rect.anchoredPosition = new Vector2(0, 120f);

            GameObject yesButtonObj = new GameObject("YesButton");
            yesButtonObj.transform.SetParent(container.transform, false);

            RectTransform yesRect = yesButtonObj.AddComponent<RectTransform>();
            yesRect.sizeDelta = new Vector2(160, 60);
            yesRect.anchoredPosition = new Vector2(-100, -40);

            Image yesImage = yesButtonObj.AddComponent<Image>();
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            Button yesButton = yesButtonObj.AddComponent<Button>();
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                RoleCommandActionRpc.Send(pc.PlayerId);
                RestoreMeetingUI(__instance, closeMeetingButton);
            }));

            AddTextToButton(yesButtonObj, GetString("ConfirmButtonText"));

            GameObject noButtonObj = new GameObject("NoButton");
            noButtonObj.transform.SetParent(container.transform, false);

            RectTransform noRect = noButtonObj.AddComponent<RectTransform>();
            noRect.sizeDelta = new Vector2(160, 60);
            noRect.anchoredPosition = new Vector2(100, -40);

            Image noImage = noButtonObj.AddComponent<Image>();
            noImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button noButton = noButtonObj.AddComponent<Button>();
            noButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                RestoreMeetingUI(__instance, closeMeetingButton);
            }));

            AddTextToButton(noButtonObj, GetString("Cancel"));
        }

        private static void AddTextToButton(GameObject parent, string text)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 30;
            tmp.color = Color.black;

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void RestoreMeetingUI(MeetingHud __instance, Transform closeMeetingButton)
        {
            if (__instance != null)
            {
                __instance.playerStates.ToList().ForEach(x =>
                {
                    if (x != null && x.gameObject != null)
                        x.gameObject.SetActive(true);

                    if (x != null && x.Buttons != null)
                        x.Buttons.transform.gameObject.SetActive(false);
                });

                if (__instance.SkipVoteButton != null && __instance.SkipVoteButton.gameObject != null)
                    __instance.SkipVoteButton.gameObject.SetActive(true);

                if (closeMeetingButton != null)
                    closeMeetingButton.gameObject.SetActive(true);
            }

            if (ExilerUI != null)
            {
                UnityEngine.Object.Destroy(ExilerUI);
                ExilerUI = null;
            }
        }

        public static void ResetForNewGame()
        {
            CleanupAfterMeeting();
        }

        public static void CleanupAfterMeeting()
        {
            if (ExilerUI != null)
            {
                UnityEngine.Object.Destroy(ExilerUI);
                ExilerUI = null;
            }
        }
    }
}
