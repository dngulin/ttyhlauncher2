using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NGettext;
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
        private readonly Translator _tr;

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

            var translationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
            _tr = new Translator(new Catalog("core", translationsPath));

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

            _ui.OnUploadSkinClicked += HandleSkinUploadClicked;
            
            _ui.OnEditProfileClicked += HandleEditProfile;
            _ui.OnAddProfileClicked += HandleAddProfile;
            _ui.OnRemoveProfileClicked += HandleRemoveProfile;
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
                    var fullVersion = new FullVersionId(prefix.Id, IndexTool.VersionAliasLatest);
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
                _ui.ShowErrorMessage(_tr._("Failed to switch into online mode!"));
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
            
            if (!_profiles.Contains(id)) {
                _ui.ShowErrorMessage(_tr._("Selected profile does not exist!"));
                return;
            }
            
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
            if (_versions.Prefixes.Length <= 0) {
                _ui.ShowErrorMessage(_tr._("There are no known client versions!"));
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

        private void HandleRemoveProfile() {
            var id = _ui.SelectedProfile;
            
            if (!_profiles.Contains(id)) {
                _ui.ShowErrorMessage(_tr._("Selected profile does not exist!"));
                return;
            }

            try {
                _profiles.Remove(id);
            }
            catch {
                _ui.ShowErrorMessage(_tr._("Failed to delete the profile!"));
                return;
            }

            var profileNames = _profiles.Names;
            _settings.Profile = profileNames.Length > 0 ? profileNames[0] : string.Empty;
            _ui.SetProfiles(_profiles.Names, _settings.Profile);
        }

        private void HandleSkinUploadClicked() {
            _ui.ShowSkinUpload(TryUploadSkin);
        }

        private async Task TryUploadSkin(string path, bool isSlim) {
            var bytes = await File.ReadAllBytesAsync(path);
            await _ttyhClient.UploadSkin(_ui.UserName, _ui.Password, bytes, isSlim);
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
                _ui.ShowErrorMessage(_tr._("Selected profile does not exist!"));
                return;
            }

            _ui.SetInteractable(false);
            
            var profileId = _ui.SelectedProfile;
            var profile = _profiles.GetProfileData(profileId);
            
            if (profile.FullVersion.Version == IndexTool.VersionAliasLatest) {
                _log.Info($"Resolving a version {profile.FullVersion}");
                var prefix = profile.FullVersion.Prefix;
                var version = _versions.Prefixes.FirstOrDefault(p => p.Id == prefix)?.LatestVersion;
                profile.FullVersion = new FullVersionId(prefix, version ?? IndexTool.VersionAliasLatest);
                _log.Info($"Version resolved to {profile.FullVersion}");
            }
            
            await CheckAndRun(profileId, profile);
            
            _ui.SetInteractable(true);
        }

        private async Task CheckAndRun(string profileId, ProfileData profile) {
            
            
            if (_ui.OfflineMode) {
                await Run(profileId, profile);
                return;
            }

            if (profile.CheckVersionFiles && !await CheckProfile(profile)) {
                return;
            }
            
            try {
                _profiles.UpdateInstalledFiles(profileId, profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage(_tr._("Failed to update selected profile! Try to enable a version checking."));
                return;
            }

            LoginResultData tokens;
            try {
                tokens = await _ttyhClient.Login(_ui.UserName, _ui.Password);
            }
            catch (ErrorAnswerException e) {
                _ui.ShowErrorMessage(_tr._("Failed to login! Are your nickname and password correct?"), e.Message);
                return;
            }
            catch (Exception) {
                _ui.ShowErrorMessage(_tr._("Failed to login! Are your nickname and password correct?"));
                return;
            }
                
            await Run(profileId, profile, tokens);
        }

        private async Task<bool> CheckProfile(ProfileData profile) {
            try {
                await _versions.FetchVersionIndexes(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage(_tr._("Failed to fetch version indexes!"));
                return false;
            }

            DownloadTarget[] fileList;
            try {
                fileList = _versions.GetVersionFilesInfo(profile.FullVersion);
            }
            catch {
                _ui.ShowErrorMessage(_tr._("Looks like version indexes are corrupted! See details in the log."));
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
                _ui.ShowErrorMessage(_tr._("Failed to check version files! See details in the log."));
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
                _ui.ShowErrorMessage(_tr._("Failed to download version files! See details in the log."));
                return false;
            }
            finally {
                _ui.HideTask();
                _ui.OnTaskCancelClicked -= cts.Cancel;
            }

            return true;
        }

        private async Task Run(string profileId, ProfileData profile, LoginResultData tokens = null) {
            if (_ui.HideOnRun)
                _ui.SetWindowVisible(false);

            try {
                if (tokens == null) {
                    await _runner.Run(profileId, profile, _ui.UserName);
                }
                else {
                    await _runner.Run(profileId, profile, _ui.UserName, tokens.ClientToken, tokens.AccessToken);
                }
            }
            catch (Exception) {
                _ui.ShowErrorMessage(_tr._("Something went wrong while minecraft running! See details in the log."));
            }

            if (_ui.HideOnRun)
                _ui.SetWindowVisible(true);
        }
    }
}