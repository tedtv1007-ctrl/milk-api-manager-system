namespace MilkApiManager.Options;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key for JWT tokens.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Token issuer claim.</summary>
    public string Issuer { get; set; } = "MilkApiManager";

    /// <summary>Token audience claim.</summary>
    public string Audience { get; set; } = "MilkApiClients";

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 480;
}
