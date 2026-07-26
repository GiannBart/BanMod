//credits and licenses in the resources folder
using BanMod;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BanMod.Translator;
using static Rewired.UI.ControlMapper.ControlMapper;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPriority(Priority.First)]
    public class MainMenuManagerStartPatch
    {
        public static SpriteRenderer Logo { get; private set; }

        private static void Postfix(MainMenuManager __instance)
        {
            try
            {
                if (__instance == null)
                {
                    Debug.LogError("MainMenuManager non è ancora disponibile.");
                    return;
                }

                if (__instance.gameModeButtons == null || __instance.gameModeButtons.transform == null || __instance.gameModeButtons.transform.parent == null)
                {
                    Debug.LogWarning("[BanMod] gameModeButtons/rightPanel non disponibile in MainMenuManagerStartPatch.");
                    return;
                }

                var rightPanel = __instance.gameModeButtons.transform.parent;

                var logoObject = new GameObject("titleLogo_BanMod");
                var logoTransform = logoObject.transform;

                Logo = logoObject.AddComponent<SpriteRenderer>();
                logoTransform.parent = rightPanel;
                logoTransform.localPosition = new Vector3(-0.16f, 0f, 1f);
                logoTransform.localScale *= 1.2f;
            }
            catch (Exception e)
            {
                Debug.LogError("[BanMod] Errore MainMenuManagerStartPatch.Postfix: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager))]
    public static class MainMenuManagerPatch
    {
        private static PassiveButton template;
        private static PassiveButton websiteButton;
        private static PassiveButton GitButton;
        private static PassiveButton KaitoButton;

        public static bool visualized = false;
        public static PassiveButton UpdateButton { get; private set; }

        private static GameObject RightPanel;
        private static Vector3 RightPanelOp;
        public static bool ShowingPanel = false;
        public static GameObject activeWorker;

        internal static void CleanupWorker()
        {
            try
            {
                if (activeWorker != null)
                {
                    Object.Destroy(activeWorker);
                    activeWorker = null;
                }

                panelState = PanelState.Hidden;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore CleanupWorker: " + e);
                activeWorker = null;
                panelState = PanelState.Hidden;
            }
        }

        private enum PanelState
        {
            Hidden,
            Showing,
            Visible,
            Hiding
        }

        private static PanelState panelState = PanelState.Hidden;

        public enum WorkerMode
        {
            Pull,
            Push
        }

        private static Sprite[] LoadFrames(string baseName, int count)
        {
            Sprite[] frames = new Sprite[count];

            for (int i = 0; i < count; i++)
            {
                try
                {
                    frames[i] = Utils.LoadSprite(
                        $"BanMod.Resources.image.{baseName}_{i + 1}.png",
                        100f
                    );
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BanMod] Errore LoadFrames {baseName}_{i + 1}: {e}");
                    frames[i] = null;
                }
            }

            return frames.Where(s => s != null).ToArray();
        }

        public class PullingWorker : MonoBehaviour
        {
            public Transform panel;
            public WorkerMode mode;

            private SpriteRenderer renderer;
            private AnimatedSprite animator;

            private Vector3 pullOffset = new(-1.8f, -2.2f, 0f);
            private Vector3 pushOffset = new(1.2f, -2.2f, 0f);

            private bool fadingOut;
            private bool isGreeting;
            private float runPhase;
            private float runAmplitude = 0.15f;
            private float runSpeed = 15f;

            public void Setup(Transform panel, WorkerMode mode)
            {
                try
                {
                    if (panel == null)
                    {
                        Debug.LogWarning("[BanMod] PullingWorker.Setup: panel nullo.");
                        enabled = false;
                        return;
                    }

                    this.panel = panel;
                    this.mode = mode;

                    renderer = gameObject.AddComponent<SpriteRenderer>();
                    if (renderer == null)
                    {
                        Debug.LogWarning("[BanMod] PullingWorker.Setup: SpriteRenderer nullo.");
                        enabled = false;
                        return;
                    }

                    renderer.sortingLayerName = "UI";
                    renderer.sortingOrder = 5000;

                    animator = gameObject.AddComponent<AnimatedSprite>();
                    if (animator == null)
                    {
                        Debug.LogWarning("[BanMod] PullingWorker.Setup: AnimatedSprite nullo.");
                        enabled = false;
                        return;
                    }

                    animator.renderer = renderer;

                    Sprite[] anim = mode == WorkerMode.Pull
                        ? LoadFrames("run_left", 4)
                        : LoadFrames("return_right", 5);

                    if (anim != null && anim.Length > 0)
                        animator.SetAnimation(anim, 12f);

                    transform.localScale = Vector3.one * 1.2f;

                    Vector3 baseOffset = mode == WorkerMode.Pull ? pullOffset : pushOffset;
                    transform.localPosition = panel.localPosition + baseOffset;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BanMod] Errore PullingWorker.Setup: " + e);
                    enabled = false;
                }
            }

            public void PlayGreeting()
            {
                try
                {
                    if (animator == null || renderer == null)
                        return;

                    Sprite[] greetFrames = LoadFrames("greet", 4);
                    if (greetFrames == null || greetFrames.Length == 0)
                        return;

                    isGreeting = true;
                    animator.SetAnimation(greetFrames, 6f);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BanMod] Errore PullingWorker.PlayGreeting: " + e);
                }
            }

            void Update()
            {
                try
                {
                    if (panel == null)
                        return;

                    if (renderer == null)
                        return;

                    if (!fadingOut)
                    {
                        if (isGreeting)
                        {
                            transform.localPosition = panel.localPosition + pullOffset;
                            return;
                        }

                        runPhase += Time.deltaTime * runSpeed;
                        float runOffset = Mathf.Sin(runPhase) * runAmplitude;

                        Vector3 baseOffset = mode == WorkerMode.Pull ? pullOffset : pushOffset;
                        baseOffset.x += mode == WorkerMode.Pull ? -runOffset : runOffset;

                        transform.localPosition = panel.localPosition + baseOffset;
                    }
                    else
                    {
                        Color c = renderer.color;
                        c.a -= Time.deltaTime * 2f;
                        renderer.color = c;

                        if (c.a <= 0f)
                        {
                            Destroy(gameObject);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BanMod] Errore PullingWorker.Update: " + e);
                    Destroy(gameObject);
                }
            }

            void OnDestroy()
            {
                try
                {
                    if (activeWorker == gameObject)
                        activeWorker = null;
                }
                catch
                {
                    activeWorker = null;
                }
            }

            public void FadeAndDestroy()
            {
                if (renderer == null)
                {
                    Destroy(gameObject);
                    return;
                }

                fadingOut = true;
            }
        }

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void Start_Postfix(MainMenuManager __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                if (template == null)
                    template = __instance.quitButton;

                if (__instance.gameModeButtons == null ||
                    __instance.gameModeButtons.transform == null ||
                    __instance.gameModeButtons.transform.parent == null)
                {
                    Debug.LogWarning("[BanMod] Start_Postfix: RightPanel non disponibile.");
                    return;
                }

                RightPanel = __instance.gameModeButtons.transform.parent.gameObject;

                if (RightPanel == null)
                    return;

                RightPanelOp = RightPanel.transform.localPosition;
                ShowingPanel = false;
                panelState = PanelState.Hidden;

                RightPanel.transform.localPosition = RightPanelOp + new Vector3(10f, 0f, 0f);

                if (__instance.screenTint != null)
                {
                    __instance.screenTint.gameObject.transform.localPosition += new Vector3(1000f, 0f);
                    __instance.screenTint.enabled = false;
                }

                if (__instance.rightPanelMask != null)
                    __instance.rightPanelMask.SetActive(true);

                if (__instance.mainMenuUI != null)
                {
                    DisableUIElement(__instance.mainMenuUI.gameObject, "BackgroundTexture");
                    DisableUIElement(__instance.mainMenuUI.gameObject, "WindowShine");

                    ModifyPanel(__instance.mainMenuUI.gameObject, "LeftPanel");
                    ModifyPanel(__instance.mainMenuUI.gameObject, "RightPanel");
                }

                var originalStars = GameObject.Find("BackgroundStarField");
                if (originalStars != null)
                    originalStars.SetActive(false);

                CreateSplashArt();

                if (template == null)
                    return;

                if (GitButton == null)
                {
                    GitButton = CreateButton(
                        "GitButton",
                        new(-2f, -1.5f, 1f),
                        new(88, 101, 242, byte.MaxValue),
                        new(148, 161, byte.MaxValue, byte.MaxValue),
                        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.GitsiteUrl)),
                        "GitHub");
                }

                if (GitButton != null)
                    GitButton.gameObject.SetActive(BanMod.ShowGitButton);

                if (websiteButton == null)
                {
                    websiteButton = CreateButton(
                        "WebsiteButton",
                        new(-2f, -1.1f, 1f),
                        new Color32(70, 130, 180, 255),
                        new Color32(65, 105, 225, 255),
                        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.LobbysiteUrl)),
                        GetString("BanMod_Site"));
                }

                if (websiteButton != null)
                    websiteButton.gameObject.SetActive(BanMod.ShowWebsiteButton);

                if (UpdateButton == null)
                {
                    UpdateButton = CreateButton(
                        "UpdateButton",
                        new(-2f, -2.3f, 1f),
                        new(251, 81, 44, byte.MaxValue),
                        new(211, 77, 48, byte.MaxValue),
                        (UnityEngine.Events.UnityAction)(() => ModUpdater.StartUpdate(ModUpdater.downloadUrl)),
                        GetString("UpdateButton"));
                }

                if (UpdateButton != null)
                    UpdateButton.gameObject.SetActive(BanMod.ShowUpdateButton);

                if (KaitoButton == null)
                {
                    KaitoButton = CreateButton(
                        "KaitoRunPreset",
                        new(-2f, -1.9f, 1f),
                        new Color32(70, 130, 180, 255),
                        new Color32(65, 105, 225, 255),
                        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.KaitositeUrl)),
                        GetString("KaitoRunPreset"));
                }

                if (KaitoButton != null)
                    KaitoButton.gameObject.SetActive(BanMod.ShowKaitoButton);

                var nameUi = NameUI.Instance;
                if (nameUi == null && __instance.gameObject != null)
                    nameUi = __instance.gameObject.AddComponent<NameUI>();

                if (nameUi != null)
                    nameUi.Initialize(template);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore MainMenuManagerPatch.Start_Postfix: " + e);
            }
        }

        [HarmonyPatch(nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
        public static void AnimatePanel()
        {
            try
            {
                if (RightPanel == null)
                    return;

                if (!RightPanel.activeInHierarchy)
                {
                    CleanupWorker();
                    return;
                }

                Vector3 shown = RightPanelOp;
                Vector3 hidden = RightPanelOp + new Vector3(10f, 0f, 0f);
                float speed = 8f;

                switch (panelState)
                {
                    case PanelState.Showing:
                        {
                            if (RightPanel == null)
                                return;

                            RightPanel.transform.localPosition = Vector3.MoveTowards(
                                RightPanel.transform.localPosition,
                                shown,
                                Time.deltaTime * speed
                            );

                            if (Vector3.Distance(RightPanel.transform.localPosition, shown) < 0.01f)
                            {
                                panelState = PanelState.Visible;

                                if (activeWorker != null)
                                {
                                    var worker = activeWorker.GetComponent<PullingWorker>();

                                    if (worker != null)
                                        worker.PlayGreeting();
                                    else
                                        activeWorker = null;
                                }
                            }

                            break;
                        }

                    case PanelState.Hiding:
                        {
                            if (RightPanel == null)
                                return;

                            RightPanel.transform.localPosition = Vector3.MoveTowards(
                                RightPanel.transform.localPosition,
                                hidden,
                                Time.deltaTime * speed
                            );

                            if (Vector3.Distance(RightPanel.transform.localPosition, hidden) < 0.01f)
                            {
                                panelState = PanelState.Hidden;

                                if (activeWorker != null)
                                {
                                    var worker = activeWorker.GetComponent<PullingWorker>();

                                    if (worker != null)
                                        worker.FadeAndDestroy();
                                    else
                                        activeWorker = null;
                                }
                            }

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore in MainMenuManagerPatch.AnimatePanel: " + e);
                CleanupWorker();
            }
        }

        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu)), HarmonyPrefix]
        public static bool OnOpenGameMode()
        {
            try
            {
                if (RightPanel == null)
                    return true;

                if (panelState == PanelState.Hidden || panelState == PanelState.Hiding)
                {
                    panelState = PanelState.Showing;
                    SpawnWorker(WorkerMode.Pull);
                    return true;
                }

                if (panelState == PanelState.Visible)
                {
                    panelState = PanelState.Hiding;

                    if (activeWorker != null)
                    {
                        var worker = activeWorker.GetComponent<PullingWorker>();

                        if (worker != null)
                        {
                            worker.mode = WorkerMode.Push;

                            var anim = worker.GetComponent<AnimatedSprite>();
                            if (anim != null)
                            {
                                Sprite[] frames = LoadFrames("return_right", 5);
                                if (frames != null && frames.Length > 0)
                                    anim.SetAnimation(frames, 12f);
                            }
                        }
                        else
                        {
                            SpawnWorker(WorkerMode.Push);
                        }
                    }
                    else
                    {
                        SpawnWorker(WorkerMode.Push);
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore OnOpenGameMode: " + e);
                CleanupWorker();
                return true;
            }
        }

        private static void SpawnWorker(WorkerMode mode)
        {
            try
            {
                if (RightPanel == null)
                {
                    Debug.LogWarning("[BanMod] Impossibile spawnare Worker: RightPanel è NULL!");
                    return;
                }

                if (activeWorker != null)
                {
                    Object.Destroy(activeWorker);
                    activeWorker = null;
                }

                activeWorker = new GameObject("BanMod_Worker");
                activeWorker.SetActive(false);

                if (RightPanel.transform != null && RightPanel.transform.parent != null)
                {
                    activeWorker.transform.SetParent(RightPanel.transform.parent, false);
                }

                var worker = activeWorker.AddComponent<PullingWorker>();

                if (worker == null)
                {
                    Object.Destroy(activeWorker);
                    activeWorker = null;
                    return;
                }

                worker.Setup(RightPanel.transform, mode);

                activeWorker.SetActive(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore SpawnWorker: " + e);
                CleanupWorker();
            }
        }

        [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Open)), HarmonyPrefix]
        public static void HidePanelPrefix()
        {
            ShowingPanel = false;
        }

        private static void ShowContactsPopup()
        {
            try
            {
                GameObject popup = new("BanMod_ContactsPopup");
                popup.transform.position = new Vector3(0f, 0f, -10f);

                var canvas = popup.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                popup.AddComponent<GraphicRaycaster>();

                var bg = new GameObject("Background");
                bg.transform.SetParent(popup.transform, false);

                var bgRect = bg.AddComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(400, 250);
                bgRect.anchoredPosition = Vector2.zero;

                var image = bg.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.85f);

                string contactText =
                    "<b><color=#D44638>Email:</color></b>\nbanmod.giannibart@gmail.com\n\n" +
                    "<b><color=#0088CC>Telegram:</color></b>\nhttps://t.me/Giannibart\n\n" +
                    "<b><color=#CCCCCC>Bug Report:</color></b>\nhttps://banmod.online/bug_report";

                var textGO = new GameObject("ContactText");
                textGO.transform.SetParent(bg.transform, false);

                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = contactText;
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.TopLeft;

                var textRect = text.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(360, 160);
                textRect.anchoredPosition = new Vector2(0, 30);

                GameObject closeGO = new GameObject("CloseButton");
                closeGO.transform.SetParent(bg.transform, false);

                var closeRect = closeGO.AddComponent<RectTransform>();
                closeRect.sizeDelta = new Vector2(120, 40);
                closeRect.anchoredPosition = new Vector2(0, -90);

                var closeImage = closeGO.AddComponent<Image>();
                closeImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                var closeBtn = closeGO.AddComponent<Button>();
                closeBtn.onClick.AddListener((Action)(() => Object.Destroy(popup)));

                var closeTextGO = new GameObject("Text");
                closeTextGO.transform.SetParent(closeGO.transform, false);

                var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
                closeText.text = "Close";
                closeText.fontSize = 18;
                closeText.alignment = TextAlignmentOptions.Center;
                closeText.color = Color.white;

                var closeTextRect = closeText.GetComponent<RectTransform>();
                closeTextRect.anchorMin = Vector2.zero;
                closeTextRect.anchorMax = Vector2.one;
                closeTextRect.offsetMin = Vector2.zero;
                closeTextRect.offsetMax = Vector2.zero;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore ShowContactsPopup: " + e);
            }
        }

        private static void ShowInfoPopup()
        {
            try
            {
                GameObject popup = new("BanMod_InfoPopup");
                popup.transform.position = new Vector3(0f, 0f, -10f);

                var canvas = popup.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                popup.AddComponent<GraphicRaycaster>();

                var bg = new GameObject("Background");
                bg.transform.SetParent(popup.transform, false);

                var bgRect = bg.AddComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(400, 320);
                bgRect.anchoredPosition = Vector2.zero;

                var image = bg.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.85f);

                string infoText = GetString("Ztext");

                var textGO = new GameObject("InfoText");
                textGO.transform.SetParent(bg.transform, false);

                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = infoText;
                text.fontSize = 16;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.TopLeft;

                var textRect = text.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(360, 220);
                textRect.anchoredPosition = new Vector2(0, 40);

                GameObject closeGO = new GameObject("CloseButton");
                closeGO.transform.SetParent(bg.transform, false);

                var closeRect = closeGO.AddComponent<RectTransform>();
                closeRect.sizeDelta = new Vector2(80, 30);
                closeRect.anchoredPosition = new Vector2(140, -140);

                var closeImage = closeGO.AddComponent<Image>();
                closeImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                var closeBtn = closeGO.AddComponent<Button>();
                closeBtn.onClick.AddListener((Action)(() => Object.Destroy(popup)));

                var closeTextGO = new GameObject("Text");
                closeTextGO.transform.SetParent(closeGO.transform, false);

                var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
                closeText.text = GetString("Close");
                closeText.fontSize = 18;
                closeText.alignment = TextAlignmentOptions.Center;
                closeText.color = Color.white;

                var closeTextRect = closeText.GetComponent<RectTransform>();
                closeTextRect.anchorMin = Vector2.zero;
                closeTextRect.anchorMax = Vector2.one;
                closeTextRect.offsetMin = Vector2.zero;
                closeTextRect.offsetMax = Vector2.zero;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore ShowInfoPopup: " + e);
            }
        }

        private static void DisableUIElement(GameObject uiParent, string elementName)
        {
            try
            {
                if (uiParent == null || string.IsNullOrEmpty(elementName))
                    return;

                var element = uiParent.FindChild<SpriteRenderer>(elementName)?.transform?.gameObject;

                if (element != null)
                {
                    element.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"Element {elementName} not found.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BanMod] Errore DisableUIElement {elementName}: {e}");
            }
        }

        private static void ModifyPanel(GameObject uiParent, string panelName)
        {
            try
            {
                if (uiParent == null || string.IsNullOrEmpty(panelName))
                    return;

                var panel = uiParent.FindChild<Transform>(panelName)?.gameObject;
                if (panel == null)
                    return;

                var panelRenderer = panel.GetComponent<SpriteRenderer>();
                if (panelRenderer != null)
                    panelRenderer.enabled = false;

                var maskedBlackScreen = panel.FindChild<Transform>("MaskedBlackScreen")?.gameObject;
                if (maskedBlackScreen != null)
                {
                    var maskedRenderer = maskedBlackScreen.GetComponent<SpriteRenderer>();
                    if (maskedRenderer != null)
                        maskedRenderer.enabled = false;

                    maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BanMod] Errore ModifyPanel {panelName}: {e}");
            }
        }

        private static void CreateSplashArt()
        {
            try
            {
                string folderPath = System.IO.Path.Combine(Application.dataPath, "..", "BAN_DATA", "IMAGE", "Background");

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                    BMLogger.Info("[BanMod] Cartella Background creata: " + folderPath);
                }

                string filePath = System.IO.Directory
                    .GetFiles(folderPath, "*.png", System.IO.SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                GameObject splashArt = new("BanMod_CustomBackground");
                splashArt.transform.position = new Vector3(0f, 0f, 20f);

                var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();

                Sprite externalSprite = null;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    externalSprite = LoadExternalBackground(filePath);
                }

                if (externalSprite != null)
                {
                    spriteRenderer.sprite = externalSprite;
                    BMLogger.Info("[BanMod] Sfondo personalizzato caricato: " + filePath);
                }
                else
                {
                    spriteRenderer.sprite = Utils.LoadSprite("BanMod.Resources.image.image.png", 150f);
                }

                if (spriteRenderer.sprite != null && Camera.main != null)
                {
                    float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
                    float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

                    Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                    if (spriteSize.x > 0f && spriteSize.y > 0f)
                    {
                        splashArt.transform.localScale = new Vector3(
                            worldScreenWidth / spriteSize.x,
                            worldScreenHeight / spriteSize.y,
                            1f
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[BanMod] Errore in CreateSplashArt: " + e);

                try
                {
                    GameObject splashArt = new("BanMod_CustomBackground_Fallback");
                    splashArt.transform.position = new Vector3(0f, 0f, 20f);

                    var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = Utils.LoadSprite("BanMod.Resources.image.image.png", 150f);

                    if (spriteRenderer.sprite != null && Camera.main != null)
                    {
                        float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
                        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

                        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                        if (spriteSize.x > 0f && spriteSize.y > 0f)
                        {
                            splashArt.transform.localScale = new Vector3(
                                worldScreenWidth / spriteSize.x,
                                worldScreenHeight / spriteSize.y,
                                1f
                            );
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    Debug.LogError("[BanMod] Anche il fallback dello sfondo è fallito: " + fallbackEx);
                }
            }
        }

        public static PassiveButton CreateButton(
            string name,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            UnityEngine.Events.UnityAction action,
            string label,
            Vector2? scale = null)
        {
            try
            {
                if (template == null)
                    return null;

                Transform parent = null;

                if (MainMenuManagerStartPatch.Logo != null)
                    parent = MainMenuManagerStartPatch.Logo.transform;

                if (parent == null && template.transform != null)
                    parent = template.transform.parent;

                if (parent == null)
                    return null;

                var button = Object.Instantiate(template, parent);
                if (button == null)
                    return null;

                button.name = name;

                var aspect = button.GetComponent<AspectPosition>();
                if (aspect != null)
                    Object.Destroy(aspect);

                button.transform.localPosition = localPosition;

                button.OnClick = new Button.ButtonClickedEvent();
                if (action != null)
                    button.OnClick.AddListener(action);

                var textTransform = button.transform.Find("FontPlacer/Text_TMP");
                TMP_Text buttonText = null;

                if (textTransform != null)
                    buttonText = textTransform.GetComponent<TMP_Text>();

                if (buttonText != null)
                {
                    buttonText.DestroyTranslator();
                    buttonText.fontSize = 3.5f;
                    buttonText.enableWordWrapping = false;
                    buttonText.text = label;
                    buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

                    var container = buttonText.transform.parent;
                    if (container != null)
                    {
                        var containerAspect = container.GetComponent<AspectPosition>();
                        if (containerAspect != null)
                            Object.Destroy(containerAspect);
                    }

                    var textAspect = buttonText.GetComponent<AspectPosition>();
                    if (textAspect != null)
                        Object.Destroy(textAspect);
                }

                SpriteRenderer normalSprite = null;
                SpriteRenderer hoverSprite = null;

                if (button.inactiveSprites != null)
                    normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();

                if (button.activeSprites != null)
                    hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();

                if (normalSprite != null)
                    normalSprite.color = normalColor;

                if (hoverSprite != null)
                    hoverSprite.color = hoverColor;

                var buttonCollider = button.GetComponent<BoxCollider2D>();
                if (buttonCollider != null)
                {
                    if (scale.HasValue)
                    {
                        if (normalSprite != null)
                            normalSprite.size = scale.Value;

                        if (hoverSprite != null)
                            hoverSprite.size = scale.Value;

                        buttonCollider.size = scale.Value;
                    }

                    buttonCollider.offset = Vector2.zero;
                }

                return button;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BanMod] Errore CreateButton {name}: {e}");
                return null;
            }
        }

        public static T FindChild<T>(this MonoBehaviour obj, string name) where T : Object
        {
            try
            {
                if (obj == null || obj.gameObject == null || string.IsNullOrEmpty(name))
                    return null;

                return obj.gameObject.GetComponentsInChildren<T>(true).FirstOrDefault(c => c != null && c.name == name);
            }
            catch
            {
                return null;
            }
        }

        public static T FindChild<T>(this GameObject obj, string name) where T : Object
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(name))
                    return null;

                return obj.GetComponentsInChildren<T>(true).FirstOrDefault(c => c != null && c.name == name);
            }
            catch
            {
                return null;
            }
        }

        private static Sprite LoadExternalBackground(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                if (!System.IO.File.Exists(path))
                    return null;

                byte[] fileData = System.IO.File.ReadAllBytes(path);
                if (fileData == null || fileData.Length == 0)
                    return null;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);

                if (tex.LoadImage(fileData))
                {
                    return Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BanMod] Errore nel caricamento dello sfondo esterno: {e}");
            }

            return null;
        }
    }
}

public class AnimatedSprite : MonoBehaviour
{
    public SpriteRenderer renderer;
    public Sprite[] frames;
    public float fps = 6f;

    private int index;
    private float timer;

    [HideFromIl2Cpp]
    public void SetAnimation(Sprite[] sprites, float fps = 6f)
    {
        try
        {
            frames = sprites;
            this.fps = fps <= 0f ? 6f : fps;
            index = 0;
            timer = 0f;

            if (renderer != null && frames != null && frames.Length > 0 && frames[0] != null)
                renderer.sprite = frames[0];
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BanMod] Errore AnimatedSprite.SetAnimation: " + e);
        }
    }

    void Update()
    {
        try
        {
            if (renderer == null)
                return;

            if (frames == null || frames.Length < 2)
                return;

            if (fps <= 0f)
                fps = 6f;

            timer += Time.deltaTime;

            if (timer >= 1f / fps)
            {
                timer = 0f;
                index = (index + 1) % frames.Length;

                if (frames[index] != null)
                    renderer.sprite = frames[index];
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BanMod] Errore AnimatedSprite.Update: " + e);
            enabled = false;
        }
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenCreateGame))]
public static class MainMenuManager_CreateGame_Patch
{
    public static void Prefix()
    {
        MainMenuManagerPatch.CleanupWorker();
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class AccountTabFixPatch
{
    public static void Postfix(MainMenuManager __instance)
    {
        try
        {
            if (__instance == null || __instance.myAccountButton == null)
                return;

            __instance.myAccountButton.OnClick.RemoveAllListeners();
            __instance.myAccountButton.OnClick.AddListener((Action)(() =>
            {
                try
                {
                    __instance.OpenGameModeMenu();
                    __instance.OpenAccountMenu();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BanMod] Errore AccountTabFixPatch click: " + e);
                }
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BanMod] Errore AccountTabFixPatch.Postfix: " + e);
        }
    }
}