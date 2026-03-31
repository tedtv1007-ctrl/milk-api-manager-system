using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// Upstream health check configuration, synced to APISIX upstream checks.
/// </summary>
public class HealthCheckConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UpstreamId { get; set; } = string.Empty;

    // --- Active Health Check ---
    /// <summary>Enable active health checks (periodically probes upstream).</summary>
    public bool ActiveEnabled { get; set; } = true;

    /// <summary>Active check HTTP path (e.g. "/health").</summary>
    public string ActiveHttpPath { get; set; } = "/health";

    /// <summary>Active check interval in seconds.</summary>
    public int ActiveIntervalSeconds { get; set; } = 10;

    /// <summary>Active healthy threshold (consecutive successes).</summary>
    public int ActiveHealthySuccesses { get; set; } = 2;

    /// <summary>Active unhealthy threshold (consecutive failures).</summary>
    public int ActiveUnhealthyFailures { get; set; } = 3;

    /// <summary>Active healthy HTTP statuses (comma-separated, e.g. "200,302").</summary>
    public string ActiveHealthyStatuses { get; set; } = "200";

    /// <summary>Active unhealthy HTTP statuses (comma-separated, e.g. "429,500,503").</summary>
    public string ActiveUnhealthyStatuses { get; set; } = "429,500,503";

    /// <summary>Timeout for active health check in seconds.</summary>
    public int ActiveTimeoutSeconds { get; set; } = 5;

    // --- Passive Health Check ---
    /// <summary>Enable passive health checks (monitors real traffic).</summary>
    public bool PassiveEnabled { get; set; }

    /// <summary>Passive healthy HTTP statuses (comma-separated).</summary>
    public string PassiveHealthyStatuses { get; set; } = "200,201,202,301,302";

    /// <summary>Passive unhealthy HTTP statuses (comma-separated).</summary>
    public string PassiveUnhealthyStatuses { get; set; } = "429,500,503";

    /// <summary>Passive unhealthy timeout count to mark as unhealthy.</summary>
    public int PassiveUnhealthyTimeouts { get; set; } = 3;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
