namespace MilkApiManager.Options;

/// <summary>
/// Configuration options for Prometheus connectivity.
/// </summary>
public class PrometheusOptions
{
    public const string SectionName = "Prometheus";

    /// <summary>Prometheus server base URL.</summary>
    public string Url { get; set; } = "http://prometheus:9090";
}
