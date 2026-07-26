//credits and licenses in the resources folder
using AmongUs.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Attributes;

namespace BanMod
{
    public class OutfitPreset
    {
        public string Name;
        public int ColorId;
        public string PetId;
        public string HatId;
        public string SkinId;
        public string VisorId;
        public string NamePlateId;
        public Sprite PreviewSprite;
    }

    public class SkinUI : MonoBehaviour
    {
        private List<OutfitPreset> outfitPresets = new List<OutfitPreset>();
        private int currentOutfitIndex = 0;

        private GameObject activePopup = null;
        private RectTransform popupRect = null;

        public static SkinUI Instance;

        private const int PreviewTextureSize = 1024;
        private const int PreviewLayer = 31;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            MenuRouter.OnPanelChanged += HandlePanelChanged;
        }

        private void OnDisable()
        {
            MenuRouter.OnPanelChanged -= HandlePanelChanged;
        }

        public bool IsPopupOpen
        {
            get { return activePopup != null; }
        }

        public bool IsPointerInsidePopup()
        {
            if (activePopup == null || popupRect == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                popupRect,
                Input.mousePosition,
                null
            );
        }

        private void HandlePanelChanged(MenuRouter.Panel p)
        {
            bool shouldBeOpen = (p == MenuRouter.Panel.SkinUI);

            if (shouldBeOpen)
            {
                if (activePopup == null)
                    ShowPopup();
            }
            else
            {
                if (activePopup != null)
                {
                    Object.Destroy(activePopup);
                    activePopup = null;
                    popupRect = null;
                }
            }
        }

        public void Update()
        {
            if (KeyBindOptions.IsBindingActive) return;

            if (Input.GetKeyDown(KeyBindOptions.K17) && !BanMod.chatOpen)
            {
                if (MenuRouter.Current == MenuRouter.Panel.SkinUI)
                    MenuRouter.Open(MenuRouter.Panel.None);
                else
                    MenuRouter.Open(MenuRouter.Panel.SkinUI);
            }
        }

        public void TogglePopup()
        {
            if (activePopup != null)
            {
                Object.Destroy(activePopup);
                activePopup = null;
                popupRect = null;
            }
            else
            {
                ShowPopup();
            }
        }

        public void ClosePopup()
        {
            if (activePopup != null)
            {
                Object.Destroy(activePopup);
                activePopup = null;
                popupRect = null;
            }
        }
        [HideFromIl2Cpp]
        private OutfitPreset FindExistingPresetByOutfit()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return null;

                var o = PlayerControl.LocalPlayer.Data.DefaultOutfit;

                return outfitPresets.FirstOrDefault(p =>
                    p != null &&
                    p.ColorId == o.ColorId &&
                    string.Equals(p.PetId, o.PetId, StringComparison.Ordinal) &&
                    string.Equals(p.HatId, o.HatId, StringComparison.Ordinal) &&
                    string.Equals(p.SkinId, o.SkinId, StringComparison.Ordinal) &&
                    string.Equals(p.VisorId, o.VisorId, StringComparison.Ordinal) &&
                    string.Equals(p.NamePlateId, o.NamePlateId, StringComparison.Ordinal)
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] FindExistingPresetByOutfit error: " + ex);
                return null;
            }
        }

        public void SaveCurrentOutfitToPresets(string presetName)
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return;

                presetName = SanitizeFileName(presetName?.Trim());
                if (string.IsNullOrWhiteSpace(presetName))
                    return;

                string baseDir = Path.Combine(Application.dataPath, "../BAN_DATA/CUSTOM/SKINPRESET");
                string imgDir = Path.Combine(baseDir, "PresetImage");
                string filePath = Path.Combine(baseDir, "Presets.txt");

                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
                if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                LoadPresetsWithImages();

                var o = PlayerControl.LocalPlayer.Data.DefaultOutfit;

                var existingPreset = FindExistingPresetByOutfit();
                if (existingPreset != null)
                {
                    string existingImgPath = Path.Combine(imgDir, existingPreset.Name + ".png");
                    TrySaveCurrentPlayerPreview(existingImgPath);

                    BMLogger.Info("[BanMod] Preset già esistente: immagine aggiornata, nessun duplicato salvato.");

                    LoadPresetsWithImages();
                    return;
                }

                string newPresetBlock =
                    $"\n[{presetName}]\n" +
                    $"colorid: {o.ColorId}\n" +
                    $"pet: {o.PetId}\n" +
                    $"hat: {o.HatId}\n" +
                    $"skin: {o.SkinId}\n" +
                    $"visor: {o.VisorId}\n" +
                    $"nameplate: {o.NamePlateId}\n";

                File.AppendAllText(filePath, newPresetBlock);

                string imgPath = Path.Combine(imgDir, presetName + ".png");
                TrySaveCurrentPlayerPreview(imgPath);

                LoadPresetsWithImages();
            }
            catch (Exception ex)
            {
                BMLogger.LogError("[BanMod] Errore salvataggio preset: " + ex);
            }
        }

        private void LoadPresetsWithImages()
        {
            outfitPresets.Clear();

            string baseDir = Path.Combine(Application.dataPath, "../BAN_DATA/CUSTOM/SKINPRESET");
            string imgDir = Path.Combine(baseDir, "PresetImage");
            string filePath = Path.Combine(baseDir, "Presets.txt");

            if (!Directory.Exists(imgDir))
                Directory.CreateDirectory(imgDir);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(
                    filePath,
                    "[Outfit1]\n" +
                    "colorID: 1\n" +
                    "pet: pet_EmptyPet\n" +
                    "hat: hat_NoHat\n" +
                    "skin: skin_None\n" +
                    "visor: visor_EmptyVisor\n" +
                    "namePlate: nameplate_default"
                );
            }

            string[] lines = File.ReadAllLines(filePath);
            OutfitPreset current = null;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    current = new OutfitPreset
                    {
                        Name = trimmed.Substring(1, trimmed.Length - 2)
                    };

                    string imgPath = Path.Combine(imgDir, current.Name + ".png");
                    if (File.Exists(imgPath))
                    {
                        current.PreviewSprite = LoadSpriteFromFile(imgPath);
                    }

                    outfitPresets.Add(current);
                }
                else if (current != null && trimmed.Contains(":"))
                {
                    int idx = trimmed.IndexOf(':');
                    string key = trimmed.Substring(0, idx).Trim().ToLower();
                    string val = trimmed.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "colorid":
                            int.TryParse(val, out current.ColorId);
                            break;
                        case "pet":
                            current.PetId = val;
                            break;
                        case "hat":
                            current.HatId = val;
                            break;
                        case "skin":
                            current.SkinId = val;
                            break;
                        case "visor":
                            current.VisorId = val;
                            break;
                        case "nameplate":
                            current.NamePlateId = val;
                            break;
                    }
                }
            }

            TryGenerateMissingCurrentPresetPreview();
        }

        private void TryGenerateMissingCurrentPresetPreview()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return;

                var outfit = PlayerControl.LocalPlayer.Data.DefaultOutfit;

                string baseDir = Path.Combine(Application.dataPath, "../BAN_DATA/CUSTOM/SKINPRESET");
                string imgDir = Path.Combine(baseDir, "PresetImage");
                if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                foreach (var preset in outfitPresets)
                {
                    if (preset.PreviewSprite != null) continue;

                    bool same =
                        preset.ColorId == outfit.ColorId &&
                        string.Equals(preset.PetId, outfit.PetId, StringComparison.Ordinal) &&
                        string.Equals(preset.HatId, outfit.HatId, StringComparison.Ordinal) &&
                        string.Equals(preset.SkinId, outfit.SkinId, StringComparison.Ordinal) &&
                        string.Equals(preset.VisorId, outfit.VisorId, StringComparison.Ordinal) &&
                        string.Equals(preset.NamePlateId, outfit.NamePlateId, StringComparison.Ordinal);

                    if (!same) continue;

                    string imgPath = Path.Combine(imgDir, preset.Name + ".png");
                    if (TrySaveCurrentPlayerPreview(imgPath))
                    {
                        preset.PreviewSprite = LoadSpriteFromFile(imgPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] TryGenerateMissingCurrentPresetPreview error: " + ex);
            }
        }

        public void ShowPopup()
        {
            LoadPresetsWithImages();

            if (activePopup != null)
            {
                Object.Destroy(activePopup);
                activePopup = null;
                popupRect = null;
            }

            if (EventSystem.current == null)
            {
                var es = new GameObject("BanMod_EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            GameObject popup = new GameObject("BanMod_SkinPopup");
            activePopup = popup;

            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;

            popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            popup.AddComponent<GraphicRaycaster>();

            var popupGroup = popup.AddComponent<CanvasGroup>();
            popupGroup.alpha = 1f;
            popupGroup.interactable = true;
            popupGroup.blocksRaycasts = true;
            popupGroup.ignoreParentGroups = true;

            var blocker = new GameObject("InputBlocker");
            blocker.transform.SetParent(popup.transform, false);
            blocker.transform.SetAsFirstSibling();

            var blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blockerRect.anchoredPosition = Vector2.zero;
            blockerRect.sizeDelta = Vector2.zero;

            var blockerImg = blocker.AddComponent<Image>();
            blockerImg.color = new Color(0f, 0f, 0f, 0.001f);
            blockerImg.raycastTarget = true;

            var blockerBtn = blocker.AddComponent<Button>();
            blockerBtn.transition = Selectable.Transition.None;
            blockerBtn.onClick.AddListener((Action)(() => { }));

            var bg = new GameObject("Background");
            bg.transform.SetParent(popup.transform, false);
            bg.transform.SetAsLastSibling();

            var bgRect = bg.AddComponent<RectTransform>();
            popupRect = bgRect;

            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);

            bgRect.sizeDelta = new Vector2(300, 330);
            bgRect.anchoredPosition = new Vector2(25, 35);

            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.95f);
            bgImg.raycastTarget = true;

            Color neutralBtn = new Color(0.22f, 0.22f, 0.22f, 1f);
            Color applyBtn = new Color(0.30f, 0.30f, 0.30f, 1f);

            var imgGO = new GameObject("PreviewImage");
            imgGO.transform.SetParent(bg.transform, false);

            var imgComp = imgGO.AddComponent<Image>();
            imgComp.rectTransform.sizeDelta = new Vector2(220, 220);
            imgComp.rectTransform.anchoredPosition = new Vector2(0, 10);
            imgComp.preserveAspect = true;

            Action UpdateDisplay = () =>
            {
                if (outfitPresets.Count == 0)
                {
                    imgComp.sprite = null;
                    imgComp.color = new Color(1, 1, 1, 0.05f);
                    return;
                }

                currentOutfitIndex = Mathf.Clamp(currentOutfitIndex, 0, outfitPresets.Count - 1);
                var p = outfitPresets[currentOutfitIndex];
                imgComp.sprite = p.PreviewSprite;
                imgComp.color = (p.PreviewSprite != null) ? Color.white : new Color(1, 1, 1, 0.05f);
            };

            UpdateDisplay();

            float btnY = -145f;

            CreateInternalSmallButton(bg.transform, "<<", new Vector2(-108, btnY), () =>
            {
                if (outfitPresets.Count == 0) return;
                currentOutfitIndex = (currentOutfitIndex - 1 + outfitPresets.Count) % outfitPresets.Count;
                UpdateDisplay();
            }, neutralBtn, new Vector2(34, 24), 14);

            CreateInternalSmallButton(bg.transform, "Apply", new Vector2(-54, btnY), () =>
            {
                if (outfitPresets.Count == 0) return;
                var sel = outfitPresets[currentOutfitIndex];
                ApplyOutfit(sel);
            }, applyBtn, new Vector2(50, 24), 13);

            CreateInternalSmallButton(bg.transform, ">>", new Vector2(0, btnY), () =>
            {
                if (outfitPresets.Count == 0) return;
                currentOutfitIndex = (currentOutfitIndex + 1) % outfitPresets.Count;
                UpdateDisplay();
            }, neutralBtn, new Vector2(34, 24), 14);

            CreateInternalSmallButton(bg.transform, "Save", new Vector2(56, btnY), () =>
            {
                string presetName = "Preset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                SaveCurrentOutfitToPresets(presetName);

                currentOutfitIndex = Mathf.Max(0, outfitPresets.Count - 1);
                UpdateDisplay();
            }, neutralBtn, new Vector2(44, 24), 12);

            CreateInternalSmallButton(bg.transform, "Delete", new Vector2(108, btnY), () =>
            {
                DeleteCurrentOutfitPreset();
                if (currentOutfitIndex >= outfitPresets.Count)
                    currentOutfitIndex = Mathf.Max(0, outfitPresets.Count - 1);

                UpdateDisplay();
            }, neutralBtn, new Vector2(50, 24), 12);

            CreateCloseButton(bg.transform, popup);
        }
        [HideFromIl2Cpp]
        private void ApplyOutfit(OutfitPreset sel)
        {
            var cp = DataManager.Player.Customization;

            cp.colorID = (byte)sel.ColorId;
            cp.Skin = sel.SkinId;
            cp.Hat = sel.HatId;
            cp.Pet = sel.PetId;
            cp.Visor = sel.VisorId;
            cp.NamePlate = sel.NamePlateId;

            DataManager.Player.Save();

            if (PlayerControl.LocalPlayer != null)
            {
                PlayerControl.LocalPlayer.RpcSetColor((byte)sel.ColorId);
                PlayerControl.LocalPlayer.RpcSetSkin(sel.SkinId);
                PlayerControl.LocalPlayer.RpcSetHat(sel.HatId);
                PlayerControl.LocalPlayer.RpcSetPet(sel.PetId);
                PlayerControl.LocalPlayer.RpcSetVisor(sel.VisorId);
                PlayerControl.LocalPlayer.RpcSetNamePlate(sel.NamePlateId);
            }
        }

        public void ForceCycleOutfit()
        {
            if (outfitPresets.Count == 0)
                LoadPresetsWithImages();

            if (outfitPresets.Count == 0)
                return;

            currentOutfitIndex = (currentOutfitIndex + 1) % outfitPresets.Count;
            ApplyOutfit(outfitPresets[currentOutfitIndex]);
        }
        [HideFromIl2Cpp]
        private void CreateInternalSmallButton(
            Transform parent,
            string label,
            Vector2 pos,
            Action action,
            Color color,
            Vector2 size,
            int fontSize)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var r = go.AddComponent<RectTransform>();
            r.sizeDelta = size;
            r.anchoredPosition = pos;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(go.transform, false);
            txt.text = label;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
        }
        [HideFromIl2Cpp]
        private void CreateInternalSmallButton(Transform parent, string label, Vector2 pos, Action action, Color color)
        {
            CreateInternalSmallButton(parent, label, pos, action, color, new Vector2(85, 35), 16);
        }

        private void DeleteCurrentOutfitPreset()
        {
            try
            {
                if (outfitPresets.Count == 0)
                    return;

                var preset = outfitPresets[currentOutfitIndex];
                if (preset == null || string.IsNullOrWhiteSpace(preset.Name))
                    return;

                string baseDir = Path.Combine(Application.dataPath, "../BAN_DATA/CUSTOM/SKINPRESET");
                string imgDir = Path.Combine(baseDir, "PresetImage");
                string filePath = Path.Combine(baseDir, "Presets.txt");
                string imgPath = Path.Combine(imgDir, preset.Name + ".png");

                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath).ToList();
                    var output = new List<string>();

                    bool skipping = false;
                    string targetHeader = "[" + preset.Name + "]";

                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();

                        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                        {
                            if (string.Equals(trimmed, targetHeader, StringComparison.OrdinalIgnoreCase))
                            {
                                skipping = true;
                                continue;
                            }
                            else
                            {
                                skipping = false;
                            }
                        }

                        if (!skipping)
                            output.Add(line);
                    }

                    File.WriteAllLines(filePath, output);
                }

                if (File.Exists(imgPath))
                    File.Delete(imgPath);

                LoadPresetsWithImages();
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] Errore eliminazione preset: " + ex);
            }
        }

        private void CreateCloseButton(Transform parent, GameObject popup)
        {
            var close = new GameObject("CloseButton");
            close.transform.SetParent(parent, false);

            var r = close.AddComponent<RectTransform>();
            r.sizeDelta = new Vector2(30, 30);
            r.anchorMin = Vector2.one;
            r.anchorMax = Vector2.one;
            r.pivot = Vector2.one;
            r.anchoredPosition = new Vector2(-5, -5);

            var img = close.AddComponent<Image>();
            img.color = new Color(0.8f, 0.1f, 0.1f, 1f);

            var btn = close.AddComponent<Button>();
            btn.onClick.AddListener((Action)(() =>
            {
                if (popup != null)
                    Object.Destroy(popup);

                if (activePopup == popup)
                    activePopup = null;

                popupRect = null;
                MenuRouter.Open(MenuRouter.Panel.None);
            }));

            var txt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(close.transform, false);
            txt.text = "X";
            txt.fontSize = 18;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
        }

        private bool TrySaveCurrentPlayerPreview(string outputPath)
        {
            try
            {
                Texture2D tex = CaptureLocalPlayerPreviewTexture(PreviewTextureSize);
                if (tex == null)
                    return false;

                byte[] png = tex.EncodeToPNG();
                if (png == null || png.Length == 0)
                {
                    Object.Destroy(tex);
                    return false;
                }

                string dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(outputPath, png);
                Object.Destroy(tex);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] Errore generazione preview preset: " + ex);
                return false;
            }
        }

        private Camera GetCaptureCamera()
        {
            if (Camera.main != null && Camera.main.enabled)
                return Camera.main;

            Camera[] cams = Camera.allCameras;
            if (cams != null)
            {
                foreach (var c in cams)
                {
                    if (c != null && c.enabled)
                        return c;
                }
            }

            return null;
        }

        private Texture2D CaptureLocalPlayerPreviewTexture(int size)
        {
            if (PlayerControl.LocalPlayer == null)
                return null;

            GameObject target = PlayerControl.LocalPlayer.gameObject;
            if (target == null)
                return null;

            Camera cam = GetCaptureCamera();
            if (cam == null)
                return null;

            SpriteRenderer[] spriteRenderers = GetPreviewRenderers(target);

            if (spriteRenderers.Length == 0)
                return null;

            List<Behaviour> disabledBehaviours = new List<Behaviour>();
            List<Renderer> disabledRenderers = new List<Renderer>();

            RenderTexture fullRt = null;
            RenderTexture scaleRt = null;
            Texture2D fullTex = null;
            Texture2D cropTex = null;
            Texture2D outTex = null;
            RenderTexture previousActive = null;
            RenderTexture previousTarget = cam.targetTexture;

            try
            {
                foreach (var tmp in target.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != null && tmp.enabled)
                    {
                        tmp.enabled = false;
                        disabledBehaviours.Add(tmp);
                    }
                }

                foreach (var r in target.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || !r.enabled) continue;

                    string n = r.gameObject.name.ToLowerInvariant();

                    bool hideThis =
                        n.Contains("name") ||
                        n.Contains("playername") ||
                        n.Contains("color") ||
                        n.Contains("colour") ||
                        n.Contains("colorblind") ||
                        n.Contains("text");

                    if (hideThis && !disabledRenderers.Contains(r))
                    {
                        r.enabled = false;
                        disabledRenderers.Add(r);
                    }
                }

                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                bool anyVisible = false;

                foreach (var sr in spriteRenderers)
                {
                    Bounds b = sr.bounds;

                    Vector3[] points = new Vector3[4];
                    points[0] = new Vector3(b.min.x, b.min.y, b.center.z);
                    points[1] = new Vector3(b.min.x, b.max.y, b.center.z);
                    points[2] = new Vector3(b.max.x, b.min.y, b.center.z);
                    points[3] = new Vector3(b.max.x, b.max.y, b.center.z);

                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(points[i]);
                        if (sp.z <= 0f) continue;

                        anyVisible = true;
                        minX = Mathf.Min(minX, sp.x);
                        minY = Mathf.Min(minY, sp.y);
                        maxX = Mathf.Max(maxX, sp.x);
                        maxY = Mathf.Max(maxY, sp.y);
                    }
                }

                if (!anyVisible)
                    return null;

                float width = Mathf.Max(1f, maxX - minX);
                float height = Mathf.Max(1f, maxY - minY);

                Bounds combinedBounds = spriteRenderers[0].bounds;
                for (int i = 1; i < spriteRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(spriteRenderers[i].bounds);
                }

                Vector3 visualCenterScreen = cam.WorldToScreenPoint(combinedBounds.center);

                float centerX = visualCenterScreen.x + (width * 0.10f);
                float centerY = visualCenterScreen.y;

                float side = Mathf.Max(width, height) * 1.00f;

                int screenW = Mathf.Max(1, Screen.width);
                int screenH = Mathf.Max(1, Screen.height);

                if (side > screenW) side = screenW;
                if (side > screenH) side = screenH;

                int cropSize = Mathf.Max(1, Mathf.RoundToInt(side));
                int cropX = Mathf.RoundToInt(centerX - cropSize * 0.5f);
                int cropY = Mathf.RoundToInt(centerY - cropSize * 0.5f);

                fullRt = new RenderTexture(screenW, screenH, 24, RenderTextureFormat.ARGB32);
                fullRt.antiAliasing = 1;
                fullRt.filterMode = FilterMode.Bilinear;
                fullRt.useMipMap = false;
                fullRt.autoGenerateMips = false;
                fullRt.Create();

                cam.targetTexture = fullRt;
                cam.Render();

                previousActive = RenderTexture.active;
                RenderTexture.active = fullRt;

                fullTex = new Texture2D(screenW, screenH, TextureFormat.ARGB32, false);
                fullTex.ReadPixels(new Rect(0, 0, screenW, screenH), 0, 0);
                fullTex.Apply(false, false);

                int pad = cropSize;

                int paddedW = screenW + pad * 2;
                int paddedH = screenH + pad * 2;

                Texture2D paddedTex = new Texture2D(paddedW, paddedH, TextureFormat.ARGB32, false);

                Color[] clearPixels = new Color[paddedW * paddedH];
                for (int i = 0; i < clearPixels.Length; i++)
                    clearPixels[i] = new Color(0f, 0f, 0f, 0f);

                paddedTex.SetPixels(clearPixels);

                Color[] fullPixels = fullTex.GetPixels(0, 0, screenW, screenH);
                paddedTex.SetPixels(pad, pad, screenW, screenH, fullPixels);
                paddedTex.Apply(false, false);

                int paddedCropX = cropX + pad;
                int paddedCropY = cropY + pad;

                Color[] cropPixels = paddedTex.GetPixels(paddedCropX, paddedCropY, cropSize, cropSize);

                cropTex = new Texture2D(cropSize, cropSize, TextureFormat.ARGB32, false);
                cropTex.SetPixels(cropPixels);
                cropTex.Apply(false, false);
                cropTex.filterMode = FilterMode.Bilinear;

                Object.Destroy(paddedTex);

                scaleRt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
                scaleRt.antiAliasing = 1;
                scaleRt.filterMode = FilterMode.Bilinear;
                scaleRt.useMipMap = false;
                scaleRt.autoGenerateMips = false;
                scaleRt.Create();

                Graphics.Blit(cropTex, scaleRt);

                RenderTexture.active = scaleRt;

                outTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                outTex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                outTex.Apply(false, false);
                outTex.filterMode = FilterMode.Bilinear;

                return outTex;
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] CaptureLocalPlayerPreviewTexture error: " + ex);

                if (outTex != null) Object.Destroy(outTex);
                return null;
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;

                foreach (var b in disabledBehaviours)
                {
                    if (b != null)
                        b.enabled = true;
                }

                foreach (var r in disabledRenderers)
                {
                    if (r != null)
                        r.enabled = true;
                }

                if (fullTex != null) Object.Destroy(fullTex);
                if (cropTex != null) Object.Destroy(cropTex);

                if (fullRt != null)
                {
                    fullRt.Release();
                    Object.Destroy(fullRt);
                }

                if (scaleRt != null)
                {
                    scaleRt.Release();
                    Object.Destroy(scaleRt);
                }
            }
        }
        [HideFromIl2Cpp]
        private Il2CppStructArray<byte> ToIl2CppByteArray(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            var arr = new Il2CppStructArray<byte>(data.Length);
            for (int i = 0; i < data.Length; i++)
                arr[i] = data[i];

            return arr;
        }
        [HideFromIl2Cpp]
        private SpriteRenderer[] GetPreviewRenderers(GameObject target)
        {
            if (target == null)
                return new SpriteRenderer[0];

            Vector3 playerPos = target.transform.position;

            return target.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r =>
                    r != null &&
                    r.enabled &&
                    r.sprite != null &&
                    Vector3.Distance(r.bounds.center, playerPos) <= 1.75f)
                .ToArray();
        }

        private Sprite LoadSpriteFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                byte[] data = File.ReadAllBytes(path);
                if (data == null || data.Length < 8)
                    return null;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);

                var il2cppData = ToIl2CppByteArray(data);
                if (il2cppData == null)
                {
                    Object.Destroy(tex);
                    return null;
                }

                bool loaded = ImageConversion.LoadImage(tex, il2cppData);
                if (!loaded || tex == null || tex.width <= 0 || tex.height <= 0)
                {
                    Object.Destroy(tex);
                    return null;
                }

                tex.filterMode = FilterMode.Bilinear;

                return Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] LoadSpriteFromFile error: " + ex);
                return null;
            }
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Preset";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), "");

            return name.Trim();
        }
    }
}