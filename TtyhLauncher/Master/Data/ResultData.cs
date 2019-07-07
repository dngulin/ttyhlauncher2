using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class ResultData {
        [JsonProperty("error")] public string Error;

        [JsonConstructor]
        protected ResultData(string error) {
            Error = error;
        }
    }
}