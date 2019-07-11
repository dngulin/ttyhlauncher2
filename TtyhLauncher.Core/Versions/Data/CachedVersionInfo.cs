using System;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Json.Ttyh;

namespace TtyhLauncher.Versions.Data {
    public class CachedVersionInfo : IComparable<CachedVersionInfo>, IComparable {
        public string Id { get; }
        public DateTimeOffset ReleaseTime { get; }

        public CachedVersionInfo(PrefixVersionsIndex.VersionEntry versionEntry) {
            Id = versionEntry.Id;
            ReleaseTime = versionEntry.ReleaseTime;
        }

        public CachedVersionInfo(VersionIndex version) {
            Id = version.Id;
            ReleaseTime = version.ReleaseTime;
        }
        
        public int CompareTo(CachedVersionInfo other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            
            return ReleaseTime.CompareTo(other.ReleaseTime);
        }

        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            
            return obj is CachedVersionInfo other ?
                CompareTo(other) :
                throw new ArgumentException($"Object must be of type {nameof(CachedVersionInfo)}");
        }
    }
}