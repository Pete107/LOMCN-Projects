using System;
using System.Collections.Generic;
using System.IO;
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
        public static bool _ready;
        public static bool Ready => _ready;
        public Func<Guid, ServerEntry> FindById;
        public Func<string, ServerEntry> FindByServerName;
        public Func<Guid, ServerEntryStatus> GetStatusByServerId;
        public Func<Guid, List<ServerEntryStatusHistory>> GetStatusHistoryByServerId;
        public Func<List<ServerEntry>> GetAllServers;
        public Action<Guid, ServerEntryStatus> UpdateServerStatus;
        public Action<Guid, ServerEntry> UpdateServer;
        public Action<ServerEntry> DeleteServer;
        public static DbHandler Instance { get; } = new DbHandler();
        private bool _running;
        public bool Running => _running;
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
            _running = true;

            FindById = serverId => FindOne(a => a.Id == serverId);
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
                    _ready = true;
                var current = GetAllServers();
                DataUpdated.Invoke(this, current);
                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }

        public void AddEntry(ServerEntry entry)
        {
            if (entry.CurrentStatus == null)
                return;
            entry.History.Add(new ServerEntryStatusHistory
            {
                EntryTime = entry.CurrentStatus.EditDate,
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

        public void UpdateSubscription(Guid id, DateTime till)
        {
            var entry = FindById(id);
            if (entry == null) return;
            entry.SubscriptionTill = till;
            Update(entry);
        }

        public void NewServer(DateTime liveDate, string serverName, string serverAddress, DateTime subscriptionTill,
            bool showUserCount = false, MirServerType mirServerType = MirServerType.Mir2Crystal, int statusPort = 3000, int gamePort = 7000)
        {
            var existing = FindOne(a => a.ServerName.ToLower() == serverName.ToLower());
            if (existing != null)
                return;
            var entry = new ServerEntry
            {
                CreateDate = DateTime.Now,
                CurrentStatus = new ServerEntryStatus(),
                History = new List<ServerEntryStatusHistory>(),
                LiveDate = liveDate,
                ServerAddress = serverAddress,
                ServerName = serverName,
                StatusPort = statusPort,
                ServerType = mirServerType,
                ShowUserCount = showUserCount,
                SubscriptionTill = subscriptionTill,
                GamePort = gamePort
            };
            AddEntry(entry);
        }

        public void UpdateStatus(Guid id, ServerEntryStatus newStatus)
        {
            var entry = FindById(id);
            if (entry == null)
                return;
            var statusHistory = new ServerEntryStatusHistory
            {
                EntryTime = entry.CurrentStatus.EditDate,
                Online = entry.CurrentStatus.Online,
                UserCount = entry.CurrentStatus.UserCount
            };
            entry.CurrentStatus = newStatus;
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