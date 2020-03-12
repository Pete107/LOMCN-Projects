using System;
using System.IO;
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
        private static bool _ready;
        public static bool Ready => _ready;
        private const ulong GuildId = 562378924135546931;
        private const ulong ChannelId = 562408484080189460;
        private DiscordClient _client;
        private InteractivityModule _interactivity;
        private StartTimes _startTimes;
        private CancellationTokenSource _cts;

        
        private DiscordChannel _channel;

        public Bot()
        {
            var config = Program.Config;
            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                EnableCompression = true,
                Token = config.Token,
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
                StringPrefix = config.Prefix,
                IgnoreExtraArguments = true,
                Dependencies = dep
            });

            cnext.RegisterCommands<Owner>();
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

        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            await Task.Yield();
            _startTimes.SocketStart = DateTime.Now;
            var guilds = await _client.GetGuildAsync(GuildId);
            var channels= await guilds.GetChannelsAsync();
            _channel = channels.FirstOrDefault(a => a.Id == ChannelId);
            BotWorker.Instance.StatusChanged += BotWorkerOnStatusChanged;
            Console.WriteLine("Bot is ready");
            _ready = true;
            StatusChecker.Instance.Start();
        }

        private void BotWorkerOnStatusChanged(object sender, string output)
        {
            if (string.IsNullOrEmpty(output)) return;
            Console.WriteLine($"Message Received : {output}");
            _channel?.SendMessageAsync(output);
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

        private static void WriteCenter(string value, int skipline = 0)
        {
            for (var i = 0; i < skipline; i++)
                Console.WriteLine();
            if (Console.WindowWidth > value.Length)
                Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
        }
    }
}