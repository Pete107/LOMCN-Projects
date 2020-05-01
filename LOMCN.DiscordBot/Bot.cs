using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
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
            try
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
                _client.ClientErrored += ClientOnClientErrored;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        private async Task ClientOnClientErrored(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, GetType().Namespace, e.Exception.ToString(), DateTime.Now);
            
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
            try
            {
                await Task.Yield();
                _startTimes.SocketStart = DateTime.Now;
                var guild = await _client.GetGuildAsync(_config.GuildId);
                var channels= await guild.GetChannelsAsync();
                _channel = channels.FirstOrDefault(a => a.Id == _config.ChannelId);
                BotWorker.Instance.StatusChanged += BotWorkerOnStatusChanged;
                Ready = true;
                Program.SetLogger(e.Client.DebugLogger);
                Program.Log("Bot ready");
            }
            catch (Exception exception)
            {
                Program.Log(exception);
            }
        }

        private async void BotWorkerOnStatusChanged(object sender, string output)
        {
            if (_channel == null)
                return;
            if (string.IsNullOrEmpty(output)) return;
            try
            {


                try
                {
                    if (_lastMessage != null)
                        await _channel.DeleteMessageAsync(_lastMessage);
                    else
                    {
                        var currentMessages = await _channel.GetMessagesAsync();
                        if (currentMessages != null)
                            await _channel.DeleteMessagesAsync(currentMessages);
                    }

                    var temp = output;
                    _lastMessage = await _channel.SendMessageAsync(temp);
                }
                catch (NotFoundException ex)
                {

                }
                catch (Exception e)
                {
                    Program.Log(e);
                }

            }
            catch (Exception e)
            {
                Program.Log(e);
            }
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