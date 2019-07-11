using Newtonsoft.Json;

namespace TtyhLauncher.Settings.Data {
    public class SettingsData {
        [JsonProperty("username")] public string UserName = "Player";
        [JsonProperty("password")] public string Password = "";
        
        [JsonProperty("save_password")] public bool SavePassword = true;
        [JsonProperty("hide_on_run")] public bool HideOnRun = true;

        [JsonProperty("profile")] public string Profile = "";
        [JsonProperty("revision")] public string Revision = "";
        
        [JsonProperty("window_width")] public int WindowWidth = 800;
        [JsonProperty("window_height")] public int WindowHeight = 600;
    }
}