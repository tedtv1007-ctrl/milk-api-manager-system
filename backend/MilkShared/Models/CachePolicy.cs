using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

/// <summary>
/// Response caching policy per route, synced to APISIX proxy-cache plugin.
/// </summary>
public class CachePolicy
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>Cache TTL in seconds.</summary>
    public int CacheTtlSeconds { get; set; } = 300;

    /// <summary>HTTP methods to cache (comma-separated, e.g. "GET,HEAD").</summary>
    public string CacheHttpMethods { get; set; } = "GET";

    /// <summary>HTTP status codes to cache (comma-separated, e.g. "200,301").</summary>
    public string CacheHttpStatuses { get; set; } = "200";

    /// <summary>Cache strategy: "disk" or "memory".</summary>
    public string CacheStrategy { get; set; } = "memory";

    /// <summary>Cache key template (optional, e.g. "$host$request_uri").</summary>
    public string? CacheKey { get; set; }

    /// <summary>Comma-separated headers that vary caching.</summary>
    public string? VaryHeaders { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
}
