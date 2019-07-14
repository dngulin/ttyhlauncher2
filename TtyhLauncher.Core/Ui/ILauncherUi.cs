using System;
using System.ComponentModel;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Utils.Data;
using TtyhLauncher.Versions.Data;

namespace TtyhLauncher.Ui {
    public interface ILauncherUi {
        event Action OnExit;
        event Action OnPlayButtonClicked;
        event Action OnTaskCancelClicked;
        event Action<bool> OnOfflineModeToggle;
        
        event Action OnAddProfileClicked;
        event Action OnEditProfileClicked;
        event Action OnRemoveProfileClicked;

        bool OfflineMode { get; set; }
        bool SavePassword { get; set; }
        bool HideOnRun { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        string SelectedProfile { get; }

        void SetWindowVisible(bool isVisible);
        void SetWindowSize(int w, int h);
        void GetWindowSize(out int w, out int h);
        
        void AppendLog(string line);
        void SetInteractable(bool interactable);
        void SetProfiles(string[] names, string selected);
        
        void ShowErrorMessage([Localizable(true)]string message, string details = null);
        bool AskForDownloads(int filesCount, long totalSize);
        
        IProgress<DownloadingState> ShowDownloadingTask();
        IProgress<CheckingState> ShowCheckingTask();
        void HideTask();

        void ShowProfile(string id, ProfileData profile, CachedPrefixInfo[] prefixes, Action<string, ProfileData> doSave);
    }
}