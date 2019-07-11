using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class AuthData {
        [JsonProperty("username")] public string UserName;
        [JsonProperty("password")] public string Password;
    }
}