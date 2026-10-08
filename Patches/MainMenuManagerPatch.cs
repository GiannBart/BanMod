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
using static BanMod.Utils.CheatUtils;
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
        //private static PassiveButton template;
        //private static PassiveButton websiteButton;
        //private static PassiveButton discordButton;
        //private static PassiveButton GitButton;
        //private static PassiveButton KaitoButton;
        //private static PassiveButton policyPrivacyButton;
        private static PassiveButton template;

        private static PassiveButton policyPrivacyButton;
        private static PassiveButton contactsButton;
        private static PassiveButton KaitoButton;
        internal static Transform LeftButtonsAnchor { get; private set; }

        public static bool visualized = false;
        public static PassiveButton CommunityButton { get; private set; }

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

                    // Restyling SOLO VISIVO dei pulsanti vanilla.
                    // Non modifica Transform, posizioni, scale o layout.
                    StyleExistingMainMenuButtons(__instance);
                }

                var originalStars = GameObject.Find("BackgroundStarField");
                if (originalStars != null)
                    originalStars.SetActive(false);

                CreateSplashArt();

                if (template == null)
                    return;

                if (LeftButtonsAnchor == null && template.transform != null && template.transform.parent != null)
                {
                    var anchorObject = new GameObject("BanMod_LeftButtonsAnchor");
                    LeftButtonsAnchor = anchorObject.transform;
                    LeftButtonsAnchor.SetParent(template.transform.parent, false);
                    LeftButtonsAnchor.localPosition = Vector3.zero;
                    LeftButtonsAnchor.localScale = Vector3.one;
                }

                //if (GitButton == null)
                //{
                //    GitButton = CreateButton(
                //        "GitButton",
                //        Vector3.zero,
                //        new Color32(36, 41, 47, 255),      // GitHub graphite
                //        new Color32(84, 73, 118, 255),     // GitHub hover violetto
                //        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.GitsiteUrl)),
                //        "GitHub");
                //}

                //if (GitButton != null)
                //    GitButton.gameObject.SetActive(BanMod.ShowGitButton);

                //if (websiteButton == null)
                //{
                //    websiteButton = CreateButton(
                //        "WebsiteButton",
                //        Vector3.zero,
                //        new Color32(4, 72, 91, 255),       // BanMod Site petrolio
                //        new Color32(0, 132, 158, 255),     // hover cyan
                //        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.LobbysiteUrl)),
                //        GetString("BanMod_Site"));
                //}

                //if (websiteButton != null)
                //    websiteButton.gameObject.SetActive(BanMod.ShowWebsiteButton);

                if (CommunityButton == null)
                {
                    CommunityButton = CreateButton(
                        "CommunityButton",
                        Vector3.zero,
                        new Color32(172, 38, 122, 255),     // Community magenta: spezza nettamente col resto
                        new Color32(224, 72, 166, 255),     // hover fucsia acceso
                        (UnityEngine.Events.UnityAction)(() => BanModCommunityBoard.OpenFromMainMenu()),
                        GetString("CommunityButton"));
                }

                if (CommunityButton != null)
                    CommunityButton.gameObject.SetActive(BanMod.ShowCommunityButton);

                if (KaitoButton == null)
                {
                    KaitoButton = CreateButton(
                        "KaitoRunPreset",
                        Vector3.zero,
                        new Color32(74, 40, 98, 255),      // Kaito viola scuro
                        new Color32(134, 69, 153, 255),    // hover viola
                        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.KaitositeUrl)),
                        GetString("KaitoRunPreset"));
                }

                if (KaitoButton != null)
                    KaitoButton.gameObject.SetActive(BanMod.ShowKaitoButton);

                //if (discordButton == null)
                //{
                //    discordButton = CreateButton(
                //        "discordButton",
                //        Vector3.zero,
                //        new Color32(70, 80, 190, 255),     // Discord blurple scuro
                //        new Color32(88, 101, 242, 255),    // Discord blurple
                //        (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.DiscordInviteUrl)),
                //        GetString("Discord"));
                //}

                //if (discordButton != null)
                //    discordButton.gameObject.SetActive(BanMod.ShowDiscordButton);

                if (policyPrivacyButton == null)
                {
                    policyPrivacyButton = CreateButton(
                        "PolicyPrivacyButton",
                        Vector3.zero,
                        new Color32(150, 34, 34, 255),
                        new Color32(205, 52, 52, 255),
                        (UnityEngine.Events.UnityAction)(() =>
                        {
                            BanModPopup.CreatePolicyPrivacyPopup();
                        }),
                        "Policy & Privacy"
                    );
                }
                if (contactsButton == null)
                {
                    contactsButton = CreateButton(
                        "ContactsButton",
                        Vector3.zero,

                        new Color32(38, 92, 125, 255),
                        new Color32(55, 145, 190, 255),

                        (UnityEngine.Events.UnityAction)(() =>
                        {
                            BanModPopup.CreateContactsPopup();
                        }),

                        "Contacts"
                    );
                }
                var nameUi = NameUI.Instance;
                if (nameUi == null && __instance.gameObject != null)
                    nameUi = __instance.gameObject.AddComponent<NameUI>();

                if (nameUi != null)
                    nameUi.Initialize(template);

                RefreshSideButtonPositions(__instance);
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
                string folderPath = System.IO.Path.Combine(Application.dataPath, "..", "DATA", "IMAGE", "Background");

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

        internal static void RefreshSideButtonPositions(MainMenuManager menu)
        {
            if (menu == null || menu.quitButton == null)
                return;

            Transform quitTransform = menu.quitButton.transform;
            Transform commonParent = quitTransform.parent;

            if (commonParent == null)
                return;

            // Distanza REALE tra il bordo destro del pulsante vanilla
            // e il bordo sinistro della colonna BanMod.
            // 0.30f evita sia la sovrapposizione sia lo spostamento eccessivo a destra.
            const float HorizontalGap = 0.30f;

            // Spaziatura verticale della colonna BanMod.
            const float VerticalGap = 0.05f;

            if (!TryGetButtonColliderBounds(menu.quitButton, out Bounds quitBounds))
                return;

            float targetLeftEdge = quitBounds.max.x + HorizontalGap;

            // Update fa da riferimento verticale in basso, alla stessa altezza di Quit.
            float baseCenterY = quitBounds.center.y;

            float normalHeight = GetButtonColliderHeight(CommunityButton, quitBounds.size.y);
            float step = normalHeight + VerticalGap;

            //PositionButtonFromLeftEdge(CommunityButton, targetLeftEdge, baseCenterY);
            //PositionButtonFromLeftEdge(KaitoButton, targetLeftEdge, baseCenterY + step);
            //PositionButtonFromLeftEdge(GitButton, targetLeftEdge, baseCenterY + step * 2f);
            //PositionButtonFromLeftEdge(websiteButton, targetLeftEdge, baseCenterY + step * 3f);
            //PositionButtonFromLeftEdge(discordButton, targetLeftEdge, baseCenterY + step * 4f);
            // Il selettore nome deve stare più in alto rispetto alla colonna.
            //var nameUi = NameUI.Instance;
            //if (nameUi != null && nameUi.RootButton != null)
            //{
            //    const float NameExtraUp = 0.60f;
            //    float nameY = baseCenterY + step * 4f + NameExtraUp;
            //    PositionButtonFromLeftEdge(nameUi.RootButton, targetLeftEdge, nameY);
            //}
            // Dal basso verso l'alto:
            // KaitoRun
            // Contacts
            // Policy & Privacy
            // Community
            // Name

            PositionButtonFromLeftEdge(
                KaitoButton,
                targetLeftEdge,
                baseCenterY
            );

            PositionButtonFromLeftEdge(
                contactsButton,
                targetLeftEdge,
                baseCenterY + step
            );

            PositionButtonFromLeftEdge(
                policyPrivacyButton,
                targetLeftEdge,
                baseCenterY + step * 2f
            );

            PositionButtonFromLeftEdge(
                CommunityButton,
                targetLeftEdge,
                baseCenterY + step * 3f
            );

            var nameUi = NameUI.Instance;

            if (nameUi != null && nameUi.RootButton != null)
            {
                const float NameExtraUp = 0.60f;

                float nameY =
                    baseCenterY +
                    step * 3f +
                    NameExtraUp;

                PositionButtonFromLeftEdge(
                    nameUi.RootButton,
                    targetLeftEdge,
                    nameY
                );
            }
        }

        private static void PositionButtonFromLeftEdge(
            PassiveButton button,
            float targetLeftEdge,
            float targetCenterY)
        {
            if (button == null)
                return;

            if (!TryGetButtonColliderBounds(button, out Bounds bounds))
                return;

            Vector3 worldPosition = button.transform.position;

            worldPosition.x += targetLeftEdge - bounds.min.x;
            worldPosition.y += targetCenterY - bounds.center.y;

            button.transform.position = worldPosition;
        }

        private static float GetButtonColliderHeight(PassiveButton button, float fallback)
        {
            if (button != null && TryGetButtonColliderBounds(button, out Bounds bounds))
                return bounds.size.y;

            return fallback;
        }

        private static bool TryGetButtonColliderBounds(PassiveButton button, out Bounds bounds)
        {
            bounds = new Bounds();

            if (button == null)
                return false;

            // Usa PRIMA il BoxCollider2D: rappresenta il rettangolo vero del bottone
            // e non include icone/testi che possono sporgere.
            BoxCollider2D box = button.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                bounds = box.bounds;
                return true;
            }

            Collider2D collider = button.GetComponent<Collider2D>();
            if (collider != null)
            {
                bounds = collider.bounds;
                return true;
            }

            SpriteRenderer renderer = button.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            return false;
        }

        // ============================================================
        // VISUAL STYLE - mix BanMod / EHR / TOHE
        // Queste funzioni modificano esclusivamente colori/rendering.
        // NON cambiano posizione, localPosition, anchoredPosition o layout.
        // ============================================================

        private static Sprite _banModButtonOverlaySprite;
        private static Sprite _banModThinBorderSprite;

        private static Sprite GetBanModButtonOverlaySprite()
        {
            if (_banModButtonOverlaySprite != null)
                return _banModButtonOverlaySprite;

            const int width = 512;
            const int height = 128;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "BanMod_ButtonOverlayTexture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte alpha = 0;

                    // Riflesso molto leggero nella parte alta: deve sembrare
                    // una lucidatura, non una texture appoggiata sopra il menu.
                    float topT = Mathf.Clamp01((y - height * 0.58f) / (height * 0.42f));
                    alpha = (byte)Mathf.Max(alpha, Mathf.RoundToInt(topT * 8f));

                    // Due bande diagonali morbide in stile EHR/TOHE.
                    float diagonal = x - y * 1.25f;

                    if (diagonal > 128f && diagonal < 200f)
                    {
                        float center = 164f;
                        float distance = Mathf.Abs(diagonal - center) / 36f;
                        byte bandAlpha = (byte)Mathf.RoundToInt(Mathf.Lerp(36f, 10f, distance));
                        alpha = (byte)Mathf.Max(alpha, bandAlpha);
                    }

                    if (diagonal > 246f && diagonal < 270f)
                    {
                        byte bandAlpha = 15;
                        alpha = (byte)Mathf.Max(alpha, bandAlpha);
                    }

                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            _banModButtonOverlaySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            _banModButtonOverlaySprite.name = "BanMod_ButtonOverlaySprite";
            return _banModButtonOverlaySprite;
        }

        private static Sprite GetBanModThinBorderSprite()
        {
            if (_banModThinBorderSprite != null)
                return _banModThinBorderSprite;

            const int width = 512;
            const int height = 128;
            const int thickness = 4;
            const int corner = 14;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "BanMod_ThinBorderTexture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outer = x >= 1 && x < width - 1 && y >= 1 && y < height - 1;
                    bool inner = x >= thickness && x < width - thickness &&
                                 y >= thickness && y < height - thickness;

                    // Smussa leggermente gli angoli per seguire meglio il bottone vanilla.
                    bool inCornerCut =
                        (x < corner && y < corner && (corner - x) + (corner - y) > corner + 5) ||
                        (x >= width - corner && y < corner && (x - (width - corner)) + (corner - y) > corner + 5) ||
                        (x < corner && y >= height - corner && (corner - x) + (y - (height - corner)) > corner + 5) ||
                        (x >= width - corner && y >= height - corner && (x - (width - corner)) + (y - (height - corner)) > corner + 5);

                    byte alpha = (byte)(outer && !inner && !inCornerCut ? 255 : 0);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            _banModThinBorderSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            _banModThinBorderSprite.name = "BanMod_ThinBorderSprite";
            return _banModThinBorderSprite;
        }

        private static void AddThinButtonBorder(SpriteRenderer target, Color32 borderColor)
        {
            if (target == null || target.sprite == null)
                return;

            try
            {
                Transform existing = target.transform.Find("BanModThinBorder");
                if (existing != null)
                    Object.Destroy(existing.gameObject);

                GameObject borderObject = new GameObject("BanModThinBorder");
                borderObject.transform.SetParent(target.transform, false);
                borderObject.transform.localRotation = Quaternion.identity;

                SpriteRenderer border = borderObject.AddComponent<SpriteRenderer>();
                border.sprite = GetBanModThinBorderSprite();
                border.sortingLayerID = target.sortingLayerID;
                border.sortingOrder = target.sortingOrder;
                border.maskInteraction = target.maskInteraction;

                if (target.sharedMaterial != null)
                    border.sharedMaterial = target.sharedMaterial;

                Vector2 visualSize = target.drawMode == SpriteDrawMode.Simple
                    ? target.sprite.bounds.size
                    : target.size;

                Vector2 nativeSize = border.sprite.bounds.size;
                if (nativeSize.x <= 0f || nativeSize.y <= 0f)
                {
                    Object.Destroy(borderObject);
                    return;
                }

                borderObject.transform.localScale = new Vector3(
                    visualSize.x / nativeSize.x,
                    visualSize.y / nativeSize.y,
                    1f
                );

                // Stesso sorting order del bottone: il piccolo Z serve solo a tenerlo sulla superficie.
                borderObject.transform.localPosition = new Vector3(0f, 0f, -0.012f);
                border.color = borderColor;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore AddThinButtonBorder: " + e);
            }
        }

        private static void AddVisualOverlay(
            SpriteRenderer target,
            float intensity,
            bool keepRightSideClean = false)
        {
            if (target == null || target.sprite == null)
                return;

            try
            {
                Transform existing = target.transform.Find("BanModVisualOverlay");
                if (existing != null)
                    Object.Destroy(existing.gameObject);

                GameObject overlayObject = new GameObject("BanModVisualOverlay");
                overlayObject.transform.SetParent(target.transform, false);
                overlayObject.transform.localRotation = Quaternion.identity;
                overlayObject.transform.localScale = Vector3.one;

                SpriteRenderer overlay = overlayObject.AddComponent<SpriteRenderer>();
                overlay.sprite = GetBanModButtonOverlaySprite();
                overlay.sortingLayerID = target.sortingLayerID;

                // Stesso sortingOrder del bottone: evita che il riflesso finisca
                // sopra popup, screen tint o altri elementi UI.
                overlay.sortingOrder = target.sortingOrder;
                overlay.maskInteraction = target.maskInteraction;

                if (target.sharedMaterial != null)
                    overlay.sharedMaterial = target.sharedMaterial;

                // I bottoni possono essere SpriteRenderer Sliced:
                // usa la dimensione visuale reale, non sprite.bounds in ogni caso.
                Vector2 visualSize;
                if (target.drawMode == SpriteDrawMode.Simple)
                    visualSize = target.sprite.bounds.size;
                else
                    visualSize = target.size;

                Vector2 overlayNativeSize = overlay.sprite.bounds.size;
                if (overlayNativeSize.x <= 0f || overlayNativeSize.y <= 0f)
                {
                    Object.Destroy(overlayObject);
                    return;
                }

                overlay.drawMode = SpriteDrawMode.Simple;

                float scaleX = visualSize.x / overlayNativeSize.x;
                float scaleY = visualSize.y / overlayNativeSize.y;

                // Per GIOCA / INVENTARIO / NEGOZIO il riflesso resta più a sinistra,
                // lasciando pulita la zona del testo sulla destra.
                if (keepRightSideClean)
                    scaleX *= 0.72f;

                overlayObject.transform.localScale = new Vector3(
                    scaleX,
                    scaleY,
                    1f
                );

                overlayObject.transform.localPosition = keepRightSideClean
                    ? new Vector3(-0.42f, 0f, -0.01f)
                    : new Vector3(0f, 0f, -0.01f);

                float clampedIntensity = Mathf.Clamp01(intensity);
                overlay.color = new Color(1f, 1f, 1f, clampedIntensity);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore AddVisualOverlay: " + e);
            }
        }

        private static void StylePassiveButton(
            PassiveButton button,
            Color32 normalColor,
            Color32 hoverColor,
            Color32 textColor,
            float overlayIntensity = 1f,
            bool keepRightSideClean = false,
            float textOutlineWidth = 0.015f,
            Color32? outlineColor = null,
            float? fontSize = null,
            bool thinWhiteButtonBorder = false,
            bool boldText = false)
        {
            if (button == null)
                return;

            try
            {
                SpriteRenderer normalSprite = null;
                SpriteRenderer hoverSprite = null;

                if (button.inactiveSprites != null)
                    normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();

                if (button.activeSprites != null)
                    hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();

                if (normalSprite != null)
                {
                    normalSprite.color = normalColor;
                    AddVisualOverlay(normalSprite, overlayIntensity, keepRightSideClean);

                    if (thinWhiteButtonBorder)
                        AddThinButtonBorder(normalSprite, new Color32(255, 255, 255, 215));
                }

                if (hoverSprite != null)
                {
                    hoverSprite.color = hoverColor;
                    AddVisualOverlay(
                        hoverSprite,
                        Mathf.Min(1f, overlayIntensity + 0.05f),
                        keepRightSideClean
                    );

                    if (thinWhiteButtonBorder)
                        AddThinButtonBorder(hoverSprite, new Color32(255, 255, 255, 245));
                }

                // Testo: un solo colore pieno e leggibile, senza outline/ombre.
                // Applichiamo lo stile a TUTTI i TMP figli del bottone: alcuni pulsanti
                // vanilla contengono piu' componenti testo e GetComponentInChildren
                // puo' prendere quello sbagliato.
                TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
                if (texts != null)
                {
                    foreach (TMP_Text buttonText in texts)
                    {
                        if (buttonText == null)
                            continue;

                        buttonText.color = textColor;
                        buttonText.outlineWidth = 0f;
                        buttonText.outlineColor = new Color32(0, 0, 0, 0);

                        if (fontSize.HasValue)
                            buttonText.fontSize = fontSize.Value;

                        buttonText.fontStyle = boldText ? FontStyles.Bold : FontStyles.Normal;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BanMod] Errore StylePassiveButton {button.name}: {e}");
            }
        }

        private static void StyleExistingMainMenuButtons(MainMenuManager menu)
        {
            if (menu == null || menu.mainMenuUI == null)
                return;

            try
            {
                PassiveButton[] buttons = menu.mainMenuUI.GetComponentsInChildren<PassiveButton>(true);
                if (buttons == null)
                    return;

                foreach (PassiveButton button in buttons)
                {
                    if (button == null || string.IsNullOrEmpty(button.name))
                        continue;

                    string n = button.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();

                    // Usiamo nomi ESATTI invece di Contains("play").
                    // Questo evita di applicare i riflessi ai pulsanti Local/Online,
                    // alle card del pannello destro o ad altri elementi del menu.
                    bool isPlay = n == "playbutton" || n == "play";
                    bool isInventory = n == "inventorybutton" || n == "inventory";
                    bool isShop = n == "shopbutton" || n == "shop";

                    bool isNews = n == "newsbutton" || n == "news";
                    bool isAccount = n == "myaccountbutton" || n == "accountbutton" || n == "myaccount";
                    bool isSettings = n == "settingsbutton" || n == "settingbutton" || n == "settings";

                    bool isCredits = n == "creditsbutton" || n == "creditbutton" || n == "credits";
                    bool isQuit = n == "quitbutton" || n == "exitbutton" || n == "quit" || n == "exit";

                    if (isPlay)
                    {
                        // GIOCA: celeste brillante. Sullo sprite vanilla questa tinta
                        // resta pulita e visibile (l'arancione veniva moltiplicato dalla
                        // texture originale e tendeva al verde).
                        StylePassiveButton(
                            button,
                            new Color32(42, 187, 218, 255),        // celeste acceso
                            new Color32(91, 220, 242, 255),        // hover celeste chiaro
                            new Color32(18, 18, 18, 255),           // testo NERO SCURO fisso
                            0.10f,
                            true,
                            0f,
                            null,
                            3.80f,
                            false,
                            false                                   // stile normale
                        );
                        continue;
                    }

                    if (isInventory || isShop)
                    {
                        // INVENTARIO / NEGOZIO: quasi neri, senza bordo, con testo giallo caldo
                        // per avere contrasto forte ma restare coerenti con il menu.
                        StylePassiveButton(
                            button,
                            new Color32(12, 17, 19, 255),          // nero grafite
                            new Color32(27, 34, 37, 255),          // hover antracite
                            new Color32(250, 250, 250, 255),       // testo BIANCO CHIARO fisso
                            0.05f,                                 // riflesso appena percepibile
                            true,
                            0f,
                            null,
                            3.72f,
                            false,                                  // nessun bordo bianco
                            false                                   // stile normale
                        );
                        continue;
                    }

                    if (isNews || isAccount || isSettings)
                    {
                        StylePassiveButton(
                            button,
                            new Color32(0, 77, 74, 255),
                            new Color32(0, 117, 110, 255),
                            new Color32(255, 255, 255, 255),
                            0.18f
                        );
                        continue;
                    }

                    if (isCredits)
                    {
                        // RICONOSCIMENTI: bronzo/ocra, distinto sia dal teal che dal rosso.
                        StylePassiveButton(
                            button,
                            new Color32(126, 92, 24, 255),
                            new Color32(166, 124, 35, 255),
                            new Color32(255, 255, 255, 255),
                            0.10f
                        );
                        continue;
                    }

                    if (isQuit)
                    {
                        // ESCI: rosso netto e immediatamente riconoscibile.
                        StylePassiveButton(
                            button,
                            new Color32(150, 34, 34, 255),
                            new Color32(205, 52, 52, 255),
                            new Color32(255, 255, 255, 255),
                            0.08f
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore StyleExistingMainMenuButtons: " + e);
            }
        }

        internal static void EnforceFixedPrimaryTextStyles(MainMenuManager menu)
        {
            if (menu == null || menu.mainMenuUI == null)
                return;

            try
            {
                PassiveButton[] buttons = menu.mainMenuUI.GetComponentsInChildren<PassiveButton>(true);
                if (buttons == null)
                    return;

                foreach (PassiveButton button in buttons)
                {
                    if (button == null || string.IsNullOrEmpty(button.name))
                        continue;

                    string n = button.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();

                    bool isPlay = n == "playbutton" || n == "play";
                    bool isInventory = n == "inventorybutton" || n == "inventory";
                    bool isShop = n == "shopbutton" || n == "shop";

                    if (!isPlay && !isInventory && !isShop)
                        continue;

                    Color32 fixedColor = isPlay
                        ? new Color32(18, 18, 18, 255)
                        : new Color32(250, 250, 250, 255);

                    TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
                    if (texts == null)
                        continue;

                    foreach (TMP_Text text in texts)
                    {
                        if (text == null)
                            continue;

                        // Forzato ogni frame: hover, click e selezione non possono cambiarlo.
                        text.color = fixedColor;
                        text.fontStyle = FontStyles.Normal;
                        text.outlineWidth = 0f;
                        text.outlineColor = new Color32(0, 0, 0, 0);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BanMod] Errore EnforceFixedPrimaryTextStyles: " + e);
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

                Transform parent = LeftButtonsAnchor;

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

                StylePassiveButton(
                    button,
                    normalColor,
                    hoverColor,
                    new Color32(250, 252, 255, 255),
                    0.34f
                );

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
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
public static class MainMenuManagerUpdatePatch
{
    public static void Postfix(MainMenuManager __instance)
    {
        Utils.MainMenuInfo.Update();
        MainMenuManagerPatch.RefreshSideButtonPositions(__instance);
        MainMenuManagerPatch.EnforceFixedPrimaryTextStyles(__instance);
    }
}