using System;
using System.Runtime.InteropServices;

namespace TtyhLauncher.Utils {
    public static class Platform {
        public static string Name {
            get {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    return "linux";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    return "osx";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    return "windows";
                }

                return "unknown";
            }
        }

        public static string Version => RuntimeInformation.OSDescription;

        public static string WordSize {
            get {
                if (IntPtr.Size == 8)
                    return "64";

                if (IntPtr.Size == 4)
                    return "32";

                return "unknown";
            }
        }

        public static char ClassPathSeparator => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
    }
}