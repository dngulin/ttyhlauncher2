using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Logs;
using TtyhLauncher.Utils.Data;

namespace TtyhLauncher.Utils {
    public class HashChecker {
        private readonly WrappedLogger _log;

        public HashChecker(ILogger logger) {
            _log = new WrappedLogger(logger, "HashChecker");
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

            var context = SynchronizationContext.Current;
            void Report(string path) {
                context?.Post(args => {
                    var filePath = args as string ?? string.Empty;
                    _log.Info($"Checking file {filePath}...");
                    
                    state.FileName = filePath;
                    state.CurrentFile++;
                    
                    progress?.Report(state);
                }, path);
            }

            var tasks = targets.Select(target => Task.Run(() => CheckTarget(target, result, Report), ct));
            await Task.WhenAll(tasks);
            
            _log.Info($"Checking files completed. Need to update {result.Count} files.");
            return result.ToArray();
        }

        private static void CheckTarget(DownloadTarget target, List<DownloadTarget> result, Action<string> report) {
            report?.Invoke(target.Path);

            if (IsSameFileExists(target))
                return;
            
            lock (result)
                result.Add(target);
        }

        private static bool IsSameFileExists(DownloadTarget target) {
            if (!File.Exists(target.Path))
                return false;
            
            var fileInfo = new FileInfo(target.Path);
            if (fileInfo.Length != target.Size)
                return false;
            
            if (string.IsNullOrEmpty(target.Sha1))
                return true;
            
            using (var fileStream = File.OpenRead(target.Path))
            using (var sha1 = new SHA1CryptoServiceProvider()) {
                var sb = new StringBuilder(40);
                
                foreach (var b in sha1.ComputeHash(fileStream))
                    sb.Append(b.ToString("x2"));

                return sb.ToString() == target.Sha1;
            }
        }
    }
}