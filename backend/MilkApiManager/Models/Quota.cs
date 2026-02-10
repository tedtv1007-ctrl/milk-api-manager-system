using System;

namespace MilkApiManager.Models
{
    public class Quota
    {
        public Guid Id { get; set; }
        public Guid ApiKeyId { get; set; }
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
        public int RequestsPerDay { get; set; }
        public DateTime LastReset { get; set; } = DateTime.UtcNow;
        public ApiKey ApiKey { get; set; }
    }
}