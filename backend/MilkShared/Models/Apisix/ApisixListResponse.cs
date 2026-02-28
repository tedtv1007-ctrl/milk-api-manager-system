using System.Text.Json.Serialization;

namespace MilkApiManager.Models.Apisix;

/// <summary>
/// Typed response wrapper for APISIX Admin API list endpoints.
/// Provides a stable API contract independent of APISIX version changes.
/// </summary>
public class ApisixListResponse<T>
{
    [JsonPropertyName("list")]
    public List<ApisixNodeItem<T>> List { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

/// <summary>
/// Individual item in an APISIX list response.
/// </summary>
public class ApisixNodeItem<T>
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public T Value { get; set; } = default!;

    [JsonPropertyName("createdIndex")]
    public long CreatedIndex { get; set; }

    [JsonPropertyName("modifiedIndex")]
    public long ModifiedIndex { get; set; }
}
