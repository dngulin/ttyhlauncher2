using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class ResultData {
        [JsonProperty("error")] public string Error;
        [JsonProperty("errorMessage")] public string ErrorMessage;
    }
}