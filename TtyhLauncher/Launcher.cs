using System.Net.Http;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Profiles;
using TtyhLauncher.Settings;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils;
using TtyhLauncher.Versions;

namespace TtyhLauncher {
    public class Launcher {
        private readonly SettingsManager _settings;
        private readonly VersionsManager _versions;
        private readonly ProfilesManager _profiles;
        private readonly HttpClient _httpClient;
        private readonly TtyhClient _ttyhClient;
        private readonly MainWindow _ui;
        private readonly ILogger _logger;
        private readonly WrappedLogger _log;

        public Launcher(SettingsManager settings, VersionsManager versions, ProfilesManager profiles,
            HttpClient httpClient, TtyhClient ttyhClient, MainWindow ui, ILogger logger, string name) {
            _settings = settings;
            _versions = versions;
            _profiles = profiles;
            
            _httpClient = httpClient;
            _ttyhClient = ttyhClient;
            
            _ui = ui;
            
            _logger = logger;
            _logger.OnLog += _ui.AppendLog;
            
            _log = new WrappedLogger(logger, name);

            _ui.DeleteEvent += (sender, args) => HandleMainWindowClose();
            _ui.OnPlayButtonClicked += HandlePlayButtonClicked;
        }

        public async void Start() {
            _log.Info("Starting...");
            LoadWindowSettings();
            
            await _versions.FetchPrefixes();
            
            if (!_profiles.Contains(_settings.Profile) && _versions.Prefixes.Count > 0) {
                var defaultPrefix = _versions.Prefixes[0];
                _settings.Profile = !_profiles.IsEmpty ? _profiles.Names[0] : _profiles.CreateDefault(defaultPrefix);
                _log.Info($"Created default profile : '{_settings.Profile}'");
            }

            _ui.ShowAll();
        }

        private void HandleMainWindowClose() {
            _log.Info("Terminating...");
            _logger.OnLog -= _ui.AppendLog;

            SaveWindowSettings();
            
            Gtk.Application.Quit();
        }

        private void LoadWindowSettings() {
            if (!_profiles.IsEmpty) {
                _ui.SetProfiles(_profiles.Names, _settings.Profile);
            }
            
            _ui.UserName = _settings.UserName;
            _ui.Password = _settings.Password;
            
            _ui.SavePassword = _settings.SavePassword;
            _ui.OfflineMode = _settings.OfflineMode;
            _ui.HideOnRun = _settings.HideOnRun;
            
            _ui.Resize(_settings.WindowWidth, _settings.WindowHeight);
        }

        private void SaveWindowSettings() {
            _settings.Profile = _ui.SelectedProfile;
            _settings.UserName = _ui.UserName;
            _settings.Password = _ui.SavePassword ? _ui.Password : string.Empty;
            
            _settings.SavePassword = _ui.SavePassword;
            _settings.OfflineMode = _ui.OfflineMode;
            _settings.HideOnRun = _ui.HideOnRun;

            _ui.GetSize(out var w, out var h);
            _settings.WindowWidth = w;
            _settings.WindowHeight = h;
        }

        private async void HandlePlayButtonClicked() {
            _ui.Sensitive = false;

            var profileId = _ui.SelectedProfile;
            
            var version = _profiles.GetVersion(profileId);
            await _versions.FetchVersionIndexes(version);
            var checkList = _versions.GetVersionFilesInfo(version);

            var hashChecker = new HashChecker(_logger);
            var downloads = await hashChecker.CheckFiles(checkList);
                
            var downloader = new Downloader(_httpClient, _logger);
            await downloader.Download(downloads);

            _profiles.Update(profileId);
            
            var tokens = await _ttyhClient.Login(_ui.UserName, _ui.Password);

            await _profiles.Run(profileId, _ui.UserName, tokens.ClientToken, tokens.AccessToken);

            _ui.Sensitive = true;
        }
    }
}