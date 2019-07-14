using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using TtyhLauncher.Profiles.Data;
using TtyhLauncher.Profiles.Exceptions;
using TtyhLauncher.Versions.Data;
using FormItem = Gtk.Builder.ObjectAttribute;

namespace TtyhLauncher.GTK {
    public class ProfileWindow : Window {
        private readonly IReadOnlyList<CachedPrefixInfo> _prefixes;
        private readonly Action<string, ProfileData> _doSave;
        
        [FormItem] private readonly Entry _entryName = null;
        [FormItem] private readonly ComboBoxText _comboPrefixes = null;
        [FormItem] private readonly ComboBoxText _comboVersions = null;
        [FormItem] private readonly CheckButton _toggleCheckVersion = null;
        
        [FormItem] private readonly Button _buttonSave = null;

        private ProfileWindow(Builder builder) : base(builder.GetObject("ProfileWindow").Handle) {
            builder.Autoconnect(this);
        }

        public ProfileWindow(
            string profileId,
            ProfileData profileData,
            IReadOnlyList<CachedPrefixInfo> prefixes,
            Action<string, ProfileData> doSave)
            : this(new Builder("ProfileWindow.glade")) {

            _prefixes = prefixes;
            _doSave = doSave;

            _entryName.Text = profileId;
            
            foreach (var prefixName in prefixes.Select(p => p.About))
                _comboPrefixes.AppendText(prefixName);
            
            _toggleCheckVersion.Active = profileData.CheckVersionFiles;
            
            _comboPrefixes.Changed += UpdateVersionsCombo;
            _buttonSave.Clicked += OnSaveClicked;
            
            var index = prefixes.ToList().FindIndex(p => p.Id == profileData.FullVersion.Prefix);
            if (index < 0 || index >= _prefixes.Count)
                return;

            var versions = _prefixes[index].Versions.Select(v => v.Id).ToArray();
            
            _comboPrefixes.Active = index;
            _comboVersions.Active = Array.IndexOf(versions, profileData.FullVersion.Version);
        }

        private void UpdateVersionsCombo(object sender, EventArgs e) {
            _comboVersions.RemoveAll();
            
            var index = _comboPrefixes.Active;
            if (index < 0 || index >= _prefixes.Count)
                return;

            var versions = _prefixes[index].Versions.Select(v => v.Id).ToArray();
            foreach (var version in versions)
                _comboVersions.AppendText(version);
            
            _comboVersions.Active = Array.IndexOf(versions, _prefixes[index].LatestVersion);
        }

        void OnSaveClicked(object sender, EventArgs e) {
            var index = _comboPrefixes.Active;
            if (index < 0 || index >= _prefixes.Count) {
                ShowError("wrong_version");
                return;
            }

            var profileId = _entryName.Text;
            var profileData = new ProfileData {
                FullVersion = new FullVersionId(_prefixes[index].Id, _comboVersions.ActiveText),
                CheckVersionFiles = _toggleCheckVersion.Active
            };

            _buttonSave.Sensitive = false;
            
            var success = false;
            try {
                _doSave(profileId, profileData);
                success = true;
            }
            catch (InvalidProfileNameException) {
                ShowError("wrong_profile_name");
            }
            catch (InvalidProfileVersionException) {
                ShowError("wrong_profile_data");
            }
            catch {
                ShowError("cant_save_profile");
            }

            _buttonSave.Sensitive = true;

            if (success) {
                Hide();
                Destroy();
            }
        }

        private void ShowError(string message) {
            var dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, message);
            dialog.Title = "Error";
            dialog.Run();
            dialog.Destroy();
        }
    }
}