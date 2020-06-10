using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using LOMCN.Common;
using LOMCN.Common.Database;
using LOMCN.Common.Database.Models;
using Newtonsoft.Json;

namespace LOMCN.DiscordBot
{
    public class Bot : IDisposable
    {
        public static bool Ready { get; private set; }
        private readonly ServerStatusRepository _serverStatusRepository = Program.ServerStatusRepository;
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

                /*_client.MessageCreated += async e =>
                {
                    if (e.Message.Content.ToLower().StartsWith("uptime"))
                    {

                    }
                };*/
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
        private readonly object _lockerObject = new object();
        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (!Ready)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    continue;
                }

                lock (_lockerObject)
                {
                    Task.Run(async () =>
                    {
                        while (Ready)
                        {
                            if (DateTime.Now >= _nextStatusCheck)
                            {
                                _nextStatusCheck = DateTime.Now + Program.Config.UpdateDelay;

                                try
                                {
                                    var request = (HttpWebRequest) WebRequest.Create(Program.Config.StatusURL);
                                    request.ContentType = "application/json";
                                    request.Credentials = CredentialCache.DefaultCredentials;
                                    var res = request.GetResponse();
                                    var serverList = new List<ServerModel>();
                                    await using (var stream = res.GetResponseStream())
                                    {
                                        if (stream != null)
                                        {
                                            using var reader = new StreamReader(stream);
                                            var result = await reader.ReadToEndAsync();
                                            serverList = JsonConvert.DeserializeObject<List<ServerModel>>(result);
                                        }
                                    }

                                    res.Close();
                                    res.Dispose();
                                    foreach (var serverModel in serverList)
                                    {
                                        if (await Program.ServerStatusRepository.EntryExists(Convert.ToInt32(serverModel.Id)))
                                        {
                                            var model = await Program.ServerStatusRepository.FindById(
                                                Convert.ToInt32(serverModel.Id));
                                            model.CurrentStatus.EditTime = DateTime.Now;
                                            model.CurrentStatus.Online = serverModel.Online == "1";
                                            model.CurrentStatus.UserCount = Convert.ToInt32(serverModel.UserCount);
                                            model.History = new List<ServerEntryStatusHistory>
                                            {
                                                new ServerEntryStatusHistory
                                                {
                                                    EntryTime = DateTime.Now, Id = Guid.NewGuid(),
                                                    Online = serverModel.Online == "1", ServerId = model.Id,
                                                    UserCount = model.CurrentStatus.UserCount
                                                }
                                            };
                                            await Program.ServerStatusRepository.UpdateCurrentStatusAsync(model);
                                        }
                                        else
                                        {
                                            var model = new ServerEntry
                                            {
                                                CurrentStatus = new ServerEntryStatus(),
                                                ExpRate = serverModel.EXPRate,
                                                Id = Guid.NewGuid(),
                                                RgbColor = "rgb(0, 0, 0)",
                                                ServerId = Convert.ToInt32(serverModel.Id),
                                                ServerName = serverModel.Name,
                                                ServerType = serverModel.Type
                                            };
                                            model.CurrentStatus.Id = Guid.NewGuid();
                                            model.CurrentStatus.EditTime = DateTime.Now;
                                            model.CurrentStatus.Online = serverModel.Online == "1";
                                            model.CurrentStatus.UserCount = Convert.ToInt32(serverModel.UserCount);
                                            model.History = new List<ServerEntryStatusHistory>
                                            {
                                                new ServerEntryStatusHistory
                                                {
                                                    EntryTime = DateTime.Now, Id = Guid.NewGuid(),
                                                    Online = serverModel.Online == "1", ServerId = model.Id,
                                                    UserCount = model.CurrentStatus.UserCount
                                                }
                                            };
                                            await _serverStatusRepository.AddServerAsync(model);
                                        }
                                        
                                    }
                                }
                                catch (Exception e)
                                {
                                    Program.Log(e);
                                }
                            }

                            if (DateTime.Now < _nextOutputTime)
                            {
                                await Task.Delay(500);
                                continue;
                            }

                            _nextOutputTime = DateTime.Now + Program.Config.OutputDelay;

                            try
                            {
                                var models = await _serverStatusRepository.GetAllAsync();
                                models = models.OrderByDescending(a => a.CurrentStatus.Online)
                                    .ThenByDescending(a => a.CurrentStatus.UserCount != -1)
                                    .ThenByDescending(a => a.CurrentStatus.UserCount).ToList();
                                var temp = "```md\r\n";
                                foreach (var serverEntry in models)
                                {
                                    temp += Program.Config.OutputFormat
                                        .Replace("$SERVERNAME$", serverEntry.ServerName)
                                        .Replace("$STATUS$", serverEntry.CurrentStatus.Online ? "Online" : "Offline")
                                        .Replace("$USERCOUNT$", $"{serverEntry.CurrentStatus.UserCount}");
                                    if (!temp.EndsWith("\r\n"))
                                        temp += "\r\n";
                                }

                                temp += "```";
                                if (_lastMessage != null)
                                    await _channel.DeleteMessageAsync(_lastMessage);
                                else
                                {
                                    var currentMessages = await _channel.GetMessagesAsync();
                                    if (currentMessages != null)
                                        await _channel.DeleteMessagesAsync(currentMessages);
                                }

                                _lastMessage = await _channel.SendMessageAsync(temp);
                            }
                            catch (Exception e)
                            {
                                Program.Log(e);
                            }

                            await Task.Delay(500);
                        }
                    });
                }
                await Task.Delay(-1);
            }
        }

        private DiscordMessage _lastMessage;
        private DateTime _nextStatusCheck;
        private DateTime _nextOutputTime;

        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            try
            {
                await Task.Yield();
                _startTimes.SocketStart = DateTime.Now;
                var guild = await _client.GetGuildAsync(_config.GuildId);
                var channels= await guild.GetChannelsAsync();
                _channel = channels.FirstOrDefault(a => a.Id == _config.ChannelId);
                Ready = true;
                Program.SetLogger(e.Client.DebugLogger);
                Program.Log("Bot ready");
            }
            catch (Exception exception)
            {
                Program.Log(exception);
            }
        }


        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _client?.Dispose();
            _interactivity = default;
        }
    }
    public class ServerModel
    {
        public string Name { get; set; }
        public string Online { get; set; }
        public string Type { get; set; }
        public string EXPRate { get; set; }
        public string UserCount { get; set; }
        public string Id { get; set; }
    }
}