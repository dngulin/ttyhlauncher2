using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Master.Data;
using TtyhLauncher.Profiles;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Settings;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils;
using TtyhLauncher.Utils.Data;
using TtyhLauncher.Versions;

namespace TtyhLauncher {
    public class Launcher {
        private readonly SettingsManager _settings;
        private readonly VersionsManager _versions;
        private readonly ProfilesManager _profiles;
        
        private readonly TtyhClient _ttyhClient;
        private readonly HashChecker _hashChecker;
        private readonly Downloader _downloader;
        
        private readonly MainWindow _ui;
        private readonly ILogger _logger;
        private readonly WrappedLogger _log;

        public Launcher(SettingsManager settings, VersionsManager versions, ProfilesManager profiles,
            HttpClient httpClient, TtyhClient ttyhClient, MainWindow ui, ILogger logger, string name) {
            _settings = settings;
            _versions = versions;
            _profiles = profiles;
            
            _ttyhClient = ttyhClient;
            _hashChecker = new HashChecker(logger);
            _downloader = new Downloader(httpClient, logger);
            
            _ui = ui;
            
            _logger = logger;
            _logger.OnLog += _ui.AppendLog;
            
            _log = new WrappedLogger(logger, name);

            _ui.DeleteEvent += (sender, args) => HandleMainWindowClose();
            _ui.OnPlayButtonClicked += HandlePlayButtonClicked;
            _ui.OnOfflineModeToggle += HandleOfflineModeToggle;
        }

        public void Start() {
            _log.Info("Starting...");

            LoadWindowSettings();
            _ui.ShowAll();
            
            TryBecomeOnline();
        }

        private void HandleMainWindowClose() {
            _log.Info("Terminating...");
            _logger.OnLog -= _ui.AppendLog;

            SaveWindowSettings();
            
            Gtk.Application.Quit();
        }

        private void HandleOfflineModeToggle(bool offline) {
            if (offline) {
                _log.Info("Set offline mode");
                return;
            }

            TryBecomeOnline();
        }
        
        private async void TryBecomeOnline() {
            _log.Info("Trying to switch to the online mode...");
            
            _ui.SetInteractable(false);
            _ui.OfflineMode = !await TryFetchPrefixes();
            _ui.SetInteractable(true);

            var state = _ui.OfflineMode ? "offline" : "online";
            _log.Info($"Is {state} now!");
            
            if (!_profiles.Contains(_settings.Profile) && _versions.Prefixes.Count > 0) {
                var defaultPrefix = _versions.Prefixes[0];
                _settings.Profile = !_profiles.IsEmpty ? _profiles.Names[0] : _profiles.CreateDefault(defaultPrefix);
                _log.Info($"Created default profile : '{_settings.Profile}'");
                
                _ui.SetProfiles(_profiles.Names, _settings.Profile);
            }
            
            if (_ui.OfflineMode)
                _ui.ShowErrorMessage("offline_mode_enabled");
        }

        private async Task<bool> TryFetchPrefixes() {
            try {
                await _versions.FetchPrefixes();
            }
            catch {
                return false;
            }

            return true;
        } 

        private void LoadWindowSettings() {
            if (!_profiles.IsEmpty) {
                _ui.SetProfiles(_profiles.Names, _settings.Profile);
            }
            
            _ui.UserName = _settings.UserName;
            _ui.Password = _settings.Password;
            
            _ui.SavePassword = _settings.SavePassword;
            _ui.HideOnRun = _settings.HideOnRun;
            
            _ui.Resize(_settings.WindowWidth, _settings.WindowHeight);
        }

        private void SaveWindowSettings() {
            _settings.Profile = _ui.SelectedProfile;
            _settings.UserName = _ui.UserName;
            _settings.Password = _ui.SavePassword ? _ui.Password : string.Empty;
            
            _settings.SavePassword = _ui.SavePassword;
            _settings.HideOnRun = _ui.HideOnRun;

            _ui.GetSize(out var w, out var h);
            _settings.WindowWidth = w;
            _settings.WindowHeight = h;
        }

        private async void HandlePlayButtonClicked() {
            if (!_profiles.Contains(_ui.SelectedProfile)) {
                _ui.ShowErrorMessage("profile_does_not_exists");
                return;
            }

            _ui.SetInteractable(false);
            
            await CheckAndRun();
            
            _ui.SetInteractable(true);
        }

        private async Task CheckAndRun() {
            var profileId = _ui.SelectedProfile;
            
            if (_ui.OfflineMode) {
                await Run(profileId);
                return;
            }
                
            var profile = _profiles.GetProfileData(profileId);
                
            if (profile.CheckVersionOnRun && !await CheckProfile(profile)) {
                return;
            }
            
            try {
                _profiles.Update(profileId);
            }
            catch {
                _ui.ShowErrorMessage("cant_sync_profile");
                return;
            }

            LoginResultData tokens;
            try {
                tokens = await _ttyhClient.Login(_ui.UserName, _ui.Password);
            }
            catch (Exception) {
                _ui.ShowErrorMessage("login_error");
                return;
            }
                
            await Run(profileId, tokens);
        }

        private async Task<bool> CheckProfile(ProfileData profile) {
            try {
                await _versions.FetchVersionIndexes(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage("cant_update_version_indexes");
                return false;
            }

            DownloadTarget[] fileList;
            try {
                fileList = _versions.GetVersionFilesInfo(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage("corrupted_version_indexes");
                return false;
            }
            
            var checkingCts = new CancellationTokenSource();
            var checkingListener = _ui.ShowCheckingTask();
            _ui.OnTaskCancelClicked += checkingCts.Cancel;
            
            DownloadTarget[] downloads;
            try {
                downloads = await _hashChecker.CheckFiles(fileList, checkingCts.Token, checkingListener);
            }
            catch (OperationCanceledException) {
                return false;
            }
            catch (Exception e){
                _ui.ShowErrorMessage("cant_check_version " + e.Message);
                return false;
            }
            finally {
                _ui.HideTask();
                _ui.OnTaskCancelClicked -= checkingCts.Cancel;
            }

            return await AskForDownloads(downloads);
        }

        private async Task<bool> AskForDownloads(DownloadTarget[] downloads) {
            if (downloads.Length <= 0) {
                return true;
            }
            
            if (!_ui.AskForDownloads(downloads.Length, downloads.Sum(d => d.Size))) {
                return false;
            }

            var cts = new CancellationTokenSource();
            var progressListener = _ui.ShowDownloadingTask();
            _ui.OnTaskCancelClicked += cts.Cancel;

            try {
                await _downloader.Download(downloads, cts.Token, progressListener);
            }
            catch (OperationCanceledException) {
                return false;
            }
            catch {
                _ui.ShowErrorMessage("cant_download_version");
                return false;
            }
            finally {
                _ui.HideTask();
                _ui.OnTaskCancelClicked -= cts.Cancel;
            }

            return true;
        }

        private async Task Run(string profileId, LoginResultData tokens = null) {
            if (_ui.HideOnRun)
                _ui.Hide();

            try {
                if (tokens == null) {
                    await _profiles.Run(profileId, _ui.UserName);
                }
                else {
                    await _profiles.Run(profileId, _ui.UserName, tokens.ClientToken, tokens.AccessToken);
                }
            }
            catch (Exception) {
                _ui.ShowErrorMessage("run_error");
            }

            if (_ui.HideOnRun)
                _ui.ShowAll();
        }
    }
}