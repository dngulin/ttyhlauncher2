using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Localization;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Master.Data;
using TtyhLauncher.Master.Exceptions;
using TtyhLauncher.Profiles;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Settings;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils;
using TtyhLauncher.Utils.Data;
using TtyhLauncher.Versions;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher {
    public class Launcher {
        private readonly SettingsManager _settings;
        private readonly VersionsManager _versions;
        private readonly ProfilesManager _profiles;
        
        private readonly TtyhClient _ttyhClient;
        private readonly GameRunner _runner;
        private readonly HashChecker _hashChecker;
        private readonly Downloader _downloader;
        
        private readonly ILauncherUi _ui;
        private readonly ILogger _logger;
        private readonly WrappedLogger _log;

        public Launcher(
            SettingsManager settings,
            VersionsManager versions,
            ProfilesManager profiles,
            HttpClient httpClient,
            TtyhClient ttyhClient,
            GameRunner runner,
            ILauncherUi ui,
            ILogger logger,
            string launcherName) {
            
            _settings = settings;
            _versions = versions;
            _profiles = profiles;
            
            _ttyhClient = ttyhClient;
            _runner = runner;
            
            _hashChecker = new HashChecker(logger);
            _downloader = new Downloader(httpClient, logger);
            
            _ui = ui;
            
            _logger = logger;
            _logger.OnLog += _ui.AppendLog;
            
            _log = new WrappedLogger(logger, launcherName);

            _ui.OnExit += HandleExit;
            _ui.OnPlayButtonClicked += HandlePlayButtonClicked;
            _ui.OnOfflineModeToggle += HandleOfflineModeToggle;
            
            _ui.OnEditProfileClicked += HandleEditProfile;
            _ui.OnAddProfileClicked += HandleAddProfile;
        }

        public void Start() {
            _log.Info("Starting...");

            LoadWindowSettings();
            _ui.SetWindowVisible(true);
            TryBecomeOnline();
        }

        private void HandleExit() {
            _log.Info("Terminating...");
            
            _logger.OnLog -= _ui.AppendLog;
            
            SaveWindowSettings();
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

            if (_profiles.IsEmpty) {
                foreach (var prefix in _versions.Prefixes) {
                    var fullVersion = new FullVersionId(prefix.Id, prefix.LatestVersion);
                    try {
                        _profiles.Create(prefix.About, new ProfileData {FullVersion = fullVersion});
                        if (!_profiles.Contains(_settings.Profile)) {
                            _settings.Profile = prefix.About;
                        }
                    }
                    catch (Exception e) {
                        _log.Error("Can't create default profile: " + e.Message);
                    }
                }

                _ui.SetProfiles(_profiles.Names, _settings.Profile);
                
            } else if (!_profiles.Contains(_settings.Profile)) {
                _settings.Profile = _profiles.Names[0];
                _ui.SetProfiles(_profiles.Names, _settings.Profile);
            }

            if (_ui.OfflineMode)
                _ui.ShowErrorMessage(Strings.FailedToBecomeOnline);
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
        
        private void HandleEditProfile() {
            var id = _ui.SelectedProfile;
            var data = _profiles.GetProfileData(id);
            _ui.ShowProfile(id, data, _versions.Prefixes, (newId, newData) => TryUpdateProfile(id, newId, newData));
        }

        private void TryUpdateProfile(string oldId, string newId, ProfileData data) {
            if (oldId != newId) {
                _profiles.Rename(oldId, newId);
                _ui.SetProfiles(_profiles.Names, newId);
            }
            
            _profiles.UpdateData(newId, data);
        }
        
        private void HandleAddProfile() {
            if (_versions.Prefixes.Count <= 0) {
                _ui.ShowErrorMessage(Strings.NoVersionsAvailable);
                return;
            }

            var prefix = _versions.Prefixes[0];
            var version = new FullVersionId(prefix.Id, prefix.LatestVersion);
            var data = new ProfileData {
                FullVersion = version
            };
            
            _ui.ShowProfile("New profile", data, _versions.Prefixes, TryCreateProfile);
        }

        private void TryCreateProfile(string id, ProfileData data) {
            _profiles.Create(id, data);
            _ui.SetProfiles(_profiles.Names, id);
        }

        private void LoadWindowSettings() {
            _ui.SetProfiles(_profiles.Names, _settings.Profile);
            
            _ui.UserName = _settings.UserName;
            _ui.Password = _settings.Password;
            
            _ui.SavePassword = _settings.SavePassword;
            _ui.HideOnRun = _settings.HideOnRun;
            
            _ui.SetWindowSize(_settings.WindowWidth, _settings.WindowHeight);
        }

        private void SaveWindowSettings() {
            _settings.Profile = _ui.SelectedProfile;
            _settings.UserName = _ui.UserName;
            _settings.Password = _ui.SavePassword ? _ui.Password : string.Empty;
            
            _settings.SavePassword = _ui.SavePassword;
            _settings.HideOnRun = _ui.HideOnRun;

            _ui.GetWindowSize(out var w, out var h);
            _settings.WindowWidth = w;
            _settings.WindowHeight = h;
        }

        private async void HandlePlayButtonClicked() {
            if (!_profiles.Contains(_ui.SelectedProfile)) {
                _ui.ShowErrorMessage(Strings.ProfileDoesNotExist);
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
                _profiles.UpdateInstalledFiles(profileId);
            }
            catch {
                _ui.ShowErrorMessage(Strings.FailedToUpdateProfile);
                return;
            }

            LoginResultData tokens;
            try {
                tokens = await _ttyhClient.Login(_ui.UserName, _ui.Password);
            }
            catch (ErrorAnswerException e) {
                _ui.ShowErrorMessage(Strings.FailedToLogin, e.Message);
                return;
            }
            catch (Exception) {
                _ui.ShowErrorMessage(Strings.FailedToLogin);
                return;
            }
                
            await Run(profileId, tokens);
        }

        private async Task<bool> CheckProfile(ProfileData profile) {
            try {
                await _versions.FetchVersionIndexes(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage(Strings.FailedToFetchIndexes);
                return false;
            }

            DownloadTarget[] fileList;
            try {
                fileList = _versions.GetVersionFilesInfo(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage(Strings.VersionIndexesCorrupted);
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
            catch {
                _ui.ShowErrorMessage(Strings.FailedToCheckVersion);
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
                _ui.ShowErrorMessage(Strings.DownloadVersionError);
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
                _ui.SetWindowVisible(false);

            try {
                var profile = _profiles.GetProfileData(profileId);
                
                if (tokens == null) {
                    await _runner.Run(profileId, profile, _ui.UserName);
                }
                else {
                    await _runner.Run(profileId, profile, _ui.UserName, tokens.ClientToken, tokens.AccessToken);
                }
            }
            catch (Exception) {
                _ui.ShowErrorMessage(Strings.RunError);
            }

            if (_ui.HideOnRun)
                _ui.SetWindowVisible(true);
        }
    }
}