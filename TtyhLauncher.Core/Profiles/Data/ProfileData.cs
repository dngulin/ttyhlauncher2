using Newtonsoft.Json;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Profiles.Data {
    public class ProfileData {
        [JsonProperty("version")] public FullVersionId FullVersion = new FullVersionId("default", "");
        [JsonProperty("check_version")] public bool CheckVersionOnRun = true;

        public ProfileData Clone() {
            return new ProfileData {
                FullVersion = new FullVersionId(FullVersion.Prefix, FullVersion.Version),
                CheckVersionOnRun = CheckVersionOnRun
            };
        }
    }
}