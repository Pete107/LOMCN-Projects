using System;
using System.Collections.Generic;

namespace LOMCN.DiscordBot
{
    public class ServerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime LiveDate { get; set; } = new DateTime(1900, 12, 12);
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public string ServerName { get; set; } = string.Empty;
        public string ServerAddress { get; set; } = string.Empty;
        public int StatusPort { get; set; } = 3000;
        /// <summary>
        /// Backup Port
        /// </summary>
        public int GamePort { get; set; } = 7000;
        public MirServerType ServerType { get; set; } = MirServerType.Mir2Crystal;
        public bool ShowUserCount { get; set; } = false;
        public DateTime SubscriptionTill { get; set; }
        public ServerEntryStatus CurrentStatus { get; set; }
        public List<ServerEntryStatusHistory> History { get; set; } = new List<ServerEntryStatusHistory>();

    }
}