using System.Collections.Generic;
using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class AssetsIndex {
        [JsonProperty("objects")] public Dictionary<string, CheckInfo> Objects = new Dictionary<string, CheckInfo>();
    }
}