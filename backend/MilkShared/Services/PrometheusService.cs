using MilkApiManager.Models;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public class PrometheusService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PrometheusService> _logger;
        private readonly string _prometheusUrl;

        public PrometheusService(HttpClient httpClient, ILogger<PrometheusService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _prometheusUrl = Environment.GetEnvironmentVariable("PROMETHEUS_URL") ?? "http://prometheus:9090";
        }

        public virtual async Task<List<AnalyticsResult>> GetMetricAsync(string query, DateTime start, DateTime end, string step)
        {
            try
            {
                var startUnix = ((DateTimeOffset)start).ToUnixTimeSeconds();
                var endUnix = ((DateTimeOffset)end).ToUnixTimeSeconds();
                
                var url = $"{_prometheusUrl}/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startUnix}&end={endUnix}&step={step}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Prometheus query failed: {Status}", response.StatusCode);
                    return new List<AnalyticsResult>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                var results = new List<AnalyticsResult>();

                if (doc.RootElement.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("result", out var resultList))
                {
                    foreach (var item in resultList.EnumerateArray())
                    {
                        var label = "Value";
                        if (item.TryGetProperty("metric", out var metric))
                        {
                            // Try to find a meaningful label
                            if (metric.TryGetProperty("consumer", out var c)) label = c.GetString();
                            else if (metric.TryGetProperty("route", out var r)) label = r.GetString();
                            else if (metric.TryGetProperty("code", out var code)) label = $"HTTP {code.GetString()}";
                        }

                        var analyticsResult = new AnalyticsResult { Label = label };
                        
                        if (item.TryGetProperty("values", out var values))
                        {
                            foreach (var v in values.EnumerateArray())
                            {
                                var ts = DateTimeOffset.FromUnixTimeSeconds((long)v[0].GetDouble()).DateTime;
                                var val = double.Parse(v[1].GetString());
                                analyticsResult.Data.Add(new MetricPoint { Timestamp = ts, Value = val });
                            }
                        }
                        results.Add(analyticsResult);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying Prometheus");
                return new List<AnalyticsResult>();
            }
        }

        public virtual async Task<Dictionary<string, double>> QueryVectorAsync(string query)
        {
            try
            {
                var url = $"{_prometheusUrl}/api/v1/query?query={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new Dictionary<string, double>();

                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                var results = new Dictionary<string, double>();

                if (doc.RootElement.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("result", out var resultList))
                {
                    foreach (var item in resultList.EnumerateArray())
                    {
                        string key = "unknown";
                        if (item.TryGetProperty("metric", out var metric))
                        {
                            // Try to grab IP or relevant label
                            if (metric.TryGetProperty("client_ip", out var ip)) key = ip.GetString() ?? "unknown";
                            else if (metric.TryGetProperty("remote_addr", out var remote)) key = remote.GetString() ?? "unknown";
                        }

                        if (item.TryGetProperty("value", out var valueArr))
                        {
                            var valStr = valueArr[1].GetString();
                            if (double.TryParse(valStr, out var val))
                            {
                                results[key] = val;
                            }
                        }
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing vector query: {Query}", query);
                return new Dictionary<string, double>();
            }
        }
    }
}
