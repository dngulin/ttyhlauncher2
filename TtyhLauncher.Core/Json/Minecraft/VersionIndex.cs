using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TtyhLauncher.Json.Minecraft {
    public class VersionIndex {
        public class VersionDownloads {
            [JsonProperty("client")] public DownloadInfo Client;
            [JsonProperty("server")] public DownloadInfo Server;
        }
        
        public class LoggingSettings {
            public class LoggingInfo {
                [JsonProperty("argument")] public string Argument;
                [JsonProperty("type")] public string Type;
                [JsonProperty("file")] public DownloadInfo File;
            }
            
            [JsonProperty("client")] public LoggingInfo Client;
        }

        public class ArgumentsData {
            [JsonProperty("game")] public JToken[] Game;
            [JsonProperty("jvm")] public JToken[] Jvm;
        }
        
        [JsonProperty("id")] public string Id;
        [JsonProperty("downloads")] public VersionDownloads Downloads;
        
        [JsonProperty("assetIndex")] public AssetsDownloadInfo AssetIndex; // New format
        [JsonProperty("assets")] public string Assets; // Old format
        
        [JsonProperty("libraries")] public LibraryInfo[] Libraries;

        [JsonProperty("minimumLauncherVersion")] public string MinimumLauncherVersion;
        [JsonProperty("mainClass")] public string MainClass;

        [JsonProperty("arguments")] public ArgumentsData Arguments; // New format
        [JsonProperty("minecraftArguments")] public string MinecraftArguments; // Old format
        
        [JsonProperty("releaseTime")] public DateTimeOffset ReleaseTime;
        [JsonProperty("time")] public DateTimeOffset Time;
        [JsonProperty("type")] public string Type;
        
        [JsonProperty("logging")] public LoggingSettings Logging;
    }
}