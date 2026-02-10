using MilkApiManager.Models.Apisix;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace MilkApiManager.Services
{
    public class ApisixClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApisixClient> _logger;
        private readonly string _adminKey;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ApisixClient(HttpClient httpClient, ILogger<ApisixClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("APISIX_ADMIN_URL") ?? "http://apisix:9180/apisix/admin/");
            _adminKey = Environment.GetEnvironmentVariable("APISIX_ADMIN_KEY") ?? "edd1c9f034335f136f87ad84b625c8f1";
            
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

        public async Task CreateRouteAsync(string id, Route routeConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"routes/{id}", routeConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully created route {id}");
        }

        public async Task DeleteRouteAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"routes/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to delete route {id}: {response.StatusCode}");
            }
        }
        
        public async Task<string> GetRoutesAsync()
        {
             var request = CreateRequest(HttpMethod.Get, "routes");
             var response = await _httpClient.SendAsync(request);
             return await response.Content.ReadAsStringAsync();
        }

        public async Task<Route?> GetRouteAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Get, $"routes/{id}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            // APISIX returns a "node" wrapper
            var node = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("node").GetProperty("value").GetRawText();
            return JsonSerializer.Deserialize<Route>(node, _jsonSerializerOptions);
        }

        public async Task UpdateRouteAsync(string id, Route routeConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"routes/{id}", routeConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully updated route {id}");
        }

        public async Task CreateServiceAsync(string id, Service serviceConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"services/{id}", serviceConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully created service {id}");
        }

        public async Task<Service?> GetServiceAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Get, $"services/{id}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var node = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("node").GetProperty("value").GetRawText();
            return JsonSerializer.Deserialize<Service>(node, _jsonSerializerOptions);
        }

        public async Task<string> GetServicesAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "services");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task UpdateServiceAsync(string id, Service serviceConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"services/{id}", serviceConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully updated service {id}");
        }

        public async Task DeleteServiceAsync(string id)
        {
            var request = CreateRequest(HttpMethod.Delete, $"services/{id}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to delete service {id}: {response.StatusCode}");
            }
        }

        public async Task CreateConsumerAsync(string username, Consumer consumerConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"consumers/{username}", consumerConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully created consumer {username}");
        }

        public async Task<Consumer?> GetConsumerAsync(string username)
        {
            var request = CreateRequest(HttpMethod.Get, $"consumers/{username}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var node = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("node").GetProperty("value").GetRawText();
            return JsonSerializer.Deserialize<Consumer>(node, _jsonSerializerOptions);
        }

        public async Task<string> GetConsumersAsync()
        {
            var request = CreateRequest(HttpMethod.Get, "consumers");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task UpdateConsumerAsync(string username, object consumerConfig)
        {
            var request = CreateRequest(HttpMethod.Put, $"consumers/{username}", consumerConfig);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Successfully updated consumer {username}");
        }

        public async Task DeleteConsumerAsync(string username)
        {
            var request = CreateRequest(HttpMethod.Delete, $"consumers/{username}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to delete consumer {username}: {response.StatusCode}");
            }
        }

        public async Task<List<string>> GetBlacklistAsync()
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

        public async Task UpdateBlacklistAsync(List<string> blacklist)
        {
            var body = new { blacklist = blacklist };
            var request = CreateRequest(HttpMethod.Put, "plugin_metadata/traffic-blocker", body);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Successfully updated traffic-blocker blacklist");
        }
    }
}
