using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TtyhLauncher.Logs;
using TtyhLauncher.Master.Data;
using TtyhLauncher.Master.Exceptions;
using TtyhLauncher.Utils;

namespace TtyhLauncher.Master {
    public class TtyhClient {
        private const string RequestPattern = "{0}/index.php?act={1}";
        
        private const string LoginAction = "login";
        private const string UploadSkinAction = "setskin";

        private readonly string _masterUrl;
        private readonly string _version;
        private readonly string _ticket;
        
        private readonly HttpClient _client;
        private readonly JsonSerializer _serializer;
        private readonly WrappedLogger _log;
        private readonly UTF8Encoding _utf8;

        public TtyhClient(string masterUrl, string version, string ticket, HttpClient client, JsonSerializer serializer, ILogger logger) {
            _masterUrl = masterUrl;
            _version = version;
            _ticket = ticket;
            
            _client = client;
            _serializer = serializer;
            _utf8 = new UTF8Encoding(false);
            _log = new WrappedLogger(logger, "TtyhClient");
        }
        
        public async Task<LoginResultData> Login(string userName, string password) {
            var url = string.Format(RequestPattern, _masterUrl, LoginAction);
            var payload = new LoginRequestData {
                Agent = new LoginRequestData.AgentData {
                    Name = "Minecraft",
                    Version = 1
                },
                Platform = new LoginRequestData.PlatformData {
                    Name = Platform.Name,
                    Version = Platform.Version,
                    WordSize = Platform.WordSize
                },
                UserName = userName,
                Password = password,
                Ticket = _ticket,
                Version = _version
            };
            
            _log.Info($"Logging as {userName}...");
            LoginResultData reply;
            try {
                reply = await PostJson<LoginResultData>(url, payload);
            }
            catch (Exception e) {
                _log.Error(e.Message);
                throw;
            }
            
            if (reply.Error != null) {
                _log.Error($"{reply.Error} '{reply.ErrorMessage}'");
                throw new ErrorAnswerException(reply.ErrorMessage ?? reply.Error);
            }
                
            _log.Info($"at: '{reply.AccessToken}', ct: '{reply.ClientToken}'");
            return reply;
        }

        public async Task UploadSkin(string userName, string password, byte[] skinData, bool isSlim) {
            var url = string.Format(RequestPattern, _masterUrl, UploadSkinAction);
            var payload = new SkinUploadRequestData {
                UserName = userName,
                Password = password,
                SkinData = Convert.ToBase64String(skinData),
                Model = isSlim ? "slim" : null
            };
            
            _log.Info("Uploading skin...");
            ResultData reply;
            try {
                reply = await PostJson<ResultData>(url, payload);
            }
            catch (Exception e) {
                _log.Error(e.Message);
                throw;
            }
            
            if (reply.Error != null) {
                _log.Error($"{reply.Error} '{reply.ErrorMessage}'");
                throw new ErrorAnswerException(reply.ErrorMessage ?? reply.Error);
            }
        }

        private async Task<TResult> PostJson<TResult>(string url, object payload) {
            var requestStream = new MemoryStream();
            
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            using (var httpContent = new StreamContent(requestStream)) {
                const bool doNotCloseStream = true;
                using (var textWriter = new StreamWriter(requestStream, _utf8, 1024, doNotCloseStream))
                using (var jsonWriter = new JsonTextWriter(textWriter) {Formatting = Formatting.None}) {
                    _serializer.Serialize(jsonWriter, payload);
                }
                
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpContent.Headers.ContentLength = requestStream.Length;
                
                requestStream.Position = 0;
                request.Content = httpContent;
                
                using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)) {
                    using (var respStream = await response.Content.ReadAsStreamAsync())
                    using (var textReader = new StreamReader(respStream))
                    using (var jsonReader = new JsonTextReader(textReader)) {
                        return _serializer.Deserialize<TResult>(jsonReader);
                    }
                }
            }
        }
    }
}