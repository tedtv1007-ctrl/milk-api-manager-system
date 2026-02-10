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
    }

    public class BlacklistRequest
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }
        
        [JsonPropertyName("action")]
        public string Action { get; set; }
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

        public async Task<List<string>> GetBlacklistedIpsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<string>>("api/Blacklist");
                return response ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task AddIpToBlacklistAsync(string ip)
        {
            var request = new BlacklistRequest { Ip = ip, Action = "add" };
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
    }
}
