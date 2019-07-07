using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class LoginResultData : ResultData {
        [JsonProperty("clientToken")] public readonly string ClientToken;
        [JsonProperty("accessToken")] public readonly string AccessToken;

        [JsonConstructor]
        public LoginResultData(string clientToken, string accessToken, string error) : base(error) {
            ClientToken = clientToken;
            AccessToken = accessToken;
        }
    }
}