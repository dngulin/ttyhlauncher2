using Newtonsoft.Json;

namespace TtyhLauncher.Versions.Data {
    public class FullVersionId {
        [JsonProperty("prefix")] public readonly string Prefix;
        [JsonProperty("version")] public readonly string Version;

        public FullVersionId(string prefix, string version) {
            Prefix = prefix;
            Version = version;
        }

        public override string ToString() {
            return $"{Prefix}/{Version}";
        }
    }
}