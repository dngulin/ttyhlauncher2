using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TtyhLauncher.Logs;
using TtyhLauncher.Master;
using TtyhLauncher.Profiles;
using TtyhLauncher.Settings;
using TtyhLauncher.Utils;
using TtyhLauncher.Versions;

namespace TtyhLauncher {
    internal static class Launcher {
        private static async Task Main() {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            const string launcherName = "TtyhLauncher2";
            const string launcherVersion = "0.0.1";
            
            const string storeUrl = "https://ttyh.ru/files/newstore";
            const string masterUrl = "https://master.ttyh.ru";
            const string directory = "ttyhlauncher2";
            
            const int logRotateCount = 3;
            const int requestTimeOut = 5;
            
            var serializer = new JsonSerializer {
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var parser = new JsonParser(serializer);

            using (var logger = new FileLogger(directory, logRotateCount)) 
            using (var settings = new SettingsManager(directory, parser, logger))
            using (var client = new HttpClient()) {
                client.Timeout = TimeSpan.FromSeconds(requestTimeOut);
                
                logger.OnLog += Console.Out.WriteLine;
                logger.Info(launcherName, "Started");

                var versions = new VersionsManager(storeUrl, directory, client, parser, logger);
                await versions.FetchPrefixes();
                
                var profiles = new ProfilesManager(directory, parser, logger, launcherName, launcherVersion);

                if (!profiles.Contains(settings.Profile)) {
                    settings.Profile = !profiles.IsEmpty ? profiles.First : profiles.CreateDefault(versions.Prefixes[0]);
                }

                var version = profiles.GetVersion(settings.Profile);
                await versions.FetchVersionIndexes(version);
                var checkList = versions.GetVersionFilesInfo(version);

                var hashChecker = new HashChecker(logger);
                var downloads = await hashChecker.CheckFiles(checkList);
                
                var downloader = new Downloader(client, logger);
                await downloader.Download(downloads);

                profiles.Update(settings.Profile);

                var ttyhClient = new TtyhClient(masterUrl, launcherVersion, settings.Ticket, client, serializer, logger);
                var tokens = await ttyhClient.Login(settings.UserName, settings.Password);

                await profiles.Run(settings.Profile, settings.UserName, tokens.ClientToken, tokens.AccessToken);

                logger.Info(launcherName, "Terminated");
            }
        }
    }
}