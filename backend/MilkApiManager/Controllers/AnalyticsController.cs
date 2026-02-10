using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

            var result = await _prometheus.GetMetricAsync(promQuery, start, end, query.Step);
            return Ok(result);
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

            var result = await _prometheus.GetMetricAsync(promQuery, start, end, query.Step);
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

            var result = await _prometheus.GetMetricAsync(promQuery, start, end, query.Step);
            return Ok(result);
        }
    }
}
