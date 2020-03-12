using System;

namespace LOMCN.DiscordBot
{
    public class ServerEntryStatusHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool Online { get; set; }
        public int UserCount { get; set; }
        public DateTime EntryTime { get; set; }
    }
}