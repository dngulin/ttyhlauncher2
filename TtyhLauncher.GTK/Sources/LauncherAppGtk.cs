using System;
using Gtk;
using TtyhLauncher.Ui;

namespace TtyhLauncher.GTK {
    public class LauncherAppGtk : LauncherApp {
        private readonly string _appId;

        public LauncherAppGtk(string appId) {
            _appId = appId;
            Application.Init();
            GLib.ExceptionManager.UnhandledException += args => {
                var msg = (args.ExceptionObject as Exception)?.Message ?? "UNKNOWN_ERROR";
                var dialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, msg);
                dialog.Title = "Unhandled Exception";
                    
                dialog.Run();
                dialog.Destroy();

                Application.Quit();
            };
        }
        
        protected override ILauncherUi CreateUi(string title) {
            var ui = new MainWindow(title);
                
            var gtkApplication = new Application(_appId, GLib.ApplicationFlags.None);
            gtkApplication.Register(GLib.Cancellable.Current);
            gtkApplication.AddWindow(ui);

            return ui;
        }

        protected override void RunEventLoop() {
            Application.Run();
        }
    }
}