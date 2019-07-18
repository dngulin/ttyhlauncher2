using Newtonsoft.Json;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Profiles.Data {
    public class ProfileData {
        [JsonProperty("version")] public FullVersionId FullVersion = new FullVersionId("default", "");
        [JsonProperty("check_version")] public bool CheckVersionFiles = true;
        
        [JsonProperty("use_custom_java_path")] public bool UseCustomJavaPath;
        [JsonProperty("custom_java_path")] public string CustomJavaPath = "";
        
        [JsonProperty("use_custom_java_args")] public bool UseCustomJavaArgs;
        [JsonProperty("custom_java_args")] public string CustomJavaArgs = "";

        public ProfileData Clone() {
            return new ProfileData {
                FullVersion = new FullVersionId(FullVersion.Prefix, FullVersion.Version),
                CheckVersionFiles = CheckVersionFiles,
                UseCustomJavaPath = UseCustomJavaPath,
                CustomJavaPath = CustomJavaPath,
                UseCustomJavaArgs = UseCustomJavaArgs,
                CustomJavaArgs = CustomJavaArgs
            };
        }
    }
}