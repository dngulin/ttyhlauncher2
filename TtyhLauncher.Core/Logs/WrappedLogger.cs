namespace TtyhLauncher.Logs {
    public class WrappedLogger {
        private readonly ILogger _logger;
        private readonly string _who;
        
        public WrappedLogger(ILogger logger, string who) {
            _logger = logger;
            _who = who;
        }
        
        public void Info(string message) => _logger.Info(_who, message);
        public void Warn(string message) => _logger.Warn(_who, message);
        public void Error(string message) => _logger.Error(_who, message);
    }
}