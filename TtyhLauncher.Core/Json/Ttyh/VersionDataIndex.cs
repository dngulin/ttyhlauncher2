using System.Collections.Generic;
using Newtonsoft.Json;
using TtyhLauncher.Json.Minecraft;

namespace TtyhLauncher.Json.Ttyh {
    public class VersionDataIndex {
        public class CustomFilesInfo {
            [JsonProperty("mutables")] public string[] Mutables;
            [JsonProperty("index")] public Dictionary<string, CheckInfo> Index;
        }

        [JsonProperty("main")] public CheckInfo Main;
        [JsonProperty("libs")] public Dictionary<string, CheckInfo> Libs;
        [JsonProperty("files")] public CustomFilesInfo Files;
    }
}