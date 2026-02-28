using MilkApiManager.Models.Apisix;
using MilkApiManager.Options;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MilkApiManager.Services
{
    public class ApisixClient : IApisixClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApisixClient> _logger;
        private readonly string _adminKey;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ApisixClient(HttpClient httpClient, ILogger<ApisixClient> logger, IOptions<ApisixOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(options.Value.AdminUrl);
            _adminKey = options.Value.AdminKey;
            
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-API-KEY", _adminKey);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, _jsonSerializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return request;
        }

        public virtual async Task CreateRouteAsync(string id, ApisixRoute routeConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"routes/{id}", routeConfig);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create route {RouteId}. APISIX returned {StatusCode}: {ErrorResponse}", id, response.StatusCode, errorContent);
            }
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully created route {RouteId}", id);
        }

        public virtual async Task DeleteRouteAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"routes/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete route {RouteId}: {StatusCode}", id, response.StatusCode);
            }
        }
        
        public virtual async Task<string> GetRoutesAsync()
        {
             var request = CreateRequest(HttpMethod.Get, "routes");
             var response = await _httpClient.SendAsync(request);
             return await response.Content.ReadAsStringAsync();
        }

        private string ExtractValueFromJson(string json)
        {
            var root = JsonSerializer.Deserialize<JsonElement>(json);
            if (root.TryGetProperty("value", out var v)) return v.GetRawText();
            if (root.TryGetProperty("node", out var n) && n.TryGetProperty("value", out var nv)) return nv.GetRawText();
            return root.GetRawText();
        }

        public virtual async Task<ApisixRoute?> GetRouteAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Get, $"routes/{id}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var valueStr = ExtractValueFromJson(json);
            return JsonSerializer.Deserialize<ApisixRoute>(valueStr, _jsonSerializerOptions);
        }

        public virtual async Task UpdateRouteAsync(string id, ApisixRoute routeConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"routes/{id}", routeConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated route {RouteId}", id);
        }

        public virtual async Task CreateServiceAsync(string id, Service serviceConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"services/{id}", serviceConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully created service {ServiceId}", id);
        }

        public virtual async Task<Service?> GetServiceAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Get, $"services/{id}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var valueStr = ExtractValueFromJson(json);
            return JsonSerializer.Deserialize<Service>(valueStr, _jsonSerializerOptions);
        }

        public virtual async Task<string> GetServicesAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "services");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public virtual async Task UpdateServiceAsync(string id, Service serviceConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"services/{id}", serviceConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated service {ServiceId}", id);
        }

        public virtual async Task DeleteServiceAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"services/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete service {ServiceId}: {StatusCode}", id, response.StatusCode);
            }
        }

        public virtual async Task CreateConsumerAsync(string username, Consumer consumerConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"consumers/{username}", consumerConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully created consumer {Username}", username);
        }

        public virtual async Task<Consumer?> GetConsumerAsync(string username)
        {
            var request = CreateRequest(HttpMethod.Get, $"consumers/{username}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var valueStr = ExtractValueFromJson(json);
            return JsonSerializer.Deserialize<Consumer>(valueStr, _jsonSerializerOptions);
        }

        public virtual async Task<string> GetConsumersAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "consumers");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public virtual async Task UpdateConsumerAsync(string username, object consumerConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"consumers/{username}", consumerConfig);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update consumer {Username}. APISIX returned {StatusCode}: {ErrorResponse}", username, response.StatusCode, errorContent);
            }
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated consumer {Username}", username);
        }

        public virtual async Task DeleteConsumerAsync(string username)
        {
            var request = CreateRequest(HttpMethod.Delete, $"consumers/{username}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete consumer {Username}: {StatusCode}", username, response.StatusCode);
            }
        }

        public virtual async Task<List<Route>> GetRoutesTypedAsync()
        {
            var json = await GetRoutesAsync();
            return ParseApisixList<Route>(json);
        }

        public virtual async Task<List<Service>> GetServicesTypedAsync()
        {
            var json = await GetServicesAsync();
            return ParseApisixList<Service>(json);
        }

        public virtual async Task<List<Consumer>> GetConsumersTypedAsync()
        {
            var json = await GetConsumersAsync();
            return ParseApisixList<Consumer>(json);
        }

        public virtual async Task<List<ConsumerGroup>> GetConsumerGroupsTypedAsync()
        {
            var json = await GetConsumerGroupsAsync();
            return ParseApisixList<ConsumerGroup>(json);
        }

        private List<T> ParseApisixList<T>(string json)
        {
            var doc = JsonDocument.Parse(json);
            var items = new List<T>();

            // APISIX v3 format: { "list": [ { "value": {...} } ] }
            if (doc.RootElement.TryGetProperty("list", out var list))
            {
                foreach (var item in list.EnumerateArray())
                {
                    if (item.TryGetProperty("value", out var val))
                    {
                        var parsed = JsonSerializer.Deserialize<T>(val.GetRawText(), _jsonSerializerOptions);
                        if (parsed != null) items.Add(parsed);
                    }
                }
            }
            // APISIX v2 format: { "node": { "nodes": [ { "value": {...} } ] } }
            else if (doc.RootElement.TryGetProperty("node", out var node) && node.TryGetProperty("nodes", out var nodes))
            {
                foreach (var item in nodes.EnumerateArray())
                {
                    if (item.TryGetProperty("value", out var val))
                    {
                        var parsed = JsonSerializer.Deserialize<T>(val.GetRawText(), _jsonSerializerOptions);
                        if (parsed != null) items.Add(parsed);
                    }
                }
            }

            return items;
        }

        public virtual async Task<List<string>> GetBlacklistAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "plugin_metadata/traffic-blocker");
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<string>();
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("value", out var value) && 
                value.TryGetProperty("blacklist", out var blacklist))
            {
                return JsonSerializer.Deserialize<List<string>>(blacklist.GetRawText()) ?? new List<string>();
            }

            return new List<string>();
        }

        public virtual async Task UpdateBlacklistAsync(List<string> blacklist)
        {
            var body = new { blacklist = blacklist };
            var request = CreateRequest(HttpMethod.Put, "plugin_metadata/traffic-blocker", body);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated traffic-blocker blacklist");
        }

        public virtual async Task UpdateGlobalPlugin(string pluginName, object body)
        {
            var request = CreateRequest(HttpMethod.Put, $"plugin_metadata/{pluginName}", body);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated plugin metadata: {PluginName}", pluginName);
        }

        // Whitelist helpers: store per-route whitelist under plugin_metadata/route-whitelist
        public virtual async Task<List<string>> GetWhitelistForRouteAsync(string routeId)
        {
            var request = CreateRequest(HttpMethod.Get, "plugin_metadata/route-whitelist");
            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<string>();
            }
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("value", out var value) &&
                value.TryGetProperty(routeId, out var arr))
            {
                return JsonSerializer.Deserialize<List<string>>(arr.GetRawText()) ?? new List<string>();
            }
            return new List<string>();
        }

        public virtual async Task UpdateWhitelistForRouteAsync(string routeId, List<string> whitelist)
        {
            // fetch existing metadata
            var bodyDoc = new Dictionary<string, object>();
            bodyDoc[routeId] = whitelist;
            var request = CreateRequest(HttpMethod.Put, "plugin_metadata/route-whitelist", bodyDoc);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated route whitelist for {RouteId}", routeId);
        }

        public virtual async Task CreateConsumerGroupAsync(string id, ConsumerGroup groupConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"consumer_groups/{id}", groupConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully created consumer group {GroupId}", id);
        }

        public virtual async Task<string> GetConsumerGroupsAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "consumer_groups");
            var response = await _httpClient.SendAsync(request);
            // APISIX might return 404 if no groups exist or feature disabled, handle gracefully?
            // Usually returns empty list.
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public virtual async Task DeleteConsumerGroupAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"consumer_groups/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to delete consumer group {GroupId}: {StatusCode}", id, response.StatusCode);
            }
        }
    }
}
