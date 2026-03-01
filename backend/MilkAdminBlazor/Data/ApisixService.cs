using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MilkAdminBlazor.Data
{
    /// <summary>
    /// Blazor frontend service that proxies requests to the MilkApiManager backend.
    /// Refactored: DTOs extracted to ApisixServiceModels.cs (A-2),
    /// ILogger injected (E-1), response checking added (E-2),
    /// hardcoded mock data removed (A-3, A-4).
    /// </summary>
    public class ApisixService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApisixService> _logger;

        public ApisixService(HttpClient httpClient, ILogger<ApisixService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // ================================================================
        // Sync Status
        // ================================================================

        public async Task<SyncStatusResponse?> GetSyncStatusAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SyncStatusResponse>("api/SyncStatus");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch sync status");
                return new SyncStatusResponse { Status = "Offline" };
            }
        }

        // ================================================================
        // Routes (frontend risk-classification view) — A-3: real backend call
        // ================================================================

        public async Task<List<ApiRoute>> GetRoutesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ApiRoute>>("api/Route/classified");
                return response ?? new List<ApiRoute>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch classified routes, returning empty list");
                return new List<ApiRoute>();
            }
        }

        // ================================================================
        // Blacklist
        // ================================================================

        public async Task<List<BlacklistEntryDto>> GetBlacklistedIpsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<BlacklistEntryDto>>("api/Blacklist");
                return response ?? new List<BlacklistEntryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch blacklisted IPs");
                return new List<BlacklistEntryDto>();
            }
        }

        public async Task AddIpToBlacklistAsync(string ip, string? reason = null, string? addedBy = null, DateTime? expiresAt = null)
        {
            var request = new BlacklistRequest { Ip = ip, Action = "add", Reason = reason, AddedBy = addedBy, ExpiresAt = expiresAt };
            var response = await _httpClient.PostAsJsonAsync("api/Blacklist", request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add IP {Ip} to blacklist: {StatusCode} {Body}", ip, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveIpFromBlacklistAsync(string ip)
        {
            var request = new BlacklistRequest { Ip = ip, Action = "remove" };
            var response = await _httpClient.PostAsJsonAsync("api/Blacklist", request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to remove IP {Ip} from blacklist: {StatusCode} {Body}", ip, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Consumers
        // ================================================================

        public async Task<List<ApiConsumer>> GetConsumersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ApiConsumer>>("api/Consumer");
                return response ?? new List<ApiConsumer>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch consumers");
                return new List<ApiConsumer>();
            }
        }

        public async Task UpdateConsumerAsync(ApiConsumer consumer)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Consumer", consumer);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update consumer {Username}: {StatusCode} {Body}", consumer.Username, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteConsumerAsync(string username)
        {
            var response = await _httpClient.DeleteAsync($"api/Consumer/{username}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete consumer {Username}: {StatusCode} {Body}", username, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Consumer Stats — A-4: real backend call instead of Random()
        // ================================================================

        public async Task<List<ConsumerStats>> GetConsumerStatsAsync(string? username = null)
        {
            try
            {
                var query = string.IsNullOrEmpty(username) ? "" : $"?username={Uri.EscapeDataString(username)}";
                var response = await _httpClient.GetFromJsonAsync<List<ConsumerStats>>($"api/Analytics/consumer-stats{query}");
                return response ?? new List<ConsumerStats>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch consumer stats for {Username}, returning empty", username ?? "all");
                return new List<ConsumerStats>();
            }
        }

        // ================================================================
        // Route Management
        // ================================================================

        public async Task UpdateRouteAsync(ApiRoute route)
        {
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

            var response = await _httpClient.PutAsJsonAsync($"api/Route/{route.Id}", body);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update route {RouteId}: {StatusCode} {Body}", route.Id, response.StatusCode, responseBody);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Analytics
        // ================================================================

        public async Task<List<AnalyticsResult>> GetAnalyticsRequestsAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/requests{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch analytics requests for consumer={Consumer}, route={Route}", consumer, route);
                return new List<AnalyticsResult>();
            }
        }

        public async Task<List<AnalyticsResult>> GetAnalyticsLatencyAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/latency{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch analytics latency for consumer={Consumer}, route={Route}", consumer, route);
                return new List<AnalyticsResult>();
            }
        }

        public async Task<List<AnalyticsResult>> GetAnalyticsErrorsAsync(string consumer, string route, DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"?consumer={consumer}&route={route}&startTime={start:O}&endTime={end:O}";
                var response = await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>($"api/Analytics/errors{query}");
                return response ?? new List<AnalyticsResult>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch analytics errors for consumer={Consumer}, route={Route}", consumer, route);
                return new List<AnalyticsResult>();
            }
        }

        public async Task<List<AnalyticsResult>> GetTopSlowRoutesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<AnalyticsResult>>("api/Analytics/top-slow-routes") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch top slow routes");
                return new();
            }
        }

        public async Task<SlaDto?> GetSlaStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SlaDto>("api/Analytics/sla");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch SLA stats");
                return new SlaDto { AvailabilityPercentage = 100, Status = "Offline" };
            }
        }

        // ================================================================
        // Whitelist
        // ================================================================

        public async Task<List<WhitelistEntryDto>> GetRouteWhitelistAsync(string routeId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<WhitelistEntryDto>>($"api/whitelist/route/{routeId}");
                return response ?? new List<WhitelistEntryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch whitelist for route {RouteId}", routeId);
                return new List<WhitelistEntryDto>();
            }
        }

        public async Task AddRouteWhitelistEntryAsync(string routeId, string ip, string? reason = null, string? addedBy = null, DateTime? expiresAt = null)
        {
            var payload = new { ip, reason, addedBy, expiresAt };
            var response = await _httpClient.PostAsJsonAsync($"api/whitelist/route/{routeId}", payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add whitelist entry for route {RouteId}, IP {Ip}: {StatusCode} {Body}", routeId, ip, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveRouteWhitelistEntryAsync(string routeId, string ip)
        {
            var response = await _httpClient.DeleteAsync($"api/whitelist/route/{routeId}/{Uri.EscapeDataString(ip)}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to remove whitelist entry for route {RouteId}, IP {Ip}: {StatusCode} {Body}", routeId, ip, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // PII Masking
        // ================================================================

        public async Task<List<PiiMaskingRuleDto>> GetPiiRulesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<PiiMaskingRuleDto>>("api/PiiMasking");
                return response ?? new List<PiiMaskingRuleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch PII masking rules");
                return new List<PiiMaskingRuleDto>();
            }
        }

        public async Task SavePiiRuleAsync(PiiMaskingRuleDto rule)
        {
            HttpResponseMessage response;
            if (rule.Id == 0)
            {
                response = await _httpClient.PostAsJsonAsync("api/PiiMasking", rule);
            }
            else
            {
                response = await _httpClient.PutAsJsonAsync($"api/PiiMasking/{rule.Id}", rule);
            }
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save PII rule {RuleId}: {StatusCode} {Body}", rule.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeletePiiRuleAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/PiiMasking/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete PII rule {RuleId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Consumer Groups (Traffic Tiers)
        // ================================================================

        public async Task<List<ConsumerGroupDto>> GetConsumerGroupsAsync()
        {
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch consumer groups");
                return new List<ConsumerGroupDto>();
            }
        }

        public async Task SaveConsumerGroupAsync(ConsumerGroupDto group)
        {
            var payload = new
            {
                id = group.Id,
                plugins = new Dictionary<string, object>
                {
                    ["limit-count"] = new
                    {
                        count = group.RateLimit,
                        time_window = 60,
                        rejected_code = 429,
                        key = "remote_addr"
                    }
                }
            };
            var response = await _httpClient.PutAsJsonAsync($"api/ConsumerGroup/{group.Id}", payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save consumer group {GroupId}: {StatusCode} {Body}", group.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteConsumerGroupAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/ConsumerGroup/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete consumer group {GroupId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Mocking & Load Testing
        // ================================================================

        public async Task<List<MockRuleDto>> GetMockRulesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MockRuleDto>>("api/Mock") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch mock rules");
                return new();
            }
        }

        public async Task SaveMockRuleAsync(MockRuleDto rule)
        {
            HttpResponseMessage response;
            if (rule.Id == 0)
                response = await _httpClient.PostAsJsonAsync("api/Mock", rule);
            else
                response = await _httpClient.PutAsJsonAsync($"api/Mock/{rule.Id}", rule);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save mock rule {RuleId}: {StatusCode} {Body}", rule.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteMockRuleAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/Mock/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete mock rule {RuleId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> RunLoadTestAsync(string url, int vus, int duration)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/LoadTest/run?url={Uri.EscapeDataString(url)}&vus={vus}&duration={duration}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorSummary = await response.Content.ReadAsStringAsync();
                _logger.LogError("Load test failed: {StatusCode} {Body}", response.StatusCode, errorSummary);
                return $"Error: Server returned {(int)response.StatusCode} {response.ReasonPhrase}\n{errorSummary}";
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("report").GetString() ?? "No report generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse load test response");
                return $"Failed to parse response: {ex.Message}";
            }
        }

        // ================================================================
        // Developer Portal / Access Requests
        // ================================================================

        public async Task<List<AccessRequestDto>> GetAccessRequestsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<AccessRequestDto>>("api/AccessRequest") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch access requests");
                return new();
            }
        }

        public async Task SubmitAccessRequestAsync(AccessRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/AccessRequest/submit", request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to submit access request: {StatusCode} {Body}", response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task ApproveRequestAsync(int id, string comment)
        {
            var response = await _httpClient.PostAsync($"api/AccessRequest/{id}/approve?comment={Uri.EscapeDataString(comment)}", null);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to approve access request {Id}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task RejectRequestAsync(int id, string reason)
        {
            var response = await _httpClient.PostAsync($"api/AccessRequest/{id}/reject?reason={Uri.EscapeDataString(reason)}", null);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to reject access request {Id}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // API Catalog
        // ================================================================

        public async Task<List<ApiServiceDto>> GetApiCatalogAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiServiceDto>>("api/ApiCatalog") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch API catalog");
                return new();
            }
        }

        // ================================================================
        // API Testing
        // ================================================================

        public async Task<List<TestScenarioDto>> GetTestScenariosAsync(int serviceId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TestScenarioDto>>($"api/TestExecution/scenarios/{serviceId}") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch test scenarios for service {ServiceId}", serviceId);
                return new();
            }
        }

        public async Task RunApiTestAsync(int scenarioId)
        {
            var response = await _httpClient.PostAsync($"api/TestExecution/run/{scenarioId}", null);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to run API test scenario {ScenarioId}: {StatusCode} {Body}", scenarioId, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // ================================================================
        // Audit Logs
        // ================================================================

        public async Task<List<AuditLogEntryDto>> GetAuditLogsAsync(int limit = 100)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<AuditLogEntryDto>>($"api/AuditLogs?limit={limit}") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch audit logs");
                return new();
            }
        }

        public async Task<Dictionary<string, int>> GetAuditStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Dictionary<string, int>>("api/AuditLogs/stats") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch audit stats");
                return new();
            }
        }

        // ================================================================
        // APISIX Gateway Management — Full Dashboard Replacement
        // ================================================================

        // --- Routes ---

        public async Task<List<ApisixRouteDto>> GetApisixRoutesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApisixRouteDto>>("api/Route") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch APISIX routes");
                return new();
            }
        }

        public async Task<ApisixRouteDto?> GetApisixRouteAsync(string id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ApisixRouteDto>($"api/Route/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch APISIX route {RouteId}", id);
                return null;
            }
        }

        public async Task SaveApisixRouteAsync(ApisixRouteDto route)
        {
            HttpResponseMessage response;
            if (string.IsNullOrEmpty(route.Id))
            {
                route.Id = Guid.NewGuid().ToString("N")[..12];
                response = await _httpClient.PostAsJsonAsync("api/Route", route);
            }
            else
            {
                response = await _httpClient.PutAsJsonAsync($"api/Route/{route.Id}", route);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save APISIX route {RouteId}: {StatusCode} {Body}", route.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteApisixRouteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/Route/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete APISIX route {RouteId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // --- Upstreams ---

        public async Task<List<ApisixStandaloneUpstreamDto>> GetUpstreamsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApisixStandaloneUpstreamDto>>("api/Upstream") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch upstreams");
                return new();
            }
        }

        public async Task SaveUpstreamAsync(ApisixStandaloneUpstreamDto upstream)
        {
            if (string.IsNullOrEmpty(upstream.Id))
                upstream.Id = Guid.NewGuid().ToString("N")[..12];
            var response = await _httpClient.PutAsJsonAsync($"api/Upstream/{upstream.Id}", upstream);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save upstream {UpstreamId}: {StatusCode} {Body}", upstream.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteUpstreamAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/Upstream/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete upstream {UpstreamId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // --- Services ---

        public async Task<List<ApisixServiceDto>> GetApisixServicesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApisixServiceDto>>("api/Service") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch APISIX services");
                return new();
            }
        }

        public async Task SaveApisixServiceAsync(ApisixServiceDto service)
        {
            if (string.IsNullOrEmpty(service.Id))
                service.Id = Guid.NewGuid().ToString("N")[..12];
            var response = await _httpClient.PutAsJsonAsync($"api/Service/{service.Id}", service);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save APISIX service {ServiceId}: {StatusCode} {Body}", service.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteApisixServiceAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/Service/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete APISIX service {ServiceId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // --- SSL Certificates ---

        public async Task<List<SslCertificateDto>> GetSslCertificatesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SslCertificateDto>>("api/SSL") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch SSL certificates");
                return new();
            }
        }

        public async Task SaveSslCertificateAsync(SslCertificateDto ssl)
        {
            if (string.IsNullOrEmpty(ssl.Id))
                ssl.Id = Guid.NewGuid().ToString("N")[..12];
            var response = await _httpClient.PutAsJsonAsync($"api/SSL/{ssl.Id}", ssl);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save SSL certificate {SslId}: {StatusCode} {Body}", ssl.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteSslCertificateAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/SSL/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete SSL certificate {SslId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // --- Global Rules ---

        public async Task<List<GlobalRuleDto>> GetGlobalRulesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<GlobalRuleDto>>("api/GlobalRule") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch global rules");
                return new();
            }
        }

        public async Task SaveGlobalRuleAsync(GlobalRuleDto rule)
        {
            if (string.IsNullOrEmpty(rule.Id))
                rule.Id = Guid.NewGuid().ToString("N")[..2];
            var response = await _httpClient.PutAsJsonAsync($"api/GlobalRule/{rule.Id}", rule);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to save global rule {RuleId}: {StatusCode} {Body}", rule.Id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteGlobalRuleAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/GlobalRule/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete global rule {RuleId}: {StatusCode} {Body}", id, response.StatusCode, body);
            }
            response.EnsureSuccessStatusCode();
        }

        // --- Dashboard Stats ---

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DashboardStatsDto>("api/ServerInfo/dashboard") ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch dashboard stats");
                return new();
            }
        }

        public async Task<string> GetServerInfoRawAsync()
        {
            try
            {
                return await _httpClient.GetStringAsync("api/ServerInfo");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch server info");
                return "{}";
            }
        }
    }
}
