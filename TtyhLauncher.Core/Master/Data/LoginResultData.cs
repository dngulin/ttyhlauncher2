using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class LoginResultData : ResultData {
        [JsonProperty("clientToken")] public string ClientToken;
        [JsonProperty("accessToken")] public string AccessToken;
    }
}