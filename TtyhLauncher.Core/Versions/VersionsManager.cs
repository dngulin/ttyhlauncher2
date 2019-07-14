using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TtyhLauncher.Json.Minecraft;
using TtyhLauncher.Json.Ttyh;
using TtyhLauncher.Logs;
using TtyhLauncher.Utils;
using TtyhLauncher.Utils.Data;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Versions {
    public class VersionsManager {
        private const string VersionsDirectory = "versions";
        private const string AssetsDirectory = "assets";
        private const string LibrariesDirectory = "libraries";
        
        private const string IndexName = "prefixes.json";

        private readonly string _storeUrl;
        
        private readonly string _versionsPath;
        private readonly string _assetsPath;
        private readonly string _librariesPath;
        
        private readonly string _indexPath;
        private readonly PrefixesIndex _index;
        
        private readonly WrappedLogger _log;
        private readonly HttpClient _client;
        private readonly JsonParser _json;

        public CachedPrefixInfo[] Prefixes { get; private set; }

        public VersionsManager(string storeUrl, string dataDir, HttpClient client, JsonParser json, ILogger logger) {
            _storeUrl = storeUrl;
            _client = client;
            _json = json;
            _log = new WrappedLogger(logger, "Versions");
            _log.Info("Initializing...");

            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // XDG_DATA_HOME
            
            _versionsPath = Path.Combine(dataPath, dataDir, VersionsDirectory);
            _assetsPath = Path.Combine(dataPath, dataDir, AssetsDirectory);
            _librariesPath = Path.Combine(dataPath, dataDir, LibrariesDirectory);
            
            _indexPath = Path.Combine(_versionsPath, IndexName);

            if (!Directory.Exists(_versionsPath))
                Directory.CreateDirectory(_versionsPath);

            if (File.Exists(_indexPath)) {
                try {
                    _index = _json.ReadFile<PrefixesIndex>(_indexPath);
                }
                catch (JsonSerializationException e) {
                    _log.Error("Can't parse local prefixes index: " + e.Message);
                }
            }

            _index = _index ?? new PrefixesIndex();
            _log.Info($"Initialized with {_index.Prefixes.Count} prefix(es)!");
        }
        
        public bool Contains(FullVersionId id) {
            var prefixId = Array.FindIndex(Prefixes, p => p.Id == id.Prefix);
            return prefixId >= 0 && Prefixes[prefixId].Versions.Any(v => v == id.Version);
        }

        public async Task FetchPrefixes() {
            _log.Info("Fetching versions...");

            PrefixesIndex prefixesIndex;
            var remoteVersions = new Dictionary<string, PrefixVersionsIndex>();
            
            try {
                _log.Info("Fetching prefixes.json...");
                prefixesIndex = await ParseJsonFromUrlAsync<PrefixesIndex>($"{_storeUrl}/prefixes.json");

                foreach (var (prefixId, _) in prefixesIndex.Prefixes) {
                    _log.Info($"Fetching prefix '{prefixId}'...");
                    
                    var versionsIndexUrl = $"{_storeUrl}/{prefixId}/versions/versions.json";
                    remoteVersions.Add(prefixId, await ParseJsonFromUrlAsync<PrefixVersionsIndex>(versionsIndexUrl));
                }
            }
            catch (HttpRequestException e) {
                _log.Error("Network request error: " + e.Message);
                throw;
            }
            catch (JsonSerializationException e) {
                _log.Error("Can't parse fetched data: " + e.Message);
                throw;
            }

            foreach (var (prefixId, prefixEntry) in prefixesIndex.Prefixes) {
                _index.Prefixes[prefixId] = prefixEntry;
            }
            
            _json.WriteFile(_index, _indexPath);
            
            UpdatePrefixes(remoteVersions);
            _log.Info("Fetching finished!");
        }

        private void UpdatePrefixes(IReadOnlyDictionary<string, PrefixVersionsIndex> remoteVersions) {
            Prefixes = new CachedPrefixInfo[_index.Prefixes.Count];
            
            var i = 0;
            foreach (var (prefixId, prefix) in _index.Prefixes) {
                _log.Info($"Looking for cached versions in the '{prefixId}' prefix...");

                if (!remoteVersions.TryGetValue(prefixId, out var versionsIndex))
                    versionsIndex = new PrefixVersionsIndex();

                var localVersions = GetLocalVersions(prefixId);
                Prefixes[i++] = new CachedPrefixInfo(prefixId, prefix.About, versionsIndex, localVersions);
            }
            
            Array.Sort(Prefixes);
        }

        private VersionIndex[] GetLocalVersions(string prefixId) {
            var list = new List<VersionIndex>();

            var prefixDir = Path.Combine(_versionsPath, prefixId);
            if (Directory.Exists(prefixDir)) {
                foreach (var version in Directory.EnumerateDirectories(prefixDir).Select(Path.GetFileName)) {
                    var versionIndexPath = Path.Combine(_versionsPath, prefixId, version, $"{version}.json");
                    if (!File.Exists(versionIndexPath))
                        continue;

                    _log.Info($"Parse local version {prefixId}/{version}.json");

                    try {
                        list.Add(_json.ReadFile<VersionIndex>(versionIndexPath));
                    }
                    catch (Exception e) {
                        _log.Info($"Can't parse version {prefixId}/{version}.json: {e.Message}");
                    }
                }
            }

            return list.ToArray();
        }

        public async Task FetchVersionIndexes(FullVersionId fullVersionId) {
            _log.Info($"Fetching {fullVersionId}...");

            try {
                await InternalFetchVersionIndexes(fullVersionId);
            }
            catch (Exception e) {
                _log.Error($"Fail! Can't fetch {fullVersionId}: {e.Message}");
                throw;
            }

            _log.Info($"Success! Version {fullVersionId} is updated!");
        }

        private async Task InternalFetchVersionIndexes(FullVersionId fullVersionId) {
            var basePath = Path.Combine(_versionsPath, fullVersionId.Prefix, fullVersionId.Version);
            var baseUrl = $"{_storeUrl}/{fullVersionId.Prefix}/{fullVersionId.Version}";

            var versionIndexName = $"{fullVersionId.Version}.json";

            foreach (var fileName in new[] {versionIndexName, "data.json"}) {
                var url = $"{baseUrl}/{fileName}";
                var path = Path.Combine(basePath, fileName);
                await DownloadFile(path, url);
            }

            var versionIndex = _json.ReadFile<VersionIndex>(Path.Combine(basePath, versionIndexName));
            
            var assetsIndexRelativePath = IndexTool.GetAssetIndexPath(versionIndex);
            var assetsIndexPath = Path.Combine(_assetsPath, "indexes", assetsIndexRelativePath);
            var assetsIndexUrl = $"{_storeUrl}/assets/indexes/{assetsIndexRelativePath}";

            await DownloadFile(assetsIndexPath, assetsIndexUrl);
        }

        public DownloadTarget[] GetVersionFilesInfo(FullVersionId fullVersionId) {
            try {
                return InternalGetVersionFilesInfo(fullVersionId);
            }
            catch (Exception e) {
                _log.Error("Error: " + e.Message);
                throw;
            }
        }

        private DownloadTarget[] InternalGetVersionFilesInfo(FullVersionId fullVersionId) {
            _log.Info($"Collecting files info for version {fullVersionId}...");
            var result = new List<DownloadTarget>(1024);
            var prefix = fullVersionId.Prefix;
            var version = fullVersionId.Version;
            
            var basePath = Path.Combine(_versionsPath, prefix, version);
            var baseUrl = $"{_storeUrl}/{prefix}/{version}";
            
            _log.Info($"Add {version}.jar to list");
            var dataIndex = _json.ReadFile<VersionDataIndex>(Path.Combine(basePath, "data.json"));

            var jarPath = Path.Combine(basePath, $"{version}.jar");
            var jarUrl = $"{baseUrl}/{version}.jar";
            result.Add(new DownloadTarget(jarPath, jarUrl, dataIndex.Main.Size, dataIndex.Main.Hash));

            if (dataIndex.Files?.Index != null) {
                _log.Info("Add custom files to list");
                foreach (var (relativePath, info) in dataIndex.Files.Index) {
                    var path = Path.Combine(basePath, "files", relativePath);
                    var url = $"{baseUrl}/files/{relativePath}";
                    result.Add(new DownloadTarget(path, url, info.Size, info.Hash));
                }
            }

            _log.Info("Add libraries to list");
            var versionIndex = _json.ReadFile<VersionIndex>(Path.Combine(basePath, $"{version}.json"));
            AppendLibsInfo(versionIndex, dataIndex, result);
            
            _log.Info("Add assets to list");
            var assetsIndexRelativePath = IndexTool.GetAssetIndexPath(versionIndex);
            var assetsIndexPath = Path.Combine(_assetsPath, "indexes", assetsIndexRelativePath);
            var assetsIndex = _json.ReadFile<AssetsIndex>(assetsIndexPath);
            AppendAssets(assetsIndex.Objects, result);

            _log.Info($"Complete! Total files: {result.Count}");
            return result.ToArray();
        }

        private void AppendLibsInfo(VersionIndex vIndex, VersionDataIndex dIndex, List<DownloadTarget> list) {
            foreach (var libraryInfo in vIndex.Libraries) {
                if (!IndexTool.IsLibraryAllowed(libraryInfo))
                    continue;

                var relativePath = IndexTool.GetLibraryPath(libraryInfo);
                
                if (!dIndex.Libs.ContainsKey(relativePath)) {
                    _log.Warn($"Data index does not contain library {libraryInfo.Name}");
                    continue;
                }
                
                var path = Path.Combine(_librariesPath, relativePath);
                var url = $"{_storeUrl}/libraries/{relativePath}";
                var fileInfo = dIndex.Libs[relativePath];
                
                list.Add(new DownloadTarget(path, url, fileInfo.Size, fileInfo.Hash));
            }
        }
        
        private void AppendAssets(Dictionary<string,CheckInfo> assets, List<DownloadTarget> list) {
            foreach (var (_, fileInfo) in assets) {
                var relativePath = $"{fileInfo.Hash.Substring(0, 2)}/{fileInfo.Hash}";
                
                var path = Path.Combine(_assetsPath, "objects", relativePath);
                var url = $"{_storeUrl}/assets/objects/{relativePath}";
                
                list.Add(new DownloadTarget(path, url, fileInfo.Size, fileInfo.Hash));
            }
        }

        private async Task<T> ParseJsonFromUrlAsync<T>(string url) {
            using (var httpStream = await _client.GetStreamAsync(url))
            using (var textReader = new StreamReader(httpStream))
            using (var jsonReader = new JsonTextReader(textReader)) {
                return _json.Serializer.Deserialize<T>(jsonReader);
            }
        }

        private async Task DownloadFile(string path, string url) {
            _log.Info($"Download {url} -> {path}");

            var dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            
            
            using (var httpStream = await _client.GetStreamAsync(url))
            using (var fileStream = File.Create(path)) {
                await httpStream.CopyToAsync(fileStream);
            }
        }
    }
}