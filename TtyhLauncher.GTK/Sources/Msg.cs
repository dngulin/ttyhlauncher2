using Gtk;

namespace TtyhLauncher.GTK {
    public static class Msg {
        public static void Error(Window parent, string message, string details = null) {
            var dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, message) {
                Title = Tr._("Error"),
                IconName = "dialog-error",
                SecondaryText = details
            };
            dialog.Run();
            dialog.Destroy();
        }

        public static bool Info(Window parent, string title, string message) {
            var dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Question, ButtonsType.OkCancel, message) {
                Title = title,
                IconName = "dialog-information"
            };

            var result = (ResponseType) dialog.Run();
            dialog.Destroy();

            return result == ResponseType.Ok;
        }
    }
}