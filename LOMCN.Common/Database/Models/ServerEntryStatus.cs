using System;

namespace LOMCN.Common.Database.Models
{
    public class ServerEntryStatus
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool Online { get; set; } = false;
        public int UserCount { get; set; } = -1;
        public DateTime EditTime { get; set; } = DateTime.MinValue;
    }
}