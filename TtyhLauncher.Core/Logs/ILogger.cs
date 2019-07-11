using System;

namespace TtyhLauncher.Logs {
    public interface ILogger {
        event Action<string> OnLog;
        
        void Info(string who, string message);
        void Warn(string who, string message);
        void Error(string who, string message);

        void WriteLine(string message);
    }
}