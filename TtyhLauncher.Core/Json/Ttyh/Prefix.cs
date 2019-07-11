using System.Collections.Generic;
using Newtonsoft.Json;

namespace TtyhLauncher.Json.Ttyh {
    public class Prefix {
        [JsonProperty("about")] public string About;
        [JsonProperty("type")] public string Type;
        [JsonProperty("latest")] public Dictionary<string, string> Latest;
    }
}