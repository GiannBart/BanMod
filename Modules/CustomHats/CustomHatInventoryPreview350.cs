//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BanMod.Modules.CustomHats
{
    public static class CustomHatInventoryPreview350
    {
        private const int CanvasSize = 350;
        private const float AlphaThreshold = 0.01f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, HatViewData> ViewDataCache = new Dictionary<string, HatViewData>();

        public static Sprite GetPreviewSprite(string cacheKey, Sprite originalSprite)
        {
            if (originalSprite == null)
                return null;

            if (string.IsNullOrEmpty(cacheKey))
                cacheKey = originalSprite.GetInstanceID().ToString();

            string finalKey = cacheKey + "|preview350|" + originalSprite.GetInstanceID();

            if (SpriteCache.TryGetValue(finalKey, out Sprite cached))
                return cached;

            Sprite preview = CreateCentered350Sprite(originalSprite);
            if (preview != null)
                SpriteCache[finalKey] = preview;

            return preview;
        }

        public static HatViewData GetPreviewViewData(string hatId, HatViewData source)
        {
            if (source == null)
                return null;

            if (string.IsNullOrEmpty(hatId))
                hatId = source.GetInstanceID().ToString();

            if (ViewDataCache.TryGetValue(hatId, out HatViewData cached))
                return cached;

            HatViewData preview = ScriptableObject.CreateInstance<HatViewData>();
            preview.name = hatId + "_InventoryPreview350";

            preview.MainImage = GetPreviewSprite(hatId + "|main", source.MainImage);
            preview.BackImage = GetPreviewSprite(hatId + "|back", source.BackImage);
            preview.LeftMainImage = GetPreviewSprite(hatId + "|leftMain", source.LeftMainImage);
            preview.LeftBackImage = GetPreviewSprite(hatId + "|leftBack", source.LeftBackImage);
            preview.FloorImage = GetPreviewSprite(hatId + "|floor", source.FloorImage != null ? source.FloorImage : source.MainImage);
            preview.LeftFloorImage = GetPreviewSprite(hatId + "|leftFloor", source.LeftFloorImage != null ? source.LeftFloorImage : source.LeftMainImage);
            preview.ClimbImage = GetPreviewSprite(hatId + "|climb", source.ClimbImage);
            preview.LeftClimbImage = GetPreviewSprite(hatId + "|leftClimb", source.LeftClimbImage);
            preview.MatchPlayerColor = source.MatchPlayerColor;
            preview.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            ViewDataCache[hatId] = preview;
            return preview;
        }

        private static Sprite CreateCentered350Sprite(Sprite sourceSprite)
        {
            try
            {
                if (sourceSprite == null || sourceSprite.texture == null)
                    return null;

                Texture2D sourceTexture = sourceSprite.texture;
                Rect rect = sourceSprite.textureRect;

                int srcX = Mathf.RoundToInt(rect.x);
                int srcY = Mathf.RoundToInt(rect.y);
                int srcW = Mathf.RoundToInt(rect.width);
                int srcH = Mathf.RoundToInt(rect.height);

                if (srcW <= 0 || srcH <= 0)
                    return sourceSprite;

                Color[] sourcePixels = sourceTexture.GetPixels(srcX, srcY, srcW, srcH);
                RectInt alphaBounds = GetAlphaBounds(sourcePixels, srcW, srcH);

                Texture2D canvas = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false);
                Color[] clear = new Color[CanvasSize * CanvasSize];
                for (int i = 0; i < clear.Length; i++)
                    clear[i] = new Color(0f, 0f, 0f, 0f);
                canvas.SetPixels(clear);

                float visibleCenterX = alphaBounds.x + alphaBounds.width * 0.5f;
                float visibleCenterY = alphaBounds.y + alphaBounds.height * 0.5f;

                int offsetX = Mathf.RoundToInt((CanvasSize * 0.5f) - visibleCenterX);
                int offsetY = Mathf.RoundToInt((CanvasSize * 0.5f) - visibleCenterY);

                for (int y = 0; y < srcH; y++)
                {
                    for (int x = 0; x < srcW; x++)
                    {
                        int dstX = x + offsetX;
                        int dstY = y + offsetY;

                        if (dstX < 0 || dstX >= CanvasSize || dstY < 0 || dstY >= CanvasSize)
                            continue;

                        canvas.SetPixel(dstX, dstY, sourcePixels[y * srcW + x]);
                    }
                }

                canvas.Apply();
                canvas.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                Sprite sprite = Sprite.Create(
                    canvas,
                    new Rect(0, 0, CanvasSize, CanvasSize),
                    new Vector2(0.5f, 0.5f),
                    sourceSprite.pixelsPerUnit
                );

                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                return sprite;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] Failed to create inventory preview 350 sprite: " + ex);
                return sourceSprite;
            }
        }

        private static RectInt GetAlphaBounds(Color[] pixels, int width, int height)
        {
            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = pixels[y * width + x];

                    if (c.a <= AlphaThreshold)
                        continue;

                    found = true;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (!found)
                return new RectInt(0, 0, width, height);

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
