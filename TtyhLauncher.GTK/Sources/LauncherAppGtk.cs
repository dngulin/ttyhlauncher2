using System;
using System.IO;
using Gtk;
using TtyhLauncher.Ui;

namespace TtyhLauncher.GTK {
    public class LauncherAppGtk : LauncherApp {
        private readonly string _appId;

        private MainWindow _ui;

        public LauncherAppGtk(string appId) {
            _appId = appId;
            
            Tr.InitCatalog("ui-gtk", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations"));

            Application.Init();
            GLib.ExceptionManager.UnhandledException += args => {
                var msg = (args.ExceptionObject as Exception)?.Message ?? "UNKNOWN_ERROR";
                var dialog = new MessageDialog(_ui, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, msg);
                dialog.Title = $"Unhandled Exception ({args.ExceptionObject.GetType()})";
                dialog.SecondaryText = (args.ExceptionObject as Exception)?.StackTrace ?? "UNKNOWN_STACK";
                    
                dialog.Run();
                dialog.Destroy();

                Application.Quit();
            };
        }
        
        protected override ILauncherUi CreateUi(string appName, string appVersion, string appUrl) {
            var ui = new MainWindow(appName, appVersion, appUrl);
                
            var gtkApplication = new Application(_appId, GLib.ApplicationFlags.None);
            gtkApplication.Register(GLib.Cancellable.Current);
            gtkApplication.AddWindow(ui);

            _ui = ui;
            return ui;
        }

        protected override void RunEventLoop() {
            Application.Run();
        }
    }
}