using System;
using System.Collections.Generic;

namespace LOMCN.DiscordBot
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
        public List<ServerEntryStatusHistory> History { get; set; } = new List<ServerEntryStatusHistory>();
    }
}