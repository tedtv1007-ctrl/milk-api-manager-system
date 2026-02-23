using MilkApiManager.Models.Apisix;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;

namespace MilkApiManager.Services;

/// <summary>
/// Interface for interacting with the APISIX Admin API.
/// </summary>
public interface IApisixClient
{
    // Routes
    Task CreateRouteAsync(string id, ApisixRoute routeConfig);
    Task DeleteRouteAsync(string id);
    Task<string> GetRoutesAsync();
    Task<ApisixRoute?> GetRouteAsync(string id);
    Task UpdateRouteAsync(string id, ApisixRoute routeConfig);

    // Services
    Task CreateServiceAsync(string id, Service serviceConfig);
    Task<Service?> GetServiceAsync(string id);
    Task<string> GetServicesAsync();
    Task UpdateServiceAsync(string id, Service serviceConfig);
    Task DeleteServiceAsync(string id);

    // Consumers
    Task CreateConsumerAsync(string username, Consumer consumerConfig);
    Task<Consumer?> GetConsumerAsync(string username);
    Task<string> GetConsumersAsync();
    Task UpdateConsumerAsync(string username, object consumerConfig);
    Task DeleteConsumerAsync(string username);

    // Consumer Groups
    Task CreateConsumerGroupAsync(string id, ConsumerGroup groupConfig);
    Task<string> GetConsumerGroupsAsync();
    Task DeleteConsumerGroupAsync(string id);

    // Security
    Task<List<string>> GetBlacklistAsync();
    Task UpdateBlacklistAsync(List<string> blacklist);
    Task UpdateGlobalPlugin(string pluginName, object body);
    Task<List<string>> GetWhitelistForRouteAsync(string routeId);
    Task UpdateWhitelistForRouteAsync(string routeId, List<string> whitelist);
}
