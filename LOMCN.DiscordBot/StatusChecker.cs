using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LOMCN.DiscordBot
{
    public class StatusChecker
    {
        public static StatusChecker Instance { get; } = new StatusChecker();
        public bool Running { get; private set; }
        private Thread _thread;
        private StatusChecker()
        {
            
        }

        public void Start()
        {
            _thread = new Thread(WorkLoop) {IsBackground = true};
            _thread.Start();
        }

        private List<ServerEntry> _servers;
        private void WorkLoop()
        {
            Running = true;
            while (Running)
            {
                if (!DbHandler.Ready)
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                _servers = DbHandler.Instance.GetAllServers();
                if (_servers.Count > 0)
                {
                    foreach (var serverEntry in _servers)
                    {
                        ServerEntryStatus status;

                        using var client = new TcpClient();
                        if (serverEntry.ShowUserCount && serverEntry.SubscriptionTill >= DateTime.Now)
                        {
                            if (client.ConnectAsync(serverEntry.ServerAddress, serverEntry.StatusPort)
                                .Wait(2000))
                            {
                                string output;
                                using (var stream = client.GetStream())
                                {
                                    var bytes = new byte[client.ReceiveBufferSize];
                                    stream.Read(bytes, 0, bytes.Length);
                                    output = Encoding.ASCII.GetString(bytes);
                                }

                                if (output.Length <= 0) return;
                                var splits = output.Replace("\0", "").Split('/');
                                int.TryParse(splits[2], out var count);
                                status = new ServerEntryStatus
                                {
                                    Id = serverEntry.CurrentStatus.Id,
                                    EditDate = DateTime.Now,
                                    Online = true,
                                    UserCount = count
                                };
                            }
                            else if (client.ConnectAsync(serverEntry.ServerAddress, serverEntry.GamePort)
                                .Wait(2000))
                            {
                                status = new ServerEntryStatus
                                {
                                    Id = serverEntry.CurrentStatus.Id,
                                    EditDate = DateTime.Now,
                                    Online = true,
                                    UserCount = 0
                                };
                            }
                            else
                            {
                                status = new ServerEntryStatus
                                {
                                    Id = serverEntry.CurrentStatus.Id,
                                    EditDate = DateTime.Now,
                                    Online = false,
                                    UserCount = -1
                                };

                            }
                        }
                        else
                        {
                            if (client.ConnectAsync(serverEntry.ServerAddress, serverEntry.GamePort)
                                .Wait(2000))
                            {
                                status = new ServerEntryStatus
                                {
                                    Id = serverEntry.CurrentStatus.Id,
                                    EditDate = DateTime.Now,
                                    Online = true,
                                    UserCount = 0
                                };
                            }
                            else
                            {
                                status = new ServerEntryStatus
                                {
                                    Id = serverEntry.CurrentStatus.Id,
                                    EditDate = DateTime.Now,
                                    Online = false,
                                    UserCount = -1
                                };

                            }
                        }

                        DbHandler.Instance.UpdateServerStatus(serverEntry.Id, status);
                    }

                    Thread.Sleep(TimeSpan.FromMinutes(15));
                }
                else Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }
    }
}