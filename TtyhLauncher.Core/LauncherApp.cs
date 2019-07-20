using System;
using System.Net.Http;
using Newtonsoft.Json;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Profiles;
using TtyhLauncher.Settings;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils;
using TtyhLauncher.Versions;

namespace TtyhLauncher {
    public abstract class LauncherApp {
        public void Run() {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
            
            const string appName = "TtyhLauncher2";
            const string appVersion = "0.0.1";
            const string appUrl = "https://github.com/dngulin/ttyhlauncher2";
            
            const string storeUrl = "https://ttyh.ru/files/newstore";
            const string masterUrl = "https://master.ttyh.ru";
            const string directory = "ttyhlauncher2";
            
            const int logRotateCount = 3;
            const int requestTimeOut = 5;
            
            var serializer = new JsonSerializer {
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var json = new JsonParser(serializer);

            using (var logger = new FileLogger(directory, logRotateCount)) 
            using (var settings = new SettingsManager(directory, json, logger))
            using (var httpClient = new HttpClient()) {
                httpClient.Timeout = TimeSpan.FromSeconds(requestTimeOut);
                logger.OnLog += Console.Out.WriteLine;

                var versions = new VersionsManager(storeUrl, directory, httpClient, json, logger);
                var profiles = new ProfilesManager(directory, json, logger);
                var ttyhClient = new TtyhClient(masterUrl, appVersion, settings.Ticket, httpClient, serializer, logger);
                var runner = new GameRunner(directory, json, logger, appName, appVersion);
                
                var ui = CreateUi(appName, appVersion, appUrl);
                var launcher = new Launcher(settings, versions, profiles, httpClient, ttyhClient, runner, ui, logger, appName);
                
                launcher.Start();
                RunEventLoop();
            }
        }

        protected abstract ILauncherUi CreateUi(string appName, string appVersion, string appUrl);
        protected abstract void RunEventLoop();
    }
}