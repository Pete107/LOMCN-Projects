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
        private readonly Config _config;
        private DbHandler()
        {
            _config = Program.Config;
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
            _thread = new Thread(WorkLoop) { IsBackground = true};
            _thread.Start();
        }

        private void WorkLoop(object obj)
        {
            

            _client = new MongoClient($"mongodb://{_config.DbHost}:{_config.DbPort}");
            _db = _client.GetDatabase("lomcn");
            Running = true;

            FindByGuid = serverId => FindOne(a => a.Id == serverId);
            FindById = serverId => FindOne(a => a.ServerId == serverId);
            FindByServerName = serverName => FindOne(a => a.ServerName.ToLower() == serverName.ToLower());
            GetStatusByServerId = serverId => FindOne(a => a.Id == serverId)?.CurrentStatus;
            GetStatusHistoryByServerId = serverId => FindOne(a => a.Id == serverId).History;
            GetAllServers = () => _db.GetCollection<ServerEntry>("mir-servers").AsQueryable().ToList();
            UpdateServerStatus = UpdateStatus;
            UpdateServer = (guid, entry) =>
            {
                var existing = FindOne(a => a.Id == guid);
                if (existing != null)
                    Update(entry);
            };
            DeleteServer = entry =>
            {
                if (entry == null || entry.Id == Guid.Empty)
                    return;
                var existing = FindOne(a => a.Id == entry.Id);
                if (existing == null)
                    return;
                _db.GetCollection<ServerEntry>("mir-servers").DeleteOne(a => a.Id == entry.Id);
            };
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
                var current = GetAllServers();
                DataUpdated.Invoke(this, current);
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
                UserCount = entry.CurrentStatus.UserCount
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

        public void NewServer(ServerModel model)
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
                ExpRate = model.EXPRate
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
            var statusHistory = new ServerEntryStatusHistory
            {
                Online = entry.CurrentStatus.Online,
                UserCount = entry.CurrentStatus.UserCount
            };
            entry.CurrentStatus = new ServerEntryStatus
            {
                Id = entry.CurrentStatus.Id,
                Online = model.Online == "1",
                UserCount = Convert.ToInt32(model.UserCount)
            };
            entry.History.Add(statusHistory);
            Update(entry);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            DataUpdated = null;
        }
    }
}