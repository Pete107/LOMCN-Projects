using System;
using System.IO;
using DSharpPlus;

namespace LOMCN.DiscordBot
{
    internal static class Program
    {
        private static Config _config;
        public static Config Config => _config;
        private static DbHandler _dbHandler;
        private static StatusChecker _statusChecker;
        private static Bot Bot { get; set; }
        private static DebugLogger _logger;

        private static void Main()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config.json")))
                _config = Config.LoadFromFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
            else
            {
                _config = new Config();
                _config.SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
            }

            if (Environment.GetEnvironmentVariable("BOT_TOKEN") != null &&
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BOT_TOKEN")) &&
                string.IsNullOrEmpty(_config.Token) ||
                _config.Token == Environment.GetEnvironmentVariable("BOT_TOKEN"))
            {
                if (Environment.GetEnvironmentVariable("BOT_TOKEN") != "YOUR_TOKEN_HERE")
                    _config.Token = Environment.GetEnvironmentVariable("BOT_TOKEN");
            }
            if (_config.Token != "YOUR_TOKEN_HERE")
            {
                _dbHandler = DbHandler.Instance;
                _statusChecker = StatusChecker.Instance;
                _dbHandler.Start();
                _statusChecker.Start();
                
                using (Bot = new Bot())
                {
                    Bot.RunAsync().Wait();
                }
            }
            _config.SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
        }

        public static void SetLogger(DebugLogger logger) => _logger = logger;

        public static void Log(string output)
        {
            _logger?.LogMessage(LogLevel.Debug, $"{typeof(Bot).Namespace}", output, DateTime.Now);
        }

        public static void Log(Exception ex)
        {
            _logger?.LogMessage(LogLevel.Error, $"{typeof(Bot).Namespace}", ex.ToString(), DateTime.Now);
        }
    }
}
