////credits and licenses in the resources folder
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//namespace BanMod
//{
//    public static class BanModPopup
//    {
//        public static GameObject CreateDisableModPopup(string title, string content)
//        {
//            GameObject popup = new GameObject("BanMod_Popup");
//            popup.transform.position = new Vector3(0f, 0f, -10f);

//            var canvas = popup.AddComponent<Canvas>();
//            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//            canvas.sortingOrder = 200;

//            popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//            popup.AddComponent<GraphicRaycaster>();

//            var bg = new GameObject("Background");
//            bg.transform.SetParent(popup.transform, false);
//            var bgRect = bg.AddComponent<RectTransform>();
//            bgRect.sizeDelta = new Vector2(500, 300);
//            bgRect.anchoredPosition = Vector2.zero;

//            var bgImage = bg.AddComponent<Image>();
//            bgImage.color = new Color(0f, 0f, 0f, 0.85f);

//            var titleGO = new GameObject("TitleText");
//            titleGO.transform.SetParent(bg.transform, false);
//            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
//            titleText.text = title;
//            titleText.fontSize = 28;
//            titleText.alignment = TextAlignmentOptions.Center;
//            titleText.color = Color.cyan;

//            var titleRect = titleText.GetComponent<RectTransform>();
//            titleRect.sizeDelta = new Vector2(460, 50);
//            titleRect.anchoredPosition = new Vector2(0, 110);

//            var contentGO = new GameObject("ContentText");
//            contentGO.transform.SetParent(bg.transform, false);
//            var contentText = contentGO.AddComponent<TextMeshProUGUI>();
//            contentText.text = content;
//            contentText.fontSize = 20;
//            contentText.color = Color.white;
//            contentText.alignment = TextAlignmentOptions.TopLeft;

//            var contentRect = contentText.GetComponent<RectTransform>();
//            contentRect.sizeDelta = new Vector2(460, 150);
//            contentRect.anchoredPosition = new Vector2(0, 20);

//            var buttonsGO = new GameObject("ButtonsContainer");
//            buttonsGO.transform.SetParent(bg.transform, false);
//            var buttonsRect = buttonsGO.AddComponent<RectTransform>();
//            buttonsRect.sizeDelta = new Vector2(460, 50);
//            buttonsRect.anchoredPosition = new Vector2(0, -110);

//            var disableBtnGO = new GameObject("DisableModButton");
//            disableBtnGO.transform.SetParent(buttonsGO.transform, false);
//            var disableRect = disableBtnGO.AddComponent<RectTransform>();
//            disableRect.sizeDelta = new Vector2(200, 50);
//            disableRect.anchoredPosition = new Vector2(-130, 0);

//            var disableImage = disableBtnGO.AddComponent<Image>();
//            disableImage.color = new Color(0.8f, 0f, 0f, 1f); 

//            var disableButton = disableBtnGO.AddComponent<Button>();
//            System.Action value = () =>
//            {
//                BanMod.DisableMod();
//                GameObject.Destroy(popup);
//            };
//            disableButton.onClick.AddListener(value);

//            var disableTextGO = new GameObject("Text");
//            disableTextGO.transform.SetParent(disableBtnGO.transform, false);
//            var disableText = disableTextGO.AddComponent<TextMeshProUGUI>();
//            disableText.text = Translator.GetString("disableModButton");
//            disableText.fontSize = 22;
//            disableText.alignment = TextAlignmentOptions.Center;
//            disableText.color = Color.white;

//            var disableTextRect = disableText.GetComponent<RectTransform>();
//            disableTextRect.anchorMin = Vector2.zero;
//            disableTextRect.anchorMax = Vector2.one;
//            disableTextRect.offsetMin = Vector2.zero;
//            disableTextRect.offsetMax = Vector2.zero;

//            var closeBtnGO = new GameObject("CloseButton");
//            closeBtnGO.transform.SetParent(buttonsGO.transform, false);
//            var closeRect = closeBtnGO.AddComponent<RectTransform>();
//            closeRect.sizeDelta = new Vector2(200, 50);
//            closeRect.anchoredPosition = new Vector2(130, 0);

//            var closeImage = closeBtnGO.AddComponent<Image>();
//            closeImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

//            var closeButton = closeBtnGO.AddComponent<Button>();
//            System.Action value1 = () =>
//            {
//                GameObject.Destroy(popup);
//            };
//            closeButton.onClick.AddListener(value1);

//            var closeTextGO = new GameObject("Text");
//            closeTextGO.transform.SetParent(closeBtnGO.transform, false);
//            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
//            closeText.text = Translator.GetString("closeButton");
//            closeText.fontSize = 22;
//            closeText.alignment = TextAlignmentOptions.Center;
//            closeText.color = Color.white;

//            var closeTextRect = closeText.GetComponent<RectTransform>();
//            closeTextRect.anchorMin = Vector2.zero;
//            closeTextRect.anchorMax = Vector2.one;
//            closeTextRect.offsetMin = Vector2.zero;
//            closeTextRect.offsetMax = Vector2.zero;

//            return popup;
//        }
//    }
//}