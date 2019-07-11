using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TtyhLauncher.Logs;
using TtyhLauncher.Utils.Data;

namespace TtyhLauncher.Utils {
    public class Downloader {
        private readonly HttpClient _client;
        private readonly WrappedLogger _log;
        
        public Downloader(HttpClient client, ILogger logger) {
            _log = new WrappedLogger(logger, "Downloader");
            _client = client;
        }

        public async Task Download(
            DownloadTarget[] targets,
            CancellationToken ct = default(CancellationToken),
            IProgress<DownloadingState> progress = null) {
            
            ct.ThrowIfCancellationRequested();
            
            _log.Info("Downloading...");
            var state = new DownloadingState {
                FileName = string.Empty,
                CurrentFile = 0,
                TotalFiles = targets.Length,
                CurrentBytes = 0,
                TotalBytes = targets.Sum(t => t.Size)
            };
            
            var buffer = new byte[4096];

            foreach (var target in targets) {
                _log.Info($"Get {target.Url} -> {target.Path}");

                state.CurrentFile++;
                state.FileName = target.Path;
                progress?.Report(state);
                
                var targetDirectoryPath = Path.GetDirectoryName(target.Path);
                if (!Directory.Exists(targetDirectoryPath))
                    Directory.CreateDirectory(targetDirectoryPath);

                try {
                    using (var httpStream = await _client.GetStreamAsync(target.Url))
                    using (var fileStream = File.Create(target.Path)) {
                        int length;
                        while ((length = await httpStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                            await fileStream.WriteAsync(buffer, 0, length, ct);

                            state.CurrentBytes += length;
                            progress?.Report(state);
                        }
                    }
                }
                catch (HttpRequestException e) {
                    _log.Error($"Can't download {target.Url}");
                    _log.Error(e.Message);
                }
                
                ct.ThrowIfCancellationRequested();
            }

            _log.Info("Downloading complete");
        }
    }
}