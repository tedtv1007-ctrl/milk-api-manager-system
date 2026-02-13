using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MilkAdminBlazor.Data
{
    public class ApiRoute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string RiskLevel { get; set; } // L1, L2, L3
        public string Owner { get; set; }
        public List<string> WhitelistIps { get; set; } = new();
    }

    public class BlacklistRequest
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("addedBy")]
        public string? AddedBy { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    public class ApiConsumer
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new List<string>();

        [JsonPropertyName("quota")]
        public ApiQuota Quota { get; set; } = new ApiQuota();
    }

    public class ApiQuota
    {
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1000;

        [JsonPropertyName("time_window")]
        public int TimeWindow { get; set; } = 3600;

        [JsonPropertyName("rejected_code")]
        public int RejectedCode { get; set; } = 429;

        [JsonPropertyName("rejected_msg")]
        public string RejectedMsg { get; set; } = "API quota exceeded. Please contact support.";
    }

    public class SyncStatusResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("lastSyncTime")]
        public DateTime? LastSyncTime { get; set; }
    }

    public class ConsumerStats
    {
        public string Username { get; set; }
        public long RequestCount { get; set; }
        public double ErrorRate { get; set; } // Percentage
        public DateTime Timestamp { get; set; }
    }

    public class MetricPoint
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    public class AnalyticsResult
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }
        [JsonPropertyName("data")]
        public List<MetricPoint> Data { get; set; } = new();
    }

    public class ApisixService
    {
        private readonly HttpClient _httpClient;

        public ApisixService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SyncStatusResponse?> GetSyncStatusAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SyncStatusResponse>("api/SyncStatus");
            }
            catch
            {
                return new SyncStatusResponse { Status = "Offline" };
            }
        }

        public async Task<List<ApiRoute>> GetRoutesAsync()
        {
            // For now, still mock or fetch from backend if ready
            return new List<ApiRoute>
            {
                new ApiRoute { Id = "1", Name = "User Profile", Uri = "/api/user/*", RiskLevel = "L3", Owner = "Customer Team" },
                new ApiRoute { Id = "2", Name = "Product List", Uri = "/api/products", RiskLevel = "L1", Owner = "Sales Team" },
                new ApiRoute { Id = "3", Name = "Payment Gateway", Uri = "/api/payment", RiskLevel = "L3", Owner = "Finance Team" },
                new ApiRoute { Id = "4", Name = "Branch Locations", Uri = "/api/locations", RiskLevel = "L1", Owner = "Ops Team" }
            };
        }

        public class BlacklistEntryDto
        {
            [JsonPropertyName("ipOrCidr")]
            public string IpOrCidr { get; set; }

            [JsonPropertyName("reason")]
            public string? Reason { get; set; }

            [JsonPropertyName("addedBy")]
            public string? AddedBy { get; set; }

            [JsonPropertyName("addedAt")]
            public DateTime? AddedAt { get; set; }

            [JsonPropertyName("expiresAt")]
            public DateTime? ExpiresAt { get; set; }
        }

        public async Task<List<BlacklistEntryDto>> GetBlacklistedIpsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<BlacklistEntryDto>>("api/Blacklist");
                return response ?? new List<BlacklistEntryDto>();
            }
            catch
            {
                return new List<BlacklistEntryDto>();
            }
        }

        public async Task AddIpToBlacklistAsync(string ip, string? reason = null, string? addedBy = null, DateTime? expiresAt = null)
        {
            var request = new BlacklistRequest { Ip = ip, Action = "add", Reason = reason, AddedBy = addedBy, ExpiresAt = expiresAt };
            await _httpClient.PostAsJsonAsync("api/Blacklist", request);
        }

        public async Task RemoveIpFromBlacklistAsync(string ip)
        {
            var request = new BlacklistRequest { Ip = ip, Action = "remove" };
            await _httpClient.PostAsJsonAsync("api/Blacklist", request);
        }

        public async Task<List<ApiConsumer>> GetConsumersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ApiConsumer>>("api/Consumer");
                return response ?? new List<ApiConsumer>();
            }
            catch
            {
                return new List<ApiConsumer>();
            }
        }

        public async Task UpdateConsumerAsync(ApiConsumer consumer)
        {
            await _httpClient.PostAsJsonAsync("api/Consumer", consumer);
        }

        public async Task DeleteConsumerAsync(string username)
        {
            await _httpClient.DeleteAsync($"api/Consumer/{username}");
        }

        public async Task<List<ConsumerStats>> GetConsumerStatsAsync(string username = null)
        {
            // In a real scenario, this would call Prometheus API or a backend proxy.
            // For now, returning mock data as per typical Prometheus metrics.
            
            var stats = new List<ConsumerStats>();
            var consumers = username == null ? (await GetConsumersAsync()).Select(c => c.Username).ToList() : new List<string> { username };
            
            if (!consumers.Any()) consumers = new List<string> { "global_user", "mobile_app", "partner_a" };

            var rng = new Random();
            foreach (var user in consumers)
            {
                stats.Add(new ConsumerStats
                {
                    Username = user,
                    RequestCount = rng.Next(1000, 50000),
                    ErrorRate = rng.NextDouble() * 5, // 0-5%
                    Timestamp = DateTime.Now
                });
            }
            return stats;
        }

        public async Task UpdateRouteAsync(ApiRoute route)
        {
            // Map frontend ApiRoute to the backend's expected APISIX route DTO
            var plugins = new Dictionary<string, object>();
            if (route.WhitelistIps != null && route.WhitelistIps.Any())
            {
                plugins["ip-restriction"] = new { whitelist = route.WhitelistIps };
            }

            var body = new
            {
                id = route.Id,
                name = route.Name,
                uris = new List<string> { route.Uri },
                service_id = (string?)null,
                plugins = plugins
            };

            await _httpClient.PutAsJsonAsync($"api/Route/{route.Id}", body);
        }

        public async Task<List<AnalyticsResult>> GetAnalyticsRequestsAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/requests{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch { return new List<AnalyticsResult>(); }
        }

        public async Task<List<AnalyticsResult>> GetAnalyticsLatencyAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/latency{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch { return new List<AnalyticsResult>(); }
        }

        public async Task<List<AnalyticsResult>> GetAnalyticsErrorsAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/errors{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch { return new List<AnalyticsResult>(); }
        }

        // --- Whitelist management for specific routes ---
        public class WhitelistEntryDto
        {
            [JsonPropertyName("ip")]
            public string Ip { get; set; }

            [JsonPropertyName("reason")]
            public string? Reason { get; set; }

            [JsonPropertyName("addedBy")]
            public string? AddedBy { get; set; }

            [JsonPropertyName("addedAt")]
            public DateTime? AddedAt { get; set; }

            [JsonPropertyName("expiresAt")]
            public DateTime? ExpiresAt { get; set; }
        }

        public async Task<List<WhitelistEntryDto>> GetRouteWhitelistAsync(string routeId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<WhitelistEntryDto>>($"api/whitelist/route/{routeId}");
                return response ?? new List<WhitelistEntryDto>();
            }
            catch
            {
                return new List<WhitelistEntryDto>();
            }
        }

        public async Task AddRouteWhitelistEntryAsync(string routeId, string ip, string? reason = null, string? addedBy = null, DateTime? expiresAt = null)
        {
            var payload = new {
                ip = ip,
                reason = reason,
                addedBy = addedBy,
                expiresAt = expiresAt
            };

            await _httpClient.PostAsJsonAsync($"api/whitelist/route/{routeId}", payload);
        }

        public async Task RemoveRouteWhitelistEntryAsync(string routeId, string ip)
        {
            await _httpClient.DeleteAsync($"api/whitelist/route/{routeId}/{Uri.EscapeDataString(ip)}");
        }
    }
}
