//credits and licenses in the resources folder
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
        private static readonly HashSet<string> FullyLoadedHatViews = new HashSet<string>();
        private static readonly HashSet<string> FullyLoadedColorViews = new HashSet<string>();
        private static readonly Dictionary<string, string> ExternalAssetPathIndex =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string BanDataDirectory = Path.Combine(BepInEx.Paths.GameRootPath, "BAN_DATA", "IMAGE");
        private static readonly string CustomHatsDirectory = Path.Combine(BanDataDirectory, "CustomHats");
        private static readonly string ExternalManifestPath = Path.Combine(CustomHatsDirectory, "hats.json");

        private const string CustomHatsServerBaseUrl = "https://server.banmod.online/public/custom-hats";
        private static readonly HttpClient CustomHatsHttpClient = CreateHttpClient();

        private const string EmbeddedManifestPath = "BanMod.Resources.image.hat.hats.json";
        private const string EmbeddedResourcePrefix = "BanMod.Resources.image.hat.";

        public static void InitEmbeddedHats()
        {
            PendingHats.Clear();
            EnsureExternalFolder();

            // Startup is intentionally blocked here: hats are registered only
            // after their complete on-disk cache is ready.
            DownloadAllExternalHats();
            LoadExternalHatsOnly();

            if (PendingHats.Count == 0)
                LoadInternalHatsFromDll();
        }

        private static void DownloadAllExternalHats()
        {
            string temporaryManifest = ExternalManifestPath + ".part";

            try
            {
                DownloadFile(
                    CustomHatsServerBaseUrl + "/manifest",
                    temporaryManifest);

                string json = File.ReadAllText(temporaryManifest);
                CustomHatsConfig config = DeserializeConfig(json);
                if (config == null || config.Hats == null)
                    throw new InvalidDataException("Invalid custom-hat manifest.");

                if (config.Files != null)
                {
                    for (int i = 0; i < config.Files.Count; i++)
                        DownloadAssetIfNeeded(config.Files[i]);
                }

                ReplaceFile(temporaryManifest, ExternalManifestPath);
            }
            catch (Exception ex)
            {
                TryDelete(temporaryManifest);
                BMLogger.Error("[CustomHats] Download failed; using existing local cache: " + ex);
            }
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BanMod-CustomHats/1.0");
            return client;
        }

        private static void DownloadFile(string url, string destination)
        {
            using (HttpResponseMessage response = CustomHatsHttpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter()
                .GetResult())
            {
                response.EnsureSuccessStatusCode();

                using (Stream source = response.Content
                    .ReadAsStreamAsync()
                    .GetAwaiter()
                    .GetResult())
                using (FileStream target = new FileStream(
                    destination,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    source.CopyTo(target);
                }
            }
        }

        private static CustomHatsConfig DeserializeConfig(string json)
        {
            return JsonSerializer.Deserialize<CustomHatsConfig>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            });
        }

        private static void DownloadAssetIfNeeded(CustomHatFile asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.Path))
                return;

            string relativePath = NormalizeSafeRelativePath(asset.Path);
            if (relativePath == null)
                throw new InvalidDataException("Unsafe custom-hat path: " + asset.Path);

            string localPath = GetSafeLocalPath(relativePath);
            if (File.Exists(localPath) && HashMatches(localPath, asset.Sha256))
                return;

            string parent = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            string temporaryPath = localPath + ".part";
            TryDelete(temporaryPath);

            string escapedPath = string.Join("/", Array.ConvertAll(
                relativePath.Split('/'), Uri.EscapeDataString));

            DownloadFile(
                CustomHatsServerBaseUrl + "/files/" + escapedPath,
                temporaryPath);

            if (!HashMatches(temporaryPath, asset.Sha256))
            {
                TryDelete(temporaryPath);
                throw new InvalidDataException("SHA-256 mismatch for " + relativePath);
            }

            ReplaceFile(temporaryPath, localPath);
        }

        private static string NormalizeSafeRelativePath(string value)
        {
            string path = value.Replace('\\', '/').TrimStart('/');
            string[] parts = path.Split('/');
            if (parts.Length == 0)
                return null;

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]) || parts[i] == "." || parts[i] == "..")
                    return null;
            }

            return string.Join("/", parts);
        }

        private static string GetSafeLocalPath(string relativePath)
        {
            string root = Path.GetFullPath(CustomHatsDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string localPath = Path.GetFullPath(Path.Combine(root,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!localPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Asset path escaped the cache directory.");
            return localPath;
        }

        private static bool HashMatches(string path, string expectedHash)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                return false;
            if (string.IsNullOrWhiteSpace(expectedHash))
                return true;

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder actual = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    actual.Append(hash[i].ToString("x2"));
                return string.Equals(actual.ToString(), expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void ReplaceFile(string source, string destination)
        {
            if (File.Exists(destination))
                File.Delete(destination);
            File.Move(source, destination);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
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

                CustomHatsConfig config = DeserializeConfig(json);

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

                CustomHatsConfig config = DeserializeConfig(json);

                if (config == null || config.Hats == null)
                    return;

                BuildExternalAssetPathIndex(config.Files);

                for (int i = 0; i < config.Hats.Count; i++)
                {
                    CustomHat hat = config.Hats[i];

                    NormalizeExternalHat(hat);
                    ResolveExternalHatPaths(hat);

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

        private static void ResolveExternalHatPaths(CustomHat hat)
        {
            if (hat == null)
                return;

            hat.Resource = ResolveExternalAssetPath(hat.Resource);
            hat.FlipResource = ResolveExternalAssetPath(hat.FlipResource);
            hat.BackResource = ResolveExternalAssetPath(hat.BackResource);
            hat.BackFlipResource = ResolveExternalAssetPath(hat.BackFlipResource);
            hat.ClimbResource = ResolveExternalAssetPath(hat.ClimbResource);
        }

        private static void BuildExternalAssetPathIndex(List<CustomHatFile> files)
        {
            ExternalAssetPathIndex.Clear();

            if (files == null)
                return;

            Dictionary<string, int> fileNameCounts =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Count; i++)
            {
                CustomHatFile asset = files[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.Path))
                    continue;

                string relativePath = NormalizeSafeRelativePath(asset.Path);
                if (relativePath == null)
                    continue;

                ExternalAssetPathIndex[relativePath] = GetSafeLocalPath(relativePath);

                string fileName = Path.GetFileName(relativePath);
                if (string.IsNullOrEmpty(fileName))
                    continue;

                if (fileNameCounts.TryGetValue(fileName, out int count))
                    fileNameCounts[fileName] = count + 1;
                else
                    fileNameCounts[fileName] = 1;
            }

            for (int i = 0; i < files.Count; i++)
            {
                CustomHatFile asset = files[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.Path))
                    continue;

                string relativePath = NormalizeSafeRelativePath(asset.Path);
                if (relativePath == null)
                    continue;

                string fileName = Path.GetFileName(relativePath);
                if (!string.IsNullOrEmpty(fileName) &&
                    fileNameCounts.TryGetValue(fileName, out int count) &&
                    count == 1)
                {
                    ExternalAssetPathIndex[fileName] = GetSafeLocalPath(relativePath);
                }
            }
        }

        private static string ResolveExternalAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            try
            {
                string relativePath = NormalizeSafeRelativePath(path);
                if (relativePath == null)
                    return null;

                if (ExternalAssetPathIndex.TryGetValue(relativePath, out string indexedPath))
                    return indexedPath;

                string root = Path.GetFullPath(CustomHatsDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);

                string fullPath = Path.GetFullPath(Path.Combine(root, normalizedPath));

                if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return null;

                return fullPath;
            }
            catch
            {
                return null;
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

                HatViewData viewData = ScriptableObject.CreateInstance<HatViewData>();
                viewData.name = customHat.ProductId + "_ViewData";

                viewData.MainImage = mainSprite;
                viewData.BackImage = null;
                viewData.LeftMainImage = null;
                viewData.LeftBackImage = null;
                viewData.FloorImage = mainSprite;
                viewData.LeftFloorImage = null;
                viewData.ClimbImage = null;
                viewData.LeftClimbImage = null;

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

                    FlipImage = null,
                    BackFlipImage = null
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

        public static HatViewData EnsureFullViewData(string prodId, HatViewData viewData)
        {
            if (string.IsNullOrEmpty(prodId) || viewData == null)
                return viewData;

            HatViewData baseViewData = ViewDataCache.TryGetValue(prodId, out HatViewData cachedBaseView)
                ? cachedBaseView
                : viewData;

            if (FullyLoadedHatViews.Contains(prodId))
                return baseViewData;

            if (!ExtensionCache.TryGetValue(prodId, out HatExtension ext))
                return baseViewData;

            Sprite backSprite = LoadBaseSprite(ext.BaseBackResourcePath);
            Sprite flipSprite = LoadBaseSprite(ext.BaseFlipResourcePath);
            Sprite backFlipSprite = LoadBaseSprite(ext.BaseBackFlipResourcePath);
            Sprite climbSprite = LoadBaseSprite(ext.BaseClimbResourcePath);

            baseViewData.BackImage = backSprite;
            baseViewData.LeftMainImage = flipSprite;
            baseViewData.LeftBackImage = backFlipSprite;
            baseViewData.FloorImage = baseViewData.MainImage;
            baseViewData.LeftFloorImage = flipSprite;

            if (climbSprite != null)
            {
                baseViewData.ClimbImage = climbSprite;
                baseViewData.LeftClimbImage = climbSprite;
            }
            else
            {
                baseViewData.ClimbImage = backSprite != null ? backSprite : baseViewData.MainImage;
                baseViewData.LeftClimbImage = backFlipSprite != null ? backFlipSprite : baseViewData.ClimbImage;
            }

            ext.FlipImage = flipSprite;
            ext.BackFlipImage = backFlipSprite;
            FullyLoadedHatViews.Add(prodId);
            return baseViewData;
        }

        private static Sprite LoadBaseSprite(string basePath)
        {
            return string.IsNullOrEmpty(basePath)
                ? null
                : LoadHatSprite(basePath + ".png");
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
                {
                    if (cached.BackImage == null && originalViewData.BackImage != null)
                        cached.BackImage = RecolorAdaptive(originalViewData.BackImage, colorId);
                    if (cached.LeftMainImage == null && originalViewData.LeftMainImage != null)
                        cached.LeftMainImage = RecolorAdaptive(originalViewData.LeftMainImage, colorId);
                    if (cached.LeftBackImage == null && originalViewData.LeftBackImage != null)
                        cached.LeftBackImage = RecolorAdaptive(originalViewData.LeftBackImage, colorId);
                    if (cached.ClimbImage == null && originalViewData.ClimbImage != null)
                        cached.ClimbImage = RecolorAdaptive(originalViewData.ClimbImage, colorId);
                    if (cached.LeftClimbImage == null && originalViewData.LeftClimbImage != null)
                        cached.LeftClimbImage = RecolorAdaptive(originalViewData.LeftClimbImage, colorId);

                    cached.FloorImage = cached.MainImage;
                    cached.LeftFloorImage = cached.LeftMainImage;
                    if (cached.LeftClimbImage == null)
                        cached.LeftClimbImage = cached.ClimbImage;

                    return cached;
                }
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

            bool externalFile = Path.IsPathRooted(path) || File.Exists(path);
            string resourcePath = externalFile
                ? Path.GetFullPath(path)
                : path.StartsWith("BanMod.Resources.", StringComparison.OrdinalIgnoreCase)
                    ? path
                    : MakeEmbeddedPath(path);

            string cacheKey = resourcePath + "|" + HatCanvasSize + "|" + HatPixelsPerUnit;
            if (HatSpriteCache.TryGetValue(cacheKey, out Sprite cached))
                return cached;

            Texture2D source = externalFile
                ? LoadExternalTexture(resourcePath)
                : BMImage.LoadTextureFromResources(resourcePath);
            if (source == null)
                return null;

            Texture2D canvas = CenterTextureOn350Canvas(source);
            if (canvas == null)
                return null;

            if (canvas != source)
                UnityEngine.Object.Destroy(source);

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
            bool includeSecondarySprites = FullyLoadedHatViews.Contains(prodId);

            if (ViewDataCache.TryGetValue(colorCacheKey, out HatViewData coloredView))
            {
                if (includeSecondarySprites && !FullyLoadedColorViews.Contains(colorCacheKey))
                    PopulateColoredSecondarySprites(colorCacheKey, coloredView, originalViewData, ext, colorId);

                return coloredView;
            }

            Sprite coloredMain = LoadHatSprite(ext.BaseResourcePath + "_" + colorId + ".png");
            coloredMain = coloredMain != null ? coloredMain : originalViewData.MainImage;

            HatViewData newView = ScriptableObject.CreateInstance<HatViewData>();
            newView.name = prodId + "_ViewData_" + colorId;

            newView.MainImage = coloredMain;
            newView.BackImage = null;
            newView.FloorImage = coloredMain;
            newView.ClimbImage = null;
            newView.LeftMainImage = null;
            newView.LeftBackImage = null;
            newView.LeftFloorImage = null;
            newView.LeftClimbImage = null;

            newView.MatchPlayerColor = originalViewData.MatchPlayerColor;
            newView.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            if (includeSecondarySprites)
                PopulateColoredSecondarySprites(colorCacheKey, newView, originalViewData, ext, colorId);

            ViewDataCache[colorCacheKey] = newView;
            return newView;
        }

        private static void PopulateColoredSecondarySprites(
            string cacheKey,
            HatViewData coloredView,
            HatViewData originalViewData,
            HatExtension ext,
            int colorId)
        {
            Sprite coloredBack = string.IsNullOrEmpty(ext.BaseBackResourcePath)
                ? null
                : LoadHatSprite(ext.BaseBackResourcePath + "_" + colorId + ".png");
            coloredBack = coloredBack != null ? coloredBack : originalViewData.BackImage;

            Sprite coloredClimb = string.IsNullOrEmpty(ext.BaseClimbResourcePath)
                ? null
                : LoadHatSprite(ext.BaseClimbResourcePath + "_" + colorId + ".png");
            coloredClimb = coloredClimb != null ? coloredClimb : originalViewData.ClimbImage;

            Sprite coloredFlip = string.IsNullOrEmpty(ext.BaseFlipResourcePath)
                ? null
                : LoadHatSprite(ext.BaseFlipResourcePath + "_" + colorId + ".png");
            coloredFlip = coloredFlip != null ? coloredFlip : originalViewData.LeftMainImage;

            Sprite coloredBackFlip = string.IsNullOrEmpty(ext.BaseBackFlipResourcePath)
                ? null
                : LoadHatSprite(ext.BaseBackFlipResourcePath + "_" + colorId + ".png");
            coloredBackFlip = coloredBackFlip != null ? coloredBackFlip : originalViewData.LeftBackImage;

            coloredView.BackImage = coloredBack;
            coloredView.ClimbImage = coloredClimb;
            coloredView.LeftMainImage = coloredFlip;
            coloredView.LeftBackImage = coloredBackFlip;
            coloredView.LeftFloorImage = coloredFlip;
            coloredView.LeftClimbImage = coloredClimb != null
                ? coloredClimb
                : (coloredBack != null ? coloredBack : coloredView.MainImage);

            FullyLoadedColorViews.Add(cacheKey);
        }
    }
}
