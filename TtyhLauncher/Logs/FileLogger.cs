using System;
using System.IO;
using System.Text;

namespace TtyhLauncher.Logs {
    public class FileLogger: ILogger, IDisposable {
        public event Action<string> OnLog;
        
        private const string LogsDirectory = "logs";
        
        private const string LevelInfo = "INFO";
        private const string LevelWarning = "WARNING";
        private const string LevelError = "ERROR";

        private readonly StreamWriter _logWriter;

        public FileLogger(string dataDirectory, int logsCount) {
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // XDG_DATA_HOME
            var logsPath = Path.Combine(dataPath, dataDirectory, LogsDirectory);
            
            if (!Directory.Exists(logsPath))
                Directory.CreateDirectory(logsPath);

            const string logFileName = "ttyh-launcher.{0}.log";
            
            for (var i = logsCount - 1; i >= 0; i--) {
                var currLog = Path.Combine(logsPath, string.Format(logFileName, i));
                if (File.Exists(currLog))
                    File.Delete(currLog);

                if (i == 0) break;
                
                var prevLog = Path.Combine(logsPath, string.Format(logFileName, i - 1));
                if (File.Exists(prevLog))
                    File.Copy(prevLog, currLog);
            }
            
            var logPath = Path.Combine(logsPath, string.Format(logFileName, 0));
            _logWriter = new StreamWriter(File.Create(logPath), Encoding.UTF8);
        }
        
        public void Info(string who, string message) => Log(LevelInfo, who, message);
        public void Warn(string who, string message) => Log(LevelWarning, who, message);
        public void Error(string who, string message) => Log(LevelError, who, message);

        public void WriteLine(string line) {
            _logWriter.WriteLine(line);
            OnLog?.Invoke(line);
        }

        private void Log(string level, string who, string message) {
            var logLine = $"{DateTime.Now} [{level}] {who}: {message}";
            
            _logWriter.WriteLine(logLine);
            OnLog?.Invoke(logLine);
        }

        public void Dispose() => _logWriter.Dispose();
    }
}