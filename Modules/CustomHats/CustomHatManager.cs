//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using static BanMod.Utils;

namespace BanMod.Modules.CustomHats
{
    public static class CustomHatManager
    {
        public static readonly List<CustomHat> PendingHats = new List<CustomHat>();
        public static readonly List<HatData> RegisteredHats = new List<HatData>();

        public static readonly Dictionary<string, HatViewData> ViewDataCache = new Dictionary<string, HatViewData>();
        public static readonly Dictionary<string, HatExtension> ExtensionCache = new Dictionary<string, HatExtension>();
        private static readonly Dictionary<string, Sprite> AdaptiveSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, HatViewData> AdaptiveViewCache = new Dictionary<string, HatViewData>();
        public const int HatCanvasSize = 350;
        public const float HatPixelsPerUnit = 112.5f;
        private static readonly Dictionary<string, Sprite> HatSpriteCache = new Dictionary<string, Sprite>();

        private const string BanDataDirectory = "./BAN_DATA/IMAGE";
        private const string CustomHatsDirectory = "./BAN_DATA/IMAGE/CustomHats";
        private const string ExternalManifestPath = "./BAN_DATA/IMAGE/CustomHats/hats.json";

        private const string EmbeddedManifestPath = "BanMod.Resources.image.hat.hats.json";
        private const string EmbeddedResourcePrefix = "BanMod.Resources.image.hat.";

        public static void InitEmbeddedHats()
        {
            PendingHats.Clear();
            LoadInternalHatsFromDll();
        }

        private static void EnsureExternalFolder()
        {
            try
            {
                if (!Directory.Exists(BanDataDirectory))
                    Directory.CreateDirectory(BanDataDirectory);

                if (!Directory.Exists(CustomHatsDirectory))
                    Directory.CreateDirectory(CustomHatsDirectory);
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] Failed to create CustomHats folder: " + ex);
            }
        }

        private static void LoadInternalHatsFromDll()
        {
            try
            {
                using Stream stream = typeof(CustomHatManager).Assembly.GetManifestResourceStream(EmbeddedManifestPath);

                if (stream == null)
                    return;

                using StreamReader reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                CustomHatsConfig config = JsonSerializer.Deserialize<CustomHatsConfig>(json, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true
                });

                if (config == null || config.Hats == null)
                    return;

                for (int i = 0; i < config.Hats.Count; i++)
                {
                    CustomHat hat = config.Hats[i];

                    NormalizeExternalHat(hat);

                    MakeHatResourcesEmbedded(hat);

                    AddPendingHat(hat);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] Failed to load embedded hats.json: " + ex);
            }
        }

        private static void LoadExternalHatsOnly()
        {
            try
            {
                if (!File.Exists(ExternalManifestPath))
                    return;

                string json = File.ReadAllText(ExternalManifestPath);

                CustomHatsConfig config = JsonSerializer.Deserialize<CustomHatsConfig>(json, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true
                });

                if (config == null || config.Hats == null)
                    return;

                for (int i = 0; i < config.Hats.Count; i++)
                {
                    CustomHat hat = config.Hats[i];

                    NormalizeExternalHat(hat);

                    AddPendingHat(hat);
                }
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] Failed to load external hats.json: " + ex);
            }
        }

        private static void MakeHatResourcesEmbedded(CustomHat hat)
        {
            if (hat == null)
                return;

            hat.Resource = MakeEmbeddedPath(hat.Resource);
            hat.FlipResource = MakeEmbeddedPath(hat.FlipResource);
            hat.BackResource = MakeEmbeddedPath(hat.BackResource);
            hat.BackFlipResource = MakeEmbeddedPath(hat.BackFlipResource);
            hat.ClimbResource = MakeEmbeddedPath(hat.ClimbResource);
        }

        private static string MakeEmbeddedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (path.StartsWith("BanMod.Resources.", StringComparison.OrdinalIgnoreCase))
                return path;

            string safePath = path.Replace("\\", ".").Replace("/", ".").Trim('.');

            return EmbeddedResourcePrefix + safePath;
        }

        private static void NormalizeExternalHat(CustomHat hat)
        {
            if (hat == null)
                return;

            if (string.IsNullOrEmpty(hat.Package))
                hat.Package = "Modded";

            if (string.IsNullOrEmpty(hat.ProductId) && !string.IsNullOrEmpty(hat.Name))
                hat.ProductId = "hat_banmod_" + SanitizeId(hat.Name);

            if (string.IsNullOrEmpty(hat.Author))
                hat.Author = "Unknown";

            if (string.IsNullOrEmpty(hat.Name))
                hat.Name = hat.ProductId;

            if (hat.Adaptive && hat.ColorVariations)
            {
                hat.Adaptive = false;
            }
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "custom";

            string result = value.ToLowerInvariant().Trim();

            char[] chars = result.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if ((c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_')
                {
                    continue;
                }

                chars[i] = '_';
            }

            return new string(chars);
        }

        private static void AddPendingHat(CustomHat hat)
        {
            if (hat == null || string.IsNullOrEmpty(hat.ProductId))
                return;

            for (int i = 0; i < PendingHats.Count; i++)
            {
                CustomHat existing = PendingHats[i];

                if (existing != null && existing.ProductId == hat.ProductId)
                    return;
            }

            PendingHats.Add(hat);
        }

        public static HatData CreateHatBehaviour(CustomHat customHat)
        {
            try
            {
                if (customHat == null)
                    return null;

                Sprite mainSprite = LoadHatSprite(customHat.Resource);

                if (mainSprite == null)
                {
                    BMLogger.Error("[CustomHats] Missing DEFAULT/resource sprite for " + customHat.Name + ": " + customHat.Resource);
                    return null;
                }

                Sprite backSprite = LoadHatSprite(customHat.BackResource);
                Sprite flipSprite = LoadHatSprite(customHat.FlipResource);
                Sprite backFlipSprite = LoadHatSprite(customHat.BackFlipResource);
                Sprite climbSprite = LoadHatSprite(customHat.ClimbResource);


                HatViewData viewData = ScriptableObject.CreateInstance<HatViewData>();
                viewData.name = customHat.ProductId + "_ViewData";

                viewData.MainImage = mainSprite;
                viewData.BackImage = backSprite;
                viewData.LeftMainImage = flipSprite;
                viewData.LeftBackImage = backFlipSprite;
                viewData.FloorImage = mainSprite;
                viewData.LeftFloorImage = flipSprite;

                if (climbSprite != null)
                {
                    viewData.ClimbImage = climbSprite;
                    viewData.LeftClimbImage = climbSprite;
                }
                else
                {
                    viewData.ClimbImage = backSprite != null ? backSprite : mainSprite;
                    viewData.LeftClimbImage = backFlipSprite != null ? backFlipSprite : viewData.ClimbImage;
                }

                viewData.MatchPlayerColor = customHat.Adaptive;
                viewData.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                HatData hat = ScriptableObject.CreateInstance<HatData>();
                hat.name = customHat.Name;
                hat.ProductId = customHat.ProductId;
                hat.BundleId = "";
                hat.Free = true;
                hat.NotInStore = true;
                hat.displayOrder = -999;
                hat.ChipOffset = new Vector2(0f, 0.2f);

                hat.InFront = !customHat.Behind;
                hat.NoBounce = !customHat.Bounce;
                hat.BlocksVisors = customHat.BlocksVisors;
                hat.StoreName = customHat.Package;
                hat.PreviewCrewmateColor = false;

                hat.ViewDataRef = null;
                hat.PreviewData = null;
                hat.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                if (customHat.Adaptive && customHat.ColorVariations)
                {
                    customHat.Adaptive = false;
                }
                HatExtension extension = new HatExtension
                {
                    Author = customHat.Author,
                    Package = customHat.Package,
                    Adaptive = customHat.Adaptive,
                    ColorVariations = customHat.ColorVariations,

                    BaseResourcePath = customHat.Resource != null ? customHat.Resource.Replace(".png", "") : "",
                    BaseBackResourcePath = customHat.BackResource != null ? customHat.BackResource.Replace(".png", "") : "",
                    BaseClimbResourcePath = customHat.ClimbResource != null ? customHat.ClimbResource.Replace(".png", "") : "",
                    BaseFlipResourcePath = customHat.FlipResource != null ? customHat.FlipResource.Replace(".png", "") : "",
                    BaseBackFlipResourcePath = customHat.BackFlipResource != null ? customHat.BackFlipResource.Replace(".png", "") : "",

                    FlipImage = flipSprite,
                    BackFlipImage = backFlipSprite
                };

                ViewDataCache[hat.ProdId] = viewData;
                ExtensionCache[hat.ProdId] = extension;

                return hat;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] CreateHatBehaviour failed for " + customHat.Name + ": " + ex);
                return null;
            }
        }
        private static Sprite RecolorAdaptive(Sprite sprite, int colorId)
        {
            if (sprite == null)
                return null;

            string key = sprite.GetInstanceID() + "_" + colorId;

            if (AdaptiveSpriteCache.TryGetValue(key, out Sprite cached))
            {
                if (cached != null && cached.texture != null)
                    return cached;
            }

            try
            {
                if (colorId < 0 || colorId >= Palette.PlayerColors.Length)
                    colorId = 0;

                Color32 primary = Palette.PlayerColors[colorId];
                Color32 secondary = Palette.ShadowColors[colorId];

                Texture2D src = sprite.texture;
                if (src == null)
                    return sprite;

                int width = src.width;
                int height = src.height;

                Color[] colors = src.GetPixels();

                for (int i = 0; i < colors.Length; i++)
                {
                    Color32 p = colors[i];

                    if (p.a == 0)
                        continue;

                    if (p.r > 240 && p.g < 15 && p.b < 15)
                    {
                        colors[i] = new Color32(primary.r, primary.g, primary.b, p.a);
                    }
                    else if (p.b > 240 && p.r < 15 && p.g < 15)
                    {
                        colors[i] = new Color32(secondary.r, secondary.g, secondary.b, p.a);
                    }
                }

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels(colors);
                tex.Apply();

                tex.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                Sprite result = Sprite.Create(
                    tex,
                    new Rect(0, 0, width, height),
                    new Vector2(0.5f, 0.5f),
                    sprite.pixelsPerUnit);

                result.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                AdaptiveSpriteCache[key] = result;

                return result;
            }
            catch (Exception ex)
            {
                BMLogger.Error("[CustomHats] RecolorAdaptive failed: " + ex);
                return sprite;
            }
        }

        public static HatViewData GetAdaptiveViewData(string prodId, HatViewData originalViewData, int colorId)
        {
            if (string.IsNullOrEmpty(prodId) || originalViewData == null)
                return originalViewData;

            if (!ExtensionCache.TryGetValue(prodId, out HatExtension ext))
                return originalViewData;

            if (!ext.Adaptive || ext.ColorVariations)
                return originalViewData;

            string key = prodId + "_adaptive_" + colorId;

            if (AdaptiveViewCache.TryGetValue(key, out HatViewData cached))
            {
                if (cached != null)
                    return cached;
            }

            HatViewData newView = ScriptableObject.CreateInstance<HatViewData>();
            newView.name = key;
            newView.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            newView.MainImage = RecolorAdaptive(originalViewData.MainImage, colorId);
            newView.BackImage = RecolorAdaptive(originalViewData.BackImage, colorId);
            newView.LeftMainImage = RecolorAdaptive(originalViewData.LeftMainImage, colorId);
            newView.LeftBackImage = RecolorAdaptive(originalViewData.LeftBackImage, colorId);
            newView.ClimbImage = RecolorAdaptive(originalViewData.ClimbImage, colorId);

            newView.FloorImage = newView.MainImage;
            newView.LeftFloorImage = newView.LeftMainImage;

            newView.LeftClimbImage = RecolorAdaptive(originalViewData.LeftClimbImage, colorId);
            if (newView.LeftClimbImage == null)
                newView.LeftClimbImage = newView.ClimbImage;

            newView.MatchPlayerColor = originalViewData.MatchPlayerColor;

            AdaptiveViewCache[key] = newView;

            return newView;
        }
        public static Sprite LoadHatSprite(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string resourcePath = path.StartsWith("BanMod.Resources.", StringComparison.OrdinalIgnoreCase)
                ? path
                : MakeEmbeddedPath(path);

            string cacheKey = resourcePath + "|" + HatCanvasSize + "|" + HatPixelsPerUnit;
            if (HatSpriteCache.TryGetValue(cacheKey, out Sprite cached))
                return cached;

            Texture2D source = BMImage.LoadTextureFromResources(resourcePath);
            if (source == null)
                return null;

            Texture2D canvas = CenterTextureOn350Canvas(source);
            if (canvas == null)
                return null;

            Sprite sprite = Sprite.Create(
                canvas,
                new Rect(0, 0, canvas.width, canvas.height),
                new Vector2(0.5f, 0.5f),
                HatPixelsPerUnit
            );

            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            HatSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Texture2D CenterTextureOn350Canvas(Texture2D source)
        {
            if (source == null)
                return null;

            if (source.width == HatCanvasSize && source.height == HatCanvasSize)
            {
                source.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                return source;
            }

            Texture2D canvas = new Texture2D(HatCanvasSize, HatCanvasSize, TextureFormat.ARGB32, false);
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[HatCanvasSize * HatCanvasSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;
            canvas.SetPixels32(pixels);

            int copyWidth = Mathf.Min(source.width, HatCanvasSize);
            int copyHeight = Mathf.Min(source.height, HatCanvasSize);

            int sourceStartX = Mathf.Max(0, (source.width - copyWidth) / 2);
            int sourceStartY = Mathf.Max(0, (source.height - copyHeight) / 2);
            int destStartX = (HatCanvasSize - copyWidth) / 2;
            int destStartY = (HatCanvasSize - copyHeight) / 2;

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    Color px = source.GetPixel(sourceStartX + x, sourceStartY + y);
                    canvas.SetPixel(destStartX + x, destStartY + y, px);
                }
            }

            canvas.Apply();
            canvas.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return canvas;
        }

        public static bool TryGetViewData(HatData hat, out HatViewData viewData)
        {
            viewData = null;

            if (hat == null || string.IsNullOrEmpty(hat.ProdId))
                return false;

            return ViewDataCache.TryGetValue(hat.ProdId, out viewData);
        }

        public static bool TryGetViewData(string prodId, out HatViewData viewData)
        {
            viewData = null;

            if (string.IsNullOrEmpty(prodId))
                return false;

            return ViewDataCache.TryGetValue(prodId, out viewData);
        }

        public static bool IsCustomHat(HatData hat)
        {
            if (hat == null || string.IsNullOrEmpty(hat.ProdId))
                return false;

            return ViewDataCache.ContainsKey(hat.ProdId);
        }
        public static HatViewData GetViewDataForColor(string prodId, HatViewData originalViewData, int colorId)
        {
            if (string.IsNullOrEmpty(prodId) || originalViewData == null)
                return originalViewData;

            if (!ExtensionCache.TryGetValue(prodId, out HatExtension ext))
                return originalViewData;

            if (ext.Adaptive)
            {
                return GetAdaptiveViewData(
                    prodId,
                    originalViewData,
                    colorId);
            }

            if (!ext.ColorVariations)
                return originalViewData;

            string colorCacheKey = prodId + "_color_" + colorId;
            if (ViewDataCache.TryGetValue(colorCacheKey, out HatViewData coloredView))
                return coloredView;

            Sprite coloredMain = LoadHatSprite(ext.BaseResourcePath + "_" + colorId + ".png");
            coloredMain = coloredMain != null ? coloredMain : originalViewData.MainImage;

            Sprite coloredBack = null;
            if (!string.IsNullOrEmpty(ext.BaseBackResourcePath))
                coloredBack = LoadHatSprite(ext.BaseBackResourcePath + "_" + colorId + ".png");
            coloredBack = coloredBack != null ? coloredBack : originalViewData.BackImage;

            Sprite coloredClimb = null;
            if (!string.IsNullOrEmpty(ext.BaseClimbResourcePath))
                coloredClimb = LoadHatSprite(ext.BaseClimbResourcePath + "_" + colorId + ".png");
            coloredClimb = coloredClimb != null ? coloredClimb : originalViewData.ClimbImage;

            Sprite coloredFlip = null;
            if (!string.IsNullOrEmpty(ext.BaseFlipResourcePath))
                coloredFlip = LoadHatSprite(ext.BaseFlipResourcePath + "_" + colorId + ".png");
            coloredFlip = coloredFlip != null ? coloredFlip : originalViewData.LeftMainImage;

            Sprite coloredBackFlip = null;
            if (!string.IsNullOrEmpty(ext.BaseBackFlipResourcePath))
                coloredBackFlip = LoadHatSprite(ext.BaseBackFlipResourcePath + "_" + colorId + ".png");
            coloredBackFlip = coloredBackFlip != null ? coloredBackFlip : originalViewData.LeftBackImage;

            HatViewData newView = ScriptableObject.CreateInstance<HatViewData>();
            newView.name = prodId + "_ViewData_" + colorId;

            newView.MainImage = coloredMain;
            newView.BackImage = coloredBack;
            newView.FloorImage = coloredMain;
            newView.ClimbImage = coloredClimb;
            newView.LeftMainImage = coloredFlip;
            newView.LeftBackImage = coloredBackFlip;

            newView.LeftFloorImage = coloredFlip;
            newView.LeftClimbImage = coloredClimb != null ? coloredClimb : (coloredBack != null ? coloredBack : coloredMain);

            newView.MatchPlayerColor = originalViewData.MatchPlayerColor;
            newView.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            ViewDataCache[colorCacheKey] = newView;
            return newView;
        }
    }
}