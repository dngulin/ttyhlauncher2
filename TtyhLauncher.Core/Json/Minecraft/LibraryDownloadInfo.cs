using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class LibraryDownloadInfo: DownloadInfo {
        [JsonProperty("totalSize")] public string Path;
    }
}