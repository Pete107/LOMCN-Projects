using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace LOMCN.DiscordBot
{
    public class Bot : IDisposable
    {
        public static bool Ready { get; private set; }

        private readonly DiscordClient _client;
        private InteractivityModule _interactivity;
        private readonly StartTimes _startTimes;
        private readonly CancellationTokenSource _cts;
        private DiscordChannel _channel;
        private readonly Config _config;
        public Bot()
        {
            _config = Program.Config;
            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                EnableCompression = true,
                Token = _config.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            _interactivity = _client.UseInteractivity(new InteractivityConfiguration()
            {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(30)
            });

            _startTimes = new StartTimes
            {
                BotStart = DateTime.Now,
                SocketStart = DateTime.MinValue
            };

            _cts = new CancellationTokenSource();

            DependencyCollection dep;
            using (var d = new DependencyCollectionBuilder())
            {
                d.AddInstance(new Dependencies
                {
                    Interactivty = _interactivity,
                    StartTimes = _startTimes,
                    Cts = _cts
                });
                dep = d.Build();
            }

            var cnext = _client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefix = _config.Prefix,
                IgnoreExtraArguments = true,
                Dependencies = dep
            });

            cnext.RegisterCommands<Interactivity>();


            _client.Ready += OnReadyAsync;
            
        }

        public async Task RunAsync()
        {
            await _client.ConnectAsync();
            await WaitForCancellationAsync();
        }

        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }

        private DiscordMessage _lastMessage;
        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            await Task.Yield();
            _startTimes.SocketStart = DateTime.Now;
            var guild = await _client.GetGuildAsync(_config.GuildId);
            var channels= await guild.GetChannelsAsync();
            _channel = channels.FirstOrDefault(a => a.Id == _config.ChannelId);
            BotWorker.Instance.StatusChanged += BotWorkerOnStatusChanged;
            Console.WriteLine("Bot is ready");
            Ready = true;
            StatusChecker.Instance.Start();
        }

        private async void BotWorkerOnStatusChanged(object sender, string output)
        {
            if (_channel == null)
                return;
            if (string.IsNullOrEmpty(output)) return;
            try
            {
                if (_lastMessage != null)
                    await _channel.DeleteMessageAsync(_lastMessage);
                else
                {
                    var currentMessages = await _channel.GetMessagesAsync();
                    await _channel.DeleteMessagesAsync(currentMessages);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _lastMessage = await _channel?.SendMessageAsync(output);
        }


        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _client?.Dispose();
            _interactivity = default;
            BotWorker.Instance.StatusChanged -= BotWorkerOnStatusChanged;
            BotWorker.Instance.Dispose();
            DbHandler.Instance.Dispose();
        }
    }
}