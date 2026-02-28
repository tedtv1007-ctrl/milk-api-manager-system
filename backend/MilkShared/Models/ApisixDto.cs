using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MilkApiManager.Models.Apisix
{
    public class Route
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("uri")]
        public required string Uri { get; set; }
        
        [JsonPropertyName("uris")]
        public List<string>? Uris { get; set; }

        [JsonPropertyName("methods")]
        public List<string>? Methods { get; set; }

        [JsonPropertyName("service_id")]
        public string? ServiceId { get; set; }

        [JsonPropertyName("upstream")]
        public Upstream? Upstream { get; set; }

        [JsonPropertyName("upstream_id")]
        public string? UpstreamId { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }

    public class Service
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("upstream")]
        public required Upstream Upstream { get; set; }
    }

    public class Upstream
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "roundrobin";

        [JsonPropertyName("nodes")]
        public required Dictionary<string, int> Nodes { get; set; }
    }

    public class Consumer
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }

        [JsonPropertyName("group_id")]
        public string? GroupId { get; set; }
    }

    public class ConsumerGroup
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }

    /// <summary>
    /// Standalone Upstream resource managed via APISIX Admin API /upstreams/{id}
    /// </summary>
    public class StandaloneUpstream
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "roundrobin";

        [JsonPropertyName("nodes")]
        public Dictionary<string, int>? Nodes { get; set; }

        [JsonPropertyName("retries")]
        public int? Retries { get; set; }

        [JsonPropertyName("timeout")]
        public UpstreamTimeout? Timeout { get; set; }

        [JsonPropertyName("checks")]
        public object? Checks { get; set; }

        [JsonPropertyName("scheme")]
        public string? Scheme { get; set; }

        [JsonPropertyName("hash_on")]
        public string? HashOn { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("pass_host")]
        public string? PassHost { get; set; }

        [JsonPropertyName("upstream_host")]
        public string? UpstreamHost { get; set; }
    }

    public class UpstreamTimeout
    {
        [JsonPropertyName("connect")]
        public double Connect { get; set; } = 6;

        [JsonPropertyName("send")]
        public double Send { get; set; } = 6;

        [JsonPropertyName("read")]
        public double Read { get; set; } = 6;
    }

    /// <summary>
    /// SSL certificate resource managed via APISIX Admin API /ssls/{id}
    /// </summary>
    public class SslCertificate
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("cert")]
        public string? Cert { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("snis")]
        public List<string>? Snis { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; } = 1;

        [JsonPropertyName("validity_start")]
        public long? ValidityStart { get; set; }

        [JsonPropertyName("validity_end")]
        public long? ValidityEnd { get; set; }
    }

    /// <summary>
    /// Global Rule resource managed via APISIX Admin API /global_rules/{id}
    /// </summary>
    public class GlobalRule
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }

    /// <summary>
    /// Plugin config for shared plugin sets, managed via APISIX Admin API /plugin_configs/{id}
    /// </summary>
    public class PluginConfig
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }
}
