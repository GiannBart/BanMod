//credits and licenses in the resources folder
using UnityEngine;

namespace BanMod
{
    public static class BanModUiStyles
    {
        // Visual-only shared theme. No menu behaviour lives in this class.
        // Palette and radii mirror the modern Auto Friend Invite UI.

        public static readonly Color WindowColor = new Color(0.055f, 0.06f, 0.075f, 0.985f);
        public static readonly Color PanelColor = new Color(0.095f, 0.105f, 0.13f, 0.98f);
        public static readonly Color ButtonColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        public static readonly Color ButtonHoverColor = new Color(0.10f, 0.28f, 0.42f, 1f);
        public static readonly Color ButtonActiveColor = new Color(0.08f, 0.22f, 0.34f, 1f);
        public static readonly Color AccentColor = new Color(0.10f, 0.45f, 0.85f, 1f);
        public static readonly Color AccentHoverColor = new Color(0.12f, 0.55f, 0.96f, 1f);
        public static readonly Color DangerColor = new Color(0.72f, 0.16f, 0.18f, 1f);
        public static readonly Color DangerHoverColor = new Color(0.82f, 0.20f, 0.22f, 1f);
        public static readonly Color SuccessColor = new Color(0.15f, 0.65f, 0.20f, 1f);
        public static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        public static readonly Color HeaderTextColor = new Color(0.93f, 0.95f, 1f, 1f);
        public static readonly Color MutedTextColor = new Color(0.62f, 0.66f, 0.74f, 1f);

        private static Texture2D windowTex;
        private static Texture2D panelTex;
        private static Texture2D buttonTex;
        private static Texture2D buttonHoverTex;
        private static Texture2D buttonActiveTex;
        private static Texture2D tintableTex;
        private static Texture2D accentTex;
        private static Texture2D accentHoverTex;
        private static Texture2D dangerTex;
        private static Texture2D dangerHoverTex;
        private static Texture2D blueBorderTex;
        private static Texture2D roundedSpriteTex;
        private static Sprite roundedSprite;

        private static GUIStyle blackWindow;
        private static GUIStyle darkBox;
        private static GUIStyle buttonDark;
        private static GUIStyle tintableButton;
        private static GUIStyle accentButton;
        private static GUIStyle dangerButton;
        private static GUIStyle toggleOffDark;
        private static GUIStyle toggleOnBlueOutline;
        private static GUIStyle titleLabel;
        private static GUIStyle headerLabel;
        private static GUIStyle bodyLabel;
        private static GUIStyle mutedLabel;

        private static Texture2D MakeRoundedTex(int size, int radius, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            Color32 fill = color;

            float r = Mathf.Max(1f, radius);
            float leftCenter = r - 0.5f;
            float rightCenter = size - r - 0.5f;
            float bottomCenter = r - 0.5f;
            float topCenter = size - r - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = x;
                    float cy = y;

                    if (x < radius)
                        cx = leftCenter;
                    else if (x >= size - radius)
                        cx = rightCenter;

                    if (y < radius)
                        cy = bottomCenter;
                    else if (y >= size - radius)
                        cy = topCenter;

                    float dx = x - cx;
                    float dy = y - cy;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    bool inCorner =
                        (x < radius || x >= size - radius) &&
                        (y < radius || y >= size - radius);

                    float alphaFactor = inCorner
                        ? Mathf.Clamp01(r + 0.5f - distance)
                        : 1f;

                    Color32 pixel = fill;
                    pixel.a = (byte)Mathf.RoundToInt(fill.a * alphaFactor);
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D MakeRoundedBorderTex(
            int size,
            int radius,
            Color fill,
            Color border,
            int thickness)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            Color32 fill32 = fill;
            Color32 border32 = border;

            float outerR = Mathf.Max(1f, radius);
            float innerR = Mathf.Max(1f, radius - thickness);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float edgeX = Mathf.Min(x + 0.5f, size - x - 0.5f);
                    float edgeY = Mathf.Min(y + 0.5f, size - y - 0.5f);

                    float ox = Mathf.Max(0f, outerR - edgeX);
                    float oy = Mathf.Max(0f, outerR - edgeY);
                    float outerDistance = Mathf.Sqrt(ox * ox + oy * oy);
                    float outerAlpha = Mathf.Clamp01(outerR + 0.5f - outerDistance);

                    float innerEdgeX = edgeX - thickness;
                    float innerEdgeY = edgeY - thickness;
                    float ix = Mathf.Max(0f, innerR - innerEdgeX);
                    float iy = Mathf.Max(0f, innerR - innerEdgeY);
                    float innerDistance = Mathf.Sqrt(ix * ix + iy * iy);
                    float innerAlpha = Mathf.Clamp01(innerR + 0.5f - innerDistance);

                    bool borderPixel =
                        edgeX <= thickness ||
                        edgeY <= thickness ||
                        innerAlpha < 0.99f;

                    Color32 pixel = borderPixel ? border32 : fill32;
                    pixel.a = (byte)Mathf.RoundToInt(pixel.a * outerAlpha);
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static RectOffset Border(int value)
        {
            RectOffset offset = new RectOffset();
            offset.left = value;
            offset.right = value;
            offset.top = value;
            offset.bottom = value;
            return offset;
        }

        private static RectOffset Padding(int horizontal, int vertical)
        {
            RectOffset offset = new RectOffset();
            offset.left = horizontal;
            offset.right = horizontal;
            offset.top = vertical;
            offset.bottom = vertical;
            return offset;
        }

        private static void EnsureTextures()
        {
            if (windowTex == null)
                windowTex = MakeRoundedTex(64, 18, WindowColor);

            if (panelTex == null)
                panelTex = MakeRoundedTex(48, 14, PanelColor);

            if (buttonTex == null)
                buttonTex = MakeRoundedTex(40, 12, ButtonColor);

            if (buttonHoverTex == null)
                buttonHoverTex = MakeRoundedTex(40, 12, ButtonHoverColor);

            if (buttonActiveTex == null)
                buttonActiveTex = MakeRoundedTex(40, 12, ButtonActiveColor);

            if (tintableTex == null)
                tintableTex = MakeRoundedTex(40, 12, Color.white);

            if (accentTex == null)
                accentTex = MakeRoundedTex(40, 12, AccentColor);

            if (accentHoverTex == null)
                accentHoverTex = MakeRoundedTex(40, 12, AccentHoverColor);

            if (dangerTex == null)
                dangerTex = MakeRoundedTex(40, 12, DangerColor);

            if (dangerHoverTex == null)
                dangerHoverTex = MakeRoundedTex(40, 12, DangerHoverColor);

            if (blueBorderTex == null)
                blueBorderTex = MakeRoundedBorderTex(
                    48,
                    14,
                    PanelColor,
                    AccentColor,
                    3
                );
        }

        // Rounded white sprite for UnityEngine.UI.Image based panels/buttons.
        // Intended strictly as a visual background; callers keep their own events/layout.
        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null)
                {
                    roundedSpriteTex = MakeRoundedTex(64, 14, Color.white);
                    roundedSprite = Sprite.Create(
                        roundedSpriteTex,
                        new Rect(0f, 0f, roundedSpriteTex.width, roundedSpriteTex.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect,
                        new Vector4(14f, 14f, 14f, 14f)
                    );
                    roundedSprite.hideFlags = HideFlags.HideAndDontSave;
                }

                return roundedSprite;
            }
        }

        public static GUIStyle BlackWindow
        {
            get
            {
                EnsureTextures();

                if (blackWindow == null || blackWindow.normal.background == null)
                {
                    blackWindow = new GUIStyle(GUI.skin.window);

                    blackWindow.normal.background = windowTex;
                    blackWindow.onNormal.background = windowTex;
                    blackWindow.hover.background = windowTex;
                    blackWindow.onHover.background = windowTex;
                    blackWindow.active.background = windowTex;
                    blackWindow.onActive.background = windowTex;
                    blackWindow.focused.background = windowTex;
                    blackWindow.onFocused.background = windowTex;

                    blackWindow.normal.textColor = Color.white;
                    blackWindow.onNormal.textColor = Color.white;

                    blackWindow.border = Border(18);
                    blackWindow.padding = Padding(14, 14);
                }

                return blackWindow;
            }
        }

        public static GUIStyle DarkBox
        {
            get
            {
                EnsureTextures();

                if (darkBox == null || darkBox.normal.background == null)
                {
                    darkBox = new GUIStyle(GUI.skin.box);

                    darkBox.normal.background = panelTex;
                    darkBox.onNormal.background = panelTex;
                    darkBox.hover.background = panelTex;
                    darkBox.onHover.background = panelTex;
                    darkBox.active.background = panelTex;
                    darkBox.onActive.background = panelTex;
                    darkBox.focused.background = panelTex;
                    darkBox.onFocused.background = panelTex;

                    darkBox.normal.textColor = Color.white;
                    darkBox.onNormal.textColor = Color.white;

                    darkBox.border = Border(14);
                    darkBox.padding = Padding(12, 10);
                }

                return darkBox;
            }
        }

        public static GUIStyle ButtonDark
        {
            get
            {
                EnsureTextures();

                if (buttonDark == null || buttonDark.normal.background == null)
                {
                    buttonDark = new GUIStyle(GUI.skin.button);

                    buttonDark.normal.background = buttonTex;
                    buttonDark.hover.background = buttonHoverTex;
                    buttonDark.active.background = buttonActiveTex;
                    buttonDark.focused.background = buttonTex;
                    buttonDark.onNormal.background = buttonTex;
                    buttonDark.onHover.background = buttonHoverTex;
                    buttonDark.onActive.background = buttonActiveTex;
                    buttonDark.onFocused.background = buttonTex;

                    SetWhiteText(buttonDark);
                    buttonDark.alignment = TextAnchor.MiddleCenter;
                    buttonDark.border = Border(12);
                    buttonDark.padding = Padding(12, 6);
                }

                return buttonDark;
            }
        }

        // White rounded base intended for code that already uses GUI.backgroundColor.
        public static GUIStyle TintableButton
        {
            get
            {
                EnsureTextures();

                if (tintableButton == null || tintableButton.normal.background == null)
                {
                    tintableButton = new GUIStyle(GUI.skin.button);

                    tintableButton.normal.background = tintableTex;
                    tintableButton.hover.background = tintableTex;
                    tintableButton.active.background = tintableTex;
                    tintableButton.focused.background = tintableTex;
                    tintableButton.onNormal.background = tintableTex;
                    tintableButton.onHover.background = tintableTex;
                    tintableButton.onActive.background = tintableTex;
                    tintableButton.onFocused.background = tintableTex;

                    SetWhiteText(tintableButton);
                    tintableButton.alignment = TextAnchor.MiddleCenter;
                    tintableButton.border = Border(12);
                    tintableButton.padding = Padding(12, 6);
                }

                return tintableButton;
            }
        }

        public static GUIStyle AccentButton
        {
            get
            {
                EnsureTextures();

                if (accentButton == null || accentButton.normal.background == null)
                {
                    accentButton = new GUIStyle(ButtonDark);
                    accentButton.normal.background = accentTex;
                    accentButton.hover.background = accentHoverTex;
                    accentButton.active.background = accentHoverTex;
                    accentButton.focused.background = accentTex;
                    accentButton.onNormal.background = accentTex;
                    accentButton.onHover.background = accentHoverTex;
                    accentButton.onActive.background = accentHoverTex;
                    accentButton.onFocused.background = accentTex;
                }

                return accentButton;
            }
        }

        public static GUIStyle DangerButton
        {
            get
            {
                EnsureTextures();

                if (dangerButton == null || dangerButton.normal.background == null)
                {
                    dangerButton = new GUIStyle(ButtonDark);
                    dangerButton.normal.background = dangerTex;
                    dangerButton.hover.background = dangerHoverTex;
                    dangerButton.active.background = dangerHoverTex;
                    dangerButton.focused.background = dangerTex;
                    dangerButton.onNormal.background = dangerTex;
                    dangerButton.onHover.background = dangerHoverTex;
                    dangerButton.onActive.background = dangerHoverTex;
                    dangerButton.onFocused.background = dangerTex;
                }

                return dangerButton;
            }
        }

        public static GUIStyle ToggleOffDark
        {
            get
            {
                if (toggleOffDark == null)
                {
                    toggleOffDark = new GUIStyle(ButtonDark);
                    toggleOffDark.fontStyle = FontStyle.Normal;
                }

                return toggleOffDark;
            }
        }

        public static GUIStyle ToggleOnBlueOutline
        {
            get
            {
                EnsureTextures();

                if (toggleOnBlueOutline == null ||
                    toggleOnBlueOutline.normal.background == null)
                {
                    toggleOnBlueOutline = new GUIStyle(ButtonDark);

                    toggleOnBlueOutline.normal.background = blueBorderTex;
                    toggleOnBlueOutline.hover.background = blueBorderTex;
                    toggleOnBlueOutline.active.background = blueBorderTex;
                    toggleOnBlueOutline.focused.background = blueBorderTex;
                    toggleOnBlueOutline.onNormal.background = blueBorderTex;
                    toggleOnBlueOutline.onHover.background = blueBorderTex;
                    toggleOnBlueOutline.onActive.background = blueBorderTex;
                    toggleOnBlueOutline.onFocused.background = blueBorderTex;

                    toggleOnBlueOutline.fontStyle = FontStyle.Bold;
                    toggleOnBlueOutline.alignment = TextAnchor.MiddleCenter;
                }

                return toggleOnBlueOutline;
            }
        }

        public static GUIStyle TitleLabel
        {
            get
            {
                if (titleLabel == null)
                {
                    titleLabel = new GUIStyle(GUI.skin.label);
                    titleLabel.fontSize = 22;
                    titleLabel.fontStyle = FontStyle.Bold;
                    titleLabel.alignment = TextAnchor.MiddleCenter;
                    titleLabel.normal.textColor = Color.white;
                }

                return titleLabel;
            }
        }

        public static GUIStyle HeaderLabel
        {
            get
            {
                if (headerLabel == null)
                {
                    headerLabel = new GUIStyle(GUI.skin.label);
                    headerLabel.fontSize = 16;
                    headerLabel.fontStyle = FontStyle.Bold;
                    headerLabel.alignment = TextAnchor.MiddleLeft;
                    headerLabel.normal.textColor = HeaderTextColor;
                }

                return headerLabel;
            }
        }

        public static GUIStyle BodyLabel
        {
            get
            {
                if (bodyLabel == null)
                {
                    bodyLabel = new GUIStyle(GUI.skin.label);
                    bodyLabel.fontSize = 13;
                    bodyLabel.alignment = TextAnchor.MiddleLeft;
                    bodyLabel.wordWrap = true;
                    bodyLabel.normal.textColor = TextColor;
                }

                return bodyLabel;
            }
        }

        public static GUIStyle MutedLabel
        {
            get
            {
                if (mutedLabel == null)
                {
                    mutedLabel = new GUIStyle(BodyLabel);
                    mutedLabel.fontSize = 12;
                    mutedLabel.normal.textColor = MutedTextColor;
                }

                return mutedLabel;
            }
        }

        private static void SetWhiteText(GUIStyle style)
        {
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            style.focused.textColor = Color.white;
            style.onNormal.textColor = Color.white;
            style.onHover.textColor = Color.white;
            style.onActive.textColor = Color.white;
            style.onFocused.textColor = Color.white;
        }
    }
}
