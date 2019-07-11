using System;
using System.IO;
using Gtk;
using TtyhLauncher.Utils.Data;

namespace TtyhLauncher.GTK {
    public class CheckingProgress : IProgress<CheckingState> {
        private readonly ProgressBar _bar;
        
        public CheckingProgress(ProgressBar bar) {
            _bar = bar;
        }

        public void Report(CheckingState state) {
            var relativeName = Path.GetFileName(state.FileName);

            _bar.Text = $"{relativeName} ({state.CurrentFile}/{state.TotalFiles})";
            _bar.Fraction = state.TotalFiles <= 0 ? 0f : (float) state.CurrentFile / state.TotalFiles;
        }
    }
}