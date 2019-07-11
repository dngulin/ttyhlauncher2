using System;
using Gtk;
using Pango;
using TtyhLauncher.Utils.Data;
using Action = System.Action;
using FormItem = Gtk.Builder.ObjectAttribute;

namespace TtyhLauncher.Ui {
    public class MainWindow: ApplicationWindow {
        private const int MaxLogLines = 256;
        
        [FormItem] private readonly CheckMenuItem _actToggleOffline = null;
        [FormItem] private readonly CheckMenuItem _actToggleSavePassword = null;
        [FormItem] private readonly CheckMenuItem _actToggleHideWindow = null;
        
        [FormItem] private readonly MenuItem _actUploadSkin = null;
        [FormItem] private readonly MenuItem _actManageProfiles = null;
        
        [FormItem] private readonly MenuItem _actAbout = null;
        
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

        public event Action OnExit;
        public event Action OnPlayButtonClicked;
        public event Action OnTaskCancelClicked;
        public event Action<bool> OnOfflineModeToggle;
        
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
        }

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

        public void ShowErrorMessage(string message) {
            var dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, message);
            dialog.Title = "Error";
            dialog.Run();
            dialog.Destroy();
        }

        public bool AskForDownloads(int filesCount, long totalSize) {
            var size = GetHumanReadableSize(totalSize);
            var msg = $"Need to download {filesCount} files with the total size {size}.";
            
            var dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Question, ButtonsType.OkCancel, msg);
            dialog.Title = "Downloads required";
            
            var result = (ResponseType) dialog.Run();
            dialog.Destroy();

            return result == ResponseType.Ok;
        }

        private static string GetHumanReadableSize(long size) {
            const long gb = 1024 * 1024 * 1024;
            const long mb = 1024 * 1024;
            const long kb = 1024;
            
            if (size > gb)
                return $"{(float) size / gb:F2} GiB";
            
            if (size > mb)
                return $"{(float) size / mb:F2} MiB";
            
            if (size > kb)
                return $"{(float) size / kb:F2} KiB";

            return $"{size} B";
        }

        public IProgress<DownloadingState> ShowDownloadingTask() {
            _labelTask.Text = "Downloading files";
            ShowTask();
            return new DownloadingProgress(_pbTask);
        }
        
        public IProgress<CheckingState> ShowCheckingTask() {
            _labelTask.Text = "Checking files";
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
    }
}