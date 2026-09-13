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

        /// <summary>
        /// Popup aggiornamenti BanMod.
        /// La lingua viene letta direttamente dalla lingua UI selezionata in Among Us.
        /// - mandatory = false: mostra "Aggiorna" e "Non aggiornare".
        /// - mandatory = true: mostra solo "Aggiorna" e mantiene BanMod bloccata.
        /// Le note della release restano esattamente quelle ricevute dal server.
        /// </summary>
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

            // Overlay a schermo intero: assorbe tutti i raycast UGUI.
            // I PassiveButton di Among Us sono bloccati separatamente dalla patch Harmony
            // BanModUpdatePopupBlockPassiveButtonsPatch finché IsUpdatePopupOpen è true.
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

            // CanvasGroup rende esplicito il blocco dell'intero canvas sottostante.
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
            // Among Us aggiorna TranslationController.currentLanguage quando la lingua UI viene cambiata.
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

    /// <summary>
    /// Among Us usa PassiveButton per molti controlli del menu. Un overlay UGUI
    /// non impedisce necessariamente a PassiveButton di ricevere il click, quindi
    /// durante il popup updater blocchiamo direttamente i suoi handler di click.
    /// </summary>
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
