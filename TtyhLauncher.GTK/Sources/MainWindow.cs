using System;
using System.Reflection;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using Pango;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils.Data;
using TtyhLauncher.Versions.Data;
using Action = System.Action;
using FormItem = Gtk.Builder.ObjectAttribute;

namespace TtyhLauncher.GTK {
    public class MainWindow: ApplicationWindow, ILauncherUi {
        private const int MaxLogLines = 256;
        
        [FormItem] private readonly CheckMenuItem _actToggleOffline = null;
        [FormItem] private readonly CheckMenuItem _actToggleSavePassword = null;
        [FormItem] private readonly CheckMenuItem _actToggleHideWindow = null;
        
        [FormItem] private readonly MenuItem _actUploadSkin = null;

        [FormItem] private readonly MenuItem _actAbout = null;
        
        [FormItem] private readonly MenuItem _actEditProfile = null;
        [FormItem] private readonly MenuItem _actAddProfile = null;
        [FormItem] private readonly MenuItem _actRemoveProfile = null;
        
        [FormItem] private readonly TextView _logTextView = null;
        private readonly TextBuffer _logBuffer;
        
        [FormItem] private readonly ComboBoxText _comboProfiles = null;
        [FormItem] private readonly Entry _entryPlayer = null;
        [FormItem] private readonly Entry _entryPassword = null;
        [FormItem] private readonly Button _buttonPlay = null;
        
        [FormItem] private readonly Stack _stackTask = null;
        [FormItem] private readonly Label _labelTaskNothing = null;
        [FormItem] private readonly Box _boxTask = null;
        [FormItem] private readonly Label _labelTask = null;
        [FormItem] private readonly ProgressBar _pbTask = null;
        [FormItem] private readonly Button _buttonTaskCancel = null;
        
        [FormItem] private readonly MenuBar _menu = null;
        [FormItem] private readonly ScrolledWindow _scroll = null;
        [FormItem] private readonly Grid _form = null;
        
        [FormItem] private readonly Image _imgLogo = null;

        public event Action OnExit;
        public event Action OnPlayButtonClicked;
        public event Action OnTaskCancelClicked;
        public event Action<bool> OnOfflineModeToggle;
        
        public event Action OnAddProfileClicked;
        public event Action OnEditProfileClicked;
        public event Action OnRemoveProfileClicked;
        
        public event Action OnUploadSkinClicked;

        public bool OfflineMode {
            get => _actToggleOffline.Active;
            set => _actToggleOffline.Active = value;
        }

        public bool SavePassword {
            get => _actToggleSavePassword.Active;
            set => _actToggleSavePassword.Active = value;
        }

        public bool HideOnRun {
            get => _actToggleHideWindow.Active;
            set => _actToggleHideWindow.Active = value;
        }

        public string UserName {
            get => _entryPlayer.Text;
            set => _entryPlayer.Text = value;
        }

        public string Password {
            get => _entryPassword.Text;
            set => _entryPassword.Text = value;
        }

        public string SelectedProfile => _comboProfiles.ActiveText;

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle) {
            builder.Autoconnect(this);
        }
        
        public MainWindow(string title) : this(new Builder("MainWindow.glade")) {
            Title = title;
            
            Icon = LoadPixbuf("icon.png");
            _imgLogo.Pixbuf = LoadPixbuf("logo.png");

            DeleteEvent += (s, a) => { OnExit?.Invoke(); Application.Quit(); };
            
            // Because of the GTK is a piece of shit the `Monospace` property doesn't work for a TextView
            // So, lets use deprecated API
#pragma warning disable 612
            _logTextView.OverrideFont(new FontDescription {Family = "Monospace"});
#pragma warning restore 612

            _logBuffer = _logTextView.Buffer;
            _logTextView.SizeAllocated += (s, a) => {
                var adj = _scroll.Vadjustment;
                adj.Value = adj.Upper - adj.PageSize;
            };
            
            _buttonPlay.Clicked += (s, a) => OnPlayButtonClicked?.Invoke();
            _buttonTaskCancel.Clicked += (s, a) => OnTaskCancelClicked?.Invoke();
            _actToggleOffline.Toggled += (s, a) => OnOfflineModeToggle?.Invoke(_actToggleOffline.Active);

            _actAddProfile.Activated += (s, a) => OnAddProfileClicked?.Invoke();
            _actEditProfile.Activated += (s, a) => OnEditProfileClicked?.Invoke();
            _actRemoveProfile.Activated += (s, a) => OnRemoveProfileClicked?.Invoke();

            _actUploadSkin.Activated += (s, a) => OnUploadSkinClicked?.Invoke();
        }
        
        public void SetWindowVisible(bool isVisible) {
            if (isVisible) {
                ShowAll();
            }
            else {
                Hide();
            }
        }

        public void SetWindowSize(int w, int h) => Resize(w, h);
        public void GetWindowSize(out int w, out int h) => GetSize(out w, out h);

        public void SetInteractable(bool interactable) {
            _menu.Sensitive = interactable;
            _scroll.Sensitive = interactable;
            _form.Sensitive = interactable;
        }

        public void SetProfiles(string[] names, string selected) {
            _comboProfiles.RemoveAll();
            foreach (var name in names)
                _comboProfiles.AppendText(name);
            
            _comboProfiles.Active = Array.IndexOf(names, selected);
        }

        public void AppendLog(string line) {
            var end = _logBuffer.EndIter;
            _logBuffer.Insert(ref end, line);
            _logBuffer.Insert(ref end, "\n");

            if (_logBuffer.LineCount < MaxLogLines) return;
            
            var cutStart = _logBuffer.StartIter;
            var cutEnd = _logBuffer.GetIterAtLine(1);
            _logBuffer.Delete(ref cutStart, ref cutEnd);
        }

        public void ShowErrorMessage(string message, string details = null) {
            Msg.Error(this, message, details);
        }

        public bool AskForDownloads(int filesCount, long totalSize) {
            var size = GetHumanReadableSize(totalSize);
            var msg = Tr._n(
                "Need to download {0} file with the total size {1}.",
                "Need to download {0} files with the total size {1}.",
                (ulong) filesCount);

            
            return Msg.Info(this, Tr._("Need to download files"), string.Format(msg, filesCount, size));
        }

        private static string GetHumanReadableSize(long size) {
            const long gb = 1024 * 1024 * 1024;
            const long mb = 1024 * 1024;
            const long kb = 1024;
            
            if (size > gb)
                return ((float) size / gb).ToString("F2") + Tr._("GiB");
            
            if (size > mb)
                return ((float) size / mb).ToString("F2") + Tr._("MiB");
            
            if (size > kb)
                return ((float) size / kb).ToString("F2") + Tr._("KiB");

            return size + Tr._("B");
        }

        public IProgress<DownloadingState> ShowDownloadingTask() {
            _labelTask.Text = Tr._("Downloading files");
            ShowTask();
            return new DownloadingProgress(_pbTask);
        }
        
        public IProgress<CheckingState> ShowCheckingTask() {
            _labelTask.Text = Tr._("Checking files");
            ShowTask();
            return new CheckingProgress(_pbTask);
        }

        private void ShowTask() {
            _pbTask.Text = "";
            _pbTask.Fraction = 0;
            
            _buttonTaskCancel.Sensitive = true;
            _stackTask.VisibleChild = _boxTask;
        }

        public void HideTask() {
            _buttonTaskCancel.Sensitive = false;
            _stackTask.VisibleChild = _labelTaskNothing;
        }

        public void ShowProfile(string id, ProfileData profile, CachedPrefixInfo[] prefixes, Action<string, ProfileData> save) {
            var window = new ProfileWindow(id, profile, prefixes, save) {TransientFor = this};
            window.ShowAll();
        }

        public void ShowSkinUpload(Func<string, bool, Task> upload) {
            var window = new SkinUploadWindow(upload) {TransientFor = this};
            window.ShowAll();
        }

        public bool ConfirmProfileDeletion(string id) {
            var questions = new[] {
                string.Format(Tr._("Profile '{0}' will be removed."), id),
                string.Format(Tr._("All data related to the profile will be lost!")),
                string.Format(Tr._("This is the last one confirmation. Please, check the profile name again."))
            };
            var title = Tr._("Confirm a profile deletion");

            foreach (var question in questions) {
                if (!Msg.Info(this, title, question))
                    return false;
            }

            return true;
        }

        private Pixbuf LoadPixbuf(string name) {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {
                return new Pixbuf(stream);
            }
        }
    }
}