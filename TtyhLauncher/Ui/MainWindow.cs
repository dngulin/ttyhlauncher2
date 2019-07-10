using System;
using Gtk;
using Pango;
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

        public event Action OnPlayButtonClicked;
        
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
            _logBuffer = _logTextView.Buffer;
            
            // Because of the GTK is a piece of shit the `Monospace` property doesn't work for a TextView
            // So, lets use deprecated API
#pragma warning disable 612
            _logTextView.OverrideFont(new FontDescription {Family = "Monospace"});
#pragma warning restore 612

            _logTextView.SizeAllocated += (s, a) => _logTextView.ScrollToIter(_logBuffer.EndIter, 0, false, 0, 0);
            _buttonPlay.Clicked += (s, a) => OnPlayButtonClicked?.Invoke();
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
    }
}