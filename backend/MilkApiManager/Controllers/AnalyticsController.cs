using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class AnalyticsController : ControllerBase
    {
        private readonly PrometheusService _prometheus;

        public AnalyticsController(PrometheusService prometheus)
        {
            _prometheus = prometheus;
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests([FromQuery] AnalyticsQuery query)
        {
            var start = query.StartTime ?? DateTime.UtcNow.AddHours(-1);
            var end = query.EndTime ?? DateTime.UtcNow;
            
            // apisix_http_requests_total
            var promQuery = "sum(irate(apisix_http_status{";
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(query.Consumer)) filters.Add($"consumer=\"{query.Consumer}\"");
            if (!string.IsNullOrEmpty(query.Route)) filters.Add($"route=\"{query.Route}\"");
            promQuery += string.Join(",", filters);
            promQuery += "}[5m])) by (consumer, route)";

            // Step should be adjusted based on time range
            var step = string.IsNullOrEmpty(query.Step) ? GetStep(start, end) : query.Step;

            var result = await _prometheus.GetMetricAsync(promQuery, start, end, step);
            return Ok(result);
        }

        private string GetStep(DateTime start, DateTime end)
        {
            var duration = end - start;
            if (duration.TotalHours <= 1) return "1m";
            if (duration.TotalDays <= 1) return "10m";
            if (duration.TotalDays <= 7) return "1h";
            return "6h";
        }

        [HttpGet("latency")]
        public async Task<IActionResult> GetLatency([FromQuery] AnalyticsQuery query)
        {
            var start = query.StartTime ?? DateTime.UtcNow.AddHours(-1);
            var end = query.EndTime ?? DateTime.UtcNow;

            var promQuery = "histogram_quantile(0.95, sum(rate(apisix_http_latency_bucket{type=\"request\",";
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(query.Consumer)) filters.Add($"consumer=\"{query.Consumer}\"");
            if (!string.IsNullOrEmpty(query.Route)) filters.Add($"route=\"{query.Route}\"");
            promQuery += string.Join(",", filters);
            promQuery += "}[5m])) by (le, consumer, route))";

            var step = string.IsNullOrEmpty(query.Step) ? GetStep(start, end) : query.Step;
            var result = await _prometheus.GetMetricAsync(promQuery, start, end, step);
            return Ok(result);
        }

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrors([FromQuery] AnalyticsQuery query)
        {
            var start = query.StartTime ?? DateTime.UtcNow.AddHours(-1);
            var end = query.EndTime ?? DateTime.UtcNow;

            // Percentage of non-2xx/3xx
            var promQuery = "sum(rate(apisix_http_status{code!~\"[23].*\",";
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(query.Consumer)) filters.Add($"consumer=\"{query.Consumer}\"");
            if (!string.IsNullOrEmpty(query.Route)) filters.Add($"route=\"{query.Route}\"");
            promQuery += string.Join(",", filters);
            promQuery += "}[5m])) / sum(rate(apisix_http_status{";
            promQuery += string.Join(",", filters);
            promQuery += "}[5m])) * 100";

            var step = string.IsNullOrEmpty(query.Step) ? GetStep(start, end) : query.Step;
            var result = await _prometheus.GetMetricAsync(promQuery, start, end, step);
            return Ok(result);
        }

        [HttpGet("top-slow-routes")]
        public async Task<IActionResult> GetTopSlowRoutes([FromQuery] int limit = 5)
        {
            // P95 latency grouped by route in the last 1 hour, sorted by descending
            var promQuery = $"topk({limit}, sort_desc(histogram_quantile(0.95, sum(rate(apisix_http_latency_bucket{{type=\"request\"}}[1h])) by (le, route))))";
            
            var start = DateTime.UtcNow.AddMinutes(-5); // We only need latest point
            var end = DateTime.UtcNow;
            
            var result = await _prometheus.GetMetricAsync(promQuery, start, end, "1m");
            return Ok(result);
        }

        [HttpGet("sla")]
        public async Task<IActionResult> GetSlaStats()
        {
            // Calculate: (Success requests / Total requests) * 100 over the last 24h
            var promQuery = "sum(rate(apisix_http_status{code=~\"[23].*\"}[24h])) / sum(rate(apisix_http_status[24h])) * 100";
            
            var result = await _prometheus.QueryVectorAsync(promQuery);
            var availability = result.Values.FirstOrDefault();
            
            return Ok(new { 
                availabilityPercentage = availability > 0 ? availability : 100.0,
                status = availability >= 99.9 ? "Gold" : availability >= 99.0 ? "Silver" : "Critical",
                lastChecked = DateTime.UtcNow
            });
        }
    }
}
