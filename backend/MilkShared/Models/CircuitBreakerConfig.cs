using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// Circuit breaker configuration per route, synced to APISIX api-breaker plugin.
/// </summary>
public class CircuitBreakerConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>HTTP status code returned when upstream is unhealthy.</summary>
    public int BreakResponseCode { get; set; } = 502;

    /// <summary>Response body returned when circuit is open.</summary>
    public string? BreakResponseBody { get; set; }

    /// <summary>Max time in seconds for circuit breaking (exponential backoff cap).</summary>
    public int MaxBreakerSec { get; set; } = 300;

    /// <summary>Comma-separated unhealthy HTTP status codes (e.g. "500,503").</summary>
    public string UnhealthyHttpStatuses { get; set; } = "500,503";

    /// <summary>Number of consecutive failures to trip the breaker.</summary>
    public int UnhealthyFailures { get; set; } = 3;

    /// <summary>Comma-separated healthy HTTP status codes (e.g. "200").</summary>
    public string HealthyHttpStatuses { get; set; } = "200";

    /// <summary>Number of consecutive successes to close the breaker.</summary>
    public int HealthySuccesses { get; set; } = 3;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
