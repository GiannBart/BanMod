//credits and licenses in the resources folder
using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class CloseMeetingManager
    {
        public static GameObject closeMeetingConfirmUI = null;

        private const string CloseMeetingButtonName = "CloseMeetingButton";
        private const string SmallRightButtonName = "SmallRightButton";

        private const string CloseMeetingIconPath = "BanMod.Resources.image.CloseMeetingIcon.png";
        private const string SmallRightButtonIconPath = "BanMod.Resources.image.SmallRightButtonIcon.png";

        public static IEnumerator WaitForCloseMeetingButton(MeetingHud __instance)
        {
            while (__instance == null)
                yield return null;

            while (__instance.playerStates == null || __instance.playerStates.Count == 0)
                yield return null;

            while (__instance.playerStates.All(pva => pva == null || pva.Buttons == null))
                yield return null;

            CreateCloseMeetingButton(__instance);
        }

        public static void CreateCloseMeetingButton(MeetingHud __instance)
        {
            if (__instance == null)
                return;

            if (AmongUsClient.Instance == null)
                return;

            bool canShowAsHost = AmongUsClient.Instance.AmHost;

            bool canShowAsNonHostJudge =
                PlayerControl.LocalPlayer != null &&
                Judge.JudgeSelected &&
                PlayerControl.LocalPlayer.PlayerId == Judge.JudgeId &&
                Judge.JudgeCanUseEnd;

            if (!canShowAsHost && !canShowAsNonHostJudge)
                return;

            CreateMainCloseMeetingButton(__instance);
            CreateSmallRightButton(__instance);
        }

        private static void CreateMainCloseMeetingButton(MeetingHud __instance)
        {
            if (__instance.transform.Find(CloseMeetingButtonName) != null)
                return;

            GameObject template = __instance.SkipVoteButton.gameObject;
            GameObject closeButtonGO = UnityEngine.Object.Instantiate(template, __instance.transform);
            closeButtonGO.name = CloseMeetingButtonName;

            closeButtonGO.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            closeButtonGO.transform.localScale = Vector3.one;

            closeButtonGO.SetActive(true);

            TextMeshPro textMesh = closeButtonGO.GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
                textMesh.gameObject.SetActive(false);

            SpriteRenderer spriteRenderer = closeButtonGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = closeButtonGO.AddComponent<SpriteRenderer>();

            var customSprite = Utils.LoadSprite(CloseMeetingIconPath, 100f);

            if (customSprite != null)
            {
                spriteRenderer.sprite = customSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = new Color(0.8f, 0.4f, 0.1f, 1f);
            }

            PassiveButton button = closeButtonGO.GetComponent<PassiveButton>();
            if (button != null)
            {
                button.OnClick.RemoveAllListeners();

                System.Action value = () =>
                {
                    if (CloseMeetingManager.closeMeetingConfirmUI != null || GuessManager.guesserUI != null)
                        return;

                    ShowCloseMeetingConfirmation(__instance);
                };

                button.OnClick.AddListener((UnityEngine.Events.UnityAction)value);
            }
        }

        private static void CreateSmallRightButton(MeetingHud __instance)
        {
            if (__instance.transform.Find(SmallRightButtonName) != null)
                return;

            GameObject template = __instance.SkipVoteButton.gameObject;
            GameObject smallButtonGO = UnityEngine.Object.Instantiate(template, __instance.transform);
            smallButtonGO.name = SmallRightButtonName;

            smallButtonGO.transform.localPosition = new Vector3(4.85f, -2.65f, 0f);

            smallButtonGO.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            smallButtonGO.SetActive(true);

            TextMeshPro textMesh = smallButtonGO.GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
                textMesh.gameObject.SetActive(false);

            SpriteRenderer spriteRenderer = smallButtonGO.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = smallButtonGO.AddComponent<SpriteRenderer>();

            var customSprite = Utils.LoadSprite(SmallRightButtonIconPath, 100f);

            if (customSprite != null)
            {
                spriteRenderer.sprite = customSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = new Color(0.2f, 0.6f, 1f, 1f);
            }

            PassiveButton button = smallButtonGO.GetComponent<PassiveButton>();
            if (button != null)
            {
                button.OnClick.RemoveAllListeners();

                System.Action value = () =>
                {
                    if (CloseMeetingManager.closeMeetingConfirmUI != null || GuessManager.guesserUI != null)
                        return;

                    MeetingVoteCloser.CloseVoteNow();
                };

                button.OnClick.AddListener((UnityEngine.Events.UnityAction)value);
            }
        }

        public static void ShowCloseMeetingConfirmation(MeetingHud __instance)
        {
            if (__instance == null)
                return;

            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);

            var closeMeetingButton = __instance.transform.Find(CloseMeetingButtonName);
            if (closeMeetingButton != null)
                closeMeetingButton.gameObject.SetActive(false);

            var smallRightButton = __instance.transform.Find(SmallRightButtonName);
            if (smallRightButton != null)
                smallRightButton.gameObject.SetActive(false);

            GameObject canvasGO = new GameObject("CloseMeetingConfirmCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            closeMeetingConfirmUI = canvasGO;

            GameObject container = new GameObject("CloseMeetingConfirmContainer");
            container.transform.SetParent(canvasGO.transform, false);

            GameObject confirmTextObj = new GameObject("ConfirmCloseText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = GetString("ConfirmCloseMeetingMessage");
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.color = Color.yellow;

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(600, 100);
            rect.anchoredPosition = new Vector2(0, 120f);

            GameObject yesButtonObj = new GameObject("YesCloseButton");
            yesButtonObj.transform.SetParent(container.transform, false);

            var yesRect = yesButtonObj.AddComponent<RectTransform>();
            yesRect.sizeDelta = new Vector2(200, 70);
            yesRect.anchoredPosition = new Vector2(-150, -40);

            var yesImage = yesButtonObj.AddComponent<Image>();
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var yesButton = yesButtonObj.AddComponent<Button>();
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                __instance.SkipVoteButton.gameObject.SetActive(true);

                if (closeMeetingButton != null)
                    closeMeetingButton.gameObject.SetActive(true);

                if (smallRightButton != null)
                    smallRightButton.gameObject.SetActive(true);

                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;

                PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
            }));

            GameObject yesTextObj = new GameObject("YesCloseText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);

            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = GetString("YesButtonText");
            yesTMP.alignment = TextAlignmentOptions.Center;
            yesTMP.fontSize = 30;
            yesTMP.color = Color.black;

            var yesTMPRect = yesTMP.GetComponent<RectTransform>();
            yesTMPRect.anchorMin = Vector2.zero;
            yesTMPRect.anchorMax = Vector2.one;
            yesTMPRect.offsetMin = Vector2.zero;
            yesTMPRect.offsetMax = Vector2.zero;

            GameObject noButtonObj = new GameObject("NoCloseButton");
            noButtonObj.transform.SetParent(container.transform, false);

            var noRect = noButtonObj.AddComponent<RectTransform>();
            noRect.sizeDelta = new Vector2(200, 70);
            noRect.anchoredPosition = new Vector2(150, -40);

            var noImage = noButtonObj.AddComponent<Image>();
            noImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var noButton = noButtonObj.AddComponent<Button>();
            noButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                __instance.SkipVoteButton.gameObject.SetActive(true);

                if (closeMeetingButton != null)
                    closeMeetingButton.gameObject.SetActive(true);

                if (smallRightButton != null)
                    smallRightButton.gameObject.SetActive(true);

                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }));

            GameObject noTextObj = new GameObject("NoCloseText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);

            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = GetString("NoButtonText");
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.fontSize = 30;
            noTMP.color = Color.black;

            var noTMPRect = noTMP.GetComponent<RectTransform>();
            noTMPRect.anchorMin = Vector2.zero;
            noTMPRect.anchorMax = Vector2.one;
            noTMPRect.offsetMin = Vector2.zero;
            noTMPRect.offsetMax = Vector2.zero;
        }

        public static void ResetForNewGame()
        {
            DestroyConfirmUI();
            DestroyMeetingButton(CloseMeetingButtonName);
            DestroyMeetingButton(SmallRightButtonName);
        }

        public static void CleanupAfterMeeting()
        {
            DestroyConfirmUI();
            DestroyMeetingButton(CloseMeetingButtonName);
            DestroyMeetingButton(SmallRightButtonName);
        }

        private static void DestroyConfirmUI()
        {
            if (closeMeetingConfirmUI != null)
            {
                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }
        }

        private static void DestroyMeetingButton(string buttonName)
        {
            if (MeetingHud.Instance == null)
                return;

            var existingButton = MeetingHud.Instance.transform.Find(buttonName);

            if (existingButton != null)
                UnityEngine.Object.Destroy(existingButton.gameObject);
        }
    }
}