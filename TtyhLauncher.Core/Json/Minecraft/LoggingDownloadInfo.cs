using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class LoggingDownloadInfo: DownloadInfo {
        [JsonProperty("id")] public string Id;
    }
}