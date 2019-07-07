using System.Collections.Generic;
using Newtonsoft.Json;

namespace TtyhLauncher.Json.Minecraft {
    public class LibraryInfo {
        public class ExtractRules {
            [JsonProperty("exclude")] public string[] Exclude;
        }
        
        public class LibraryDownloads {
            [JsonProperty("artifact")] public LibraryDownloadInfo Artifact;
            [JsonProperty("classifiers")] public Dictionary<string, LibraryDownloadInfo> Classifiers;
        }

        public class LibraryRule {
            public class OsInfo {
                [JsonProperty("name")] public string Name;
            }
            
            [JsonProperty("action")] public string Action;
            [JsonProperty("os")] public OsInfo Os;
        }

        [JsonProperty("name")] public string Name;
        [JsonProperty("rules")] public LibraryRule[] Rules;
        [JsonProperty("natives")] public Dictionary<string, string> Natives; // os -> classifier
        [JsonProperty("downloads")] public LibraryDownloads Downloads;
        [JsonProperty("extract")] public ExtractRules Extract;
    }
}