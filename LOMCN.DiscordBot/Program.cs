using System;
using System.IO;

namespace LOMCN.DiscordBot
{
    internal static class Program
    {
        private static Config _config;
        public static Config Config => _config;
        private static DbHandler _dbHandler;
        private static StatusChecker _statusChecker;
        private static Bot _bot;
        public static Bot Bot => _bot;
        private static void Main()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")))
                _config = Config.LoadFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
            else
            {
                _config = new Config();
                _config.SaveToFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
            }

            if (Environment.GetEnvironmentVariable("BOT_TOKEN") != null &&
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BOT_TOKEN")) &&
                string.IsNullOrEmpty(_config.Token) ||
                _config.Token == Environment.GetEnvironmentVariable("BOT_TOKEN"))
            {
                _config.Token = Environment.GetEnvironmentVariable("BOT_TOKEN");
            }
            if (_config.Token != "YOUR_TOKEN_HERE")
            {
                _dbHandler = DbHandler.Instance;
                _statusChecker = StatusChecker.Instance;
                _dbHandler.Start();
                _statusChecker.Start();
                using (_bot = new Bot())
                {
                    Bot.RunAsync().Wait();
                }
            }
            _config.SaveToFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
        }
    }
}
