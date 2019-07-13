using System;
using System.Collections.Generic;
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

        void ShowErrorMessage(string message);
        bool AskForDownloads(int filesCount, long totalSize);
        
        IProgress<DownloadingState> ShowDownloadingTask();
        IProgress<CheckingState> ShowCheckingTask();
        void HideTask();

        void ShowProfile(string id, ProfileData profile, IReadOnlyList<CachedPrefixInfo> prefixes, Action<string, ProfileData> doSave);
    }
}