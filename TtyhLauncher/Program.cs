using System;
using System.Net.Http;
using Newtonsoft.Json;
using Gtk;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Profiles;
using TtyhLauncher.Settings;
using TtyhLauncher.Ui;
using TtyhLauncher.Utils;
using TtyhLauncher.Versions;

namespace TtyhLauncher {
    internal static class Program {
        private static void Main() {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            const string appId = "ru.ttyh.launcher2";
            const string appName = "TtyhLauncher2";
            const string appVersion = "0.0.1";
            
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
                var profiles = new ProfilesManager(directory, json, logger, appName, appVersion);
                var ttyhClient = new TtyhClient(masterUrl, appVersion, settings.Ticket, httpClient, serializer, logger);
                
                Application.Init();
                GLib.ExceptionManager.UnhandledException += args => {
                    var msg = (args.ExceptionObject as Exception)?.Message ?? "UNKNOWN_ERROR";
                    var dialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, msg);
                    dialog.Title = "Unhandled Exception";
                    
                    dialog.Run();
                    dialog.Destroy();

                    Application.Quit();
                };
                
                var window = new MainWindow($"{appName} - {appVersion}");
                
                var gtkApplication = new Application(appId, GLib.ApplicationFlags.None);
                gtkApplication.Register(GLib.Cancellable.Current);
                gtkApplication.AddWindow(window);

                var launcher = new Launcher(settings, versions, profiles, httpClient, ttyhClient, window, logger, appName);
                
                launcher.Start();
                Application.Run();
            }
        }
    }
}