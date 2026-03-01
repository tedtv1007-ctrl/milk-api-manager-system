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

        public override Task CreateRouteAsync(string id, ApisixRoute routeConfig)
        {
            _logger.LogInformation("MockApisixClient: Creating route {Id}", id);
            _routes[id] = routeConfig;
            return Task.CompletedTask;
        }

        public override Task DeleteRouteAsync(string id)
        {
            _routes.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public override Task<string> GetRoutesAsync()
        {
            var list = _routes.Select(kv => new { value = kv.Value }).ToList();
            var response = new { node = new { nodes = list } };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task<ApisixRoute?> GetRouteAsync(string id)
        {
            if (_routes.TryGetValue(id, out var route))
            {
                return Task.FromResult<ApisixRoute?>(route);
            }
            throw new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound);
        }

        public override Task UpdateRouteAsync(string id, ApisixRoute routeConfig)
        {
            _routes[id] = routeConfig;
            return Task.CompletedTask;
        }

        public override Task CreateServiceAsync(string id, Service serviceConfig)
        {
            _services[id] = serviceConfig;
            return Task.CompletedTask;
        }

        public override Task<Service?> GetServiceAsync(string id)
        {
            if (_services.TryGetValue(id, out var service))
            {
                return Task.FromResult<Service?>(service);
            }
            throw new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound);
        }

        public override Task<string> GetServicesAsync()
        {
            var list = _services.Select(kv => new { value = kv.Value }).ToList();
            var response = new { node = new { nodes = list } };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task UpdateServiceAsync(string id, Service serviceConfig)
        {
            _services[id] = serviceConfig;
            return Task.CompletedTask;
        }

        public override Task DeleteServiceAsync(string id)
        {
            _services.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public override Task CreateConsumerAsync(string username, Consumer consumerConfig)
        {
            _consumers[username] = consumerConfig;
            return Task.CompletedTask;
        }

        public override Task<Consumer?> GetConsumerAsync(string username)
        {
             if (_consumers.TryGetValue(username, out var consumer))
            {
                return Task.FromResult<Consumer?>(consumer);
            }
            throw new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound);
        }

        public override Task<string> GetConsumersAsync()
        {
            var list = _consumers.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task UpdateConsumerAsync(string username, object consumerConfig)
        {
            if (!_consumers.ContainsKey(username))
            {
                _consumers[username] = new Consumer { Username = username };
            }
            return Task.CompletedTask;
        }

        public override Task DeleteConsumerAsync(string username)
        {
            _consumers.TryRemove(username, out _);
            return Task.CompletedTask;
        }

        public override Task<List<string>> GetBlacklistAsync()
        {
            return Task.FromResult(_blacklist);
        }

        public override Task UpdateBlacklistAsync(List<string> blacklist)
        {
            _blacklist = blacklist;
            return Task.CompletedTask;
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

        public override Task UpdateWhitelistForRouteAsync(string routeId, List<string> whitelist)
        {
            _whitelists[routeId] = whitelist;
            return Task.CompletedTask;
        }

        // --- Consumer Groups ---
        public override Task CreateConsumerGroupAsync(string id, ConsumerGroup groupConfig)
        {
            _consumerGroups[id] = groupConfig;
            return Task.CompletedTask;
        }

        public override Task<string> GetConsumerGroupsAsync()
        {
            var list = _consumerGroups.Select(kv => new { value = kv.Value }).ToList();
            var response = new { node = new { nodes = list } };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task DeleteConsumerGroupAsync(string id)
        {
            _consumerGroups.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        // --- Upstreams ---
        public override Task CreateUpstreamAsync(string id, StandaloneUpstream upstreamConfig)
        {
            _upstreams[id] = upstreamConfig;
            return Task.CompletedTask;
        }

        public override Task<StandaloneUpstream?> GetUpstreamAsync(string id)
        {
            if (_upstreams.TryGetValue(id, out var upstream))
                return Task.FromResult<StandaloneUpstream?>(upstream);
            throw new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound);
        }

        public override Task<string> GetUpstreamsAsync()
        {
            var list = _upstreams.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task UpdateUpstreamAsync(string id, StandaloneUpstream upstreamConfig)
        {
            _upstreams[id] = upstreamConfig;
            return Task.CompletedTask;
        }

        public override Task DeleteUpstreamAsync(string id)
        {
            _upstreams.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        // --- SSL ---
        public override Task CreateSslAsync(string id, SslCertificate sslConfig)
        {
            _ssls[id] = sslConfig;
            return Task.CompletedTask;
        }

        public override Task<SslCertificate?> GetSslAsync(string id)
        {
            if (_ssls.TryGetValue(id, out var ssl))
                return Task.FromResult<SslCertificate?>(ssl);
            throw new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound);
        }

        public override Task<string> GetSslsAsync()
        {
            var list = _ssls.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task UpdateSslAsync(string id, SslCertificate sslConfig)
        {
            _ssls[id] = sslConfig;
            return Task.CompletedTask;
        }

        public override Task DeleteSslAsync(string id)
        {
            _ssls.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        // --- Global Rules ---
        public override Task CreateGlobalRuleAsync(string id, GlobalRule ruleConfig)
        {
            _globalRules[id] = ruleConfig;
            return Task.CompletedTask;
        }

        public override Task<string> GetGlobalRulesAsync()
        {
            var list = _globalRules.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task DeleteGlobalRuleAsync(string id)
        {
            _globalRules.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        // --- Plugin Configs ---
        public override Task CreatePluginConfigAsync(string id, PluginConfig configData)
        {
            _pluginConfigs[id] = configData;
            return Task.CompletedTask;
        }

        public override Task<string> GetPluginConfigsAsync()
        {
            var list = _pluginConfigs.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task DeletePluginConfigAsync(string id)
        {
            _pluginConfigs.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        // --- Server Info ---
        public override Task<string> GetServerInfoAsync()
        {
            var info = new { version = "3.11.0-mock", hostname = "mock-apisix", boot_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            return Task.FromResult(JsonSerializer.Serialize(info));
        }
    }
}
