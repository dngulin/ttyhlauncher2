using System;
using System.IO;
using Newtonsoft.Json;
using TtyhLauncher.Logs;
using TtyhLauncher.Settings.Data;
using TtyhLauncher.Utils;

namespace TtyhLauncher.Settings {
    public class SettingsManager: IDisposable {
        private const string SettingsFileName = "settings.json";

        private readonly string _settingsPath;
        private readonly SettingsData _settings;
        
        private readonly WrappedLogger _log;
        private readonly JsonParser _json;

        public string Profile {
            get => _settings.Profile;
            set => _settings.Profile = value;
        }

        public string UserName {
            get => _settings.UserName;
            set => _settings.UserName = value;
        }
        
        public string Password {
            get => _settings.Password;
            set => _settings.Password = value;
        }
        
        public bool OfflineMode {
            get => _settings.IsOffline;
            set => _settings.IsOffline = value;
        }

        public bool SavePassword {
            get => _settings.SavePassword;
            set => _settings.SavePassword = value;
        }

        public bool HideOnRun {
            get => _settings.HideOnRun;
            set => _settings.HideOnRun = value;
        }
        
        public int WindowWidth {
            get => _settings.WindowWidth;
            set => _settings.WindowWidth = value;
        }
        
        public int WindowHeight {
            get => _settings.WindowHeight;
            set => _settings.WindowHeight = value;
        }

        public string Ticket => _settings.Revision;

        public SettingsManager(string configDir, JsonParser json, ILogger logger) {
            _json = json;
            _log = new WrappedLogger(logger, "Settings");

            var configPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // XDG_CONFIG_HOME

            var settingsDir = Path.Combine(configPath, configDir);
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);
            
            _settingsPath = Path.Combine(settingsDir, SettingsFileName);
            
            if (File.Exists(_settingsPath)) {
                try {
                    _settings = _json.ReadFile<SettingsData>(_settingsPath);
                }
                catch (JsonSerializationException e) {
                    _log.Error("Can't parse settings: " + e.Message);
                }
            }

            _settings = _settings ?? new SettingsData();
            
            if (string.IsNullOrEmpty(_settings.Revision))
                _settings.Revision = Guid.NewGuid().ToString();

            _log.Info("Initialized!");
        }

        public void Dispose() {
            _log.Info("Save...");
            _json.WriteFile(_settings, _settingsPath);
        }
    }
}