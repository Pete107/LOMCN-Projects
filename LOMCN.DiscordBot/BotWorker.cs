using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace LOMCN.DiscordBot
{
    internal class BotWorker : IDisposable
    {
        public static BotWorker Instance { get; } = new BotWorker();
        public event EventHandler<string> StatusChanged;
        private List<ServerEntry> _models;
        private string _output = string.Empty;
        private Timer _timer;
        private BotWorker()
        {
            DbHandler.Instance.DataUpdated += InstanceOnDataUpdated;
            _timer = new Timer {Interval = 5000, Enabled = true};
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(_output)) return;
            StatusChanged?.Invoke(null, _output);
            if (Bot.Ready)
                _output = string.Empty;
        }

        private void InstanceOnDataUpdated(object sender, List<ServerEntry> e)
        {
            _models = e;
            lock (_models)
            {
                _models = _models.OrderByDescending(a => a.CurrentStatus.Online)
                    .ThenByDescending(a => a.CurrentStatus.UserCount != -1)
                    .ThenByDescending(a => a.CurrentStatus.UserCount).ToList();
                var temp = "```md\r\n";
                foreach (var serverEntry in _models)
                {

                    temp += Program.Config.OutputFormat
                        .Replace("$SERVERNAME$", serverEntry.ServerName)
                        .Replace("$STATUS$", serverEntry.CurrentStatus.Online ? "Online" : "Offline")
                        .Replace("$USERCOUNT$", $"{serverEntry.CurrentStatus.UserCount}");
                    if (!temp.EndsWith("\r\n"))
                        temp += "\r\n";
                }
                temp += "```";
                _output = temp;
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= TimerOnElapsed;
                _timer.Stop();
                _timer.Dispose();
            }

            _timer = null;
            StatusChanged = null;
            _models?.Clear();
            _models = null;
            _output = null;
        }
    }
}