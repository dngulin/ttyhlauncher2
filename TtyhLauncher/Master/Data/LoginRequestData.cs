using Newtonsoft.Json;

namespace TtyhLauncher.Master.Data {
    public class LoginRequestData : AuthData {
        public class AgentData {
            [JsonProperty("name")] public string Name;
            [JsonProperty("version")] public int Version;
        }

        public class PlatformData {
            [JsonProperty("os")] public string Name;
            [JsonProperty("version")] public string Version;
            [JsonProperty("word")] public string WordSize;
        }

        [JsonProperty("agent")] public AgentData Agent;
        [JsonProperty("platform")] public PlatformData Platform;
        [JsonProperty("ticket")] public string Ticket;
        [JsonProperty("launcherVersion")] public string Version;
    }
}