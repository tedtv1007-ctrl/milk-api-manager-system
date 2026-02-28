using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MilkApiManager.Models;
using Novell.Directory.Ldap;

namespace MilkApiManager.Services;

/// <summary>
/// Handles user authentication via LDAP and JWT token generation.
/// In test mode, allows a built-in demo user for easy demonstration.
/// </summary>
public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly bool _isTestMode;

    // LDAP group name → UserRole mapping
    private static readonly Dictionary<string, UserRole> GroupRoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "api-admins", UserRole.Admin },
        { "api-operators", UserRole.Operator },
        { "api-viewers", UserRole.Viewer },
        // Common AD group name patterns
        { "Domain Admins", UserRole.Admin },
        { "IT Operations", UserRole.Operator },
    };

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _isTestMode = Environment.GetEnvironmentVariable("USE_TEST_MODE") == "true";
    }

    /// <summary>
    /// Authenticate a user against LDAP (or demo mode) and return a JWT.
    /// </summary>
    public async Task<LoginResponse?> AuthenticateAsync(string username, string password)
    {
        List<string> roles;

        var useDemoAuth = Environment.GetEnvironmentVariable("USE_DEMO_AUTH") == "true";

        if (_isTestMode || useDemoAuth)
        {
            var demoRoles = AuthenticateDemo(username, password);
            if (demoRoles == null) return null;
            roles = demoRoles;
        }
        else
        {
            var ldapRoles = await Task.Run(() => AuthenticateLdap(username, password));
            if (ldapRoles == null) return null;
            roles = ldapRoles;
        }

        var token = GenerateJwtToken(username, roles);
        var expMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 480);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expMinutes),
            DisplayName = username,
            Roles = roles
        };
    }

    /// <summary>
    /// Demo mode: allows admin/admin, operator/operator, viewer/viewer.
    /// </summary>
    private List<string>? AuthenticateDemo(string username, string password)
    {
        var demoUsers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "admin", new List<string> { "Admin", "Operator", "Viewer" } },
            { "operator", new List<string> { "Operator", "Viewer" } },
            { "viewer", new List<string> { "Viewer" } },
        };

        if (demoUsers.TryGetValue(username, out var roles) && password == username)
        {
            _logger.LogInformation("Demo login successful for user {Username}", username);
            return roles;
        }

        _logger.LogWarning("Demo login failed for user {Username}", username);
        return null;
    }

    /// <summary>
    /// Authenticate against real LDAP/AD server.
    /// </summary>
    private List<string>? AuthenticateLdap(string username, string password)
    {
        var ldapHost = _configuration["Ldap:Host"];
        var ldapPort = _configuration.GetValue("Ldap:Port", 389);
        var searchBase = _configuration["Ldap:SearchBase"] ?? "dc=example,dc=com";

        if (string.IsNullOrEmpty(ldapHost))
        {
            _logger.LogWarning("LDAP host is not configured. Falling back to Demo auth for testing.");
            return AuthenticateDemo(username, password);
        }

        try
        {
            using var connection = new LdapConnection();
            connection.Connect(ldapHost, ldapPort);

            // Try binding with user's credentials (simple bind)
            var userDn = $"cn={username},{searchBase}";
            connection.Bind(userDn, password);

            if (!connection.Bound)
            {
                _logger.LogWarning("LDAP bind failed for user {Username}", username);
                return null;
            }

            _logger.LogInformation("LDAP authentication successful for user {Username}", username);

            // Fetch user's group memberships
            var roles = GetUserRolesFromLdap(connection, username, searchBase);
            
            if (roles.Count == 0)
            {
                // If no specific role mapping found, grant Viewer by default
                roles.Add(UserRole.Viewer.ToString());
            }

            return roles;
        }
        catch (LdapException ex)
        {
            _logger.LogWarning("LDAP authentication failed for user {Username}: {Error}", username, ex.LdapErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during LDAP authentication for user {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Look up user's LDAP group membership and map to application roles.
    /// </summary>
    private List<string> GetUserRolesFromLdap(LdapConnection connection, string username, string searchBase)
    {
        var roles = new HashSet<string>();

        try
        {
            var filter = $"(&(objectClass=groupOfUniqueNames)(uniqueMember=cn={username},{searchBase}))";
            var results = connection.Search(
                searchBase,
                LdapConnection.ScopeSub,
                filter,
                new[] { "cn" },
                false
            );

            while (results.HasMore())
            {
                try
                {
                    var entry = results.Next();
                    var cn = entry.GetAttribute("cn")?.StringValue;
                    if (cn != null && GroupRoleMap.TryGetValue(cn, out var role))
                    {
                        roles.Add(role.ToString());
                        // Admin implies Operator and Viewer
                        if (role == UserRole.Admin)
                        {
                            roles.Add(UserRole.Operator.ToString());
                            roles.Add(UserRole.Viewer.ToString());
                        }
                        else if (role == UserRole.Operator)
                        {
                            roles.Add(UserRole.Viewer.ToString());
                        }
                    }
                }
                catch (LdapReferralException) { continue; }
            }
        }
        catch (LdapException ex)
        {
            _logger.LogWarning("Failed to query LDAP groups for user {Username}: {Error}", username, ex.LdapErrorMessage);
        }

        return roles.ToList();
    }

    /// <summary>
    /// Generate a signed JWT token with user identity and role claims.
    /// </summary>
    private string GenerateJwtToken(string username, List<string> roles)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? _configuration["Jwt:Secret"] 
            ?? "milk-api-default-jwt-secret-change-in-production-32chars!";
        var issuer = _configuration["Jwt:Issuer"] ?? "MilkApiManager";
        var audience = _configuration["Jwt:Audience"] ?? "MilkApiClients";
        var expMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 480);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
