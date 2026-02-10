using System;

namespace MilkApiManager.Models
{
    public class UsageRecord
    {
        public Guid Id { get; set; }
        public Guid ApiKeyId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Endpoint { get; set; }
        public int RequestCount { get; set; } = 1;
        public ApiKey ApiKey { get; set; }
    }
}