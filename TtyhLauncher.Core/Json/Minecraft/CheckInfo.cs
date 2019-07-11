using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class CheckInfo {
        [JsonProperty("hash")] public string Hash;
        [JsonProperty("size")] public long Size;
    }
}