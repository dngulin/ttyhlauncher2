using Newtonsoft.Json;

namespace TtyhLauncher.Profiles.Data {
    public class FilesIndex {
        [JsonProperty("installed")] public string[] Installed = new string[0];
    }
}