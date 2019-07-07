using Newtonsoft.Json;

namespace TtyhLauncher.Settings.Data {
    public class SettingsData {
        [JsonProperty("username")] public string UserName = "Player";
        [JsonProperty("password")] public string Password = "";
        [JsonProperty("profile")] public string Profile = "";
        [JsonProperty("revision")] public string Revision = "";
    }
}