using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// Request/Response transformation rule per route, synced to APISIX proxy-rewrite plugin.
/// </summary>
public class RequestTransformRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>Phase: "request" or "response".</summary>
    [Required]
    public string Phase { get; set; } = "request";

    /// <summary>Operation type: "add_header", "remove_header", "rename_header", "rewrite_uri", "rewrite_host".</summary>
    [Required]
    public string OperationType { get; set; } = "add_header";

    /// <summary>The key (header name, uri pattern, etc.).</summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>The value to set/replace.</summary>
    public string? Value { get; set; }

    /// <summary>Execution order (lower runs first).</summary>
    public int Priority { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
