using Gtk;

namespace TtyhLauncher.GTK {
    public static class Msg {
        public static void Error(Window parent, string message, string details = null) {
            var dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, message) {
                Title = "Error",
                SecondaryText = details
            };
            dialog.Run();
            dialog.Destroy();
        }
    }
}