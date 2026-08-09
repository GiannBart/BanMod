// credits and licenses in the resources folder

using AmongUs.GameOptions;
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

        // =========================================================
        // 6 MAIN BUTTONS
        // =========================================================

        public static PassiveButton GeneralButton;
        public static PassiveButton GameModesButton;
        public static PassiveButton ModerationButton;
        public static PassiveButton RolesButton;
        public static PassiveButton TasksButton;
        public static PassiveButton SabotagesButton;

        public const string MenuName = "ModTab";

        public static MainTab CurrentMainTab { get; private set; }
            = MainTab.Game;


        // =========================================================
        // BUTTON LAYOUT
        // =========================================================

        private static readonly Vector3 ButtonPositionLeft =
            new(-3.86f, -2.19f, -2.00f);

        private static readonly Vector3 ButtonPositionRight =
            new(-2.46f, -2.19f, -2.00f);

        private static readonly Vector3 ButtonSize =
            new(0.40f, 0.35f, 1.00f);

        private const float ButtonRowSpacing = 0.28f;


        // =========================================================
        // CATEGORY HEADERS
        // =========================================================

        private static readonly Dictionary<
            OptionCategory,
            CategoryHeaderMasked
        > CategoryHeaders = new();


        // =========================================================
        // ORDINE DELLE SOTTOCATEGORIE
        // =========================================================

        private static readonly OptionCategory[] CategoryOrder =
        {
            // GENERAL
            // GENERAL
            OptionCategory.Lobby,
            OptionCategory.Chat,
            OptionCategory.Appearance,
            OptionCategory.Protection,

            // GAME MODES
            OptionCategory.GameMode,
            OptionCategory.SNS,
            OptionCategory.FFA,
            OptionCategory.Seeker,
            OptionCategory.Gameplay,
            OptionCategory.Meetings,

            // MODERATION
            OptionCategory.Levels,
            OptionCategory.Blocklist,
            OptionCategory.Cheat,
            OptionCategory.Afk,
            OptionCategory.Cam,
            OptionCategory.CamTask,
            OptionCategory.Follow,
            OptionCategory.Spamlist,
            OptionCategory.Wordlist,

            // ROLES
            OptionCategory.Impostor,
            OptionCategory.Engineer,
            OptionCategory.Watcher,
            OptionCategory.Scientist,
            OptionCategory.Guesser,
            OptionCategory.Jester,
            OptionCategory.Exiler,
            OptionCategory.Profiler,
            OptionCategory.Judge,
            OptionCategory.Immortal,

            // TASKS
            OptionCategory.Task,
            OptionCategory.Common,
            OptionCategory.Short,
            OptionCategory.Long,

            // SABOTAGES
            OptionCategory.SabotageOption,
            OptionCategory.Sabotage
        };


        // =========================================================
        // START
        // =========================================================

        [HarmonyPatch(nameof(GameSettingMenu.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            if (SettingsTab != null)
                return;


            // =====================================================
            // CREATE CUSTOM SETTINGS TAB
            // =====================================================

            SettingsTab = Object.Instantiate(
                __instance.GameSettingsTab,
                __instance.GameSettingsTab.transform.parent
            );

            SettingsTab.name = MenuName;

            ClearTabContents(SettingsTab);


            // Sposta leggermente elementi vanilla
            var gameSettingsLabel =
                __instance.transform.Find("GameSettingsLabel");

            if (gameSettingsLabel)
            {
                gameSettingsLabel.localPosition +=
                    Vector3.up * 0.2f;
            }

            __instance.MenuDescriptionText
                .transform
                .parent
                .localPosition += Vector3.up * 0.4f;

            __instance.GamePresetsButton
                .transform
                .parent
                .localPosition += Vector3.up * 0.5f;


            // =====================================================
            // GENERAL
            // =====================================================

            GeneralButton = Object.Instantiate(
                __instance.GameSettingsButton,
                __instance.GameSettingsButton.transform.parent
            );

            ConfigureButton(
                GeneralButton,
                "GeneralButton",
                GetButtonPosition(0, true),
                GetTranslatedOrFallback(
                    "MainTab.Host",
                    "Host"
                ),
                12f
            );


            // =====================================================
            // GAME MODES
            // =====================================================

            GameModesButton = Object.Instantiate(
                GeneralButton,
                GeneralButton.transform.parent
            );

            ConfigureButton(
                GameModesButton,
                "GameModesButton",
                GetButtonPosition(0, false),
                GetTranslatedOrFallback(
                    "MainTab.Game",
                    "Game"
                ),
                10f
            );


            // =====================================================
            // MODERATION
            // =====================================================

            ModerationButton = Object.Instantiate(
                GeneralButton,
                GeneralButton.transform.parent
            );

            ConfigureButton(
                ModerationButton,
                "ModerationButton",
                GetButtonPosition(1, true),
                GetTranslatedOrFallback(
                    "MainTab.Moderation",
                    "Moderation"
                ),
                10f
            );


            // =====================================================
            // ROLES
            // =====================================================

            RolesButton = Object.Instantiate(
                GeneralButton,
                GeneralButton.transform.parent
            );

            ConfigureButton(
                RolesButton,
                "RolesButton",
                GetButtonPosition(1, false),
                GetTranslatedOrFallback(
                    "MainTab.Roles",
                    "Roles"
                ),
                12f
            );


            // =====================================================
            // TASKS
            // =====================================================

            TasksButton = Object.Instantiate(
                GeneralButton,
                GeneralButton.transform.parent
            );

            ConfigureButton(
                TasksButton,
                "TasksButton",
                GetButtonPosition(2, true),
                GetTranslatedOrFallback(
                    "MainTab.Tasks",
                    "Tasks"
                ),
                12f
            );


            // =====================================================
            // SABOTAGES
            // =====================================================

            SabotagesButton = Object.Instantiate(
                GeneralButton,
                GeneralButton.transform.parent
            );

            ConfigureButton(
                SabotagesButton,
                "SabotagesButton",
                GetButtonPosition(2, false),
                GetTranslatedOrFallback(
                    "MainTab.Sabotages",
                    "Sabotages"
                ),
                10f
            );


            // =====================================================
            // BUTTON EVENTS
            // =====================================================

            GeneralButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Host,
                        "GeneralTabDescription",
                        "General settings.",
                        GeneralButton
                    )
                )
            );


            GameModesButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Game,
                        "GameModesTabDescription",
                        "Game mode settings.",
                        GameModesButton
                    )
                )
            );


            ModerationButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Moderation,
                        "ModerationTabDescription",
                        "Moderation and protection settings.",
                        ModerationButton
                    )
                )
            );


            RolesButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Roles,
                        "RolesTabDescription",
                        "Role settings.",
                        RolesButton
                    )
                )
            );


            TasksButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Tasks,
                        "TasksTabDescription",
                        "Task settings.",
                        TasksButton
                    )
                )
            );


            SabotagesButton.OnClick.AddListener(
                (Action)(() =>
                    OpenMainTab(
                        __instance,
                        MainTab.Sabotages,
                        "SabotagesTabDescription",
                        "Sabotage settings.",
                        SabotagesButton
                    )
                )
            );


            // =====================================================
            // CREATE CONTENT
            // =====================================================

            CreateAllCategoryHeaders(__instance);
            CreateAllOptionRows(__instance);

            HideCustomTab();
        }


        // =========================================================
        // CLEAR CLONED TAB
        // =========================================================

        private static void ClearTabContents(GameOptionsMenu tab)
        {
            foreach (
                var vanillaOption
                in tab.GetComponentsInChildren<OptionBehaviour>()
            )
            {
                Object.Destroy(vanillaOption.gameObject);
            }

            foreach (
                var vanillaHeader
                in tab.GetComponentsInChildren<CategoryHeaderMasked>()
            )
            {
                vanillaHeader.gameObject.SetActive(false);
            }

            tab.Children =
                new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();

            tab.scrollBar.ContentYBounds.max = 0f;
        }


        // =========================================================
        // BUTTON POSITION
        // =========================================================

        private static Vector3 GetButtonPosition(
            int row,
            bool left)
        {
            Vector3 basePosition =
                left
                    ? ButtonPositionLeft
                    : ButtonPositionRight;

            return basePosition +
                   Vector3.down *
                   (ButtonRowSpacing * row);
        }


        // =========================================================
        // CONFIGURE BUTTON
        // =========================================================

        private static void ConfigureButton(
            PassiveButton button,
            string objectName,
            Vector3 position,
            string text,
            float maximumFontSize)
        {
            button.name = objectName;

            button.gameObject.SetActive(true);

            button.transform.localPosition = position;
            button.transform.localScale = ButtonSize;

            button.buttonText.DestroyTranslator();

            button.buttonText.text = text;
            button.buttonText.color = Color.white;

            button.buttonText.alignment =
                TextAlignmentOptions.Center;

            button.buttonText.enableWordWrapping = false;
            button.buttonText.enableAutoSizing = true;

            button.buttonText.fontSizeMin = 6f;
            button.buttonText.fontSizeMax = maximumFontSize;

            SetButtonColor(
                button,
                BanMod.UnityModColor
            );
        }


        // =========================================================
        // OPEN MAIN TAB
        // =========================================================

        private static void OpenMainTab(
            GameSettingMenu menu,
            MainTab tab,
            string descriptionKey,
            string fallbackDescription,
            PassiveButton selectedButton)
        {
            menu.ChangeTab(-1, false);

            HideCustomTab();

            SettingsTab.gameObject.SetActive(true);

            menu.MenuDescriptionText.text =
                GetTranslatedOrFallback(
                    descriptionKey,
                    fallbackDescription
                );

            CurrentMainTab = tab;

            SelectMainButton(selectedButton);

            ApplyCurrentTabVisibility();

            GameOptionsMenuUpdatePatch.RefreshLayout(
                SettingsTab
            );
        }


        // =========================================================
        // HIDE TAB
        // =========================================================

        private static void HideCustomTab()
        {
            if (SettingsTab != null)
                SettingsTab.gameObject.SetActive(false);
        }


        // =========================================================
        // SELECT BUTTON
        // =========================================================

        private static void SelectMainButton(
            PassiveButton selectedButton)
        {
            if (GeneralButton)
                GeneralButton.SelectButton(false);

            if (GameModesButton)
                GameModesButton.SelectButton(false);

            if (ModerationButton)
                ModerationButton.SelectButton(false);

            if (RolesButton)
                RolesButton.SelectButton(false);

            if (TasksButton)
                TasksButton.SelectButton(false);

            if (SabotagesButton)
                SabotagesButton.SelectButton(false);


            if (selectedButton)
                selectedButton.SelectButton(true);
        }


        // =========================================================
        // CREATE OPTION ROWS
        // =========================================================

        private static void CreateAllOptionRows(
            GameSettingMenu __instance)
        {
            var template =
                __instance.GameSettingsTab.stringOptionOrigin;

            var scOptions =
                new Il2CppSystem.Collections.Generic.List<
                    OptionBehaviour
                >();


            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption =
                        Object.Instantiate(
                            template,
                            SettingsTab.settingsContainer
                        );

                    scOptions.Add(stringOption);

                    stringOption.SetClickMask(
                        __instance.GameSettingsButton.ClickMask
                    );

                    stringOption.SetUpFromData(
                        stringOption.data,
                        GameOptionsMenu.MASK_LAYER
                    );

                    stringOption.OnValueChanged =
                        new Action<OptionBehaviour>((o) => { });

                    stringOption.Value =
                        stringOption.oldValue =
                            option.CurrentValue;

                    stringOption.ValueText.text =
                        option.GetString();

                    stringOption.name =
                        option.Name;

                    stringOption.TitleText.text =
                        GetString(option.Name);


                    // =============================================
                    // CHILD INDENTATION
                    // =============================================

                    float indent = 0f;

                    var parent = option.Parent;

                    while (parent != null)
                    {
                        indent += 0.15f;
                        parent = parent.Parent;
                    }


                    stringOption.LabelBackground.size +=
                        new Vector2(
                            2f - indent * 2f,
                            0f
                        );

                    stringOption.LabelBackground
                        .transform
                        .localPosition +=
                        new Vector3(
                            -1f + indent,
                            0f,
                            0f
                        );


                    stringOption.TitleText
                        .rectTransform
                        .sizeDelta +=
                        new Vector2(
                            2f - indent * 2f,
                            0f
                        );

                    stringOption.TitleText
                        .transform
                        .localPosition +=
                        new Vector3(
                            -1f + indent,
                            0f,
                            0f
                        );


                    option.OptionBehaviour =
                        stringOption;
                }

                option.OptionBehaviour
                    .gameObject
                    .SetActive(false);
            }


            SettingsTab.Children = scOptions;
        }


        // =========================================================
        // CREATE CATEGORY HEADERS
        // =========================================================

        private static void CreateAllCategoryHeaders(
            GameSettingMenu __instance)
        {
            CategoryHeaders.Clear();


            foreach (OptionCategory category in CategoryOrder)
            {
                // Non creare header per categorie
                // che non hanno nessuna opzione.
                if (OptionItem.GetOptions(category).Count == 0)
                    continue;


                var header =
                    Object.Instantiate(
                        __instance
                            .GameSettingsTab
                            .categoryHeaderOrigin,
                        SettingsTab.settingsContainer
                    );


                header.Title.text =
                    TranslateHeader(category);


                header.Background.material.SetInt(
                    PlayerMaterial.MaskLayer,
                    GameOptionsMenu.MASK_LAYER
                );


                header.transform.localScale =
                    Vector3.one *
                    GameOptionsMenu.HEADER_SCALE;


                header.gameObject.SetActive(false);

                CategoryHeaders[category] =
                    header;
            }
        }


        // =========================================================
        // CATEGORY ORDER
        // =========================================================

        public static IEnumerable<OptionCategory>
            GetCategoryOrder()
        {
            return CategoryOrder;
        }


        // =========================================================
        // HEADER TRANSLATION
        // =========================================================

        private static string TranslateHeader(
            OptionCategory category)
        {
            string key =
                category.GetHeaderKey();

            return GetTranslatedOrFallback(
                key,
                category.ToString()
            );
        }


        // =========================================================
        // TRANSLATION FALLBACK
        // =========================================================

        private static string GetTranslatedOrFallback(
            string key,
            string fallback)
        {
            string translated;

            try
            {
                translated = GetString(key);
            }
            catch
            {
                return fallback;
            }


            if (string.IsNullOrWhiteSpace(translated))
                return fallback;


            if (
                translated.StartsWith(
                    "<INVALID:",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return fallback;
            }


            return translated;
        }


        // =========================================================
        // OPTION VISIBILITY
        // =========================================================

        public static bool ShouldShowOption(
            OptionItem option)
        {
            if (option == null)
                return false;


            if (!option.IsVisibleByParent())
                return false;


            if (option.MainTab != CurrentMainTab)
                return false;


            // =====================================================
            // HIDE & SEEK
            // =====================================================

            if (option.Category == OptionCategory.Seeker)
            {
                return IsHideAndSeekSafe();
            }


            // GameMode selector non serve nel
            // vanilla Hide & Seek
            if (
                option.Category == OptionCategory.GameMode &&
                IsHideAndSeekSafe()
            )
            {
                return false;
            }


            // =====================================================
            // SNS
            // =====================================================

            if (
                option.Category == OptionCategory.SNS &&
                !IsSnSMode()
            )
            {
                return false;
            }


            // =====================================================
            // FFA
            // =====================================================

            if (
                option.Category == OptionCategory.FFA &&
                !IsFFAMode()
            )
            {
                return false;
            }


            return true;
        }


        // =========================================================
        // APPLY VISIBILITY
        // =========================================================

        public static void ApplyCurrentTabVisibility()
        {
            foreach (
                var header
                in CategoryHeaders.Values
            )
            {
                if (header != null)
                    header.gameObject.SetActive(false);
            }


            foreach (
                var option
                in OptionItem.AllOptions
            )
            {
                if (
                    option == null ||
                    option.OptionBehaviour == null
                )
                {
                    continue;
                }


                option.OptionBehaviour
                    .gameObject
                    .SetActive(
                        ShouldShowOption(option)
                    );


                option.Refresh();
            }
        }


        // =========================================================
        // GET HEADERS
        // =========================================================

        public static IReadOnlyDictionary<
            OptionCategory,
            CategoryHeaderMasked
        > GetHeaders()
        {
            return CategoryHeaders;
        }


        // =========================================================
        // SNS CHECK
        // =========================================================

        private static bool IsSnSMode()
        {
            try
            {
                return
                    Options.GameMode != null &&
                    (GameModeType)
                        Options.GameMode.GetValue()
                    == GameModeType.SnS;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // FFA CHECK
        // =========================================================

        private static bool IsFFAMode()
        {
            try
            {
                return
                    Options.GameMode != null &&
                    (GameModeType)
                        Options.GameMode.GetValue()
                    == GameModeType.FFA;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // HIDE & SEEK CHECK
        // =========================================================

        private static bool IsHideAndSeekSafe()
        {
            try
            {
                return
                    GameManager.Instance != null &&
                    GameManager.Instance.IsHideAndSeek();
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // BUTTON COLOR
        // =========================================================

        private static void SetButtonColor(
            PassiveButton button,
            Color color)
        {
            var activeSprite =
                button.activeSprites
                    .GetComponent<SpriteRenderer>();

            var selectedSprite =
                button.selectedSprites
                    .GetComponent<SpriteRenderer>();


            activeSprite.color =
                selectedSprite.color =
                    color;
        }


        // =========================================================
        // VANILLA TAB CHANGE
        // =========================================================

        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab))]
        [HarmonyPrefix]
        public static void ChangeTabPrefix(
            bool previewOnly)
        {
            if (previewOnly)
                return;


            HideCustomTab();

            SelectMainButton(null);


            foreach (
                var header
                in CategoryHeaders.Values
            )
            {
                if (header != null)
                    header.gameObject.SetActive(false);
            }
        }
    }


    // =============================================================
    // LAYOUT UPDATE
    // =============================================================

    [HarmonyPatch(
        typeof(GameOptionsMenu),
        nameof(GameOptionsMenu.Update)
    )]
    public static class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;


        public static void Postfix(
            GameOptionsMenu __instance)
        {
            if (
                __instance.name !=
                GameSettingMenuPatch.MenuName
            )
            {
                return;
            }


            _timer += Time.deltaTime;


            if (_timer < 0.1f)
                return;


            _timer = 0f;


            RefreshLayout(__instance);
        }


        // =========================================================
        // REFRESH LAYOUT
        // =========================================================

        public static void RefreshLayout(
            GameOptionsMenu __instance)
        {
            float offset = 2.6f;


            // Nascondi tutti gli header
            foreach (
                var header
                in GameSettingMenuPatch
                    .GetHeaders()
                    .Values
            )
            {
                if (header != null)
                    header.gameObject.SetActive(false);
            }


            // Nascondi/mostra opzioni
            foreach (
                var option
                in OptionItem.AllOptions
            )
            {
                if (
                    option == null ||
                    option.OptionBehaviour == null
                )
                {
                    continue;
                }


                option.OptionBehaviour
                    .gameObject
                    .SetActive(
                        GameSettingMenuPatch
                            .ShouldShowOption(option)
                    );
            }


            // =====================================================
            // CATEGORY ORDER
            // =====================================================

            foreach (
                OptionCategory category
                in GameSettingMenuPatch.GetCategoryOrder()
            )
            {
                if (
                    !GameSettingMenuPatch
                        .GetHeaders()
                        .TryGetValue(
                            category,
                            out var header
                        )
                    ||
                    header == null
                )
                {
                    continue;
                }


                var visibleOptions =
                    OptionItem
                        .GetOptions(category)
                        .Where(
                            option =>
                                option.OptionBehaviour != null &&
                                GameSettingMenuPatch
                                    .ShouldShowOption(option)
                        )
                        .ToList();


                if (visibleOptions.Count == 0)
                    continue;


                // =================================================
                // HEADER
                // =================================================

                header.gameObject.SetActive(true);

                offset -=
                    GameOptionsMenu.HEADER_HEIGHT;

                header.transform.localPosition =
                    new Vector3(
                        GameOptionsMenu.HEADER_X,
                        offset,
                        -2f
                    );


                // =================================================
                // OPTIONS
                // =================================================

                foreach (
                    var option
                    in visibleOptions
                )
                {
                    option.OptionBehaviour
                        .gameObject
                        .SetActive(true);


                    offset -=
                        GameOptionsMenu.SPACING_Y;


                    option.OptionBehaviour
                        .transform
                        .localPosition =
                        new Vector3(
                            GameOptionsMenu.START_POS_X,
                            offset,
                            -2f
                        );


                    option.OptionBehaviour
                        .TitleText
                        .color =
                        option.NameColor;
                }
            }


            // =====================================================
            // SCROLL SIZE
            // =====================================================

            __instance.scrollBar
                .ContentYBounds
                .max =
                Math.Max(
                    0f,
                    (-offset) - 1.5f
                );
        }
    }


    // =============================================================
    // STRING OPTION FIX
    // =============================================================

    [HarmonyPatch(typeof(StringOption))]
    public static class StringOptionFixPatch
    {
        // =========================================================
        // INITIALIZE
        // =========================================================

        [HarmonyPatch(nameof(StringOption.Initialize))]
        [HarmonyPrefix]
        public static bool InitializePrefix(
            StringOption __instance)
        {
            return __instance.data != null;
        }


        // =========================================================
        // INCREASE
        // =========================================================

        [HarmonyPatch(nameof(StringOption.Increase))]
        [HarmonyPrefix]
        public static bool IncreasePrefix(
            StringOption __instance)
        {
            OptionItem option =
                OptionItem.AllOptions.FirstOrDefault(
                    opt =>
                        opt.OptionBehaviour ==
                        __instance
                );


            if (option == null)
                return true;


            int amount =
                Input.GetKey(KeyCode.LeftShift)
                    ? 5
                    : 1;


            option.SetValue(
                option.CurrentValue + amount
            );


            __instance.ValueText.text =
                option.GetString();


            return false;
        }


        // =========================================================
        // DECREASE
        // =========================================================

        [HarmonyPatch(nameof(StringOption.Decrease))]
        [HarmonyPrefix]
        public static bool DecreasePrefix(
            StringOption __instance)
        {
            OptionItem option =
                OptionItem.AllOptions.FirstOrDefault(
                    opt =>
                        opt.OptionBehaviour ==
                        __instance
                );


            if (option == null)
                return true;


            int amount =
                Input.GetKey(KeyCode.LeftShift)
                    ? 5
                    : 1;


            option.SetValue(
                option.CurrentValue - amount
            );


            __instance.ValueText.text =
                option.GetString();


            return false;
        }
    }
}
