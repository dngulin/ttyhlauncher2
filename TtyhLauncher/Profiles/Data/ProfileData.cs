using Newtonsoft.Json;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Profiles.Data {
    public class ProfileData {
        [JsonProperty("version")] public FullVersionId Version = new FullVersionId("default", "");
        [JsonProperty("check_version")] public bool CheckVersionOnRun = true;
        [JsonProperty("update_profile")] public bool UpdateProfileOnRun = true;
    }
}