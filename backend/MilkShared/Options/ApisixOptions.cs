namespace MilkApiManager.Options;

/// <summary>
/// Configuration options for APISIX Admin API connectivity.
/// </summary>
public class ApisixOptions
{
    public const string SectionName = "Apisix";

    /// <summary>APISIX Admin API base URL.</summary>
    public string AdminUrl { get; set; } = "http://apisix:9180/apisix/admin/";

    /// <summary>APISIX Admin API key for authentication.</summary>
    public string AdminKey { get; set; } = string.Empty;

    /// <summary>APISIX public-facing gateway URL (used for test execution, etc.).</summary>
    public string PublicUrl { get; set; } = "http://apisix:9080";
}
