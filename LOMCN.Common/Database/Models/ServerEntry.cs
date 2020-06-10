using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace LOMCN.Common.Database.Models
{
    public class ServerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int ServerId { get; set; } = -1;
        
        public string ServerName { get; set; } = string.Empty;
        
        public string ServerType { get; set; } = string.Empty;
        public string ExpRate { get; set; } = string.Empty;
        public string RgbColor { get; set; } = string.Empty;
        public ServerEntryStatus CurrentStatus { get; set; }
        [BsonIgnore]
        public List<ServerEntryStatusHistory> History = new List<ServerEntryStatusHistory>();
    }
}