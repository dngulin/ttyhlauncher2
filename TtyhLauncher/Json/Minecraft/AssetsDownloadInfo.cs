using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class AssetsDownloadInfo: DownloadInfo {
        [JsonProperty("id")] public string Id;
        [JsonProperty("totalSize")] public long TotalSize;
    }
}