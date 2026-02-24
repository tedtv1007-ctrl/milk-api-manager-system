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
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Uri { get; set; }
        public required string RiskLevel { get; set; } // L1, L2, L3
        public required string Owner { get; set; }
        public List<string> WhitelistIps { get; set; } = new();
    }

    public class BlacklistRequest
    {
        [JsonPropertyName("ip")]
        public required string Ip { get; set; }

        [JsonPropertyName("action")]
        public required string Action { get; set; }

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
        public required string Username { get; set; }

        [JsonPropertyName("desc")]
        public required string Desc { get; set; }

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
        public required string Status { get; set; }

        [JsonPropertyName("lastSyncTime")]
        public DateTime? LastSyncTime { get; set; }
    }

    public class ConsumerStats
    {
        public required string Username { get; set; }
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
        public required string Label { get; set; }
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
            public required string IpOrCidr { get; set; }

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
            public required string Ip { get; set; }

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

        // --- PII Masking Management ---
        public class PiiMaskingRuleDto
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("routeId")]
            public string RouteId { get; set; } = string.Empty;

            [JsonPropertyName("fieldPath")]
            public string FieldPath { get; set; } = string.Empty;

            [JsonPropertyName("regexPattern")]
            public string RegexPattern { get; set; } = ".*";

            [JsonPropertyName("replacePattern")]
            public string ReplacePattern { get; set; } = "***";

            [JsonPropertyName("isActive")]
            public bool IsActive { get; set; } = true;

            [JsonPropertyName("updatedAt")]
            public DateTime UpdatedAt { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;
        }

        public async Task<List<PiiMaskingRuleDto>> GetPiiRulesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<PiiMaskingRuleDto>>("api/PiiMasking");
                return response ?? new List<PiiMaskingRuleDto>();
            }
            catch { return new List<PiiMaskingRuleDto>(); }
        }

        public async Task SavePiiRuleAsync(PiiMaskingRuleDto rule)
        {
            if (rule.Id == 0)
            {
                await _httpClient.PostAsJsonAsync("api/PiiMasking", rule);
            }
            else
            {
                await _httpClient.PutAsJsonAsync($"api/PiiMasking/{rule.Id}", rule);
            }
        }

        public async Task DeletePiiRuleAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/PiiMasking/{id}");
        }

        // --- Consumer Groups (Traffic Tiers) ---
        public class ConsumerGroupDto
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            [JsonPropertyName("desc")]
            public string? Desc { get; set; }

            // Simplified quota view for UI
            public int RateLimit { get; set; } = 1000;
        }

        public async Task<List<ConsumerGroupDto>> GetConsumerGroupsAsync()
        {
            try
            {
                // In a real implementation, we would map the complex APISIX plugin config to a simple DTO
                var groups = await _httpClient.GetFromJsonAsync<List<JsonElement>>("api/ConsumerGroup");
                var result = new List<ConsumerGroupDto>();
                
                foreach (var g in groups ?? new List<JsonElement>())
                {
                    var id = g.GetProperty("id").GetString() ?? "";
                    var rate = 0;
                    
                    if (g.TryGetProperty("plugins", out var plugins) && 
                        plugins.TryGetProperty("limit-count", out var limit))
                    {
                        rate = limit.GetProperty("count").GetInt32();
                    }

                    result.Add(new ConsumerGroupDto { Id = id, RateLimit = rate });
                }
                return result;
            }
            catch { return new List<ConsumerGroupDto>(); }
        }

        public async Task SaveConsumerGroupAsync(ConsumerGroupDto group)
        {
            // Construct APISIX payload
            var payload = new
            {
                id = group.Id,
                plugins = new Dictionary<string, object>
                {
                    ["limit-count"] = new
                    {
                        count = group.RateLimit,
                        time_window = 60, // 1 minute default
                        rejected_code = 429,
                        key = "remote_addr"
                    }
                }
            };
            await _httpClient.PutAsJsonAsync($"api/ConsumerGroup/{group.Id}", payload);
        }

        public async Task DeleteConsumerGroupAsync(string id)
        {
            await _httpClient.DeleteAsync($"api/ConsumerGroup/{id}");
        }

        // --- Mocking & Load Testing ---
        public class MockRuleDto
        {
            public int Id { get; set; }
            public string RouteId { get; set; } = "";
            public int ResponseStatusCode { get; set; } = 200;
            public string ResponseBody { get; set; } = "{}";
            public string ContentType { get; set; } = "application/json";
            public bool IsEnabled { get; set; } = true;
        }

        public async Task<List<MockRuleDto>> GetMockRulesAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<List<MockRuleDto>>("api/Mock") ?? new(); }
            catch { return new(); }
        }

        public async Task SaveMockRuleAsync(MockRuleDto rule)
        {
            if (rule.Id == 0) await _httpClient.PostAsJsonAsync("api/Mock", rule);
            else await _httpClient.PutAsJsonAsync($"api/Mock/{rule.Id}", rule);
        }

        public async Task DeleteMockRuleAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/Mock/{id}");
        }

        public async Task<string> RunLoadTestAsync(string url, int vus, int duration)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/LoadTest/run?url={Uri.EscapeDataString(url)}&vus={vus}&duration={duration}");
            request.Headers.Add("X-API-KEY", "milk-admin-secret-key-change-me");
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorSummary = await response.Content.ReadAsStringAsync();
                return $"Error: Server returned {(int)response.StatusCode} {response.ReasonPhrase}\n{errorSummary}";
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("report").GetString() ?? "No report generated.";
            }
            catch (Exception ex)
            {
                return $"Failed to parse response: {ex.Message}";
            }
        }

        // --- Developer Portal / Access Requests ---
        public class AccessRequestDto
        {
            public int Id { get; set; }
            public string ProjectName { get; set; } = "";
            public string ApplicantEmail { get; set; } = "";
            public string RequestedTier { get; set; } = "Free";
            public string Purpose { get; set; } = "";
            public string Status { get; set; } = "Pending";
            public string? AdminComment { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task<List<AccessRequestDto>> GetAccessRequestsAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<List<AccessRequestDto>>("api/AccessRequest") ?? new(); }
            catch { return new(); }
        }

        public async Task SubmitAccessRequestAsync(AccessRequestDto request)
        {
            await _httpClient.PostAsJsonAsync("api/AccessRequest/submit", request);
        }

        public async Task ApproveRequestAsync(int id, string comment)
        {
            await _httpClient.PostAsync($"api/AccessRequest/{id}/approve?comment={Uri.EscapeDataString(comment)}", null);
        }

        public async Task RejectRequestAsync(int id, string reason)
        {
            await _httpClient.PostAsync($"api/AccessRequest/{id}/reject?reason={Uri.EscapeDataString(reason)}", null);
        }

        // --- API Catalog ---
        public class ApiServiceDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string BasePath { get; set; } = "";
            public string OpenApiUrl { get; set; } = "";
            public string OwnerTeam { get; set; } = "";
        }

        public async Task<List<ApiServiceDto>> GetApiCatalogAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<List<ApiServiceDto>>("api/ApiCatalog") ?? new(); }
            catch { return new(); }
        }

        // --- API Testing ---
        public class TestScenarioDto
        {
            public int Id { get; set; }
            public int ServiceId { get; set; }
            public string Name { get; set; } = "";
            public string Endpoint { get; set; } = "/";
            public string HttpMethod { get; set; } = "GET";
            public int ExpectedStatusCode { get; set; } = 200;
            public string? LastResult { get; set; }
            public DateTime? LastRunAt { get; set; }
        }

        public async Task<List<TestScenarioDto>> GetTestScenariosAsync(int serviceId)
        {
            try { return await _httpClient.GetFromJsonAsync<List<TestScenarioDto>>($"api/TestExecution/scenarios/{serviceId}") ?? new(); }
            catch { return new(); }
        }

        public async Task RunApiTestAsync(int scenarioId)
        {
            await _httpClient.PostAsync($"api/TestExecution/run/{scenarioId}", null);
        }

        public async Task<List<AnalyticsResult>> GetTopSlowRoutesAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>("api/Analytics/top-slow-routes") ?? new(); }
            catch { return new(); }
        }

        public class SlaDto
        {
            public double AvailabilityPercentage { get; set; }
            public string Status { get; set; } = "Unknown";
        }

        public async Task<SlaDto?> GetSlaStatsAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<SlaDto>("api/Analytics/sla"); }
            catch { return new SlaDto { AvailabilityPercentage = 100, Status = "Offline" }; }
        }
    }
}
