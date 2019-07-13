using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Json.Ttyh;
using TtyhLauncher.Logs;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Profiles.Exceptions;
using TtyhLauncher.Utils;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Profiles {
    public class ProfilesManager {
        private const string ProfilesDirectory = "profiles";
        private const string VersionsDirectory = "versions";
        private const string LibrariesDirectory = "libraries";
        private const string AssetsDirectory = "assets";
        private const string NativesDirectory = "natives";
        
        private const string ProfileIndexName = "profile.json";
        private const string FilesIndexName = "files.json";
        private const string DataIndexName = "data.json";
        
        private readonly JsonParser _json;
        private readonly WrappedLogger _log;
        private readonly ILogger _runLogger;
        private readonly StringBuilder _hashStringBuilder;

        private readonly string _profilesPath;
        private readonly string _versionsPath;
        private readonly string _librariesPath;
        private readonly string _assetsPath;
        private readonly string _nativesPath;

        private readonly string _launcherName;
        private readonly string _launcherVersion;
        
        private readonly Dictionary<string, ProfileData> _profiles = new Dictionary<string, ProfileData>();
        
        public ProfilesManager(string dataDirectory, JsonParser json, ILogger logger, string launcherName, string launcherVersion) {
            _launcherName = launcherName;
            _launcherVersion = launcherVersion;
            
            _json = json;
            _hashStringBuilder = new StringBuilder(40);
            _runLogger = logger;
            _log = new WrappedLogger(logger, "Profiles");
            _log.Info("Initializing...");
            
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // XDG_DATA_HOME
            _profilesPath = Path.Combine(dataPath, dataDirectory, ProfilesDirectory);
            _versionsPath = Path.Combine(dataPath, dataDirectory, VersionsDirectory);
            _librariesPath = Path.Combine(dataPath, dataDirectory, LibrariesDirectory);
            _assetsPath = Path.Combine(dataPath, dataDirectory, AssetsDirectory);
            _nativesPath = Path.Combine(dataPath, dataDirectory, NativesDirectory);
            
            FindProfiles();
            _log.Info("Initialized!");
        }

        public bool IsEmpty => _profiles.Count <= 0;

        public string[] Names {
            get {
                var names = new string[_profiles.Count];
                _profiles.Keys.CopyTo(names, 0);
                Array.Sort(names);
                return names;
            }
        }

        private void FindProfiles() {
            if (!Directory.Exists(_profilesPath))
                Directory.CreateDirectory(_profilesPath);

            if (!Directory.Exists(_profilesPath))
                return;
            
            foreach (var profileId in Directory.EnumerateDirectories(_profilesPath).Select(Path.GetFileName)) {
                var path = Path.Combine(_profilesPath, profileId, ProfileIndexName);
                if (!File.Exists(path))
                    continue;
                    
                try {
                    _profiles.Add(profileId, _json.ReadFile<ProfileData>(path));
                    _log.Info($"Found profile: {profileId}");
                }
                catch (Exception e) {
                    _log.Error($"Corrupted profile {profileId}: {e.Message}");
                }
            }
        }

        public bool Contains(string profileId) => _profiles.ContainsKey(profileId);

        public ProfileData GetProfileData(string id) => _profiles[id].Clone();

        public string CreateDefault(CachedPrefixInfo prefixInfo) {
            var fullVersion = new FullVersionId(prefixInfo.Id, prefixInfo.LatestVersion);
            var prefixId = $"{prefixInfo.About} {fullVersion.Version}";
            
            Create(prefixId, new ProfileData {FullVersion = fullVersion});
            return prefixId;
        }

        public void Create(string profileId, ProfileData profileData) {
            _log.Info($"Create new profile {profileId}");
            
            if (_profiles.ContainsKey(profileId)) {
                _log.Error($"Profile with name '{profileId}' already exists in cache!");
                throw new InvalidProfileNameException();
            }
            
            if (!ValidateProfileId(profileId)) {
                _log.Error("Invalid profile id!");
                throw new InvalidProfileNameException();
            }

            if (!ValidateVersion(profileData.FullVersion)) {
                _log.Error($"Cant create profile {profileId}. Invalid version!");
                throw new InvalidProfileVersionException();
            }
            
            var profileDir = Path.Combine(_profilesPath, profileId);
            if (!Directory.Exists(profileDir))
                Directory.CreateDirectory(profileDir);
            
            _json.WriteFile(profileData, Path.Combine(profileDir, ProfileIndexName));
            _profiles.Add(profileId, profileData.Clone());
        }

        public void Rename(string oldId, string newId) {
            _log.Info($"Rename '{oldId}' -> '{newId}'");
            if (!_profiles.ContainsKey(oldId)) {
                _log.Error($"Profile with name '{oldId}' does not exist in cache!");
                throw new InvalidProfileNameException();
            }

            if (_profiles.ContainsKey(newId)) {
                _log.Error($"Profile with name '{newId}' already exists in cache!");
                throw new InvalidProfileNameException();
            }

            if (!ValidateProfileId(newId)) {
                _log.Error("Invalid new profile id!");
                throw new InvalidProfileNameException();
            }
            
            Directory.Move(Path.Combine(_profilesPath, oldId), Path.Combine(_profilesPath, newId));

            _profiles[newId] = _profiles[oldId];
            _profiles.Remove(oldId);
        }

        public void UpdateData(string profileId, ProfileData profileData) {
            _log.Info($"Update profile {profileId}");
            
            if (!_profiles.ContainsKey(profileId)) {
                _log.Error($"Profile with name '{profileId}' does not exist in cache!");
                throw new InvalidProfileNameException();
            }
            
            _json.WriteFile(profileData, Path.Combine(_profilesPath, profileId, ProfileIndexName));
            _profiles[profileId] = profileData.Clone();
        }

        private static bool ValidateVersion(FullVersionId version) {
            return version.Prefix != null && version.Version != null;
        }

        private static bool ValidateProfileId(string id) {
            if (string.IsNullOrEmpty(id))
                return false;

            if (id.Trim() == string.Empty)
                return false;

            var allowed = new[] {' ', '-', '_', '.', ',', '!', '?'};

            return id.All(ch => char.IsLetterOrDigit(ch) || allowed.Contains(ch));
        }


        public void UpdateInstalledFiles(string profileId) {
            var profileDir = Path.Combine(_profilesPath, profileId);
            var versionId = _profiles[profileId].FullVersion;
            InstallFiles(profileDir, versionId);
        }

        private void InstallFiles(string profileDir, FullVersionId versionId) {
            var filesIndexPath = Path.Combine(profileDir, FilesIndexName);
            var dataIndexPath = Path.Combine(_versionsPath, versionId.Prefix, versionId.Version, DataIndexName);

            var dataIndex = _json.ReadFile<VersionDataIndex>(dataIndexPath);

            if (File.Exists(filesIndexPath)) {
                var filesIndex = _json.ReadFile<FilesIndex>(filesIndexPath);
                var obsoleteFiles = dataIndex.Files != null ?
                    filesIndex.Installed.Except(dataIndex.Files.Index.Keys) :
                    filesIndex.Installed;
                
                foreach (var fileName in obsoleteFiles)
                    File.Delete(Path.Combine(profileDir, fileName));
            }

            if (dataIndex.Files == null) {
                File.Delete(filesIndexPath);
                return;
            }

            foreach (var (fileName, fileInfo) in dataIndex.Files.Index) {
                var src = Path.Combine(_versionsPath, versionId.Prefix, versionId.Version, "files", fileName);
                var dst = Path.Combine(profileDir, fileName);

                var isMutable = dataIndex.Files.Mutables?.Contains(fileName) ?? false;
                if (IsSameFileExists(dst, fileInfo.Size, fileInfo.Hash, isMutable))
                    continue;
                
                var baseDir = Path.GetDirectoryName(dst);
                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);
                    
                File.Copy(src, dst, true);
            }
            
            _json.WriteFile(new FilesIndex {Installed = dataIndex.Files.Index.Keys.ToArray()}, filesIndexPath);
        }
        
        private bool IsSameFileExists(string path, long size, string hash, bool isMutable) {
            var fileInfo = new FileInfo(path);
            
            if (!fileInfo.Exists)
                return false;

            if (isMutable)
                return true;

            if (fileInfo.Length != size)
                return false;

            using (var fileStream = File.OpenRead(path))
            using (var sha1 = new SHA1CryptoServiceProvider()) {
                _hashStringBuilder.Clear();
                
                foreach (var b in sha1.ComputeHash(fileStream))
                    _hashStringBuilder.Append(b.ToString("x2"));

                return _hashStringBuilder.ToString() == hash;
            }
        }

        public async Task Run(string profileId, string userName) {
            var clientToken = Guid.NewGuid().ToString();
            var accessToken = Guid.NewGuid().ToString();
            await Run(profileId, userName, clientToken, accessToken);
        }

        public async Task Run(string profileId, string userName, string clientToken, string accessToken) {
            var profile = _profiles[profileId];
            var prefix = profile.FullVersion.Prefix;
            var version = profile.FullVersion.Version;
            
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
            
            var gameArgsMap = new Dictionary<string, string> {
                {"${auth_player_name}", userName},
                {"${version_name}", version},
                {"${auth_uuid}", clientToken},
                {"${auth_access_token}", accessToken},
                {"${game_directory}", Path.Combine(_profilesPath, profileId)},
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
            var exitCode = await RunProcessAsync("java", args);
            _log.Info($"Client terminated with exit code {exitCode}");

            Directory.Delete(nativesDir, true);
        }

        private async Task<int> RunProcessAsync(string application, IEnumerable<string> args) {
            var info = new ProcessStartInfo(application) {
                RedirectStandardOutput = true,
                RedirectStandardError = true
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

            var context = SynchronizationContext.Current;
            
            process.Exited += (sender, args) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += (sender, args) => context.Post(RunLog, args.Data);
            process.ErrorDataReceived += (sender, args) => context.Post(RunLog, args.Data);

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
                _runLogger.WriteLine(line);
        }
    }
}