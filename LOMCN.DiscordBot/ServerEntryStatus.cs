using System;

namespace LOMCN.DiscordBot
{
    public class ServerEntryStatus
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime EditDate { get; set; } = DateTime.Now;
        public bool Online { get; set; } = false;
        public int UserCount { get; set; } = -1;
    }
}