//credits and licenses in the resources folder
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BanMod
{
    public static class BanModPopup
    {
        private static GameObject activeUpdatePopup;

        public static bool IsUpdatePopupOpen
        {
            get
            {
                try { return activeUpdatePopup != null; }
                catch { return false; }
            }
        }

        public static GameObject CreateDisableModPopup(string title, string content)
        {
            GameObject popup = new GameObject("BanMod_Popup");
            popup.transform.position = new Vector3(0f, 0f, -10f);

            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            popup.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(popup.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(500, 300);
            bgRect.anchoredPosition = Vector2.zero;

            var bgImage = bg.AddComponent<Image>();
            ApplyModernImage(bgImage, BanModUiStyles.WindowColor, true);

            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(bg.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 28;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = BanModUiStyles.AccentHoverColor;

            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(460, 50);
            titleRect.anchoredPosition = new Vector2(0, 110);

            var contentGO = new GameObject("ContentText");
            contentGO.transform.SetParent(bg.transform, false);
            var contentText = contentGO.AddComponent<TextMeshProUGUI>();
            contentText.text = content;
            contentText.fontSize = 20;
            contentText.color = Color.white;
            contentText.alignment = TextAlignmentOptions.TopLeft;

            var contentRect = contentText.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(460, 150);
            contentRect.anchoredPosition = new Vector2(0, 20);

            var buttonsGO = new GameObject("ButtonsContainer");
            buttonsGO.transform.SetParent(bg.transform, false);
            var buttonsRect = buttonsGO.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(460, 50);
            buttonsRect.anchoredPosition = new Vector2(0, -110);

            var disableBtnGO = new GameObject("DisableModButton");
            disableBtnGO.transform.SetParent(buttonsGO.transform, false);
            var disableRect = disableBtnGO.AddComponent<RectTransform>();
            disableRect.sizeDelta = new Vector2(200, 50);
            disableRect.anchoredPosition = new Vector2(-130, 0);

            var disableImage = disableBtnGO.AddComponent<Image>();
            ApplyModernImage(disableImage, BanModUiStyles.DangerColor, false);

            var disableButton = disableBtnGO.AddComponent<Button>();
            ApplyButtonTransition(disableButton, disableImage);
            System.Action value = () =>
            {
                BanMod.DisableMod();
                GameObject.Destroy(popup);
            };
            disableButton.onClick.AddListener(value);

            var disableTextGO = new GameObject("Text");
            disableTextGO.transform.SetParent(disableBtnGO.transform, false);
            var disableText = disableTextGO.AddComponent<TextMeshProUGUI>();
            disableText.text = Translator.GetString("disableModButton");
            disableText.fontSize = 22;
            disableText.alignment = TextAlignmentOptions.Center;
            disableText.color = Color.white;

            var disableTextRect = disableText.GetComponent<RectTransform>();
            disableTextRect.anchorMin = Vector2.zero;
            disableTextRect.anchorMax = Vector2.one;
            disableTextRect.offsetMin = Vector2.zero;
            disableTextRect.offsetMax = Vector2.zero;

            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(buttonsGO.transform, false);
            var closeRect = closeBtnGO.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(200, 50);
            closeRect.anchoredPosition = new Vector2(130, 0);

            var closeImage = closeBtnGO.AddComponent<Image>();
            ApplyModernImage(closeImage, BanModUiStyles.ButtonColor, false);

            var closeButton = closeBtnGO.AddComponent<Button>();
            ApplyButtonTransition(closeButton, closeImage);
            System.Action value1 = () =>
            {
                GameObject.Destroy(popup);
            };
            closeButton.onClick.AddListener(value1);

            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = Translator.GetString("closeButton");
            closeText.fontSize = 22;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;

            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            return popup;
        }

        public static GameObject CreateUpdatePopup(
            string title,
            string content,
            bool mandatory,
            Action onUpdate,
            Action onSkip = null)
        {
            try
            {
                if (activeUpdatePopup != null)
                    GameObject.Destroy(activeUpdatePopup);
            }
            catch { }

            string language = GetAmongUsLanguageKey();

            GameObject popup = new GameObject("BanMod_UpdatePopup");
            activeUpdatePopup = popup;
            UnityEngine.Object.DontDestroyOnLoad(popup);

            popup.transform.position = Vector3.zero;

            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50000;

            var scaler = popup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            popup.AddComponent<GraphicRaycaster>();

            var blocker = new GameObject("InputBlocker");
            blocker.transform.SetParent(popup.transform, false);

            var blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.pivot = new Vector2(0.5f, 0.5f);
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blockerRect.anchoredPosition = Vector2.zero;
            blockerRect.sizeDelta = Vector2.zero;

            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, mandatory ? 0.82f : 0.68f);
            blockerImage.raycastTarget = true;

            var blockerGroup = blocker.AddComponent<CanvasGroup>();
            blockerGroup.alpha = 1f;
            blockerGroup.interactable = true;
            blockerGroup.blocksRaycasts = true;
            blockerGroup.ignoreParentGroups = false;

            var blockerButton = blocker.AddComponent<Button>();
            blockerButton.targetGraphic = blockerImage;
            blockerButton.transition = Selectable.Transition.None;
            System.Action blockerAction = () => { };
            blockerButton.onClick.AddListener(blockerAction);

            var panel = new GameObject("UpdatePanel");
            panel.transform.SetParent(blocker.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 650f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            ApplyModernImage(panelImage, BanModUiStyles.WindowColor, true);
            panelImage.raycastTarget = true;

            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(panel.transform, false);

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = mandatory
                ? UpdateText(language, "mandatory_title")
                : UpdateText(language, "available_title");
            titleText.fontSize = 31f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = mandatory
                ? BanModUiStyles.DangerColor
                : BanModUiStyles.AccentHoverColor;
            titleText.raycastTarget = false;

            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(30f, -72f);
            titleRect.offsetMax = new Vector2(-30f, -18f);

            var releaseGO = new GameObject("ReleaseTitleText");
            releaseGO.transform.SetParent(panel.transform, false);

            var releaseText = releaseGO.AddComponent<TextMeshProUGUI>();
            releaseText.text = string.IsNullOrWhiteSpace(title) ? "BanMod" : title.Trim();
            releaseText.fontSize = 21f;
            releaseText.fontStyle = FontStyles.Bold;
            releaseText.alignment = TextAlignmentOptions.Center;
            releaseText.color = Color.white;
            releaseText.raycastTarget = false;

            var releaseRect = releaseText.rectTransform;
            releaseRect.anchorMin = new Vector2(0f, 1f);
            releaseRect.anchorMax = new Vector2(1f, 1f);
            releaseRect.pivot = new Vector2(0.5f, 1f);
            releaseRect.offsetMin = new Vector2(30f, -105f);
            releaseRect.offsetMax = new Vector2(-30f, -73f);

            var modeGO = new GameObject("ModeText");
            modeGO.transform.SetParent(panel.transform, false);

            var modeText = modeGO.AddComponent<TextMeshProUGUI>();
            modeText.text = mandatory
                ? UpdateText(language, "mandatory_mode")
                : UpdateText(language, "optional_mode");
            modeText.fontSize = 16.5f;
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.color = new Color(0.78f, 0.80f, 0.86f, 1f);
            modeText.raycastTarget = false;

            var modeRect = modeText.rectTransform;
            modeRect.anchorMin = new Vector2(0f, 1f);
            modeRect.anchorMax = new Vector2(1f, 1f);
            modeRect.pivot = new Vector2(0.5f, 1f);
            modeRect.offsetMin = new Vector2(30f, -145f);
            modeRect.offsetMax = new Vector2(-30f, -108f);

            var notesHeaderGO = new GameObject("ReleaseNotesHeader");
            notesHeaderGO.transform.SetParent(panel.transform, false);

            var notesHeader = notesHeaderGO.AddComponent<TextMeshProUGUI>();
            notesHeader.text = UpdateText(language, "release_notes");
            notesHeader.fontSize = 18f;
            notesHeader.fontStyle = FontStyles.Bold;
            notesHeader.alignment = TextAlignmentOptions.Left;
            notesHeader.color = BanModUiStyles.AccentHoverColor;
            notesHeader.raycastTarget = false;

            var notesHeaderRect = notesHeader.rectTransform;
            notesHeaderRect.anchorMin = new Vector2(0f, 1f);
            notesHeaderRect.anchorMax = new Vector2(1f, 1f);
            notesHeaderRect.pivot = new Vector2(0.5f, 1f);
            notesHeaderRect.offsetMin = new Vector2(45f, -180f);
            notesHeaderRect.offsetMax = new Vector2(-45f, -148f);

            var scrollGO = new GameObject("ReleaseNotesScroll");
            scrollGO.transform.SetParent(panel.transform, false);

            var scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(38f, 105f);
            scrollRectTransform.offsetMax = new Vector2(-38f, -184f);

            var scrollBg = scrollGO.AddComponent<Image>();
            ApplyModernImage(scrollBg, new Color(0f, 0f, 0f, 0.26f), false);
            scrollBg.raycastTarget = true;

            scrollGO.AddComponent<RectMask2D>();

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.viewport = scrollRectTransform;

            var bodyGO = new GameObject("ReleaseNotesText");
            bodyGO.transform.SetParent(scrollGO.transform, false);

            var bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyText.text = string.IsNullOrWhiteSpace(content)
                ? UpdateText(language, "no_details")
                : content.Trim();
            bodyText.fontSize = 20f;
            bodyText.color = Color.white;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableWordWrapping = true;
            bodyText.raycastTarget = false;

            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -14f);
            bodyRect.sizeDelta = new Vector2(-30f, 1f);

            var bodyFitter = bodyGO.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = bodyRect;
            scroll.verticalNormalizedPosition = 1f;

            var buttonsGO = new GameObject("ButtonsContainer");
            buttonsGO.transform.SetParent(panel.transform, false);

            var buttonsRect = buttonsGO.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0f, 0f);
            buttonsRect.anchorMax = new Vector2(1f, 0f);
            buttonsRect.pivot = new Vector2(0.5f, 0f);
            buttonsRect.offsetMin = new Vector2(30f, 25f);
            buttonsRect.offsetMax = new Vector2(-30f, 91f);

            float updateX = mandatory ? 0f : -145f;

            var updateBtnGO = new GameObject("UpdateButton");
            updateBtnGO.transform.SetParent(buttonsGO.transform, false);

            var updateRect = updateBtnGO.AddComponent<RectTransform>();
            updateRect.anchorMin = new Vector2(0.5f, 0.5f);
            updateRect.anchorMax = new Vector2(0.5f, 0.5f);
            updateRect.sizeDelta = new Vector2(255f, 56f);
            updateRect.anchoredPosition = new Vector2(updateX, 0f);

            var updateImage = updateBtnGO.AddComponent<Image>();
            ApplyModernImage(updateImage, BanModUiStyles.AccentColor, false);

            var updateButton = updateBtnGO.AddComponent<Button>();
            ApplyButtonTransition(updateButton, updateImage);

            var updateTextGO = new GameObject("Text");
            updateTextGO.transform.SetParent(updateBtnGO.transform, false);

            var updateText = updateTextGO.AddComponent<TextMeshProUGUI>();
            updateText.text = UpdateText(language, "update_button");
            updateText.fontSize = 23f;
            updateText.fontStyle = FontStyles.Bold;
            updateText.alignment = TextAlignmentOptions.Center;
            updateText.color = Color.white;
            updateText.raycastTarget = false;

            var updateTextRect = updateText.rectTransform;
            updateTextRect.anchorMin = Vector2.zero;
            updateTextRect.anchorMax = Vector2.one;
            updateTextRect.offsetMin = Vector2.zero;
            updateTextRect.offsetMax = Vector2.zero;

            Button skipButton = null;
            bool updateClickAccepted = false;

            Action updateAction = () =>
            {
                if (updateClickAccepted)
                    return;

                updateClickAccepted = true;
                updateButton.interactable = false;
                if (skipButton != null)
                    skipButton.interactable = false;

                updateText.text = UpdateText(language, "updating_button");

                try
                {
                    onUpdate?.Invoke();
                }
                catch (Exception ex)
                {
                    updateClickAccepted = false;
                    updateButton.interactable = true;
                    if (skipButton != null)
                        skipButton.interactable = true;
                    updateText.text = UpdateText(language, "update_button");

                    try
                    {
                        Debug.LogError("[BANMOD UPDATE] Unable to start update: " + ex.Message);
                    }
                    catch { }
                }
            };
            updateButton.onClick.AddListener(updateAction);

            if (!mandatory)
            {
                var skipBtnGO = new GameObject("SkipButton");
                skipBtnGO.transform.SetParent(buttonsGO.transform, false);

                var skipRect = skipBtnGO.AddComponent<RectTransform>();
                skipRect.anchorMin = new Vector2(0.5f, 0.5f);
                skipRect.anchorMax = new Vector2(0.5f, 0.5f);
                skipRect.sizeDelta = new Vector2(255f, 56f);
                skipRect.anchoredPosition = new Vector2(145f, 0f);

                var skipImage = skipBtnGO.AddComponent<Image>();
                ApplyModernImage(skipImage, BanModUiStyles.ButtonColor, false);

                skipButton = skipBtnGO.AddComponent<Button>();
                ApplyButtonTransition(skipButton, skipImage);

                Action skipAction = () =>
                {
                    try
                    {
                        onSkip?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Debug.LogError("[BANMOD UPDATE] Skip callback failed: " + ex.Message);
                        }
                        catch { }
                    }

                    try
                    {
                        GameObject.Destroy(popup);
                    }
                    catch { }

                    if (activeUpdatePopup == popup)
                        activeUpdatePopup = null;
                };
                skipButton.onClick.AddListener(skipAction);

                var skipTextGO = new GameObject("Text");
                skipTextGO.transform.SetParent(skipBtnGO.transform, false);

                var skipText = skipTextGO.AddComponent<TextMeshProUGUI>();
                skipText.text = UpdateText(language, "skip_button");
                skipText.fontSize = 22f;
                skipText.alignment = TextAlignmentOptions.Center;
                skipText.color = Color.white;
                skipText.raycastTarget = false;

                var skipTextRect = skipText.rectTransform;
                skipTextRect.anchorMin = Vector2.zero;
                skipTextRect.anchorMax = Vector2.one;
                skipTextRect.offsetMin = Vector2.zero;
                skipTextRect.offsetMax = Vector2.zero;
            }

            return popup;
        }

        private static string GetAmongUsLanguageKey()
        {
            try
            {
                if (TranslationController.Instance != null)
                {
                    string raw = TranslationController.Instance.currentLanguage.languageID.ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                        return NormalizeAmongUsLanguage(raw);
                }
            }
            catch (Exception ex)
            {
                try { Debug.LogWarning("[BANMOD UPDATE] Unable to read Among Us language: " + ex.Message); } catch { }
            }

            return "en";
        }

        private static string NormalizeAmongUsLanguage(string raw)
        {
            string value = (raw ?? "").Trim().ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "");

            if (value.Contains("ital")) return "it";
            if (value.Contains("german") || value.Contains("deutsch")) return "de";
            if (value.Contains("french") || value.Contains("franc")) return "fr";
            if (value.Contains("spanish") || value.Contains("latam") || value.Contains("espan")) return "es";
            if (value.Contains("portugu") || value.Contains("brazil")) return "pt";
            if (value.Contains("dutch") || value.Contains("neder")) return "nl";
            if (value.Contains("russian") || value.Contains("russ")) return "ru";
            if (value.Contains("korean") || value.Contains("korea")) return "ko";
            if (value.Contains("japanese") || value.Contains("japan")) return "ja";
            if (value.Contains("schinese") || value.Contains("simplified") || value.Contains("zhhans")) return "zh-hans";
            if (value.Contains("tchinese") || value.Contains("traditional") || value.Contains("zhhant")) return "zh-hant";
            if (value.Contains("filipino") || value.Contains("tagalog")) return "fil";
            if (value.Contains("irish") || value.Contains("gaeilge")) return "ga";
            if (value.Contains("polish") || value.Contains("polski")) return "pl";
            if (value.Contains("turkish") || value.Contains("turk")) return "tr";
            return "en";
        }

        private static string UpdateText(string language, string key)
        {
            switch (language)
            {
                case "it":
                    switch (key)
                    {
                        case "available_title": return "Aggiornamento disponibile";
                        case "mandatory_title": return "Aggiornamento obbligatorio";
                        case "optional_mode": return "Aggiornamento facoltativo";
                        case "mandatory_mode": return "BanMod resta bloccata finché l'aggiornamento non viene installato.";
                        case "release_notes": return "Modifiche della release";
                        case "no_details": return "Nessun dettaglio disponibile per questa release.";
                        case "update_button": return "Aggiorna";
                        case "updating_button": return "Aggiornamento...";
                        case "skip_button": return "Non aggiornare";
                    }
                    break;

                case "de":
                    switch (key)
                    {
                        case "available_title": return "Update verfügbar";
                        case "mandatory_title": return "Erforderliches Update";
                        case "optional_mode": return "Optionales Update";
                        case "mandatory_mode": return "BanMod bleibt gesperrt, bis das Update installiert wurde.";
                        case "release_notes": return "Änderungen dieser Version";
                        case "no_details": return "Für diese Version sind keine Details verfügbar.";
                        case "update_button": return "Aktualisieren";
                        case "updating_button": return "Wird aktualisiert...";
                        case "skip_button": return "Nicht aktualisieren";
                    }
                    break;

                case "fr":
                    switch (key)
                    {
                        case "available_title": return "Mise à jour disponible";
                        case "mandatory_title": return "Mise à jour obligatoire";
                        case "optional_mode": return "Mise à jour facultative";
                        case "mandatory_mode": return "BanMod reste bloqué jusqu'à l'installation de la mise à jour.";
                        case "release_notes": return "Modifications de la version";
                        case "no_details": return "Aucun détail n'est disponible pour cette version.";
                        case "update_button": return "Mettre à jour";
                        case "updating_button": return "Mise à jour...";
                        case "skip_button": return "Ne pas mettre à jour";
                    }
                    break;

                case "es":
                    switch (key)
                    {
                        case "available_title": return "Actualización disponible";
                        case "mandatory_title": return "Actualización obligatoria";
                        case "optional_mode": return "Actualización opcional";
                        case "mandatory_mode": return "BanMod permanecerá bloqueado hasta que se instale la actualización.";
                        case "release_notes": return "Cambios de la versión";
                        case "no_details": return "No hay detalles disponibles para esta versión.";
                        case "update_button": return "Actualizar";
                        case "updating_button": return "Actualizando...";
                        case "skip_button": return "No actualizar";
                    }
                    break;

                case "pt":
                    switch (key)
                    {
                        case "available_title": return "Atualização disponível";
                        case "mandatory_title": return "Atualização obrigatória";
                        case "optional_mode": return "Atualização opcional";
                        case "mandatory_mode": return "BanMod permanecerá bloqueado até que a atualização seja instalada.";
                        case "release_notes": return "Alterações da versão";
                        case "no_details": return "Nenhum detalhe está disponível para esta versão.";
                        case "update_button": return "Atualizar";
                        case "updating_button": return "Atualizando...";
                        case "skip_button": return "Não atualizar";
                    }
                    break;

                case "nl":
                    switch (key)
                    {
                        case "available_title": return "Update beschikbaar";
                        case "mandatory_title": return "Verplichte update";
                        case "optional_mode": return "Optionele update";
                        case "mandatory_mode": return "BanMod blijft geblokkeerd totdat de update is geïnstalleerd.";
                        case "release_notes": return "Wijzigingen in deze versie";
                        case "no_details": return "Er zijn geen details beschikbaar voor deze versie.";
                        case "update_button": return "Bijwerken";
                        case "updating_button": return "Bijwerken...";
                        case "skip_button": return "Niet bijwerken";
                    }
                    break;

                case "ru":
                    switch (key)
                    {
                        case "available_title": return "Доступно обновление";
                        case "mandatory_title": return "Обязательное обновление";
                        case "optional_mode": return "Необязательное обновление";
                        case "mandatory_mode": return "BanMod останется заблокированным, пока обновление не будет установлено.";
                        case "release_notes": return "Изменения версии";
                        case "no_details": return "Для этой версии нет подробностей.";
                        case "update_button": return "Обновить";
                        case "updating_button": return "Обновление...";
                        case "skip_button": return "Не обновлять";
                    }
                    break;

                case "ko":
                    switch (key)
                    {
                        case "available_title": return "업데이트 사용 가능";
                        case "mandatory_title": return "필수 업데이트";
                        case "optional_mode": return "선택적 업데이트";
                        case "mandatory_mode": return "업데이트가 설치될 때까지 BanMod 사용이 차단됩니다.";
                        case "release_notes": return "릴리스 변경 사항";
                        case "no_details": return "이 릴리스에 대한 세부 정보가 없습니다.";
                        case "update_button": return "업데이트";
                        case "updating_button": return "업데이트 중...";
                        case "skip_button": return "업데이트 안 함";
                    }
                    break;

                case "ja":
                    switch (key)
                    {
                        case "available_title": return "アップデートがあります";
                        case "mandatory_title": return "必須アップデート";
                        case "optional_mode": return "任意アップデート";
                        case "mandatory_mode": return "アップデートがインストールされるまで BanMod は使用できません。";
                        case "release_notes": return "リリースの変更内容";
                        case "no_details": return "このリリースの詳細はありません。";
                        case "update_button": return "アップデート";
                        case "updating_button": return "更新中...";
                        case "skip_button": return "更新しない";
                    }
                    break;

                case "zh-hans":
                    switch (key)
                    {
                        case "available_title": return "有可用更新";
                        case "mandatory_title": return "必须更新";
                        case "optional_mode": return "可选更新";
                        case "mandatory_mode": return "安装更新之前，BanMod 将保持锁定。";
                        case "release_notes": return "版本更新内容";
                        case "no_details": return "此版本没有可用的详细信息。";
                        case "update_button": return "更新";
                        case "updating_button": return "正在更新...";
                        case "skip_button": return "不更新";
                    }
                    break;

                case "zh-hant":
                    switch (key)
                    {
                        case "available_title": return "有可用更新";
                        case "mandatory_title": return "必須更新";
                        case "optional_mode": return "選擇性更新";
                        case "mandatory_mode": return "安裝更新之前，BanMod 將保持鎖定。";
                        case "release_notes": return "版本更新內容";
                        case "no_details": return "此版本沒有可用的詳細資訊。";
                        case "update_button": return "更新";
                        case "updating_button": return "正在更新...";
                        case "skip_button": return "不更新";
                    }
                    break;

                case "fil":
                    switch (key)
                    {
                        case "available_title": return "May available na update";
                        case "mandatory_title": return "Kailangang update";
                        case "optional_mode": return "Opsyonal na update";
                        case "mandatory_mode": return "Mananatiling naka-lock ang BanMod hanggang ma-install ang update.";
                        case "release_notes": return "Mga pagbabago sa release";
                        case "no_details": return "Walang detalye para sa release na ito.";
                        case "update_button": return "I-update";
                        case "updating_button": return "Nag-a-update...";
                        case "skip_button": return "Huwag i-update";
                    }
                    break;

                case "ga":
                    switch (key)
                    {
                        case "available_title": return "Nuashonrú ar fáil";
                        case "mandatory_title": return "Nuashonrú éigeantach";
                        case "optional_mode": return "Nuashonrú roghnach";
                        case "mandatory_mode": return "Fanfaidh BanMod faoi ghlas go dtí go mbeidh an nuashonrú suiteáilte.";
                        case "release_notes": return "Athruithe sa leagan";
                        case "no_details": return "Níl sonraí ar fáil don leagan seo.";
                        case "update_button": return "Nuashonraigh";
                        case "updating_button": return "Á nuashonrú...";
                        case "skip_button": return "Ná nuashonraigh";
                    }
                    break;

                case "pl":
                    switch (key)
                    {
                        case "available_title": return "Dostępna aktualizacja";
                        case "mandatory_title": return "Wymagana aktualizacja";
                        case "optional_mode": return "Opcjonalna aktualizacja";
                        case "mandatory_mode": return "BanMod pozostanie zablokowany do czasu zainstalowania aktualizacji.";
                        case "release_notes": return "Zmiany w wydaniu";
                        case "no_details": return "Brak szczegółów dla tego wydania.";
                        case "update_button": return "Aktualizuj";
                        case "updating_button": return "Aktualizowanie...";
                        case "skip_button": return "Nie aktualizuj";
                    }
                    break;

                case "tr":
                    switch (key)
                    {
                        case "available_title": return "Güncelleme mevcut";
                        case "mandatory_title": return "Zorunlu güncelleme";
                        case "optional_mode": return "İsteğe bağlı güncelleme";
                        case "mandatory_mode": return "Güncelleme yüklenene kadar BanMod kilitli kalacaktır.";
                        case "release_notes": return "Sürüm değişiklikleri";
                        case "no_details": return "Bu sürüm için ayrıntı bulunmuyor.";
                        case "update_button": return "Güncelle";
                        case "updating_button": return "Güncelleniyor...";
                        case "skip_button": return "Güncelleme";
                    }
                    break;
            }

            switch (key)
            {
                case "available_title": return "Update available";
                case "mandatory_title": return "Mandatory update";
                case "optional_mode": return "Optional update";
                case "mandatory_mode": return "BanMod remains locked until the update is installed.";
                case "release_notes": return "Release changes";
                case "no_details": return "No details are available for this release.";
                case "update_button": return "Update";
                case "updating_button": return "Updating...";
                case "skip_button": return "Do not update";
                default: return "";
            }
        }
        public static GameObject CreatePolicyPrivacyPopup()
        {
            string language = GetAmongUsLanguageKey();

            GameObject popup = new GameObject("BanMod_PolicyPrivacyPopup");
            popup.transform.position = Vector3.zero;

            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50000;

            var scaler = popup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            popup.AddComponent<GraphicRaycaster>();


            // =========================
            // BACKGROUND / INPUT BLOCKER
            // =========================

            var blocker = new GameObject("InputBlocker");
            blocker.transform.SetParent(popup.transform, false);

            var blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.72f);
            blockerImage.raycastTarget = true;


            // =========================
            // PANEL
            // =========================

            var panel = new GameObject("PolicyPrivacyPanel");
            panel.transform.SetParent(blocker.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 720f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();
            ApplyModernImage(panelImage, BanModUiStyles.WindowColor, true);
            panelImage.raycastTarget = true;


            // =========================
            // TITLE
            // =========================

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = PolicyUiText(language, "title");
            titleText.fontSize = 32f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = BanModUiStyles.AccentHoverColor;
            titleText.raycastTarget = false;

            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(30f, -75f);
            titleRect.offsetMax = new Vector2(-30f, -20f);


            // =========================
            // SCROLL AREA
            // =========================

            var scrollGO = new GameObject("PolicyScroll");
            scrollGO.transform.SetParent(panel.transform, false);

            var scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(40f, 105f);
            scrollRectTransform.offsetMax = new Vector2(-40f, -90f);

            var scrollBg = scrollGO.AddComponent<Image>();
            ApplyModernImage(
                scrollBg,
                new Color(0f, 0f, 0f, 0.25f),
                false
            );

            scrollBg.raycastTarget = true;

            scrollGO.AddComponent<RectMask2D>();

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            scroll.viewport = scrollRectTransform;


            // =========================
            // POLICY TEXT
            // =========================

            var bodyGO = new GameObject("PolicyText");
            bodyGO.transform.SetParent(scrollGO.transform, false);

            var bodyText = bodyGO.AddComponent<TextMeshProUGUI>();

            bodyText.text = GetPolicyPrivacyText(language);

            bodyText.fontSize = 19f;
            bodyText.color = Color.white;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableWordWrapping = true;
            bodyText.richText = true;
            bodyText.raycastTarget = false;

            var bodyRect = bodyText.rectTransform;

            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);

            bodyRect.anchoredPosition = new Vector2(0f, -15f);
            bodyRect.sizeDelta = new Vector2(-40f, 1f);

            var fitter = bodyGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = bodyRect;

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;


            // =========================
            // CLOSE BUTTON
            // =========================

            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(panel.transform, false);

            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(240f, 58f);
            closeRect.anchoredPosition = new Vector2(0f, 25f);

            var closeImage = closeGO.AddComponent<Image>();
            ApplyModernImage(
                closeImage,
                BanModUiStyles.ButtonColor,
                false
            );

            var closeButton = closeGO.AddComponent<Button>();
            ApplyButtonTransition(closeButton, closeImage);

            Action closeAction = () =>
            {
                GameObject.Destroy(popup);
            };

            closeButton.onClick.AddListener(closeAction);


            // Testo pulsante

            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeGO.transform, false);

            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = PolicyUiText(language, "close");
            closeText.fontSize = 22f;
            closeText.fontStyle = FontStyles.Bold;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;
            closeText.raycastTarget = false;

            var closeTextRect = closeText.rectTransform;
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            return popup;
        }
        public static GameObject CreateContactsPopup()
        {
            GameObject popup = new GameObject("BanMod_ContactsPopup");
            popup.transform.position = Vector3.zero;

            // =========================
            // CANVAS
            // =========================

            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50000;

            var scaler = popup.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            popup.AddComponent<GraphicRaycaster>();


            // =========================
            // DARK BACKGROUND
            // =========================

            var blocker = new GameObject("InputBlocker");
            blocker.transform.SetParent(popup.transform, false);

            var blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.68f);
            blockerImage.raycastTarget = true;


            // =========================
            // PANEL
            // =========================

            var panel = new GameObject("ContactsPanel");
            panel.transform.SetParent(blocker.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            panelRect.sizeDelta = new Vector2(560f, 600f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.AddComponent<Image>();

            ApplyModernImage(
                panelImage,
                BanModUiStyles.WindowColor,
                true
            );

            panelImage.raycastTarget = true;


            // =========================
            // TITLE
            // =========================

            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();

            titleText.text = "Contacts";
            titleText.fontSize = 32f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = BanModUiStyles.AccentHoverColor;
            titleText.raycastTarget = false;

            var titleRect = titleText.rectTransform;

            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);

            titleRect.offsetMin = new Vector2(20f, -75f);
            titleRect.offsetMax = new Vector2(-20f, -20f);


            // =========================
            // SUBTITLE
            // =========================

            var subtitleGO = new GameObject("Subtitle");
            subtitleGO.transform.SetParent(panel.transform, false);

            var subtitle = subtitleGO.AddComponent<TextMeshProUGUI>();

            subtitle.text = "Choose a contact or service";
            subtitle.fontSize = 17f;
            subtitle.color = new Color(0.75f, 0.78f, 0.84f, 1f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.raycastTarget = false;

            var subtitleRect = subtitle.rectTransform;

            subtitleRect.anchorMin = new Vector2(0f, 1f);
            subtitleRect.anchorMax = new Vector2(1f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);

            subtitleRect.offsetMin = new Vector2(20f, -108f);
            subtitleRect.offsetMax = new Vector2(-20f, -75f);


            // =========================
            // CONTACT BUTTONS
            // =========================

            CreateContactButton(
                panel.transform,
                "<color=#FF8A00>Website</color>",
                175f,
                BanMod.LobbysiteUrl
            );

            CreateContactButton(
                panel.transform,
                "<color=#F0F0F0>GitHub</color>",
                105f,
                BanMod.GitsiteUrl
            );

            CreateContactButton(
                panel.transform,
                "<color=#7289DA>Discord</color>",
                35f,
                BanMod.DiscordUrl
            );

            CreateContactButton(
                panel.transform,
                "<color=#5865F2>Discord Server</color>",
                -35f,
                BanMod.DiscordInviteUrl
            );

            CreateContactButton(
                panel.transform,
                "<color=#229ED9>Telegram</color>",
                -105f,
                "https://t.me/Giannibart"
            );

            CreateContactButton(
                panel.transform,
                "<color=#EA4335>Google Mail</color>",
                -175f,
                "Mailto:banmod.giannibart@gmail.com"
            );


            // =========================
            // CLOSE
            // =========================

            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(panel.transform, false);

            var closeRect = closeGO.AddComponent<RectTransform>();

            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);

            closeRect.sizeDelta = new Vector2(220f, 52f);
            closeRect.anchoredPosition = new Vector2(0f, 22f);

            var closeImage = closeGO.AddComponent<Image>();

            ApplyModernImage(
                closeImage,
                BanModUiStyles.ButtonColor,
                false
            );

            var closeButton = closeGO.AddComponent<Button>();

            ApplyButtonTransition(
                closeButton,
                closeImage
            );

            Action closeAction = () =>
            {
                GameObject.Destroy(popup);
            };

            closeButton.onClick.AddListener(closeAction);


            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeGO.transform, false);

            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();

            closeText.text = "Close";
            closeText.fontSize = 21f;
            closeText.fontStyle = FontStyles.Bold;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;
            closeText.raycastTarget = false;

            var closeTextRect = closeText.rectTransform;

            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            return popup;
        }
        private static void CreateContactButton(
    Transform parent,
    string label,
    float y,
    string url)
        {
            var buttonGO = new GameObject(
                "Contact_" + label.Replace(" ", "")
            );

            buttonGO.transform.SetParent(parent, false);

            var rect = buttonGO.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.sizeDelta = new Vector2(430f, 54f);
            rect.anchoredPosition = new Vector2(0f, y);


            // Background
            var image = buttonGO.AddComponent<Image>();

            ApplyModernImage(
                image,
                BanModUiStyles.ButtonColor,
                false
            );


            // Button
            var button = buttonGO.AddComponent<Button>();

            ApplyButtonTransition(
                button,
                image
            );


            // Click
            Action clickAction = () =>
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Application.OpenURL(url);
                }
            };

            button.onClick.AddListener(clickAction);


            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var text = textGO.AddComponent<TextMeshProUGUI>();

            text.text = label;
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            var textRect = text.rectTransform;

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        private static string PolicyUiText(string language, string key)
        {
            switch (language)
            {
                case "it":
                    return key == "title" ? "Policy & Privacy" : "Chiudi";
                case "de":
                    return key == "title" ? "Richtlinien & Datenschutz" : "Schließen";
                case "fr":
                    return key == "title" ? "Règles & Confidentialité" : "Fermer";
                case "es":
                    return key == "title" ? "Política y Privacidad" : "Cerrar";
                case "pt":
                    return key == "title" ? "Política e Privacidade" : "Fechar";
                case "nl":
                    return key == "title" ? "Beleid & Privacy" : "Sluiten";
                case "ru":
                    return key == "title" ? "Правила и конфиденциальность" : "Закрыть";
                case "ko":
                    return key == "title" ? "정책 및 개인정보 보호" : "닫기";
                case "ja":
                    return key == "title" ? "ポリシーとプライバシー" : "閉じる";
                case "zh-hans":
                    return key == "title" ? "政策与隐私" : "关闭";
                case "zh-hant":
                    return key == "title" ? "政策與隱私" : "關閉";
                case "fil":
                    return key == "title" ? "Patakaran at Privacy" : "Isara";
                case "ga":
                    return key == "title" ? "Polasaí & Príobháideachas" : "Dún";
                case "pl":
                    return key == "title" ? "Zasady i prywatność" : "Zamknij";
                case "tr":
                    return key == "title" ? "Politika ve Gizlilik" : "Kapat";
                default:
                    return key == "title" ? "Policy & Privacy" : "Close";
            }
        }

        private static string GetPolicyPrivacyText(string language)
        {
            switch (language)
            {
                case "it":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacy, Termini e Regole</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Uso facoltativo</color></b></size>\n" +
                        "<size=16>BanMod è facoltativa. Installandola e utilizzandola dichiari di aver avuto accesso alla Privacy Policy e accetti i Termini e le Regole BanMod. Se non li accetti, non usare BanMod o i servizi collegati.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Server corretto</color></b></size>\n" +
                        "<size=16>Le funzioni compatibili con gameplay non modificato possono essere usate nell'ambiente Originale/Vanilla. Le funzioni che modificano gameplay o altri giocatori richiedono l'ambiente/server Modded appropriato. L'uso improprio può comportare restrizioni o ban da parte di Innersloth o dei gestori dei server; BanMod non controlla tali decisioni.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Sicurezza e altre mod</color></b></size>\n" +
                        "<size=16>BanMod controlla compatibilità, integrità e possibili cheat. Una mod conosciuta e compatibile può essere consentita. Una mod sconosciuta o non verificata può rendere indisponibili i <color=#80D8FF>Servizi Extra</color>. Cheat menu conosciuti o modifiche vietate possono causare l'autodisattivazione di BanMod.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Servizi Extra</color></b></size>\n" +
                        "<size=16>Alcuni nomi interni possono ancora usare il termine legacy \"Premium\". Nel progetto attuale indica Servizi Extra facoltativi, non vantaggi di gameplay a pagamento. La disponibilità dipende da sicurezza, integrità, compatibilità e rispetto delle regole.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Dati e autenticazione</color></b></size>\n" +
                        "<size=16>BanMod può trattare FriendCode, nome giocatore, piattaforma, lingua, dati lobby, versione/build, hash tecnici di mod/plugin, contenuti Community/Chat e dati dei report. Le API private usano FriendCode + token BanMod; il server conserva una rappresentazione hash del token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Indirizzi IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>Gli IP degli utenti normali non vengono conservati come storico.</color> Possono essere elaborati temporaneamente per rete, rate limiting e sicurezza. Un IP bloccato per attività malevola, abusiva o seriamente sospetta può essere conservato permanentemente per applicare il blocco firewall.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Report, log e servizi esterni</color></b></size>\n" +
                        "<size=16>I report possono includere informazioni tecniche e log BepInEx necessari a supporto, debug, moderazione e sicurezza. Funzioni opzionali possono interagire con servizi esterni come Discord, GitHub, Telegram o il sito BanMod, soggetti alle rispettive policy.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Privacy e Termini completi: https://banmod.online/policies\nContatto: banmod.giannibart@gmail.com\nUltimo aggiornamento: 6 ottobre 2026</color></size>";

                case "de":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Datenschutz, Bedingungen & Regeln</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Freiwillige Nutzung</color></b></size>\n" +
                        "<size=16>BanMod ist optional. Durch Installation und Nutzung bestätigst du, dass dir die Datenschutzrichtlinie zugänglich gemacht wurde, und akzeptierst die BanMod-Bedingungen und -Regeln. Wenn du nicht zustimmst, nutze BanMod oder die verbundenen Dienste nicht.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Richtiger Server</color></b></size>\n" +
                        "<size=16>Funktionen ohne Gameplay-Änderungen können in der Original-/Vanilla-Umgebung verwendet werden. Funktionen, die Gameplay oder andere Spieler beeinflussen, benötigen die passende Modded-Umgebung bzw. den passenden Server. Missbrauch kann zu Einschränkungen oder Sperren durch Innersloth oder Serverbetreiber führen; BanMod kontrolliert diese Entscheidungen nicht.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Sicherheit und andere Mods</color></b></size>\n" +
                        "<size=16>BanMod prüft Kompatibilität, Integrität und mögliche Cheats. Bekannte kompatible Mods können erlaubt sein. Unbekannte oder nicht verifizierte Mods können <color=#80D8FF>Extra Services</color> deaktivieren. Bekannte Cheat-Menüs oder verbotene Änderungen können dazu führen, dass BanMod sich selbst deaktiviert.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Einige interne Bezeichnungen verwenden noch den alten Begriff \"Premium\". Im aktuellen Projekt bedeutet dies optionale Extra Services und keine bezahlten Gameplay-Vorteile. Die Verfügbarkeit hängt von Sicherheit, Integrität, Kompatibilität und Regeltreue ab.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Daten und Authentifizierung</color></b></size>\n" +
                        "<size=16>BanMod kann FriendCode, Spielername, Plattform, Sprache, Lobby-Daten, Version/Build, technische Mod-/Plugin-Hashes, Community-/Chat-Inhalte und Report-Daten verarbeiten. Private APIs verwenden FriendCode + BanMod-Token; der Server speichert eine Hash-Darstellung des Tokens.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP-Adressen</color></b></size>\n" +
                        "<size=16><color=#80FF80>Normale Benutzer-IP-Adressen werden nicht als Verlauf gespeichert.</color> Sie können vorübergehend für Netzwerk, Rate-Limiting und Sicherheit verarbeitet werden. Eine wegen bösartiger, missbräuchlicher oder stark verdächtiger Aktivität gesperrte IP kann dauerhaft zur Durchsetzung der Firewall-Sperre gespeichert werden.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Reports, Logs und externe Dienste</color></b></size>\n" +
                        "<size=16>Reports können technische Informationen und BepInEx-Logs enthalten, die für Support, Debugging, Moderation und Sicherheit benötigt werden. Optionale Funktionen können externe Dienste wie Discord, GitHub, Telegram oder die BanMod-Webseite nutzen; dort gelten deren eigene Richtlinien.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Vollständige Richtlinien: https://banmod.online/policies\nKontakt: banmod.giannibart@gmail.com\nAktualisiert: 6. Oktober 2026</color></size>";

                case "fr":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Confidentialité, Conditions & Règles</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Utilisation facultative</color></b></size>\n" +
                        "<size=16>BanMod est facultatif. En l'installant et en l'utilisant, vous reconnaissez avoir eu accès à la Politique de confidentialité et acceptez les Conditions et Règles de BanMod. Si vous n'acceptez pas, n'utilisez pas BanMod ni ses services connectés.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Serveur approprié</color></b></size>\n" +
                        "<size=16>Les fonctions compatibles avec un gameplay non modifié peuvent être utilisées dans l'environnement Original/Vanilla. Les fonctions qui modifient le gameplay ou les autres joueurs nécessitent l'environnement/serveur Modded approprié. Une mauvaise utilisation peut entraîner des restrictions ou bannissements par Innersloth ou les opérateurs de serveurs; BanMod ne contrôle pas ces décisions.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Sécurité et autres mods</color></b></size>\n" +
                        "<size=16>BanMod vérifie la compatibilité, l'intégrité et les cheats potentiels. Les mods connus et compatibles peuvent être autorisés. Les mods inconnus ou non vérifiés peuvent rendre les <color=#80D8FF>Services Extra</color> indisponibles. Les cheat menus connus ou modifications interdites peuvent provoquer l'auto-désactivation de BanMod.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Services Extra</color></b></size>\n" +
                        "<size=16>Certains noms internes utilisent encore l'ancien terme \"Premium\". Dans le projet actuel, il désigne des Services Extra facultatifs, pas des avantages de gameplay payants. Leur disponibilité dépend de la sécurité, de l'intégrité, de la compatibilité et du respect des règles.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Données et authentification</color></b></size>\n" +
                        "<size=16>BanMod peut traiter le FriendCode, le nom du joueur, la plateforme, la langue, les données de lobby, la version/build, les hash techniques des mods/plugins, le contenu Community/Chat et les données de rapports. Les API privées utilisent FriendCode + jeton BanMod; le serveur conserve une représentation hash du jeton.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Adresses IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>Les IP des utilisateurs normaux ne sont pas conservées comme historique.</color> Elles peuvent être traitées temporairement pour le réseau, le rate limiting et la sécurité. Une IP bloquée pour activité malveillante, abusive ou fortement suspecte peut être conservée de façon permanente pour appliquer le blocage pare-feu.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Rapports, logs et services externes</color></b></size>\n" +
                        "<size=16>Les rapports peuvent inclure des informations techniques et des logs BepInEx nécessaires au support, au débogage, à la modération et à la sécurité. Des fonctions optionnelles peuvent interagir avec Discord, GitHub, Telegram ou le site BanMod, soumis à leurs propres politiques.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Politique complète: https://banmod.online/policies\nContact: banmod.giannibart@gmail.com\nMise à jour: 6 octobre 2026</color></size>";

                case "es":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacidad, Términos y Reglas</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Uso opcional</color></b></size>\n" +
                        "<size=16>BanMod es opcional. Al instalarlo y utilizarlo confirmas que has tenido acceso a la Política de Privacidad y aceptas los Términos y Reglas de BanMod. Si no los aceptas, no uses BanMod ni sus servicios conectados.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Servidor correcto</color></b></size>\n" +
                        "<size=16>Las funciones compatibles con gameplay sin modificar pueden usarse en el entorno Original/Vanilla. Las funciones que cambian el gameplay o afectan a otros jugadores requieren el entorno/servidor Modded apropiado. El uso indebido puede provocar restricciones o baneos por Innersloth u operadores de servidores; BanMod no controla esas decisiones.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Seguridad y otros mods</color></b></size>\n" +
                        "<size=16>BanMod comprueba compatibilidad, integridad y posibles cheats. Los mods conocidos y compatibles pueden permitirse. Los mods desconocidos o no verificados pueden desactivar los <color=#80D8FF>Servicios Extra</color>. Los cheat menus conocidos o modificaciones prohibidas pueden hacer que BanMod se desactive automáticamente.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Servicios Extra</color></b></size>\n" +
                        "<size=16>Algunos nombres internos todavía usan el término heredado \"Premium\". En el proyecto actual significa Servicios Extra opcionales, no ventajas de gameplay de pago. Su disponibilidad depende de seguridad, integridad, compatibilidad y cumplimiento de las reglas.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Datos y autenticación</color></b></size>\n" +
                        "<size=16>BanMod puede procesar FriendCode, nombre del jugador, plataforma, idioma, datos de lobby, versión/build, hashes técnicos de mods/plugins, contenido Community/Chat y datos de reportes. Las API privadas usan FriendCode + token BanMod; el servidor conserva una representación hash del token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Direcciones IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>Las IP de usuarios normales no se conservan como historial.</color> Pueden procesarse temporalmente para red, rate limiting y seguridad. Una IP bloqueada por actividad maliciosa, abusiva o muy sospechosa puede conservarse permanentemente para aplicar el bloqueo del firewall.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Reportes, logs y servicios externos</color></b></size>\n" +
                        "<size=16>Los reportes pueden incluir información técnica y logs de BepInEx necesarios para soporte, depuración, moderación y seguridad. Funciones opcionales pueden interactuar con Discord, GitHub, Telegram o el sitio de BanMod, sujetos a sus propias políticas.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Política completa: https://banmod.online/policies\nContacto: banmod.giannibart@gmail.com\nActualizado: 6 de octubre de 2026</color></size>";

                case "pt":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacidade, Termos e Regras</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Uso opcional</color></b></size>\n" +
                        "<size=16>BanMod é opcional. Ao instalar e usar, você confirma que teve acesso à Política de Privacidade e aceita os Termos e Regras do BanMod. Se não concordar, não use BanMod nem os serviços conectados.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Servidor correto</color></b></size>\n" +
                        "<size=16>Funções compatíveis com gameplay não modificado podem ser usadas no ambiente Original/Vanilla. Funções que alteram o gameplay ou afetam outros jogadores exigem o ambiente/servidor Modded apropriado. Uso indevido pode resultar em restrições ou banimentos por Innersloth ou operadores de servidores; BanMod não controla essas decisões.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Segurança e outros mods</color></b></size>\n" +
                        "<size=16>BanMod verifica compatibilidade, integridade e possíveis cheats. Mods conhecidos e compatíveis podem ser permitidos. Mods desconhecidos ou não verificados podem desativar os <color=#80D8FF>Serviços Extra</color>. Cheat menus conhecidos ou modificações proibidas podem fazer BanMod se autodesativar.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Serviços Extra</color></b></size>\n" +
                        "<size=16>Alguns nomes internos ainda usam o termo legado \"Premium\". No projeto atual isso significa Serviços Extra opcionais, não vantagens de gameplay pagas. A disponibilidade depende de segurança, integridade, compatibilidade e cumprimento das regras.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Dados e autenticação</color></b></size>\n" +
                        "<size=16>BanMod pode processar FriendCode, nome do jogador, plataforma, idioma, dados de lobby, versão/build, hashes técnicos de mods/plugins, conteúdo Community/Chat e dados de relatórios. APIs privadas usam FriendCode + token BanMod; o servidor armazena uma representação hash do token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Endereços IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>IPs de usuários normais não são mantidos como histórico.</color> Podem ser processados temporariamente para rede, rate limiting e segurança. Um IP bloqueado por atividade maliciosa, abusiva ou fortemente suspeita pode ser mantido permanentemente para aplicar o bloqueio de firewall.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Relatórios, logs e serviços externos</color></b></size>\n" +
                        "<size=16>Relatórios podem incluir informações técnicas e logs BepInEx necessários para suporte, depuração, moderação e segurança. Recursos opcionais podem interagir com Discord, GitHub, Telegram ou o site BanMod, sujeitos às próprias políticas.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Política completa: https://banmod.online/policies\nContato: banmod.giannibart@gmail.com\nAtualizado: 6 de outubro de 2026</color></size>";

                case "nl":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacy, Voorwaarden & Regels</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Optioneel gebruik</color></b></size>\n" +
                        "<size=16>BanMod is optioneel. Door BanMod te installeren en te gebruiken bevestig je dat de Privacy Policy beschikbaar was en accepteer je de BanMod-voorwaarden en -regels. Als je niet akkoord gaat, gebruik BanMod of de gekoppelde diensten dan niet.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Juiste server</color></b></size>\n" +
                        "<size=16>Functies zonder gameplaywijzigingen kunnen in de Originele/Vanilla-omgeving worden gebruikt. Functies die gameplay of andere spelers beïnvloeden vereisen de juiste Modded-omgeving/server. Misbruik kan leiden tot beperkingen of bans door Innersloth of serverbeheerders; BanMod beheert die beslissingen niet.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Beveiliging en andere mods</color></b></size>\n" +
                        "<size=16>BanMod controleert compatibiliteit, integriteit en mogelijke cheats. Bekende compatibele mods kunnen worden toegestaan. Onbekende of niet-geverifieerde mods kunnen <color=#80D8FF>Extra Services</color> uitschakelen. Bekende cheatmenu's of verboden wijzigingen kunnen BanMod automatisch laten uitschakelen.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Sommige interne namen gebruiken nog de oude term \"Premium\". In het huidige project betekent dit optionele Extra Services, geen betaalde gameplayvoordelen. Beschikbaarheid hangt af van beveiliging, integriteit, compatibiliteit en naleving van de regels.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Gegevens en authenticatie</color></b></size>\n" +
                        "<size=16>BanMod kan FriendCode, spelersnaam, platform, taal, lobbygegevens, versie/build, technische hashes van mods/plugins, Community/Chat-inhoud en rapportgegevens verwerken. Privé-API's gebruiken FriendCode + BanMod-token; de server bewaart een hashweergave van het token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP-adressen</color></b></size>\n" +
                        "<size=16><color=#80FF80>IP-adressen van normale gebruikers worden niet als geschiedenis opgeslagen.</color> Ze kunnen tijdelijk worden verwerkt voor netwerk, rate limiting en beveiliging. Een IP dat wegens kwaadaardige, misbruikende of ernstig verdachte activiteit is geblokkeerd kan permanent worden bewaard om de firewallblokkade af te dwingen.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Rapporten, logs en externe diensten</color></b></size>\n" +
                        "<size=16>Rapporten kunnen technische informatie en BepInEx-logs bevatten die nodig zijn voor support, debugging, moderatie en beveiliging. Optionele functies kunnen Discord, GitHub, Telegram of de BanMod-website gebruiken, onder hun eigen beleid.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Volledig beleid: https://banmod.online/policies\nContact: banmod.giannibart@gmail.com\nBijgewerkt: 6 oktober 2026</color></size>";

                case "ru":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Конфиденциальность, Условия и Правила</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Добровольное использование</color></b></size>\n" +
                        "<size=16>BanMod является необязательной модификацией. Устанавливая и используя её, вы подтверждаете, что имели доступ к Политике конфиденциальности, и принимаете Условия и Правила BanMod. Если вы не согласны, не используйте BanMod и связанные сервисы.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Правильный сервер</color></b></size>\n" +
                        "<size=16>Функции без изменения геймплея могут использоваться в Original/Vanilla-среде. Функции, меняющие геймплей или влияющие на других игроков, требуют подходящей Modded-среды/сервера. Неправильное использование может привести к ограничениям или банам со стороны Innersloth или операторов серверов; BanMod не контролирует эти решения.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Безопасность и другие моды</color></b></size>\n" +
                        "<size=16>BanMod проверяет совместимость, целостность и возможные читы. Известные совместимые моды могут быть разрешены. Неизвестные или непроверенные моды могут отключить <color=#80D8FF>Extra Services</color>. Известные cheat menu или запрещённые модификации могут привести к автоматическому отключению BanMod.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Некоторые внутренние названия всё ещё используют старый термин \"Premium\". В текущем проекте это означает дополнительные необязательные сервисы, а не платные преимущества в игре. Доступность зависит от безопасности, целостности, совместимости и соблюдения правил.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Данные и аутентификация</color></b></size>\n" +
                        "<size=16>BanMod может обрабатывать FriendCode, имя игрока, платформу, язык, данные лобби, версию/build, технические хэши модов/плагинов, содержимое Community/Chat и данные отчётов. Приватные API используют FriendCode + токен BanMod; сервер хранит хэш-представление токена.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP-адреса</color></b></size>\n" +
                        "<size=16><color=#80FF80>IP обычных пользователей не сохраняются как история.</color> Они могут временно обрабатываться для сети, rate limiting и безопасности. IP, заблокированный из-за вредоносной, злоупотребляющей или серьёзно подозрительной активности, может храниться постоянно для применения firewall-блокировки.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Отчёты, логи и внешние сервисы</color></b></size>\n" +
                        "<size=16>Отчёты могут включать технические данные и логи BepInEx, необходимые для поддержки, отладки, модерации и безопасности. Дополнительные функции могут взаимодействовать с Discord, GitHub, Telegram или сайтом BanMod на условиях их собственных политик.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Полная политика: https://banmod.online/policies\nКонтакт: banmod.giannibart@gmail.com\nОбновлено: 6 октября 2026</color></size>";

                case "ko":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - 개인정보, 이용약관 및 규칙</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>선택적 사용</color></b></size>\n" +
                        "<size=16>BanMod는 선택 사항입니다. 설치하고 사용하면 개인정보 처리방침을 확인할 수 있었음을 인정하고 BanMod 이용약관 및 규칙에 동의하는 것입니다. 동의하지 않으면 BanMod 또는 연결 서비스를 사용하지 마십시오.</size>\n\n" +
                        "<size=18><b><color=#FFD080>올바른 서버</color></b></size>\n" +
                        "<size=16>게임플레이를 변경하지 않는 기능은 Original/Vanilla 환경에서 사용할 수 있습니다. 게임플레이 또는 다른 플레이어에게 영향을 주는 기능은 적절한 Modded 환경/서버가 필요합니다. 부적절한 사용은 Innersloth 또는 서버 운영자의 제한이나 차단으로 이어질 수 있으며 BanMod는 이러한 결정을 통제하지 않습니다.</size>\n\n" +
                        "<size=18><b><color=#FFD080>보안 및 다른 모드</color></b></size>\n" +
                        "<size=16>BanMod는 호환성, 무결성 및 치트 가능성을 확인합니다. 알려진 호환 모드는 허용될 수 있습니다. 알 수 없거나 검증되지 않은 모드는 <color=#80D8FF>Extra Services</color>를 사용할 수 없게 할 수 있습니다. 알려진 치트 메뉴나 금지된 수정은 BanMod의 자동 비활성화를 유발할 수 있습니다.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>일부 내부 이름에는 기존 용어인 \"Premium\"이 남아 있을 수 있습니다. 현재 프로젝트에서는 유료 게임플레이 이점이 아니라 선택적 추가 서비스를 의미합니다. 제공 여부는 보안, 무결성, 호환성 및 규칙 준수에 따라 달라집니다.</size>\n\n" +
                        "<size=18><b><color=#FFD080>데이터 및 인증</color></b></size>\n" +
                        "<size=16>BanMod는 FriendCode, 플레이어 이름, 플랫폼, 언어, 로비 데이터, 버전/build, 모드/플러그인 기술 해시, Community/Chat 콘텐츠 및 신고 데이터를 처리할 수 있습니다. 비공개 API는 FriendCode + BanMod 토큰을 사용하며 서버는 토큰의 해시 표현을 저장합니다.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP 주소</color></b></size>\n" +
                        "<size=16><color=#80FF80>일반 사용자 IP는 기록 이력으로 저장되지 않습니다.</color> 네트워크, rate limiting 및 보안을 위해 일시적으로 처리될 수 있습니다. 악성, 남용 또는 심각하게 의심스러운 활동으로 차단된 IP는 방화벽 차단을 유지하기 위해 영구 저장될 수 있습니다.</size>\n\n" +
                        "<size=18><b><color=#FFD080>신고, 로그 및 외부 서비스</color></b></size>\n" +
                        "<size=16>신고에는 지원, 디버깅, 운영 및 보안에 필요한 기술 정보와 BepInEx 로그가 포함될 수 있습니다. 선택 기능은 Discord, GitHub, Telegram 또는 BanMod 웹사이트와 연동될 수 있으며 해당 서비스의 정책이 적용됩니다.</size>\n\n" +
                        "<size=15><color=#AAAAAA>전체 정책: https://banmod.online/policies\n문의: banmod.giannibart@gmail.com\n업데이트: 2026년 10월 6일</color></size>";

                case "ja":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - プライバシー・利用規約・ルール</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>任意の利用</color></b></size>\n" +
                        "<size=16>BanMod は任意の Mod です。インストールして利用することで、プライバシーポリシーを確認できる状態にあったことを認め、BanMod の利用規約とルールに同意したものとみなされます。同意しない場合は BanMod または関連サービスを使用しないでください。</size>\n\n" +
                        "<size=18><b><color=#FFD080>正しいサーバー</color></b></size>\n" +
                        "<size=16>ゲームプレイを変更しない機能は Original/Vanilla 環境で利用できます。ゲームプレイや他のプレイヤーに影響する機能には適切な Modded 環境/サーバーが必要です。不適切な利用により Innersloth またはサーバー運営者から制限・BAN を受ける可能性があり、BanMod はその判断を管理しません。</size>\n\n" +
                        "<size=18><b><color=#FFD080>セキュリティと他の Mod</color></b></size>\n" +
                        "<size=16>BanMod は互換性、整合性、チートの可能性を確認します。既知で互換性のある Mod は許可される場合があります。不明または未確認の Mod がある場合、<color=#80D8FF>Extra Services</color> が利用できなくなることがあります。既知のチートメニューや禁止された改変は BanMod の自動無効化につながる場合があります。</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>一部の内部名称には旧称 \"Premium\" が残っています。現在のプロジェクトでは、課金によるゲームプレイ上の優位性ではなく、任意の追加サービスを意味します。利用可否はセキュリティ、整合性、互換性、ルール遵守に依存します。</size>\n\n" +
                        "<size=18><b><color=#FFD080>データと認証</color></b></size>\n" +
                        "<size=16>BanMod は FriendCode、プレイヤー名、プラットフォーム、言語、ロビー情報、バージョン/build、Mod/プラグインの技術的ハッシュ、Community/Chat の内容、レポート情報を処理する場合があります。非公開 API は FriendCode + BanMod トークンを使用し、サーバーはトークンのハッシュ表現を保存します。</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP アドレス</color></b></size>\n" +
                        "<size=16><color=#80FF80>通常ユーザーの IP は履歴として保存されません。</color> ネットワーク、rate limiting、セキュリティのため一時的に処理される場合があります。悪意ある、乱用的、または重大に疑わしい活動でブロックされた IP は、ファイアウォール制限を維持するため永続保存される場合があります。</size>\n\n" +
                        "<size=18><b><color=#FFD080>レポート、ログ、外部サービス</color></b></size>\n" +
                        "<size=16>レポートにはサポート、デバッグ、モデレーション、セキュリティに必要な技術情報や BepInEx ログが含まれる場合があります。任意機能は Discord、GitHub、Telegram、BanMod サイトなど外部サービスと連携する場合があり、それぞれのポリシーが適用されます。</size>\n\n" +
                        "<size=15><color=#AAAAAA>完全なポリシー: https://banmod.online/policies\n連絡先: banmod.giannibart@gmail.com\n更新日: 2026年10月6日</color></size>";

                case "zh-hans":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - 隐私、条款与规则</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>自愿使用</color></b></size>\n" +
                        "<size=16>BanMod 是可选 Mod。安装并使用即表示你已可查看隐私政策，并接受 BanMod 的条款与规则。如不同意，请勿使用 BanMod 或其连接服务。</size>\n\n" +
                        "<size=18><b><color=#FFD080>正确的服务器</color></b></size>\n" +
                        "<size=16>不改变游戏玩法的功能可在 Original/Vanilla 环境使用。会改变游戏玩法或影响其他玩家的功能需要使用合适的 Modded 环境/服务器。不当使用可能导致 Innersloth 或服务器运营者的限制或封禁；BanMod 不控制这些决定。</size>\n\n" +
                        "<size=18><b><color=#FFD080>安全与其他 Mod</color></b></size>\n" +
                        "<size=16>BanMod 会检查兼容性、完整性和潜在作弊。已知且兼容的 Mod 可能被允许。未知或未验证的 Mod 可能导致 <color=#80D8FF>Extra Services</color> 不可用。已知作弊菜单或被禁止的修改可能导致 BanMod 自动停用。</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>某些内部名称仍可能使用旧称 \"Premium\"。在当前项目中，它表示可选的额外服务，而不是付费游戏优势。是否可用取决于安全、完整性、兼容性和规则遵守情况。</size>\n\n" +
                        "<size=18><b><color=#FFD080>数据与身份验证</color></b></size>\n" +
                        "<size=16>BanMod 可能处理 FriendCode、玩家名称、平台、语言、大厅数据、版本/build、Mod/插件技术哈希、Community/Chat 内容和举报数据。私有 API 使用 FriendCode + BanMod token；服务器保存 token 的哈希表示。</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP 地址</color></b></size>\n" +
                        "<size=16><color=#80FF80>普通用户 IP 不会作为历史记录保存。</color> IP 可为网络、rate limiting 和安全而临时处理。因恶意、滥用或严重可疑活动而被封锁的 IP 可被永久保存，以持续执行防火墙封锁。</size>\n\n" +
                        "<size=18><b><color=#FFD080>举报、日志与外部服务</color></b></size>\n" +
                        "<size=16>举报可能包含支持、调试、管理和安全所需的技术信息及 BepInEx 日志。可选功能可能与 Discord、GitHub、Telegram 或 BanMod 网站等外部服务交互，并受其各自政策约束。</size>\n\n" +
                        "<size=15><color=#AAAAAA>完整政策: https://banmod.online/policies\n联系: banmod.giannibart@gmail.com\n更新: 2026年10月6日</color></size>";

                case "zh-hant":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - 隱私、條款與規則</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>自願使用</color></b></size>\n" +
                        "<size=16>BanMod 是可選 Mod。安裝並使用即表示你已可查看隱私政策，並接受 BanMod 的條款與規則。如不同意，請勿使用 BanMod 或其連線服務。</size>\n\n" +
                        "<size=18><b><color=#FFD080>正確的伺服器</color></b></size>\n" +
                        "<size=16>不改變遊戲玩法的功能可在 Original/Vanilla 環境使用。會改變遊戲玩法或影響其他玩家的功能需要使用合適的 Modded 環境/伺服器。不當使用可能導致 Innersloth 或伺服器營運者的限制或封禁；BanMod 不控制這些決定。</size>\n\n" +
                        "<size=18><b><color=#FFD080>安全與其他 Mod</color></b></size>\n" +
                        "<size=16>BanMod 會檢查相容性、完整性和潛在作弊。已知且相容的 Mod 可能被允許。未知或未驗證的 Mod 可能導致 <color=#80D8FF>Extra Services</color> 無法使用。已知作弊選單或被禁止的修改可能導致 BanMod 自動停用。</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>部分內部名稱仍可能使用舊稱 \"Premium\"。在目前專案中，它代表可選的額外服務，而不是付費遊戲優勢。是否可用取決於安全、完整性、相容性與規則遵守情況。</size>\n\n" +
                        "<size=18><b><color=#FFD080>資料與驗證</color></b></size>\n" +
                        "<size=16>BanMod 可能處理 FriendCode、玩家名稱、平台、語言、大廳資料、版本/build、Mod/插件技術雜湊、Community/Chat 內容及檢舉資料。私有 API 使用 FriendCode + BanMod token；伺服器保存 token 的雜湊表示。</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP 位址</color></b></size>\n" +
                        "<size=16><color=#80FF80>一般使用者 IP 不會作為歷史記錄保存。</color> IP 可為網路、rate limiting 與安全而暫時處理。因惡意、濫用或高度可疑活動而被封鎖的 IP 可被永久保存，以持續套用防火牆封鎖。</size>\n\n" +
                        "<size=18><b><color=#FFD080>檢舉、日誌與外部服務</color></b></size>\n" +
                        "<size=16>檢舉可能包含支援、除錯、管理與安全所需的技術資訊及 BepInEx 日誌。可選功能可能與 Discord、GitHub、Telegram 或 BanMod 網站等外部服務互動，並受其各自政策約束。</size>\n\n" +
                        "<size=15><color=#AAAAAA>完整政策: https://banmod.online/policies\n聯絡: banmod.giannibart@gmail.com\n更新: 2026年10月6日</color></size>";

                case "fil":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacy, Mga Tuntunin at Patakaran</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Opsyonal na paggamit</color></b></size>\n" +
                        "<size=16>Opsyonal ang BanMod. Sa pag-install at paggamit nito, kinikilala mong nabigyan ka ng access sa Privacy Policy at tinatanggap mo ang BanMod Terms at Rules. Kung hindi ka sang-ayon, huwag gamitin ang BanMod o mga konektadong serbisyo.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Tamang server</color></b></size>\n" +
                        "<size=16>Ang mga feature na hindi nagbabago ng gameplay ay maaaring gamitin sa Original/Vanilla environment. Ang mga feature na nagbabago ng gameplay o nakaaapekto sa ibang manlalaro ay nangangailangan ng angkop na Modded environment/server. Ang maling paggamit ay maaaring humantong sa restriction o ban mula sa Innersloth o server operators; hindi kontrolado ng BanMod ang mga desisyong iyon.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Security at ibang mods</color></b></size>\n" +
                        "<size=16>Sinusuri ng BanMod ang compatibility, integrity at posibleng cheats. Maaaring payagan ang kilala at compatible na mods. Ang unknown o unverified mods ay maaaring mag-disable ng <color=#80D8FF>Extra Services</color>. Ang kilalang cheat menus o prohibited modifications ay maaaring magpa-auto-disable sa BanMod.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>May ilang internal names na gumagamit pa rin ng lumang salitang \"Premium\". Sa kasalukuyang proyekto, ibig sabihin nito ay optional Extra Services, hindi bayad na gameplay advantages. Ang availability ay nakadepende sa security, integrity, compatibility at pagsunod sa rules.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Data at authentication</color></b></size>\n" +
                        "<size=16>Maaaring iproseso ng BanMod ang FriendCode, player name, platform, language, lobby data, version/build, technical hashes ng mods/plugins, Community/Chat content at report data. Gumagamit ang private APIs ng FriendCode + BanMod token; hash representation ng token ang iniimbak ng server.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP addresses</color></b></size>\n" +
                        "<size=16><color=#80FF80>Hindi iniimbak bilang history ang IP ng normal users.</color> Maaari itong pansamantalang iproseso para sa network, rate limiting at security. Ang IP na na-block dahil sa malicious, abusive o seryosong suspicious activity ay maaaring permanenteng itago para ipatupad ang firewall block.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Reports, logs at external services</color></b></size>\n" +
                        "<size=16>Maaaring kasama sa reports ang technical information at BepInEx logs na kailangan para sa support, debugging, moderation at security. Maaaring makipag-ugnayan ang optional features sa Discord, GitHub, Telegram o BanMod website at sakop ang mga ito ng sarili nilang policies.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Buong policy: https://banmod.online/policies\nContact: banmod.giannibart@gmail.com\nUpdated: 6 October 2026</color></size>";

                case "ga":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Príobháideachas, Téarmaí & Rialacha</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Úsáid roghnach</color></b></size>\n" +
                        "<size=16>Tá BanMod roghnach. Trí é a shuiteáil agus a úsáid, admhaíonn tú go raibh an Polasaí Príobháideachais ar fáil duit agus glacann tú le Téarmaí agus Rialacha BanMod. Mura n-aontaíonn tú, ná húsáid BanMod ná na seirbhísí nasctha.</size>\n\n" +
                        "<size=18><b><color=#FFD080>An freastalaí ceart</color></b></size>\n" +
                        "<size=16>Is féidir gnéithe nach n-athraíonn gameplay a úsáid sa timpeallacht Original/Vanilla. Teastaíonn timpeallacht/freastalaí Modded cuí ó ghnéithe a athraíonn gameplay nó a théann i bhfeidhm ar imreoirí eile. D'fhéadfadh srianta nó toirmeasc ó Innersloth nó oibreoirí freastalaí teacht as mí-úsáid; ní rialaíonn BanMod na cinntí sin.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Slándáil agus mods eile</color></b></size>\n" +
                        "<size=16>Seiceálann BanMod comhoiriúnacht, sláine agus cheats féideartha. Féadfar mods aitheanta agus comhoiriúnacha a cheadú. Féadfaidh mods anaithnide nó neamhfhíoraithe <color=#80D8FF>Extra Services</color> a dhéanamh neamh-inrochtana. Féadfaidh cheat menus aitheanta nó modhnuithe toirmiscthe BanMod a dhíchumasú go huathoibríoch.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Úsáideann roinnt ainmneacha inmheánacha an téarma oidhreachta \"Premium\" fós. Sa tionscadal reatha ciallaíonn sé seirbhísí breise roghnacha, ní buntáistí gameplay íoctha. Braitheann infhaighteacht ar shlándáil, sláine, comhoiriúnacht agus cloí leis na rialacha.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Sonraí agus fíordheimhniú</color></b></size>\n" +
                        "<size=16>Féadfaidh BanMod FriendCode, ainm imreora, ardán, teanga, sonraí lobby, leagan/build, hashes teicniúla mod/plugin, ábhar Community/Chat agus sonraí tuairisce a phróiseáil. Úsáideann APIanna príobháideacha FriendCode + BanMod token; stórálann an freastalaí léiriú hash den token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Seoltaí IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>Ní choinnítear IPanna gnáthúsáideoirí mar stair.</color> Féadfar iad a phróiseáil go sealadach don líonra, rate limiting agus slándáil. Féadfar IP blocáilte mar gheall ar ghníomhaíocht mhailíseach, mhí-úsáideach nó thar a bheith amhrasach a choinneáil go buan chun bloc an bhalla dóiteáin a chur i bhfeidhm.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Tuairiscí, logaí agus seirbhísí seachtracha</color></b></size>\n" +
                        "<size=16>Féadfaidh tuairiscí faisnéis theicniúil agus logaí BepInEx a bheith iontu a theastaíonn do thacaíocht, dífhabhtú, modhnóireacht agus slándáil. Féadfaidh gnéithe roghnacha idirghníomhú le Discord, GitHub, Telegram nó suíomh BanMod, faoi réir a bpolasaithe féin.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Polasaí iomlán: https://banmod.online/policies\nTeagmháil: banmod.giannibart@gmail.com\nNuashonraithe: 6 Deireadh Fómhair 2026</color></size>";

                case "pl":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Prywatność, Warunki i Zasady</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Dobrowolne użycie</color></b></size>\n" +
                        "<size=16>BanMod jest opcjonalny. Instalując i używając go, potwierdzasz, że miałeś dostęp do Polityki Prywatności i akceptujesz Warunki oraz Zasady BanMod. Jeśli ich nie akceptujesz, nie używaj BanMod ani połączonych usług.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Właściwy serwer</color></b></size>\n" +
                        "<size=16>Funkcje bez zmian gameplayu mogą być używane w środowisku Original/Vanilla. Funkcje zmieniające gameplay lub wpływające na innych graczy wymagają odpowiedniego środowiska/serwera Modded. Niewłaściwe użycie może skutkować ograniczeniami lub banami ze strony Innersloth albo operatorów serwerów; BanMod nie kontroluje tych decyzji.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Bezpieczeństwo i inne mody</color></b></size>\n" +
                        "<size=16>BanMod sprawdza zgodność, integralność i możliwe cheaty. Znane i zgodne mody mogą być dozwolone. Nieznane lub niezweryfikowane mody mogą wyłączyć <color=#80D8FF>Extra Services</color>. Znane cheat menu lub zabronione modyfikacje mogą spowodować automatyczne wyłączenie BanMod.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Niektóre wewnętrzne nazwy nadal używają historycznego terminu \"Premium\". W obecnym projekcie oznacza on opcjonalne usługi dodatkowe, a nie płatne przewagi w rozgrywce. Dostępność zależy od bezpieczeństwa, integralności, zgodności i przestrzegania zasad.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Dane i uwierzytelnianie</color></b></size>\n" +
                        "<size=16>BanMod może przetwarzać FriendCode, nazwę gracza, platformę, język, dane lobby, wersję/build, techniczne hashe modów/pluginów, treści Community/Chat i dane raportów. Prywatne API używają FriendCode + tokenu BanMod; serwer przechowuje reprezentację hash tokenu.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Adresy IP</color></b></size>\n" +
                        "<size=16><color=#80FF80>Adresy IP zwykłych użytkowników nie są przechowywane jako historia.</color> Mogą być tymczasowo przetwarzane dla sieci, rate limiting i bezpieczeństwa. IP zablokowane z powodu złośliwej, nadużywającej lub poważnie podejrzanej aktywności może być przechowywane trwale w celu egzekwowania blokady firewall.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Raporty, logi i usługi zewnętrzne</color></b></size>\n" +
                        "<size=16>Raporty mogą zawierać informacje techniczne i logi BepInEx potrzebne do wsparcia, debugowania, moderacji i bezpieczeństwa. Opcjonalne funkcje mogą współpracować z Discord, GitHub, Telegram lub stroną BanMod i podlegają politykom tych usług.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Pełna polityka: https://banmod.online/policies\nKontakt: banmod.giannibart@gmail.com\nAktualizacja: 6 października 2026</color></size>";

                case "tr":
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Gizlilik, Koşullar ve Kurallar</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>İsteğe bağlı kullanım</color></b></size>\n" +
                        "<size=16>BanMod isteğe bağlıdır. Kurup kullanarak Gizlilik Politikasına erişebildiğinizi kabul eder ve BanMod Koşulları ile Kurallarını kabul etmiş olursunuz. Kabul etmiyorsanız BanMod'u veya bağlı hizmetleri kullanmayın.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Doğru sunucu</color></b></size>\n" +
                        "<size=16>Oynanışı değiştirmeyen özellikler Original/Vanilla ortamında kullanılabilir. Oynanışı değiştiren veya diğer oyuncuları etkileyen özellikler uygun Modded ortam/sunucu gerektirir. Yanlış kullanım Innersloth veya sunucu yöneticileri tarafından kısıtlama ya da ban ile sonuçlanabilir; BanMod bu kararları kontrol etmez.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Güvenlik ve diğer modlar</color></b></size>\n" +
                        "<size=16>BanMod uyumluluk, bütünlük ve olası hileleri kontrol eder. Bilinen ve uyumlu modlara izin verilebilir. Bilinmeyen veya doğrulanmamış modlar <color=#80D8FF>Extra Services</color> kullanımını kapatabilir. Bilinen cheat menu veya yasaklı değişiklikler BanMod'un kendini otomatik olarak devre dışı bırakmasına neden olabilir.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Bazı dahili adlarda eski \"Premium\" terimi hâlâ kullanılabilir. Güncel projede bu, ücretli oynanış avantajları değil, isteğe bağlı ek hizmetler anlamına gelir. Kullanılabilirlik güvenlik, bütünlük, uyumluluk ve kurallara uymaya bağlıdır.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Veriler ve kimlik doğrulama</color></b></size>\n" +
                        "<size=16>BanMod FriendCode, oyuncu adı, platform, dil, lobby verileri, sürüm/build, mod/plugin teknik hashleri, Community/Chat içeriği ve rapor verilerini işleyebilir. Özel API'ler FriendCode + BanMod token kullanır; sunucu tokenın hash temsilini saklar.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP adresleri</color></b></size>\n" +
                        "<size=16><color=#80FF80>Normal kullanıcı IP'leri geçmiş olarak saklanmaz.</color> Ağ, rate limiting ve güvenlik amacıyla geçici olarak işlenebilir. Kötü niyetli, suistimal içeren veya ciddi biçimde şüpheli etkinlik nedeniyle engellenen IP, firewall engelini uygulamak için kalıcı olarak saklanabilir.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Raporlar, loglar ve harici hizmetler</color></b></size>\n" +
                        "<size=16>Raporlar destek, hata ayıklama, moderasyon ve güvenlik için gerekli teknik bilgiler ile BepInEx loglarını içerebilir. İsteğe bağlı özellikler Discord, GitHub, Telegram veya BanMod sitesiyle etkileşebilir ve bu hizmetlerin kendi politikaları geçerlidir.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Tam politika: https://banmod.online/policies\nİletişim: banmod.giannibart@gmail.com\nGüncelleme: 6 Ekim 2026</color></size>";

                default:
                    return
                        "<size=26><b><color=#FF8A00>BanMod - Privacy, Terms & Rules</color></b></size>\n\n" +
                        "<size=18><b><color=#FFD080>Optional use</color></b></size>\n" +
                        "<size=16>BanMod is optional. By installing and using it, you acknowledge that the Privacy Policy was made available to you and you accept the BanMod Terms and Rules. If you do not agree, do not use BanMod or its connected services.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Correct server</color></b></size>\n" +
                        "<size=16>Features compatible with unmodified gameplay may be used in the Original/Vanilla environment. Features that change gameplay or affect other players require the appropriate Modded environment/server. Misuse may result in restrictions or bans by Innersloth or server operators; BanMod does not control those decisions.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Security and other mods</color></b></size>\n" +
                        "<size=16>BanMod checks compatibility, integrity and possible cheats. Known compatible mods may be allowed. Unknown or unverified mods may make <color=#80D8FF>Extra Services</color> unavailable. Known cheat menus or prohibited modifications may cause BanMod to disable itself.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Extra Services</color></b></size>\n" +
                        "<size=16>Some internal names may still use the legacy term \"Premium\". In the current project this means optional Extra Services, not paid gameplay advantages. Availability depends on security, integrity, compatibility and compliance with the rules.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Data and authentication</color></b></size>\n" +
                        "<size=16>BanMod may process FriendCode, player name, platform, language, lobby data, version/build, technical mod/plugin hashes, Community/Chat content and report data. Private APIs use FriendCode + BanMod token; the server stores a hash representation of the token.</size>\n\n" +
                        "<size=18><b><color=#FFD080>IP addresses</color></b></size>\n" +
                        "<size=16><color=#80FF80>Normal user IPs are not retained as a history.</color> They may be processed temporarily for networking, rate limiting and security. An IP blocked for malicious, abusive or seriously suspicious activity may be stored permanently to enforce the firewall block.</size>\n\n" +
                        "<size=18><b><color=#FFD080>Reports, logs and external services</color></b></size>\n" +
                        "<size=16>Reports may include technical information and BepInEx logs needed for support, debugging, moderation and security. Optional features may interact with external services such as Discord, GitHub, Telegram or the BanMod website, subject to their own policies.</size>\n\n" +
                        "<size=15><color=#AAAAAA>Full policy: https://banmod.online/policies\nContact: banmod.giannibart@gmail.com\nLast updated: 6 October 2026</color></size>";
            }
        }

        private static void ApplyModernImage(Image image, Color color, bool addOutline)
        {
            if (image == null)
                return;

            image.sprite = BanModUiStyles.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            if (addOutline)
            {
                Outline outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = image.gameObject.AddComponent<Outline>();

                outline.effectColor = new Color(
                    BanModUiStyles.AccentColor.r,
                    BanModUiStyles.AccentColor.g,
                    BanModUiStyles.AccentColor.b,
                    0.24f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
            }
        }

        private static void ApplyButtonTransition(Button button, Image image)
        {
            if (button == null || image == null)
                return;

            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        public static GameObject CreateMessagePopup(string title, string content)
        {
            return CreateMessagePopup(title, content, null);
        }

        public static GameObject CreateMessagePopup(string title, string content, Action onClose)
        {
            try
            {
                BanModCommunicationUi.EnsureCreated();

                if (BanModCommunicationUi.Instance != null)
                {
                    BanModCommunicationUi.Instance.ShowMessagePopup(title, content, onClose);
                    return BanModCommunicationUi.Instance.gameObject;
                }
            }
            catch (Exception ex)
            {
                try { Debug.LogError("[BANMOD] Failed to open message popup: " + ex.Message); } catch { }
            }

            try
            {
                Debug.LogWarning("[BANMOD POPUP] " + (title ?? "BANMOD") + "\n" + (content ?? ""));
            }
            catch { }

            return null;
        }
    }

    [HarmonyPatch]
    public static class BanModUpdatePopupBlockPassiveButtonsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] names =
            {
                "ReceiveClickDown",
                "ReceiveClickUp",
                "ReceiveClick",
                "ReceiveClickUpHandler",
                "DoClick",
                "OnClick",
                "OnMouseDown",
                "OnMouseUp",
                "OnMouseUpAsButton"
            };

            Type passiveButtonType = typeof(PassiveButton);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] methods = passiveButtonType.GetMethods(flags);

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];

                for (int j = 0; j < methods.Length; j++)
                {
                    MethodInfo method = methods[j];
                    if (method != null && method.Name == name)
                        yield return method;
                }
            }
        }

        public static bool Prefix()
        {
            try
            {
                if (BanModPopup.IsUpdatePopupOpen)
                    return false;
            }
            catch { }

            return true;
        }
    }
}
