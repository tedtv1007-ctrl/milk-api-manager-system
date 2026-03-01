using System.Text.Json.Serialization;

namespace MilkAdminBlazor.Data
{
    // ================================================================
    // DTOs extracted from ApisixService (A-2: God Class refactor)
    // ================================================================

    public class ApiRoute
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Uri { get; set; }
        public required string RiskLevel { get; set; } // L1, L2, L3
        public required string Owner { get; set; }
        public List<string> WhitelistIps { get; set; } = new();
    }

    public class BlacklistRequest
    {
        [JsonPropertyName("ip")]
        public required string Ip { get; set; }

        [JsonPropertyName("action")]
        public required string Action { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("addedBy")]
        public string? AddedBy { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    public class ApiConsumer
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("desc")]
        public required string Desc { get; set; }

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
        public required string Status { get; set; }

        [JsonPropertyName("lastSyncTime")]
        public DateTime? LastSyncTime { get; set; }
    }

    public class ConsumerStats
    {
        public required string Username { get; set; }
        public long RequestCount { get; set; }
        public double ErrorRate { get; set; } // Percentage
        public DateTime Timestamp { get; set; }
    }

    public class MetricPoint
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    public class AnalyticsResult
    {
        [JsonPropertyName("label")]
        public required string Label { get; set; }
        [JsonPropertyName("data")]
        public List<MetricPoint> Data { get; set; } = new();
    }

    public class BlacklistEntryDto
    {
        [JsonPropertyName("ipOrCidr")]
        public required string IpOrCidr { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("addedBy")]
        public string? AddedBy { get; set; }

        [JsonPropertyName("addedAt")]
        public DateTime? AddedAt { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    public class WhitelistEntryDto
    {
        [JsonPropertyName("ip")]
        public required string Ip { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("addedBy")]
        public string? AddedBy { get; set; }

        [JsonPropertyName("addedAt")]
        public DateTime? AddedAt { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    public class PiiMaskingRuleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("routeId")]
        public string RouteId { get; set; } = string.Empty;

        [JsonPropertyName("fieldPath")]
        public string FieldPath { get; set; } = string.Empty;

        [JsonPropertyName("regexPattern")]
        public string RegexPattern { get; set; } = ".*";

        [JsonPropertyName("replacePattern")]
        public string ReplacePattern { get; set; } = "***";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class ConsumerGroupDto
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("desc")]
        public string? Desc { get; set; }

        // Simplified quota view for UI
        public int RateLimit { get; set; } = 1000;
    }

    public class MockRuleDto
    {
        public int Id { get; set; }
        public string RouteId { get; set; } = "";
        public int ResponseStatusCode { get; set; } = 200;
        public string ResponseBody { get; set; } = "{}";
        public string ContentType { get; set; } = "application/json";
        public bool IsEnabled { get; set; } = true;
    }

    public class AccessRequestDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = "";
        public string ApplicantEmail { get; set; } = "";
        public string RequestedTier { get; set; } = "Free";
        public string Purpose { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string? AdminComment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string BasePath { get; set; } = "";
        public string OpenApiUrl { get; set; } = "";
        public string OwnerTeam { get; set; } = "";
    }

    public class TestScenarioDto
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } = "";
        public string Endpoint { get; set; } = "/";
        public string HttpMethod { get; set; } = "GET";
        public int ExpectedStatusCode { get; set; } = 200;
        public string? LastResult { get; set; }
        public DateTime? LastRunAt { get; set; }
    }

    public class SlaDto
    {
        public double AvailabilityPercentage { get; set; }
        public string Status { get; set; } = "Unknown";
    }

    public class AuditLogEntryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("clientIp")]
        public string? ClientIp { get; set; }

        [JsonPropertyName("detailsJson")]
        public string? DetailsJson { get; set; }
    }

    // --- APISIX Gateway Management DTOs ---

    public class ApisixRouteDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "/*";

        [JsonPropertyName("uris")]
        public List<string>? Uris { get; set; }

        [JsonPropertyName("methods")]
        public List<string>? Methods { get; set; }

        [JsonPropertyName("service_id")]
        public string? ServiceId { get; set; }

        [JsonPropertyName("upstream_id")]
        public string? UpstreamId { get; set; }

        [JsonPropertyName("upstream")]
        public ApisixUpstreamDto? Upstream { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }

    public class ApisixUpstreamDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "roundrobin";

        [JsonPropertyName("nodes")]
        public Dictionary<string, int>? Nodes { get; set; }
    }

    public class ApisixStandaloneUpstreamDto
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

        [JsonPropertyName("scheme")]
        public string? Scheme { get; set; }

        [JsonPropertyName("pass_host")]
        public string? PassHost { get; set; }
    }

    public class ApisixServiceDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("upstream")]
        public ApisixUpstreamDto? Upstream { get; set; }
    }

    public class SslCertificateDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snis")]
        public List<string> Snis { get; set; } = new();

        [JsonPropertyName("cert")]
        public string Cert { get; set; } = "";

        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("status")]
        public int Status { get; set; } = 1;

        [JsonPropertyName("hasCert")]
        public bool? HasCert { get; set; }

        [JsonPropertyName("hasKey")]
        public bool? HasKey { get; set; }

        [JsonPropertyName("validity_start")]
        public long? ValidityStart { get; set; }

        [JsonPropertyName("validity_end")]
        public long? ValidityEnd { get; set; }
    }

    public class GlobalRuleDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("plugins")]
        public Dictionary<string, object>? Plugins { get; set; }
    }

    public class DashboardStatsDto
    {
        [JsonPropertyName("routeCount")]
        public int RouteCount { get; set; }

        [JsonPropertyName("serviceCount")]
        public int ServiceCount { get; set; }

        [JsonPropertyName("upstreamCount")]
        public int UpstreamCount { get; set; }

        [JsonPropertyName("consumerCount")]
        public int ConsumerCount { get; set; }

        [JsonPropertyName("sslCount")]
        public int SslCount { get; set; }

        [JsonPropertyName("globalRuleCount")]
        public int GlobalRuleCount { get; set; }
    }
}
