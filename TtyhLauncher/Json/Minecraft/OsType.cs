using System.Runtime.Serialization;

namespace TtyhLauncher.Json.Minecraft {
    public enum OsType {
        [EnumMember(Value = "linux")] Linux,
        [EnumMember(Value = "windows")] Windows,
        [EnumMember(Value = "osx")] Osx
    }
}