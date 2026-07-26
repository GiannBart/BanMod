//credits and licenses in the resources folder
using AmongUs.GameOptions;
using BanMod;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static BanMod.Translator;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    public static class GameSettingMenuPatch
    {
        public static GameOptionsMenu SettingsTab;
        public static PassiveButton SettingsButton;
        public static PassiveButton BanButton;
        public static PassiveButton ModdedButton;
        public static PassiveButton OtherButton;

        public const string MenuName = "ModTab";

        public static MainTab CurrentMainTab { get; private set; } = MainTab.Settings;

        private static readonly Dictionary<OptionCategory, CategoryHeaderMasked> CategoryHeaders = new();

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            if (SettingsTab != null) return;

            SettingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            SettingsTab.name = MenuName;

            var vanillaOptions = SettingsTab.GetComponentsInChildren<OptionBehaviour>();
            foreach (var vanillaOption in vanillaOptions)
                Object.Destroy(vanillaOption.gameObject);

            var gameSettingsLabel = __instance.transform.Find("GameSettingsLabel");
            if (gameSettingsLabel)
                gameSettingsLabel.localPosition += Vector3.up * 0.2f;
            __instance.MenuDescriptionText.transform.parent.localPosition += Vector3.up * 0.4f;
            __instance.GamePresetsButton.transform.parent.localPosition += Vector3.up * 0.5f;

            SettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            SettingsButton.name = "SettingsButton";
            SettingsButton.transform.localPosition = new Vector3(-3.86f, -2.23f, -2.00f);
            ResizeButton(SettingsButton, 0.4f, 0.5f);
            SettingsButton.buttonText.DestroyTranslator();
            SettingsButton.buttonText.text = GetString("HostSettingsLabel");
            SettingsButton.buttonText.fontSize = 12;
            SettingsButton.buttonText.color = Color.white;
            SetButtonColor(SettingsButton, BanMod.UnityModColor);

            BanButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            BanButton.name = "BanButton";
            BanButton.transform.localPosition = new Vector3(-2.46f, -2.23f, -2.00f);
            ResizeButton(BanButton, 0.4f, 0.5f);
            BanButton.buttonText.text = GetString("BanOption");
            BanButton.buttonText.fontSize = 12;
            BanButton.buttonText.color = Color.white;
            SetButtonColor(BanButton, BanMod.UnityModColor);

            ModdedButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            ModdedButton.name = "ModdedButton";
            ModdedButton.transform.localPosition = new Vector3(-3.86f, -2.63f, -2.00f);
            ResizeButton(ModdedButton, 0.4f, 0.5f);
            ModdedButton.buttonText.text = GetString("GeneralOption");
            ModdedButton.buttonText.fontSize = 12;
            ModdedButton.buttonText.color = Color.white;
            SetButtonColor(ModdedButton, BanMod.UnityModColor);

            OtherButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            OtherButton.name = "OtherButton";
            OtherButton.transform.localPosition = new Vector3(-2.46f, -2.63f, -2.00f);
            ResizeButton(OtherButton, 0.4f, 0.5f);
            UpdateOtherButtonText();
            OtherButton.buttonText.fontSize = 12;
            OtherButton.buttonText.color = Color.white;
            SetButtonColor(OtherButton, BanMod.UnityModColor);

            SettingsButton.OnClick.AddListener((Action)(() => OpenMainTab(__instance, MainTab.Settings, "SettingsTabDescription", SettingsButton)));
            BanButton.OnClick.AddListener((Action)(() => OpenMainTab(__instance, MainTab.Ban, "BanTabDescription", BanButton)));
            ModdedButton.OnClick.AddListener((Action)(() => OpenMainTab(__instance, MainTab.Modded, "ModdedTabDescription", ModdedButton)));
            OtherButton.OnClick.AddListener((Action)(() =>
            {
                UpdateOtherButtonText();
                OpenMainTab(__instance, MainTab.Other, IsHideAndSeekSafe() ? "HaS" : "OtherTabDescription", OtherButton);
            }));

            CreateAllCategoryHeaders(__instance);
            CreateAllOptionRows(__instance);

            SettingsTab.gameObject.SetActive(false);
        }

        private static void OpenMainTab(GameSettingMenu menu, MainTab tab, string descriptionKey, PassiveButton selectedButton)
        {
            menu.ChangeTab(-1, false);
            SettingsTab.gameObject.SetActive(true);
            menu.MenuDescriptionText.text = GetString(descriptionKey);

            CurrentMainTab = tab;
            SelectMainButton(selectedButton);
            ApplyCurrentTabVisibility();
            GameOptionsMenuUpdatePatch.RefreshLayout(SettingsTab);
        }

        private static void SelectMainButton(PassiveButton selectedButton)
        {
            if (SettingsButton) SettingsButton.SelectButton(false);
            if (BanButton) BanButton.SelectButton(false);
            if (ModdedButton) ModdedButton.SelectButton(false);
            if (OtherButton) OtherButton.SelectButton(false);
            if (selectedButton) selectedButton.SelectButton(true);
        }

        private static void CreateAllOptionRows(GameSettingMenu __instance)
        {
            var template = __instance.GameSettingsTab.stringOptionOrigin;
            var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();

            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, SettingsTab.settingsContainer);
                    scOptions.Add(stringOption);
                    stringOption.SetClickMask(__instance.GameSettingsButton.ClickMask);
                    stringOption.SetUpFromData(stringOption.data, GameOptionsMenu.MASK_LAYER);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;
                    stringOption.TitleText.text = GetString(option.Name);

                    var indent = 0f;
                    var parent = option.Parent;
                    while (parent != null)
                    {
                        indent += 0.15f;
                        parent = parent.Parent;
                    }
                    stringOption.LabelBackground.size += new Vector2(2f - indent * 2, 0f);
                    stringOption.LabelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
                    stringOption.TitleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
                    stringOption.TitleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

                    option.OptionBehaviour = stringOption;
                }

                option.OptionBehaviour.gameObject.SetActive(false);
            }

            SettingsTab.Children = scOptions;
        }

        private static void CreateAllCategoryHeaders(GameSettingMenu __instance)
        {
            CategoryHeaders.Clear();

            foreach (var category in GetCategoryOrder())
            {
                if (CategoryHeaders.ContainsKey(category))
                    continue;

                var h = Object.Instantiate(__instance.GameSettingsTab.categoryHeaderOrigin, SettingsTab.settingsContainer);
                h.Title.text = TranslateHeader(category);
                h.Background.material.SetInt(PlayerMaterial.MaskLayer, GameOptionsMenu.MASK_LAYER);
                h.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;
                h.gameObject.SetActive(false);
                CategoryHeaders[category] = h;
            }
        }

        private static IEnumerable<OptionCategory> GetCategoryOrder()
        {
            // Ordine reale di OptionHolder: non devi piu' scrivere 20 RenderGroup a mano.
            return OptionItem.AllOptions
                .Select(o => o.Category)
                .Distinct();
        }

        private static string TranslateHeader(OptionCategory category)
        {
            string key = category.GetHeaderKey();
            string translated = GetString(key);
            if (string.IsNullOrWhiteSpace(translated) || translated.StartsWith("<INVALID:", StringComparison.OrdinalIgnoreCase))
                return category.ToString();
            return translated;
        }

        public static bool ShouldShowOption(OptionItem option)
        {
            if (option == null)
                return false;

            if (!option.IsVisibleByParent())
                return false;

            if (option.MainTab != CurrentMainTab)
                return false;

            if (CurrentMainTab == MainTab.Other && IsHideAndSeekSafe())
                return option.Category == OptionCategory.Seeker;

            if (option.Category == OptionCategory.Seeker && !IsHideAndSeekSafe())
                return false;

            if (option.Category == OptionCategory.SNS && !IsSnSMode())
                return false;

            if (option.Category == OptionCategory.FFA && !IsFFAMode())
                return false;

            if (option.Category == OptionCategory.GameMode && IsHideAndSeekSafe())
                return false;

            if (option.Category == OptionCategory.GameModeHnS && !IsHideAndSeekSafe())
                return false;

            return true;
        }

        public static void ApplyCurrentTabVisibility()
        {
            foreach (var header in CategoryHeaders.Values)
                if (header != null) header.gameObject.SetActive(false);

            foreach (var option in OptionItem.AllOptions)
            {
                if (option?.OptionBehaviour == null)
                    continue;

                option.OptionBehaviour.gameObject.SetActive(ShouldShowOption(option));
                option.Refresh();
            }
        }

        public static IReadOnlyDictionary<OptionCategory, CategoryHeaderMasked> GetHeaders()
        {
            return CategoryHeaders;
        }

        private static bool IsSnSMode()
        {
            try
            {
                return Options.GameMode != null && (GameModeType)Options.GameMode.GetValue() == GameModeType.SnS;
            }
            catch
            {
                return false;
            }
        }
        private static bool IsFFAMode()
        {
            try
            {
                return Options.GameMode != null && (GameModeType)Options.GameMode.GetValue() == GameModeType.FFA;
            }
            catch
            {
                return false;
            }
        }
        private static bool IsHideAndSeekSafe()
        {
            try
            {
                return GameManager.Instance != null && GameManager.Instance.IsHideAndSeek();
            }
            catch
            {
                return false;
            }
        }

        private static void UpdateOtherButtonText()
        {
            if (OtherButton == null || OtherButton.buttonText == null)
                return;

            OtherButton.buttonText.text = GetString(IsHideAndSeekSafe() ? "SeekerOption" : "RoleOption");
        }

        private static void SetButtonColor(PassiveButton button, Color color)
        {
            var activeSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = button.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = selectedSprite.color = color;
        }

        private static void ResizeButton(PassiveButton button, float scaleX, float scaleY = 1f)
        {
            button.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static void ChangeTabPrefix(bool previewOnly)
        {
            if (!previewOnly)
            {
                if (SettingsTab) SettingsTab.gameObject.SetActive(false);
                if (SettingsButton) SettingsButton.SelectButton(false);
                if (BanButton) BanButton.SelectButton(false);
                if (ModdedButton) ModdedButton.SelectButton(false);
                if (OtherButton) OtherButton.SelectButton(false);

                foreach (var header in CategoryHeaders.Values)
                    if (header != null) header.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;
        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.name == GameSettingMenuPatch.MenuName)
            {
                _timer += Time.deltaTime;
                if (_timer < 0.1f) return;
                _timer = 0f;
                RefreshLayout(__instance);
            }
        }

        public static void RefreshLayout(GameOptionsMenu __instance)
        {
            float offset = 2.6f;

            // Reset completo: cosi' cambiando bottone non rimangono header della tab vecchia.
            foreach (var header in GameSettingMenuPatch.GetHeaders().Values)
                if (header != null) header.gameObject.SetActive(false);

            foreach (var option in OptionItem.AllOptions)
            {
                if (option?.OptionBehaviour == null)
                    continue;
                option.OptionBehaviour.gameObject.SetActive(GameSettingMenuPatch.ShouldShowOption(option));
            }

            foreach (var category in OptionItem.AllOptions.Select(o => o.Category).Distinct())
            {
                if (!GameSettingMenuPatch.GetHeaders().TryGetValue(category, out var header) || header == null)
                    continue;

                var visibleOptions = OptionItem.GetOptions(category)
                    .Where(opt => opt.OptionBehaviour != null && GameSettingMenuPatch.ShouldShowOption(opt))
                    .ToList();

                if (visibleOptions.Count == 0)
                    continue;

                header.gameObject.SetActive(true);
                offset -= GameOptionsMenu.HEADER_HEIGHT;
                header.transform.localPosition = new Vector3(GameOptionsMenu.HEADER_X, offset, -2f);

                foreach (var option in visibleOptions)
                {
                    option.OptionBehaviour.gameObject.SetActive(true);
                    offset -= GameOptionsMenu.SPACING_Y;
                    option.OptionBehaviour.transform.localPosition = new Vector3(GameOptionsMenu.START_POS_X, offset, -2f);
                    option.OptionBehaviour.TitleText.color = option.NameColor;
                }
            }

            __instance.scrollBar.ContentYBounds.max = Math.Max(0f, (-offset) - 1.5f);
        }
    }

    [HarmonyPatch(typeof(StringOption))]
    public static class StringOptionFixPatch
    {
        [HarmonyPatch(nameof(StringOption.Initialize)), HarmonyPrefix]
        public static bool InitializePrefix(StringOption __instance)
        {
            return __instance.data != null;
        }

        [HarmonyPatch(nameof(StringOption.Increase)), HarmonyPrefix]
        public static bool Inc(StringOption __instance)
        {
            var o = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (o == null) return true;
            o.SetValue(o.CurrentValue + (Input.GetKey(KeyCode.LeftShift) ? 5 : 1));
            __instance.ValueText.text = o.GetString();
            return false;
        }

        [HarmonyPatch(nameof(StringOption.Decrease)), HarmonyPrefix]
        public static bool Dec(StringOption __instance)
        {
            var o = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (o == null) return true;
            o.SetValue(o.CurrentValue - (Input.GetKey(KeyCode.LeftShift) ? 5 : 1));
            __instance.ValueText.text = o.GetString();
            return false;
        }
    }
}
