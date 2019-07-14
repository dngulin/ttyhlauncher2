using System;
using System.Collections.Generic;
using System.Linq;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Json.Ttyh;

namespace TtyhLauncher.Versions.Data {
    public class CachedPrefixInfo : IComparable<CachedPrefixInfo>, IComparable {
        private const string ReleaseKey = "release";
        private const string LatestAlias = "latest";

        public string Id { get; }
        public string About { get; }
        public string LatestVersion { get; }

        public string[] Versions { get; }

        public CachedPrefixInfo(
            string id,
            string about,
            PrefixVersionsIndex remoteIndex,
            VersionIndex[] localVersions) {
            
            Id = id;
            About = about;
            
            remoteIndex.Latest.TryGetValue(ReleaseKey, out var latest);
            
            var versions = new List<CachedVersionInfo>(remoteIndex.Versions.Length + localVersions.Length + 1) {
                new CachedVersionInfo(LatestAlias)
            };

            var knownIds = new HashSet<string> {LatestAlias};

            if (remoteIndex.Versions != null) {
                foreach (var versionEntry in remoteIndex.Versions) {
                    if (versionEntry.Id == LatestAlias)
                        continue;

                    knownIds.Add(versionEntry.Id);
                    versions.Add(new CachedVersionInfo(versionEntry));
                }
            }

            foreach (var versionIndex in localVersions) {
                if (!knownIds.Contains(versionIndex.Id)) {
                    versions.Add(new CachedVersionInfo(versionIndex));
                }
            }
            
            versions.Sort();

            Versions = versions.Select(v => v.Id).ToArray();
            
            LatestVersion = latest ?? (versions.Count > 0 ? versions[0].Id : null);
        }
        
        public int CompareTo(CachedPrefixInfo other) {
            if (ReferenceEquals(null, other)) return 1;
            if (ReferenceEquals(this, other)) return 0;
            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }

        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is CachedPrefixInfo other ? 
                CompareTo(other) :
                throw new ArgumentException($"Object must be of type {nameof(CachedPrefixInfo)}");
        }
    }
}