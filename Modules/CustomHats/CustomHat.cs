//credits and licenses in the resources folder
using System.Text.Json.Serialization;

namespace BanMod.Modules.CustomHats
{
    public class CustomHat
    {
        [JsonPropertyName("name")]
        public string Name;

        [JsonPropertyName("productId")]
        public string ProductId;

        [JsonPropertyName("author")]
        public string Author;

        [JsonPropertyName("package")]
        public string Package;

        [JsonPropertyName("resource")]
        public string Resource;

        [JsonPropertyName("backresource")]
        public string BackResource;

        [JsonPropertyName("climbresource")]
        public string ClimbResource;

        [JsonPropertyName("flipresource")]
        public string FlipResource;

        [JsonPropertyName("backflipresource")]
        public string BackFlipResource;

        [JsonPropertyName("bounce")]
        public bool Bounce = true;

        [JsonPropertyName("adaptive")]
        public bool Adaptive;

        [JsonPropertyName("behind")]
        public bool Behind;

        [JsonPropertyName("blocksVisors")]
        public bool BlocksVisors;

        [JsonPropertyName("colorVariations")]
        public bool ColorVariations;
    }

    public class CustomHatFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; }
    }
}
