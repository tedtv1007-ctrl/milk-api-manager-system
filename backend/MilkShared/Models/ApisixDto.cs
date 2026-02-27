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
}
