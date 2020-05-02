using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace LOMCN.DiscordBot
{
    public class DbHandler : IDisposable
    {
        public static bool Ready { get; private set; }

        public Func<Guid, ServerEntry> FindByGuid;
        public Func<int, ServerEntry> FindById;
        public Func<string, ServerEntry> FindByServerName;
        public Func<Guid, ServerEntryStatus> GetStatusByServerId;
        public Func<Guid, List<ServerEntryStatusHistory>> GetStatusHistoryByServerId;
        public Func<List<ServerEntry>> GetAllServers;
        public Action<ServerModel> UpdateServerStatus;
        public Action<Guid, ServerEntry> UpdateServer;
        public Action<ServerEntry> DeleteServer;
        public static DbHandler Instance { get; } = new DbHandler();
        public bool Running { get; private set; }

        private MongoClient _client;
        private IMongoDatabase _db;
        public event EventHandler<List<ServerEntry>> DataUpdated;
        private Config _config => Program.Config;
        private DbHandler()
        {
            BsonClassMap.RegisterClassMap<ServerEntry>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id).SetIdGenerator(CombGuidGenerator.Instance);
            });
            BsonClassMap.RegisterClassMap<ServerEntryStatus>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id).SetIdGenerator(CombGuidGenerator.Instance);
            });
            BsonClassMap.RegisterClassMap<ServerEntryStatusHistory>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id).SetIdGenerator(CombGuidGenerator.Instance);
            });
        }

        private Thread _thread;
        public void Start()
        {
            if (Running) return;
            _thread = new Thread(WorkLoop) { IsBackground = true};
            _thread.Start();
        }

        private void WorkLoop(object obj)
        {
            try
            {
                _client = new MongoClient($"{_config.DbHost}");
                _db = _client.GetDatabase("lomcn");


                FindByGuid = serverId =>
                {
                    try
                    {
                        return FindOne(a => a.Id == serverId);
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                FindById = serverId =>
                {
                    try
                    {
                        return FindOne(a => a.ServerId == serverId);
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                FindByServerName = serverName =>
                {
                    try
                    {
                        return FindOne(a => a.ServerName.ToLower() == serverName.ToLower());
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                GetStatusByServerId = serverId => 
                {
                    try
                    {
                        return FindOne(a => a.Id == serverId)?.CurrentStatus;
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                GetStatusHistoryByServerId = serverId =>
                {
                    try
                    {
                        return FindOne(a => a.Id == serverId).History;
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                GetAllServers = () => {
                    try
                    {
                        return _db.GetCollection<ServerEntry>("mir-servers").AsQueryable().ToList();
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                        return null;
                    }
                };
                UpdateServerStatus = UpdateStatus;
                UpdateServer = (guid, entry) =>
                {
                    try
                    {
                        var existing = FindOne(a => a.Id == guid);
                        if (existing != null)
                            Update(entry);
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                    }
                };
                DeleteServer = entry =>
                {
                    try
                    {
                        if (entry == null || entry.Id == Guid.Empty)
                            return;
                        var existing = FindOne(a => a.Id == entry.Id);
                        if (existing == null)
                            return;
                        _db.GetCollection<ServerEntry>("mir-servers").DeleteOne(a => a.Id == entry.Id);
                    }
                    catch (Exception e)
                    {
                        Program.Log(e);
                    }
                };
                Running = true;
            }
            catch (Exception e)
            {
                Program.Log(e);
                throw;
            }
            while (Running)
            {
                
                if (!Bot.Ready ||
                    DataUpdated == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    continue;
                }
                if (!Ready)
                    Ready = true;
                try
                {
                    var current = GetAllServers();
                    DataUpdated.Invoke(this, current);
                }
                catch (Exception e)
                {
                    Program.Log(e);
                }
                Thread.Sleep(_config.UpdateDelay);
            }
        }

        private void AddEntry(ServerEntry entry)
        {
            if (entry.CurrentStatus == null)
                return;
            entry.History.Add(new ServerEntryStatusHistory
            {
                Online = entry.CurrentStatus.Online,
                UserCount = entry.CurrentStatus.UserCount,
                EntryTime = entry.CurrentStatus.EditTime
            });
            _db.GetCollection<ServerEntry>("mir-servers").InsertOne(entry);
        }

        private void Update(ServerEntry entry)
        {
            _db.GetCollection<ServerEntry>("mir-servers").ReplaceOne(a => a.Id == entry.Id, entry);
        }

        private ServerEntry FindOne(Expression<Func<ServerEntry, bool>> search)
        {
            var servers = _db.GetCollection<ServerEntry>("mir-servers").AsQueryable().ToList();
            var query = servers.AsQueryable();
            return query.FirstOrDefault(search);
        }

        private void NewServer(ServerModel model)
        {
            var existing = FindOne(a => a.ServerName.ToLower() == model.Name.ToLower());
            if (existing != null)
                return;
            var entry = new ServerEntry
            {
                CurrentStatus = new ServerEntryStatus(),
                History = new List<ServerEntryStatusHistory>(),
                ServerName = model.Name,
                ServerType = model.Type,
                ServerId = Convert.ToInt32(model.Id),
                ExpRate = model.EXPRate,
                RgbColor = string.Empty
            };
            AddEntry(entry);
        }

        private void UpdateStatus(ServerModel model)
        {
            var id = Convert.ToInt32(model.Id);
            var entry = FindById(id);
            if (entry == null)
            {
                NewServer(model);
                return;
            }
            entry.CurrentStatus = new ServerEntryStatus
            {
                Id = entry.CurrentStatus.Id,
                Online = model.Online == "1",
                UserCount = Convert.ToInt32(model.UserCount),
                EditTime = DateTime.Now
            };
            var statusHistory = new ServerEntryStatusHistory
            {
                Online = entry.CurrentStatus.Online,
                UserCount = entry.CurrentStatus.UserCount,
                EntryTime = entry.CurrentStatus.EditTime
            };
            entry.History.Add(statusHistory);
            Update(entry);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Running = false;
            DataUpdated = null;
            FindById = null;
            FindByGuid = null;
            FindByServerName = null;
            GetStatusByServerId = null;
            GetStatusHistoryByServerId = null;
            GetAllServers = null;
            UpdateServerStatus = null;
            UpdateServer = null;
            DeleteServer = null;
            _client = null;
            _db = null;
            DataUpdated = null;
            _thread = null;
        }
    }
}