using System;
using System.Collections.Generic;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Json.Ttyh;

namespace TtyhLauncher.Versions.Data {
    public class CachedPrefixInfo : IComparable<CachedPrefixInfo>, IComparable {
        private const string ReleaseKey = "release";

        public string Id { get; }
        public string About { get; }
        public string LatestVersion { get; }

        private readonly List<CachedVersionInfo> _versions;
        public IReadOnlyList<CachedVersionInfo> Versions => _versions;

        public CachedPrefixInfo(
            string id,
            string about,
            PrefixVersionsIndex remoteIndex,
            VersionIndex[] localVersions) {
            
            Id = id;
            About = about;
            
            remoteIndex.Latest.TryGetValue(ReleaseKey, out var latest);
            
            _versions = new List<CachedVersionInfo>(remoteIndex.Versions.Length + localVersions.Length);
            
            var knownIds = new HashSet<string>();

            if (remoteIndex.Versions != null) {
                foreach (var versionEntry in remoteIndex.Versions) {
                    knownIds.Add(versionEntry.Id);
                    _versions.Add(new CachedVersionInfo(versionEntry));
                }
            }

            foreach (var versionIndex in localVersions) {
                if (!knownIds.Contains(versionIndex.Id)) {
                    _versions.Add(new CachedVersionInfo(versionIndex));
                }
            }
            
            _versions.Sort();
            
            LatestVersion = latest ?? (_versions.Count > 0 ? _versions[0].Id : null);
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