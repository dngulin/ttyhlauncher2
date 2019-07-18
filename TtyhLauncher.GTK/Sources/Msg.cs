using Gtk;

namespace TtyhLauncher.GTK {
    public static class Msg {
        public static void Error(Window parent, string message, string details = null) {
            var dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, message) {
                Title = Tr._("Error"),
                SecondaryText = details
            };
            dialog.Run();
            dialog.Destroy();
        }

        public static bool Ask(Window parent, string title, string message) {
            var dialog = new MessageDialog(parent, DialogFlags.Modal, MessageType.Question, ButtonsType.OkCancel, message);
            dialog.Title = title;
            
            var result = (ResponseType) dialog.Run();
            dialog.Destroy();

            return result == ResponseType.Ok;
        }
    }
}