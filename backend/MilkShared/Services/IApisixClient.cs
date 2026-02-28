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

    // Routes (Typed)
    Task<List<ApisixRoute>> GetRoutesTypedAsync();

    // Services
    Task CreateServiceAsync(string id, Service serviceConfig);
    Task<Service?> GetServiceAsync(string id);
    Task<string> GetServicesAsync();
    Task UpdateServiceAsync(string id, Service serviceConfig);
    Task DeleteServiceAsync(string id);

    // Services (Typed)
    Task<List<Service>> GetServicesTypedAsync();

    // Upstreams (Standalone)
    Task CreateUpstreamAsync(string id, StandaloneUpstream upstreamConfig);
    Task<StandaloneUpstream?> GetUpstreamAsync(string id);
    Task<string> GetUpstreamsAsync();
    Task<List<StandaloneUpstream>> GetUpstreamsTypedAsync();
    Task UpdateUpstreamAsync(string id, StandaloneUpstream upstreamConfig);
    Task DeleteUpstreamAsync(string id);

    // SSL Certificates
    Task CreateSslAsync(string id, SslCertificate sslConfig);
    Task<SslCertificate?> GetSslAsync(string id);
    Task<string> GetSslsAsync();
    Task<List<SslCertificate>> GetSslsTypedAsync();
    Task UpdateSslAsync(string id, SslCertificate sslConfig);
    Task DeleteSslAsync(string id);

    // Global Rules
    Task CreateGlobalRuleAsync(string id, GlobalRule ruleConfig);
    Task<string> GetGlobalRulesAsync();
    Task<List<GlobalRule>> GetGlobalRulesTypedAsync();
    Task DeleteGlobalRuleAsync(string id);

    // Plugin Configs
    Task CreatePluginConfigAsync(string id, PluginConfig configData);
    Task<string> GetPluginConfigsAsync();
    Task<List<PluginConfig>> GetPluginConfigsTypedAsync();
    Task DeletePluginConfigAsync(string id);

    // Server Info
    Task<string> GetServerInfoAsync();

    // Consumers
    Task CreateConsumerAsync(string username, Consumer consumerConfig);
    Task<Consumer?> GetConsumerAsync(string username);
    Task<string> GetConsumersAsync();
    Task UpdateConsumerAsync(string username, object consumerConfig);
    Task DeleteConsumerAsync(string username);

    // Consumers (Typed)
    Task<List<Consumer>> GetConsumersTypedAsync();

    // Consumer Groups
    Task CreateConsumerGroupAsync(string id, ConsumerGroup groupConfig);
    Task<string> GetConsumerGroupsAsync();
    Task DeleteConsumerGroupAsync(string id);

    // Consumer Groups (Typed)
    Task<List<ConsumerGroup>> GetConsumerGroupsTypedAsync();

    // Security
    Task<List<string>> GetBlacklistAsync();
    Task UpdateBlacklistAsync(List<string> blacklist);
    Task UpdateGlobalPlugin(string pluginName, object body);
    Task<List<string>> GetWhitelistForRouteAsync(string routeId);
    Task UpdateWhitelistForRouteAsync(string routeId, List<string> whitelist);
}
