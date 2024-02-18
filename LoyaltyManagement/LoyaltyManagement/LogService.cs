using Microsoft.Extensions.Options;

namespace LoyaltyManagement
{
    public class LogService
    {
        private readonly LogSettings _settings;
        public LogService(IOptions<LogSettings> settings)
        {
            _settings = settings.Value;
        }
        public List<string> GetLogLevel()
        {
            return _settings.LogLevel;
        }
        public void Log(string logFilePath, string log)
        {
            System.IO.File.AppendAllText(logFilePath, log);
        }
    }
}
