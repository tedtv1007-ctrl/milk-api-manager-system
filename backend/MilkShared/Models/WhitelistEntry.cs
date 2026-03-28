using System;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models
{
    public class WhitelistEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string RouteId { get; set; }

        [Required]
        public required IPAddress IpCidr { get; set; }

        public string? Reason { get; set; }

        public string? AddedBy { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }
    }
}