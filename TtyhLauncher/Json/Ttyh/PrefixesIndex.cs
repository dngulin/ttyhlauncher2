using System.Collections.Generic;
using Newtonsoft.Json;

namespace TtyhLauncher.Json.Ttyh {
    public class PrefixesIndex {
        public class PrefixEntry {
            [JsonProperty("about")] public readonly string About;
            [JsonProperty("type")] public readonly string Type;
        }
        
        [JsonProperty("prefixes")]
        public Dictionary<string, PrefixEntry> Prefixes = new Dictionary<string, PrefixEntry>();
    }
}