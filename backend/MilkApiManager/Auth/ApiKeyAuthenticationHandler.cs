using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MilkApiManager.Options;
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
        UrlEncoder encoder,
        IOptions<AuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        var auth = authOptions.Value;
        var configuredApiKey = auth.ApiAuthKey;
        var isTestMode = auth.UseTestMode;
        var useDemoAuth = auth.UseDemoAuth;

        if (string.IsNullOrWhiteSpace(configuredApiKey) && !(isTestMode || useDemoAuth))
        {
            throw new InvalidOperationException("Auth:ApiAuthKey must be configured in non-test environments.");
        }

        _apiKey = string.IsNullOrWhiteSpace(configuredApiKey)
            ? AuthOptions.DefaultApiAuthKey
            : configuredApiKey;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey) || string.IsNullOrEmpty(extractedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = extractedKey.ToString();

        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedKey),
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

        Logger.LogWarning("Invalid API Key provided. Provided: {Provided}, Expected (masked): {Expected}", providedKey, _apiKey.Substring(0, Math.Min(5, _apiKey.Length)) + "...");
        return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
    }
}
