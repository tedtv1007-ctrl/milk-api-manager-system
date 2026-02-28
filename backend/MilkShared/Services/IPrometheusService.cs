using MilkApiManager.Models;

namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for querying Prometheus metrics.
/// </summary>
public interface IPrometheusService
{
    Task<List<AnalyticsResult>> GetMetricAsync(string query, DateTime start, DateTime end, string step);
    Task<Dictionary<string, double>> QueryVectorAsync(string query);
}
