using System;
using System.IO;
using Gtk;
using TtyhLauncher.Utils.Data;

namespace TtyhLauncher.GTK {
    public class DownloadingProgress : IProgress<DownloadingState> {
        private readonly ProgressBar _bar;
        
        public DownloadingProgress(ProgressBar bar) {
            _bar = bar;
        }

        public void Report(DownloadingState state) {
            var relativeName = Path.GetFileName(state.FileName);

            _bar.Text = $"{relativeName} ({state.CurrentFile}/{state.TotalFiles})";
            _bar.Fraction = state.TotalBytes <= 0 ? 0f : (float) state.CurrentBytes / state.TotalBytes;
        }
    }
}