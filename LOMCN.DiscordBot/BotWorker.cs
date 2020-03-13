using System;
using System.Collections.Generic;
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
            _timer = new Timer {Interval = 1000, Enabled = true};
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_output))
            {
                StatusChanged?.Invoke(null, _output);
                if (Bot.Ready)
                    _output = string.Empty;
            }
        }

        private void InstanceOnDataUpdated(object sender, List<ServerEntry> e)
        {
            _models = e;
            lock (_models)
            {
                _output = "```md\r\n";
                foreach (var serverEntry in _models)
                {

                    _output +=
                        $"<{serverEntry.ServerName}> [{serverEntry.CurrentStatus.UserCount}][{(serverEntry.CurrentStatus.Online ? "Online" : "Offline")}]\r\n";
                }

                _output += "```";
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
            _models.Clear();
            _models = null;
            _output = null;
        }
    }
}