using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TtyhLauncher.Json.Minecraft;

namespace TtyhLauncher.Utils {
    public static class IndexTool {
        public const string VersionTypeRelease = "release";
        public const string VersionAliasLatest = "latest";
        
        public static string GetAssetIndexName(VersionIndex versionIndex) {
            if (versionIndex.AssetIndex != null) {
                return $"{versionIndex.AssetIndex.Sha1}/{versionIndex.AssetIndex.Id}";
            }
            
            return versionIndex.Assets;
        }
        
        public static string GetAssetIndexPath(VersionIndex versionIndex) {
            return $"{GetAssetIndexName(versionIndex)}.json";
        }
        
        public static bool IsLibraryAllowed(LibraryInfo lib) {
            if (lib.Rules == null)
                return true;

            var allowed = false;

            foreach (var rule in lib.Rules) {
                var matchingOs = string.Equals(Platform.Name, rule.Os?.Name, StringComparison.Ordinal);
                var undefinedOs = string.IsNullOrEmpty(rule.Os?.Name);
                
                if (rule.Action == "allow" && (matchingOs || undefinedOs)) {
                    allowed = true;
                } else if (rule.Action == "disallow" && matchingOs) {
                    allowed = false;
                }
            }

            return allowed;
        }
        
        // From: "package.dot.separated:name:version"
        // To:   "package/slash/separated/name/version/name-version[-classifier].jar"
        public static string GetLibraryPath(LibraryInfo lib) {
            var tokens = lib.Name.Split(':');
            
            var package = tokens[0].Replace('.', '/');
            var name = tokens[1];
            var version = tokens[2];

            var classifier = "";
            if (lib.Natives != null) {
                classifier = "-" + lib.Natives[Platform.Name].Replace("${arch}", Platform.WordSize);
            }

            return $"{package}/{name}/{version}/{name}-{version}{classifier}.jar";
        }

        public static string[] GetMinecraftArguments(VersionIndex index) {
            if (index.Arguments?.Game == null) {
                return index.MinecraftArguments.Split(' ');
            }

            var result = new List<string>();
            
            foreach (var argToken in index.Arguments.Game) {
                if (argToken.Type == JTokenType.String)
                    result.Add(argToken.Value<string>());
            }
            
            return result.ToArray();
        }
    }
}