using System;

namespace MilkApiManager.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; }
        public required string KeyHash { get; set; } // 僅存 Hash
        public required string Owner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastRotatedAt { get; set; } // 新增：上次輪轉時間
        public bool IsActive { get; set; }
        public required string Scopes { get; set; } // JSON: ["read", "write"]
        public required string ContactEmail { get; set; } // 新增：通知聯絡人
    }

    public class CreateKeyRequest
    {
        public required string Owner { get; set; }
        public int ValidityDays { get; set; } = 90; // 預設 90 天
        public string Scopes { get; set; } = "[\"read\"]";
        public string ContactEmail { get; set; } = "";
    }
}