using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    class SkinUploadRequestData : AuthData {
        [JsonProperty("skinData")] public string SkinData;
        [JsonProperty("skinModel")] public string Model;
    }
}