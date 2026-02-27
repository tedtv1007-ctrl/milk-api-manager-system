using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MilkApiManager.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string AuthenticationType => DefaultScheme;
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string API_KEY_HEADER = "X-API-KEY";
    private readonly string _apiKey;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _apiKey = Environment.GetEnvironmentVariable("API_AUTH_KEY") 
            ?? "milk-admin-secret-key-change-me";
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey) || string.IsNullOrEmpty(extractedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(extractedKey.ToString()),
                Encoding.UTF8.GetBytes(_apiKey)))
        {
            var claims = new[] { 
                new Claim(ClaimTypes.Name, "ApiKeyUser"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        Logger.LogWarning("Invalid API Key provided.");
        return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
    }
}
