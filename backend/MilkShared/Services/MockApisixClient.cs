using MilkApiManager.Models.Apisix;
using MilkApiManager.Options;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MilkApiManager.Services
{
    public class MockApisixClient : ApisixClient
    {
        private static readonly ConcurrentDictionary<string, ApisixRoute> _routes = new();
        private static readonly ConcurrentDictionary<string, Consumer> _consumers = new();
        private static readonly ConcurrentDictionary<string, Service> _services = new();
        private static readonly ConcurrentDictionary<string, ConsumerGroup> _consumerGroups = new();
        private static readonly ConcurrentDictionary<string, StandaloneUpstream> _upstreams = new();
        private static readonly ConcurrentDictionary<string, SslCertificate> _ssls = new();
        private static readonly ConcurrentDictionary<string, GlobalRule> _globalRules = new();
        private static readonly ConcurrentDictionary<string, PluginConfig> _pluginConfigs = new();
        private static readonly ConcurrentDictionary<string, List<string>> _whitelists = new();
        private static List<string> _blacklist = new();

        private readonly ILogger<ApisixClient> _logger;

        public MockApisixClient(HttpClient httpClient, ILogger<ApisixClient> logger, IOptions<ApisixOptions> options) : base(httpClient, logger, options)
        {
            _logger = logger;
            _logger.LogInformation("MockApisixClient initialized (Static Hash: {Hash})", GetHashCode());
        }

        public override async Task CreateRouteAsync(string id, ApisixRoute routeConfig)
        {
            await Task.Yield();
            _logger.LogInformation("MockApisixClient: Creating route {Id}", id);
            _routes[id] = routeConfig;
        }

        public override async Task DeleteRouteAsync(string id)
        {
            await Task.Yield();
            _routes.TryRemove(id, out _);
        }

        public override async Task<string> GetRoutesAsync()
        {
            await Task.Yield();
            var list = _routes.Select(kv => new { value = kv.Value, key = $"/apisix/routes/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task<ApisixRoute?> GetRouteAsync(string id)
        {
            await Task.Yield();
            if (_routes.TryGetValue(id, out var route))
            {
                return route;
            }
            return null;
        }

        public override async Task UpdateRouteAsync(string id, ApisixRoute routeConfig)
        {
            await Task.Yield();
            _routes[id] = routeConfig;
        }

        public override async Task CreateServiceAsync(string id, Service serviceConfig)
        {
            await Task.Yield();
            _services[id] = serviceConfig;
        }

        public override async Task<Service?> GetServiceAsync(string id)
        {
            await Task.Yield();
            if (_services.TryGetValue(id, out var service))
            {
                return service;
            }
            return null;
        }

        public override async Task<string> GetServicesAsync()
        {
            await Task.Yield();
            var list = _services.Select(kv => new { value = kv.Value, key = $"/apisix/services/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task UpdateServiceAsync(string id, Service serviceConfig)
        {
            await Task.Yield();
            _services[id] = serviceConfig;
        }

        public override async Task DeleteServiceAsync(string id)
        {
            await Task.Yield();
            _services.TryRemove(id, out _);
        }

        public override async Task CreateConsumerAsync(string username, Consumer consumerConfig)
        {
            await Task.Yield();
            _consumers[username] = consumerConfig;
        }

        public override async Task<Consumer?> GetConsumerAsync(string username)
        {
            await Task.Yield();
            if (_consumers.TryGetValue(username, out var consumer))
            {
                return consumer;
            }
            return null;
        }

        public override async Task<string> GetConsumersAsync()
        {
            await Task.Yield();
            var list = _consumers.Select(kv => new { value = kv.Value, key = $"/apisix/consumers/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task UpdateConsumerAsync(string username, object consumerConfig)
        {
            await Task.Yield();
            if (!_consumers.ContainsKey(username))
            {
                _consumers[username] = new Consumer { Username = username };
            }
        }

        public override async Task DeleteConsumerAsync(string username)
        {
            await Task.Yield();
            _consumers.TryRemove(username, out _);
        }

        public override Task<List<string>> GetBlacklistAsync()
        {
            return Task.FromResult(_blacklist);
        }

        public override async Task UpdateBlacklistAsync(List<string> blacklist)
        {
            await Task.Yield();
            _blacklist = blacklist;
        }

        public override Task UpdateGlobalPlugin(string pluginName, object body)
        {
            return Task.CompletedTask;
        }

        // --- Whitelist (per-route) ---
        public override Task<List<string>> GetWhitelistForRouteAsync(string routeId)
        {
            if (_whitelists.TryGetValue(routeId, out var list))
            {
                return Task.FromResult(list);
            }
            return Task.FromResult(new List<string>());
        }

        public override async Task UpdateWhitelistForRouteAsync(string routeId, List<string> whitelist)
        {
            await Task.Yield();
            _whitelists[routeId] = whitelist;
        }

        // --- Consumer Groups ---
        public override async Task CreateConsumerGroupAsync(string id, ConsumerGroup groupConfig)
        {
            await Task.Yield();
            _consumerGroups[id] = groupConfig;
        }

        public override async Task<string> GetConsumerGroupsAsync()
        {
            await Task.Yield();
            var list = _consumerGroups.Select(kv => new { value = kv.Value, key = $"/apisix/consumer_groups/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task DeleteConsumerGroupAsync(string id)
        {
            await Task.Yield();
            _consumerGroups.TryRemove(id, out _);
        }

        // --- Upstreams ---
        public override async Task CreateUpstreamAsync(string id, StandaloneUpstream upstreamConfig)
        {
            await Task.Yield();
            _upstreams[id] = upstreamConfig;
        }

        public override async Task<StandaloneUpstream?> GetUpstreamAsync(string id)
        {
            await Task.Yield();
            if (_upstreams.TryGetValue(id, out var upstream))
                return upstream;
            return null;
        }

        public override async Task<string> GetUpstreamsAsync()
        {
            await Task.Yield();
            var list = _upstreams.Select(kv => new { value = kv.Value, key = $"/apisix/upstreams/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task UpdateUpstreamAsync(string id, StandaloneUpstream upstreamConfig)
        {
            await Task.Yield();
            _upstreams[id] = upstreamConfig;
        }

        public override async Task DeleteUpstreamAsync(string id)
        {
            await Task.Yield();
            _upstreams.TryRemove(id, out _);
        }

        // --- SSL ---
        public override async Task CreateSslAsync(string id, SslCertificate sslConfig)
        {
            await Task.Yield();
            _ssls[id] = sslConfig;
        }

        public override async Task<SslCertificate?> GetSslAsync(string id)
        {
            await Task.Yield();
            if (_ssls.TryGetValue(id, out var ssl))
                return ssl;
            return null;
        }

        public override async Task<string> GetSslsAsync()
        {
            await Task.Yield();
            var list = _ssls.Select(kv => new { value = kv.Value, key = $"/apisix/ssls/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task UpdateSslAsync(string id, SslCertificate sslConfig)
        {
            await Task.Yield();
            _ssls[id] = sslConfig;
        }

        public override async Task DeleteSslAsync(string id)
        {
            await Task.Yield();
            _ssls.TryRemove(id, out _);
        }

        // --- Global Rules ---
        public override async Task CreateGlobalRuleAsync(string id, GlobalRule ruleConfig)
        {
            await Task.Yield();
            _globalRules[id] = ruleConfig;
        }

        public override async Task<string> GetGlobalRulesAsync()
        {
            await Task.Yield();
            var list = _globalRules.Select(kv => new { value = kv.Value, key = $"/apisix/global_rules/{kv.Key}" }).ToList();
            var response = new { list = list, total = list.Count };
            return JsonSerializer.Serialize(response);
        }

        public override async Task DeleteGlobalRuleAsync(string id)
        {
            await Task.Yield();
            _globalRules.TryRemove(id, out _);
        }

        // --- Plugin Configs ---
        public override async Task CreatePluginConfigAsync(string id, PluginConfig configData)
        {
            await Task.Yield();
            _pluginConfigs[id] = configData;
        }

        public override async Task<string> GetPluginConfigsAsync()
        {
            await Task.Yield();
            var list = _pluginConfigs.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return JsonSerializer.Serialize(response);
        }

        public override async Task DeletePluginConfigAsync(string id)
        {
            await Task.Yield();
            _pluginConfigs.TryRemove(id, out _);
        }

        // --- Server Info ---
        public override async Task<string> GetServerInfoAsync()
        {
            await Task.Yield();
            var info = new { version = "3.11.0-mock", hostname = "mock-apisix", boot_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            return JsonSerializer.Serialize(info);
        }
    }
}
