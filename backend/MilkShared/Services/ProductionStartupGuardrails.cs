using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MilkApiManager.Services;

public static class ProductionStartupGuardrails
{
    private const string DefaultApiAuthKey = "milk-admin-secret-key-change-me";
    private const string DefaultJwtSecret = "milk-api-default-jwt-secret-change-in-production-32chars!";
    private const string DefaultApisixAdminKey = "edd1c9f034335f136f87ad84b625c88b";

    public static void ValidateForApi(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        ValidateCommon(environment, configuration);

        var jwtSecret = configuration["JWT_SECRET"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == DefaultJwtSecret)
        {
            throw new InvalidOperationException("Production guardrail: JWT secret must be explicitly configured and must not use default placeholder.");
        }

        var apiAuthKey = configuration["API_AUTH_KEY"]
            ?? Environment.GetEnvironmentVariable("API_AUTH_KEY");
        if (string.IsNullOrWhiteSpace(apiAuthKey) || apiAuthKey == DefaultApiAuthKey)
        {
            throw new InvalidOperationException("Production guardrail: API_AUTH_KEY must be explicitly configured and must not use default placeholder.");
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Any(IsLocalOrigin))
        {
            throw new InvalidOperationException("Production guardrail: CORS allowed origins must not include localhost/127.0.0.1 entries.");
        }
    }

    public static void ValidateForWorker(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        ValidateCommon(environment, configuration);
    }

    private static void ValidateCommon(IHostEnvironment environment, IConfiguration configuration)
    {
        var isTestMode = (configuration["USE_TEST_MODE"] ?? Environment.GetEnvironmentVariable("USE_TEST_MODE")) == "true";
        var useDemoAuth = (configuration["USE_DEMO_AUTH"] ?? Environment.GetEnvironmentVariable("USE_DEMO_AUTH")) == "true";

        if (isTestMode || useDemoAuth)
        {
            throw new InvalidOperationException("Production guardrail: USE_TEST_MODE and USE_DEMO_AUTH must both be disabled.");
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Production guardrail: DefaultConnection connection string must be configured.");
        }

        if (ContainsInsensitive(connectionString, "Host=localhost") ||
            ContainsInsensitive(connectionString, "Host=127.0.0.1") ||
            ContainsInsensitive(connectionString, "Username=milk_user") ||
            ContainsInsensitive(connectionString, "Password=milk_password") ||
            ContainsInsensitive(connectionString, "SSL Mode=Disable"))
        {
            throw new InvalidOperationException("Production guardrail: DefaultConnection contains insecure local/default settings.");
        }

        var apisixAdminKey = configuration["APISIX_ADMIN_KEY"]
            ?? Environment.GetEnvironmentVariable("APISIX_ADMIN_KEY");
        if (string.IsNullOrWhiteSpace(apisixAdminKey) || apisixAdminKey == DefaultApisixAdminKey)
        {
            throw new InvalidOperationException("Production guardrail: APISIX_ADMIN_KEY must be explicitly configured and must not use default placeholder.");
        }
    }

    private static bool IsLocalOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        return origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || origin.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsInsensitive(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
