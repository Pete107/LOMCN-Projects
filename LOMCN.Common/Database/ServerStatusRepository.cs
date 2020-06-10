using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LOMCN.Common.Database.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace LOMCN.Common.Database
{
    public sealed class ServerStatusRepository
    {
        private readonly IMongoDatabase _db;
        private const string ServersTable = "mir-servers";
        private const string StatsTable = "server-stats";

        static ServerStatusRepository()
        {
            BsonClassMap.RegisterClassMap<ServerEntry>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.History);
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

        public ServerStatusRepository(string hostAddress)
        {
            var client = new MongoClient(hostAddress);
            _db = client.GetDatabase("lomcn");
        }

        public ServerStatusRepository(Config config)
        {
            var client = new MongoClient($"{config.DbHost}");
            _db = client.GetDatabase("lomcn");
        }

        public async Task<bool> EntryExists(int id) =>
            await FindById(id) != null;

        public async Task<ServerEntry> FindById(int id) =>
            await _db.GetCollection<ServerEntry>(ServersTable).Find(a => a.ServerId == id).FirstOrDefaultAsync();

        public async Task<List<ServerEntry>> GetAllAsync()
        {
            var servers = await _db.GetCollection<ServerEntry>(ServersTable).AsQueryable().ToListAsync();
            foreach (var serverEntry in servers)
            {
                serverEntry.History = (await _db.GetCollection<ServerEntryStatusHistory>(StatsTable)
                    .FindAsync(a => a.ServerId == serverEntry.Id)).ToList();
            }
            return servers;
        }

        public async Task<List<ServerEntry>> GetAllAsync(Expression<Func<ServerEntryStatusHistory, bool>> search)
        {
            var servers = await _db.GetCollection<ServerEntry>(ServersTable).AsQueryable().ToListAsync();
            foreach (var serverEntry in servers)
            {
                serverEntry.History = await (await _db.GetCollection<ServerEntryStatusHistory>(StatsTable).FindAsync(search))
                    .ToListAsync();
            }

            return servers;
        }

        public async Task AddStat(ServerEntry serverEntry)
        {
            var status = new ServerEntryStatusHistory
            {
                EntryTime = serverEntry.CurrentStatus.EditTime,
                Id = Guid.NewGuid(),
                Online = serverEntry.CurrentStatus.Online,
                ServerId = serverEntry.Id,
                UserCount = serverEntry.CurrentStatus.UserCount
            };
            await _db.GetCollection<ServerEntryStatusHistory>(StatsTable).InsertOneAsync(status);
        }

        public async Task AddServerAsync(ServerEntry serverEntry)
        {
            await AddStat(serverEntry);
            await _db.GetCollection<ServerEntry>(ServersTable).InsertOneAsync(serverEntry);
        }

        public async Task UpdateRgbColour(ServerEntry serverEntry)
        {
            if (!await EntryExists(serverEntry.ServerId)) return;
            await _db.GetCollection<ServerEntry>(ServersTable).ReplaceOneAsync(a => a.Id == serverEntry.Id, serverEntry);
        }

        public async Task UpdateCurrentStatusAsync(ServerEntry serverEntry)
        {
            var existingModel = await FindById(serverEntry.ServerId);
            if (existingModel == null)
            {
                await AddServerAsync(serverEntry);
                return;
            }
            
            await AddStat(existingModel);
            
        }

        public async Task<ServerEntry> FindByServerName(string serverName)
        {
            serverName = serverName.ToLower();
            var foundServer = await _db.GetCollection<ServerEntry>(ServersTable)
                .Find(a => a.ServerName.ToLower() == serverName).FirstOrDefaultAsync();
            if (foundServer == null) return null;
            var stats = await (await _db.GetCollection<ServerEntryStatusHistory>(StatsTable)
                .FindAsync(a => a.ServerId == foundServer.Id)).ToListAsync();
            foundServer.History = stats;
            return foundServer;
        }
    }
}