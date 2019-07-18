using System;
using System.Threading.Tasks;
using Gtk;
using TtyhLauncher.Master.Exceptions;
using FormItem = Gtk.Builder.ObjectAttribute;

namespace TtyhLauncher.GTK {
    public class SkinUploadWindow : Window {
        [FormItem] private readonly FileChooserButton _buttonSelect = null;
        [FormItem] private readonly CheckButton _toggleSlim = null;
        [FormItem] private readonly Button _buttonUpload = null;
        
        private readonly Func<string, bool, Task> _upload;
        
        private SkinUploadWindow(Builder builder) : base(builder.GetObject("SkinUploadWindow").Handle) {
            builder.Autoconnect(this);
        }

        public SkinUploadWindow(Func<string, bool, Task> upload) : this(new Builder("SkinUploadWindow.glade")) {
            _upload = upload;
            _buttonUpload.Clicked += OnUploadClicked;
        }

        private async void OnUploadClicked(object sender, EventArgs e) {
            Sensitive = false;

            bool success;
            try {
                await _upload(_buttonSelect.Filename, _toggleSlim.Active);
                success = true;
            }
            catch (ErrorAnswerException answerException) {
                Msg.Error(this, Tr._("Failed to upload skin!"), answerException.Message);
                success = false;
            }
            catch {
                Msg.Error(this, Tr._("Failed to upload skin!"));
                success = false;
            }

            Sensitive = true;

            if (!success)
                return;
            
            Hide();
            Destroy();
        }
    }
}