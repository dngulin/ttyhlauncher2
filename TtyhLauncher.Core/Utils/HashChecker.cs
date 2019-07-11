using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Logs;
using TtyhLauncher.Utils.Data;

namespace TtyhLauncher.Utils {
    public class HashChecker {
        private readonly WrappedLogger _log;
        private readonly StringBuilder _sb;
        
        public HashChecker(ILogger logger) {
            _log = new WrappedLogger(logger, "HashChecker");
            _sb = new StringBuilder(40);
        }
        
        public async Task<DownloadTarget[]> CheckFiles(
            DownloadTarget[] targets,
            CancellationToken ct = default(CancellationToken),
            IProgress<CheckingState> progress = null) {
            
            ct.ThrowIfCancellationRequested();
            
            _log.Info("Checking files...");
            var result = new List<DownloadTarget>(targets.Length);
            var state = new CheckingState {
                FileName = string.Empty,
                CurrentFile = 0,
                TotalFiles = targets.Length
            };
            

            foreach (var target in targets) {
                _log.Info($"Checking file {target.Path}...");
                
                state.FileName = target.Path;
                state.CurrentFile++;
                progress?.Report(state);
                
                if (!await IsSameFileExists(target, ct))
                    result.Add(target);
            }
            
            _log.Info($"Checking files completed. Need to update {result.Count} files.");
            return result.ToArray();
        }

        private async Task<bool> IsSameFileExists(DownloadTarget target, CancellationToken ct = default(CancellationToken)) {
            ct.ThrowIfCancellationRequested();
            
            if (!File.Exists(target.Path))
                return false;
            
            var fileInfo = new FileInfo(target.Path);
            if (fileInfo.Length != target.Size)
                return false;
            
            if (string.IsNullOrEmpty(target.Sha1))
                return true;
            
            using (var fileStream = File.OpenRead(target.Path))
            using (var sha1 = new SHA1CryptoServiceProvider()) {
                var hash = await Task.Run(() => sha1.ComputeHash(fileStream), ct);

                _sb.Clear();
                foreach (var b in hash)
                    _sb.Append(b.ToString("x2"));

                return _sb.ToString() == target.Sha1;
            }
        }
    }
}