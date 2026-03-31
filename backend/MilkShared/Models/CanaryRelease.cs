using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// Canary release / traffic split configuration per route.
/// Synced to APISIX traffic-split plugin for weighted routing.
/// </summary>
public class CanaryRelease
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Primary upstream ID (stable version).</summary>
    [Required]
    public string StableUpstreamId { get; set; } = string.Empty;

    /// <summary>Canary upstream ID (new version).</summary>
    [Required]
    public string CanaryUpstreamId { get; set; } = string.Empty;

    /// <summary>Weight for stable upstream (0-100).</summary>
    public int StableWeight { get; set; } = 90;

    /// <summary>Weight for canary upstream (0-100).</summary>
    public int CanaryWeight { get; set; } = 10;

    /// <summary>Status: "active", "paused", "completed", "rolled_back".</summary>
    public string Status { get; set; } = "active";

    /// <summary>Optional match rules in JSON (header-based, cookie-based).</summary>
    public string? MatchRulesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
