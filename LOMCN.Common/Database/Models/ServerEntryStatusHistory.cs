using System;

namespace LOMCN.Common.Database.Models
{
    public class ServerEntryStatusHistory
    {
        public Guid ServerId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool Online { get; set; }
        public int UserCount { get; set; }
        public DateTime EntryTime { get; set; }
    }
}