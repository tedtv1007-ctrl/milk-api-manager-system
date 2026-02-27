using System;
using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models
{
    public class BlacklistEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string IpOrCidr { get; set; }

        public string? Reason { get; set; }

        public string? AddedBy { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }
    }
}