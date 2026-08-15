//credits and licenses in the resources folder
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BanMod.Modules.CustomHats
{
    public class CustomHatsConfig
    {
        [JsonPropertyName("hats")]
        public List<CustomHat> Hats { get; set; }

        [JsonPropertyName("files")]
        public List<CustomHatFile> Files { get; set; }
    }
}
