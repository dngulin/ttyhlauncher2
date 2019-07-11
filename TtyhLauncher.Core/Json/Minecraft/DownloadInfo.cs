using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class DownloadInfo {
        [JsonProperty("sha1")] public string Sha1;
        [JsonProperty("size")] public long Size;
        [JsonProperty("url")] public string Url;
    }
}