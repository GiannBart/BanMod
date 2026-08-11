//credits and licenses in the resources folder
using UnityEngine;

namespace BanMod
{
    public static class BanModUiStyles
    {
        private static Texture2D blackTex;
        private static Texture2D darkTex;
        private static Texture2D blueBorderTex;
        private static Texture2D blueTex;
        private static Texture2D redTex;

        private static GUIStyle blackWindow;
        private static GUIStyle darkBox;
        private static GUIStyle buttonDark;
        private static GUIStyle toggleOffDark;
        private static GUIStyle toggleOnBlueOutline;

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Texture2D MakeBorderTex(int width, int height, Color fill, Color border, int thickness)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < thickness || y < thickness || x >= width - thickness || y >= height - thickness;
                    pixels[y * width + x] = isBorder ? border : fill;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static void EnsureTextures()
        {
            if (blackTex == null) blackTex = MakeTex(2, 2, Color.black);
            if (darkTex == null) darkTex = MakeTex(2, 2, new Color(0.06f, 0.06f, 0.06f, 1f));
            if (blueTex == null) blueTex = MakeTex(2, 2, new Color(0.02f, 0.10f, 0.18f, 1f));
            if (redTex == null) redTex = MakeTex(2, 2, new Color(0.80f, 0f, 0f, 1f));
            if (blueBorderTex == null) blueBorderTex = MakeBorderTex(32, 32, new Color(0.02f, 0.02f, 0.02f, 1f), new Color(0f, 0.55f, 1f, 1f), 3);
        }

        public static GUIStyle BlackWindow
        {
            get
            {
                EnsureTextures();
                if (blackWindow == null || blackWindow.normal.background == null)
                {
                    blackWindow = new GUIStyle(GUI.skin.window);
                    blackWindow.normal.background = blackTex;
                    blackWindow.onNormal.background = blackTex;
                    blackWindow.hover.background = blackTex;
                    blackWindow.onHover.background = blackTex;
                    blackWindow.active.background = blackTex;
                    blackWindow.onActive.background = blackTex;
                    blackWindow.focused.background = blackTex;
                    blackWindow.onFocused.background = blackTex;
                    blackWindow.normal.textColor = Color.white;
                    blackWindow.onNormal.textColor = Color.white;
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
                    darkBox.normal.background = darkTex;
                    darkBox.onNormal.background = darkTex;
                    darkBox.hover.background = darkTex;
                    darkBox.onHover.background = darkTex;
                    darkBox.active.background = darkTex;
                    darkBox.onActive.background = darkTex;
                    darkBox.normal.textColor = Color.white;
                    darkBox.onNormal.textColor = Color.white;
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
                    buttonDark.normal.background = darkTex;
                    buttonDark.hover.background = blueTex;
                    buttonDark.active.background = blueTex;
                    buttonDark.focused.background = darkTex;
                    buttonDark.onNormal.background = darkTex;
                    buttonDark.onHover.background = blueTex;
                    buttonDark.onActive.background = blueTex;
                    buttonDark.onFocused.background = darkTex;
                    buttonDark.normal.textColor = Color.white;
                    buttonDark.hover.textColor = Color.white;
                    buttonDark.active.textColor = Color.white;
                    buttonDark.focused.textColor = Color.white;
                    buttonDark.onNormal.textColor = Color.white;
                    buttonDark.onHover.textColor = Color.white;
                    buttonDark.onActive.textColor = Color.white;
                    buttonDark.onFocused.textColor = Color.white;
                    buttonDark.alignment = TextAnchor.MiddleCenter;
                }
                return buttonDark;
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
                if (toggleOnBlueOutline == null || toggleOnBlueOutline.normal.background == null)
                {
                    toggleOnBlueOutline = new GUIStyle(GUI.skin.button);
                    toggleOnBlueOutline.normal.background = blueBorderTex;
                    toggleOnBlueOutline.hover.background = blueBorderTex;
                    toggleOnBlueOutline.active.background = blueBorderTex;
                    toggleOnBlueOutline.focused.background = blueBorderTex;
                    toggleOnBlueOutline.onNormal.background = blueBorderTex;
                    toggleOnBlueOutline.onHover.background = blueBorderTex;
                    toggleOnBlueOutline.onActive.background = blueBorderTex;
                    toggleOnBlueOutline.onFocused.background = blueBorderTex;
                    toggleOnBlueOutline.normal.textColor = Color.white;
                    toggleOnBlueOutline.hover.textColor = Color.white;
                    toggleOnBlueOutline.active.textColor = Color.white;
                    toggleOnBlueOutline.focused.textColor = Color.white;
                    toggleOnBlueOutline.onNormal.textColor = Color.white;
                    toggleOnBlueOutline.onHover.textColor = Color.white;
                    toggleOnBlueOutline.onActive.textColor = Color.white;
                    toggleOnBlueOutline.onFocused.textColor = Color.white;
                    toggleOnBlueOutline.fontStyle = FontStyle.Bold;
                    toggleOnBlueOutline.alignment = TextAnchor.MiddleCenter;
                }
                return toggleOnBlueOutline;
            }
        }
    }
}
