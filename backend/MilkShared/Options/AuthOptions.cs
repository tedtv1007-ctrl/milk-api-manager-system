namespace MilkApiManager.Options;

/// <summary>
/// Configuration options for authentication mode and API key management.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Shared API key used for X-API-KEY header authentication.</summary>
    public string ApiAuthKey { get; set; } = string.Empty;

    /// <summary>When true, enables in-memory test database and mock APISIX client.</summary>
    public bool UseTestMode { get; set; }

    /// <summary>When true, enables demo accounts (admin/admin, operator/operator, viewer/viewer).</summary>
    public bool UseDemoAuth { get; set; }

    /// <summary>Default API key placeholder — used only when test/demo mode is active and no key is configured.</summary>
    public const string DefaultApiAuthKey = "milk-admin-secret-key-change-me";

    /// <summary>Default JWT secret placeholder — must not be used in production.</summary>
    public const string DefaultJwtSecret = "milk-api-default-jwt-secret-change-in-production-32chars!";
}
