using System;
using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; }

        [Required]
        public required string KeyHash { get; set; } // 僅存 Hash

        [Required, StringLength(200)]
        public required string Owner { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastRotatedAt { get; set; } // 上次輪轉時間
        public bool IsActive { get; set; }

        [Required]
        public required string Scopes { get; set; } // JSON: ["read", "write"]

        [Required, EmailAddress]
        public required string ContactEmail { get; set; } // 通知聯絡人
    }

    public class CreateKeyRequest
    {
        [Required, StringLength(200)]
        public required string Owner { get; set; }

        [Range(1, 3650)]
        public int ValidityDays { get; set; } = 90;

        public string Scopes { get; set; } = "[\"read\"]";

        [EmailAddress]
        public string? ContactEmail { get; set; }
    }
}