using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TtyhLauncher.Json.Ttyh {
    public class PrefixVersionsIndex {
        public class VersionEntry {
            [JsonProperty("id")] public string Id;
            [JsonProperty("time")] public DateTimeOffset Time;
            [JsonProperty("releaseTime")] public DateTimeOffset ReleaseTime;
            [JsonProperty("type")] public string Type;
        }
        
        [JsonProperty("latest")] public Dictionary<string, string> Latest = new Dictionary<string, string>();
        [JsonProperty("versions")] public VersionEntry[] Versions = new VersionEntry[0];
    }
}