using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        private const string ProfileIndexName = "profile.json";
        private const string FilesIndexName = "files.json";
        private const string DataIndexName = "data.json";
        
        private readonly JsonParser _json;
        private readonly WrappedLogger _log;
        private readonly StringBuilder _hashStringBuilder;

        private readonly string _profilesPath;
        private readonly string _versionsPath;

        
        private readonly Dictionary<string, ProfileData> _profiles = new Dictionary<string, ProfileData>();
        
        public ProfilesManager(string dataDirectory, JsonParser json, ILogger logger) {
            _json = json;
            _hashStringBuilder = new StringBuilder(40);
            _log = new WrappedLogger(logger, "Profiles");
            _log.Info("Initializing...");
            
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // XDG_DATA_HOME
            _profilesPath = Path.Combine(dataPath, dataDirectory, ProfilesDirectory);
            _versionsPath = Path.Combine(dataPath, dataDirectory, VersionsDirectory);

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
    }
}