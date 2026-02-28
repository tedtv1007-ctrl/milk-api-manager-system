using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MilkApiManager.Services;

namespace MilkApiManager.Tests.Services;

public class ProductionStartupGuardrailsTests
{
    [Fact]
    public void ValidateForApi_NonProduction_AllowsInsecureDefaults()
    {
        var config = BuildConfig(
            "Host=localhost;Username=milk_user;Password=milk_password;SSL Mode=Disable",
            new[] { "http://localhost:5000" },
            new Dictionary<string, string?>
            {
                ["USE_TEST_MODE"] = "true",
                ["USE_DEMO_AUTH"] = "true"
            });

        var env = new FakeHostEnvironment("Development");
        ProductionStartupGuardrails.ValidateForApi(config, env);
    }

    [Fact]
    public void ValidateForApi_Production_WithDemoAuth_Throws()
    {
        var config = BuildConfig(
            "Host=db.prod.internal;Username=svc_user;Password=strong-password;SSL Mode=Require",
            new[] { "https://portal.example.com" },
            new Dictionary<string, string?>
            {
                ["USE_TEST_MODE"] = "false",
                ["USE_DEMO_AUTH"] = "true",
                ["JWT_SECRET"] = "this-is-a-prod-jwt-secret-value",
                ["API_AUTH_KEY"] = "prod-api-auth-key-value",
                ["APISIX_ADMIN_KEY"] = "prod-apisix-admin-key"
            });

        var env = new FakeHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => ProductionStartupGuardrails.ValidateForApi(config, env));
    }

    [Fact]
    public void ValidateForApi_Production_WithInsecureConnectionString_Throws()
    {
        var config = BuildConfig(
            "Host=localhost;Username=milk_user;Password=milk_password;SSL Mode=Disable",
            new[] { "https://portal.example.com" },
            new Dictionary<string, string?>
            {
                ["USE_TEST_MODE"] = "false",
                ["USE_DEMO_AUTH"] = "false",
                ["JWT_SECRET"] = "this-is-a-prod-jwt-secret-value",
                ["API_AUTH_KEY"] = "prod-api-auth-key-value",
                ["APISIX_ADMIN_KEY"] = "prod-apisix-admin-key"
            });

        var env = new FakeHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => ProductionStartupGuardrails.ValidateForApi(config, env));
    }

    [Fact]
    public void ValidateForApi_Production_WithSecureSettings_DoesNotThrow()
    {
        var config = BuildConfig(
            "Host=db.prod.internal;Port=5432;Database=milk;Username=svc_milk;Password=super-strong;SSL Mode=Require",
            new[] { "https://portal.example.com" },
            new Dictionary<string, string?>
            {
                ["USE_TEST_MODE"] = "false",
                ["USE_DEMO_AUTH"] = "false",
                ["JWT_SECRET"] = "this-is-a-prod-jwt-secret-value",
                ["API_AUTH_KEY"] = "prod-api-auth-key-value",
                ["APISIX_ADMIN_KEY"] = "prod-apisix-admin-key"
            });

        var env = new FakeHostEnvironment("Production");

        ProductionStartupGuardrails.ValidateForApi(config, env);
    }

    [Fact]
    public void ValidateForWorker_Production_WithDefaultApisixKey_Throws()
    {
        var config = BuildConfig(
            "Host=db.prod.internal;Port=5432;Database=milk;Username=svc_milk;Password=super-strong;SSL Mode=Require",
            new[] { "https://portal.example.com" },
            new Dictionary<string, string?>
            {
                ["USE_TEST_MODE"] = "false",
                ["USE_DEMO_AUTH"] = "false",
                ["APISIX_ADMIN_KEY"] = "edd1c9f034335f136f87ad84b625c88b"
            });

        var env = new FakeHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => ProductionStartupGuardrails.ValidateForWorker(config, env));
    }

    private static IConfiguration BuildConfig(string connectionString, string[] allowedOrigins, Dictionary<string, string?>? extra = null)
    {
        var map = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
        };

        for (var i = 0; i < allowedOrigins.Length; i++)
        {
            map[$"Cors:AllowedOrigins:{i}"] = allowedOrigins[i];
        }

        if (extra != null)
        {
            foreach (var pair in extra)
            {
                map[pair.Key] = pair.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(map)
            .Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "MilkApiManager.Tests";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
