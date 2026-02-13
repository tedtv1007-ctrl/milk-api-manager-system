using MilkApiManager.Models.Apisix;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public class MockApisixClient : ApisixClient
    {
        private static readonly ConcurrentDictionary<string, ApisixRoute> _routes = new();
        private static readonly ConcurrentDictionary<string, Consumer> _consumers = new();
        private static readonly ConcurrentDictionary<string, Service> _services = new();
        private static List<string> _blacklist = new();

        public MockApisixClient(HttpClient httpClient, ILogger<ApisixClient> logger) : base(httpClient, logger)
        {
        }

        public override Task CreateRouteAsync(string id, ApisixRoute routeConfig)
        {
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
            // APISIX returns { list: [ { value: { username: "..." } } ] } or similar
            // ConsumerController parses "list" property.
            var list = _consumers.Select(kv => new { value = kv.Value }).ToList();
            var response = new { list = list };
            return Task.FromResult(JsonSerializer.Serialize(response));
        }

        public override Task UpdateConsumerAsync(string username, object consumerConfig)
        {
            // consumerConfig is generic object, simplified mock handling
            // In real app, we might need more logic, but for now assuming it succeeds
            // Ideally we should deserialize to Consumer, but config is object.
            // We'll just create a dummy consumer or try to parse if needed.
            // For E2E tests, we just need the API to return 200.
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
    }
}
