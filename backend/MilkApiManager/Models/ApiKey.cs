using System;

namespace MilkApiManager.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; }
        public string KeyHash { get; set; } // 僅存 Hash
        public string Owner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string Scopes { get; set; } // JSON: ["read", "write"]
    }

    public class CreateKeyRequest
    {
        public string Owner { get; set; }
        public int ValidityDays { get; set; }
    }
}
