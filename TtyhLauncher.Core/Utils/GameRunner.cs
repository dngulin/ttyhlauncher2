using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Logs;
using TtyhLauncher.Profiles.Data;

namespace TtyhLauncher.Utils {
    public class GameRunner {
        private const string ProfilesDirectory = "profiles";
        private const string VersionsDirectory = "versions";
        private const string LibrariesDirectory = "libraries";
        private const string AssetsDirectory = "assets";
        private const string NativesDirectory = "natives";
        
        private readonly string _librariesPath;
        private readonly string _assetsPath;
        private readonly string _nativesPath;
        private readonly string _profilesPath;
        private readonly string _versionsPath;
        
        private readonly string _launcherName;
        private readonly string _launcherVersion;
        
        private readonly JsonParser _json;
        
        private readonly ILogger _rawLogger;
        private readonly WrappedLogger _log;

        public GameRunner(string dataDirectory, JsonParser json, ILogger logger, string launcherName, string launcherVersion) {
            _json = json;
            _rawLogger = logger;
            _log = new WrappedLogger(logger, "GameRunner");
            
            _launcherName = launcherName;
            _launcherVersion = launcherVersion;
            
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // XDG_DATA_HOME
            _profilesPath = Path.Combine(dataPath, dataDirectory, ProfilesDirectory);
            _versionsPath = Path.Combine(dataPath, dataDirectory, VersionsDirectory);
            _librariesPath = Path.Combine(dataPath, dataDirectory, LibrariesDirectory);
            _assetsPath = Path.Combine(dataPath, dataDirectory, AssetsDirectory);
            _nativesPath = Path.Combine(dataPath, dataDirectory, NativesDirectory);
        }


        public async Task Run(string id, ProfileData data, string userName) {
            var clientToken = Guid.NewGuid().ToString();
            var accessToken = Guid.NewGuid().ToString();
            await Run(id, data, userName, clientToken, accessToken);
        }

        public async Task Run(string id, ProfileData data, string userName, string clientToken, string accessToken) {
            var prefix = data.FullVersion.Prefix;
            var version = data.FullVersion.Version;

            var versionIndexPath = Path.Combine(_versionsPath, prefix, version, $"{version}.json");
            var versionIndex = _json.ReadFile<VersionIndex>(versionIndexPath);

            var nativesDir = Path.Combine(_nativesPath, Guid.NewGuid().ToString());
            if (Directory.Exists(nativesDir))
                Directory.CreateDirectory(nativesDir);

            var classPath = new List<string>();

            foreach (var libraryInfo in versionIndex.Libraries) {
                if (!IndexTool.IsLibraryAllowed(libraryInfo))
                    continue;

                var libPath = Path.Combine(_librariesPath, IndexTool.GetLibraryPath(libraryInfo));

                if (libraryInfo.Natives != null)
                    ZipFile.ExtractToDirectory(libPath, nativesDir, true);
                else
                    classPath.Add(libPath);
            }

            classPath.Add(Path.Combine(_versionsPath, prefix, version, $"{version}.jar"));

            var args = new List<string> {
                "-Dline.separator=\r\n",
                "-Dfile.encoding=UTF8",
                "-Dminecraft.launcher.brand=" + _launcherName,
                "-Dminecraft.launcher.version=" + _launcherVersion,
                "-Djava.library.path=" + nativesDir,
                "-cp",
                string.Join(Platform.ClassPathSeparator, classPath),
                versionIndex.MainClass
            };

            var profileDir = Path.Combine(_profilesPath, id);
            var gameArgsMap = new Dictionary<string, string> {
                {"${auth_player_name}", userName},
                {"${version_name}", version},
                {"${auth_uuid}", clientToken},
                {"${auth_access_token}", accessToken},
                {"${game_directory}", profileDir},
                {"${assets_root}", _assetsPath},
                {"${assets_index_name}", IndexTool.GetAssetIndexName(versionIndex)},
                {"${user_properties}", "{}"},
                {"${user_type}", "mojang"},
                {"${version_type}", prefix}
            };

            var gameArguments = IndexTool.GetMinecraftArguments(versionIndex);
            for (var i = 0; i < gameArguments.Length; i++) {
                var token = gameArguments[i];
                if (gameArgsMap.ContainsKey(token))
                    gameArguments[i] = gameArgsMap[token];
            }

            args.AddRange(gameArguments);

            _log.Info($"java {string.Join(' ', args)}");
            var exitCode = await RunProcessAsync("java", profileDir, args);
            _log.Info($"Client terminated with exit code {exitCode}");

            Directory.Delete(nativesDir, true);
        }

        private async Task<int> RunProcessAsync(string application, string workDir, IEnumerable<string> args) {
            var info = new ProcessStartInfo(application) {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            foreach (var arg in args) {
                info.ArgumentList.Add(arg);
            }

            using (var process = new Process {StartInfo = info, EnableRaisingEvents = true}) {
                return await RunProcessAsync(process);
            }
        }

        private Task<int> RunProcessAsync(Process process) {
            var tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) => tcs.SetResult(process.ExitCode);

            var context = SynchronizationContext.Current;
            if (context != null) {
                process.OutputDataReceived += (sender, args) => context.Post(RunLog, args.Data);
                process.ErrorDataReceived += (sender, args) => context.Post(RunLog, args.Data);
            }
            else {
                _log.Warn("Can't listen to process messages!");
            }

            var started = process.Start();
            if (!started) {
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private void RunLog(object data) {
            if (data is string line)
                _rawLogger.WriteLine(line);
        }
    }
}