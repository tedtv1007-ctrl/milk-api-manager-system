using MilkApiManager.Models;

namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for authentication (LDAP / Demo) and JWT token generation.
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> AuthenticateAsync(string username, string password);
}
