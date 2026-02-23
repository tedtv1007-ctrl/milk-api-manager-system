using System.Text.Json.Serialization;

namespace MilkApiManager.Models;

/// <summary>
/// Request model for user login via LDAP/AD credentials.
/// </summary>
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// Response model returned after successful authentication.
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Defines the available user roles for RBAC.
/// Mapped from LDAP group membership.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>Full administrative access — manage security, users, and configuration.</summary>
    Admin,
    /// <summary>Operational access — manage routes, blacklists, and whitelists.</summary>
    Operator,
    /// <summary>Read-only access — view dashboards, logs, and analytics.</summary>
    Viewer
}
