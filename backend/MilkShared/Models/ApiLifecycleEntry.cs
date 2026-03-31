using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// API lifecycle management - track API versions, deprecation, and sunset dates.
/// </summary>
public class ApiLifecycleEntry
{
    [Key]
    public int Id { get; set; }

    /// <summary>The API service identifier (route or service name).</summary>
    [Required]
    public string ApiIdentifier { get; set; } = string.Empty;

    /// <summary>API version string (e.g. "v1", "v2.1").</summary>
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>Lifecycle status: "planning", "active", "deprecated", "retired".</summary>
    [Required]
    public string Status { get; set; } = "active";

    /// <summary>Date this version was published/activated.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Date this version was deprecated (still serving but discouraged).</summary>
    public DateTime? DeprecatedAt { get; set; }

    /// <summary>Scheduled sunset date (will stop serving after this date).</summary>
    public DateTime? SunsetAt { get; set; }

    /// <summary>Date this version was fully retired.</summary>
    public DateTime? RetiredAt { get; set; }

    /// <summary>Deprecation notice message (returned in Sunset/Deprecation headers).</summary>
    public string? DeprecationNotice { get; set; }

    /// <summary>URL pointing to the successor API version documentation.</summary>
    public string? SuccessorUrl { get; set; }

    /// <summary>Owner team or contact.</summary>
    public string? OwnerTeam { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
