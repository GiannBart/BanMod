//credits and licenses in the resources folder
using AmongUs.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BanMod
{
    public class NameUI : MonoBehaviour
    {
        public static NameUI Instance;

        private PassiveButton template;

        private readonly List<string> presetNames = new List<string>();
        private int currentNameIndex = 0;

        private PassiveButton rootButton;
        private PassiveButton prevButton;
        private PassiveButton applyButton;
        private PassiveButton nextButton;

        private TMP_Text valueText;

        private bool created = false;

        private static readonly Color32 MainNormalColor = new Color32(70, 130, 180, 255);
        private static readonly Color32 MainHoverColor = new Color32(65, 105, 225, 255);

        private static readonly Color32 MiniNormalColor = new Color32(70, 130, 180, 255);
        private static readonly Color32 MiniHoverColor = new Color32(65, 105, 225, 255);

        private static readonly Color32 ApplyNormalColor = new Color32(70, 130, 180, 255);
        private static readonly Color32 ApplyHoverColor = new Color32(65, 105, 225, 255);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Initialize(PassiveButton buttonTemplate)
        {
            template = buttonTemplate;

            if (template == null)
            {
                Debug.LogError("[BanMod] Initialize NameUI fallita: template nullo.");
                return;
            }

            if (!IsButtonAlive(rootButton))
                created = false;

            if (!created)
                CreateSelector();
            else
                RefreshUI();
        }

        public void RefreshUI()
        {
            if (!IsButtonAlive(rootButton))
            {
                created = false;
                CreateSelector();
                return;
            }

            LoadNamesFromFile();
            SyncIndexWithCurrentPlayerName();
            RefreshDisplayedName();
        }

        public void DestroySelector()
        {
            if (rootButton != null) Object.Destroy(rootButton.gameObject);
            if (prevButton != null) Object.Destroy(prevButton.gameObject);
            if (applyButton != null) Object.Destroy(applyButton.gameObject);
            if (nextButton != null) Object.Destroy(nextButton.gameObject);

            rootButton = null;
            prevButton = null;
            applyButton = null;
            nextButton = null;
            valueText = null;

            created = false;
        }

        public void CreateSelector()
        {
            if (created && IsButtonAlive(rootButton)) return;

            DestroySelector();

            if (template == null)
            {
                Debug.LogError("[BanMod] Template del bottone nullo.");
                return;
            }

            LoadNamesFromFile();
            SyncIndexWithCurrentPlayerName();

            Transform parent = MainMenuManagerStartPatch.Logo != null
                ? MainMenuManagerStartPatch.Logo.transform
                : null;

            if (parent == null)
            {
                Debug.LogError("[BanMod] Parent del bottone non trovato.");
                return;
            }

            Action action = () => { };
            rootButton = CreateMenuButton(
                name: "BanMod_NameRoot",
                parent: parent,
                localPosition: new Vector3(-2f, -0.55f, 1f),
                normalColor: MainNormalColor,
                hoverColor: MainHoverColor,
                action: action,
                label: "",
                scale: new Vector2(1.75f, 0.75f),
                fontSize: 3.5f
            );

            if (rootButton == null)
            {
                Debug.LogError("[BanMod] Impossibile creare il bottone principale.");
                return;
            }

            rootButton.transform.SetAsLastSibling();

            CreateMainText(rootButton.transform);
            CreateMiniButtons(rootButton.transform);
            RefreshDisplayedName();

            created = true;
        }

        private void CreateMainText(Transform parent)
        {
            var templateText = parent.Find("FontPlacer/Text_TMP")?.GetComponent<TMP_Text>();
            if (templateText == null)
            {
                Debug.LogError("[BanMod] TMP_Text del template non trovato.");
                return;
            }

            templateText.DestroyTranslator();
            templateText.gameObject.SetActive(false);

            valueText = Object.Instantiate(templateText, parent);
            valueText.name = "Value_TMP";
            valueText.gameObject.SetActive(true);
            valueText.DestroyTranslator();
            valueText.text = GetCurrentDisplayedName();
            valueText.fontSize = 3.0f;
            valueText.enableWordWrapping = false;
            valueText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            valueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.transform.localScale = Vector3.one;

            var textAspect = valueText.GetComponent<AspectPosition>();
            if (textAspect != null) Object.Destroy(textAspect);

            var rect = valueText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1.55f, 0.30f);
            rect.anchoredPosition = new Vector2(0f, 0.10f);

            valueText.transform.localPosition = new Vector3(0f, 0.10f, -1f);
        }

        private void CreateMiniButtons(Transform parent)
        {
            const float rowY = -0.16f;

            prevButton = CreateMenuButton(
                name: "BanMod_NamePrev",
                parent: parent,
                localPosition: new Vector3(-0.56f, rowY, -2f),
                normalColor: MiniNormalColor,
                hoverColor: MiniHoverColor,
                action: (UnityEngine.Events.UnityAction)SelectPreviousName,
                label: "<<",
                scale: new Vector2(0.20f, 0.14f),
                fontSize: 0.50f
            );

            applyButton = CreateMenuButton(
                name: "BanMod_NameApply",
                parent: parent,
                localPosition: new Vector3(0f, rowY, -2f),
                normalColor: ApplyNormalColor,
                hoverColor: ApplyHoverColor,
                action: (UnityEngine.Events.UnityAction)ApplySelectedName,
                label: "APPLY",
                scale: new Vector2(0.34f, 0.14f),
                fontSize: 0.42f
            );

            nextButton = CreateMenuButton(
                name: "BanMod_NameNext",
                parent: parent,
                localPosition: new Vector3(0.56f, rowY, -2f),
                normalColor: MiniNormalColor,
                hoverColor: MiniHoverColor,
                action: (UnityEngine.Events.UnityAction)SelectNextName,
                label: ">>",
                scale: new Vector2(0.20f, 0.14f),
                fontSize: 0.50f
            );
        }

        private void SelectPreviousName()
        {
            if (presetNames.Count == 0) return;
            currentNameIndex = (currentNameIndex - 1 + presetNames.Count) % presetNames.Count;
            RefreshDisplayedName();
        }

        private void SelectNextName()
        {
            if (presetNames.Count == 0) return;
            currentNameIndex = (currentNameIndex + 1) % presetNames.Count;
            RefreshDisplayedName();
        }

        private void ApplySelectedName()
        {
            string finalName = GetCurrentDisplayedName().Trim();
            if (string.IsNullOrWhiteSpace(finalName)) return;

            DataManager.Player.Customization.Name = finalName;
            DataManager.Player.Save();
            RefreshDisplayedName();
        }

        public void RefreshDisplayedName()
        {
            if (valueText == null) return;

            string nameToShow = GetCurrentDisplayedName();

            if (nameToShow == "\uFFA0")
            {
                valueText.text = "INVISIBLE NAME";
                valueText.color = Color.gray; 
            }
            else
            {
                valueText.text = nameToShow;
                valueText.color = Color.white; 
            }
        }

        private string GetCurrentDisplayedName()
        {
            if (presetNames.Count == 0)
                return DataManager.Player.Customization.Name;

            if (currentNameIndex < 0 || currentNameIndex >= presetNames.Count)
                currentNameIndex = 0;

            return presetNames[currentNameIndex];
        }

        private void SyncIndexWithCurrentPlayerName()
        {
            string currentPlayerName = DataManager.Player.Customization.Name?.Trim() ?? "";

            if (presetNames.Count == 0)
            {
                currentNameIndex = 0;
                return;
            }

            int foundIndex = presetNames.FindIndex(n =>
                string.Equals(n.Trim(), currentPlayerName, StringComparison.OrdinalIgnoreCase));

            currentNameIndex = foundIndex >= 0 ? foundIndex : 0;
        }

        private void LoadNamesFromFile()
        {
            try
            {
                string path = Path.Combine(Application.dataPath, "../BAN_DATA/CUSTOM/NAME/Names.txt");

                presetNames.Clear();

                if (File.Exists(path))
                {
                    presetNames.AddRange(
                        File.ReadAllLines(path)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => DecodeEncodedNonAsciiCharacters(s.Trim()))
                    );
                }

                if (presetNames.Count == 0)
                {
                    presetNames.Add("BanMod_Player");

                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllLines(path, presetNames.ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[BanMod] Errore caricamento nomi: " + ex);
            }
        }

        private string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(value, @"\\u(?<Value>[a-zA-Z0-9]{4})", m =>
            {
                return ((char)int.Parse(
                    m.Groups["Value"].Value,
                    System.Globalization.NumberStyles.HexNumber)).ToString();
            });
        }

        private bool IsButtonAlive(PassiveButton button)
        {
            return button != null && button.gameObject != null;
        }

        private PassiveButton CreateMenuButton(
            string name,
            Transform parent,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            UnityEngine.Events.UnityAction action,
            string label,
            Vector2? scale = null,
            float fontSize = 3.5f)
        {
            if (template == null) return null;

            var button = Object.Instantiate(template, parent);
            button.name = name;

            var buttonAspect = button.GetComponent<AspectPosition>();
            if (buttonAspect != null) Object.Destroy(buttonAspect);

            button.transform.localPosition = localPosition;
            button.transform.localScale = Vector3.one;

            button.OnClick = new Button.ButtonClickedEvent();
            button.OnClick.AddListener(action);

            var buttonText = button.transform.Find("FontPlacer/Text_TMP")?.GetComponent<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.DestroyTranslator();
                buttonText.fontSize = fontSize;
                buttonText.enableWordWrapping = false;
                buttonText.text = label;
                buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                buttonText.verticalAlignment = VerticalAlignmentOptions.Middle;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.overflowMode = TextOverflowModes.Overflow;
                buttonText.transform.localScale = Vector3.one;

                var container = buttonText.transform.parent;
                if (container != null)
                {
                    var containerAspect = container.GetComponent<AspectPosition>();
                    if (containerAspect != null) Object.Destroy(containerAspect);
                }

                var textAspect = buttonText.GetComponent<AspectPosition>();
                if (textAspect != null) Object.Destroy(textAspect);

                var rect = buttonText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = scale ?? rect.sizeDelta;

                buttonText.transform.localPosition = new Vector3(0.17f, -0.01f, buttonText.transform.localPosition.z);
            }

            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = normalColor;
            hoverSprite.color = hoverColor;

            var buttonCollider = button.GetComponent<BoxCollider2D>();
            if (scale.HasValue)
            {
                normalSprite.size = scale.Value;
                hoverSprite.size = scale.Value;
                buttonCollider.size = scale.Value;
            }

            buttonCollider.offset = Vector2.zero;
            return button;
        }
    }
}