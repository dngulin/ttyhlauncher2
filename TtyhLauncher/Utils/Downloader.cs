using System.IO;
using System.Net.Http;
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

        public async Task Download(DownloadTarget[] targets) {
            _log.Info("Downloading...");

            var buffer = new byte[4096];

            foreach (var target in targets) {
                _log.Info($"Get {target.Url} -> {target.Path}");
                
                var targetDirectoryPath = Path.GetDirectoryName(target.Path);
                if (!Directory.Exists(targetDirectoryPath))
                    Directory.CreateDirectory(targetDirectoryPath);

                try {
                    using (var httpStream = await _client.GetStreamAsync(target.Url))
                    using (var fileStream = File.Create(target.Path)) {
                        int length;
                        while ((length = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            await fileStream.WriteAsync(buffer, 0, length);
                    }
                }
                catch (HttpRequestException e) {
                    _log.Error($"Can't download {target.Url}");
                    _log.Error(e.Message);
                }
                
            }

            _log.Info("Downloading complete");
        }
    }
}