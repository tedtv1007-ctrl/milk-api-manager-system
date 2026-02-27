using MilkApiManager.Data;
using MilkApiManager.Services;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// OpenTelemetry Observability
var otel = builder.Services.AddOpenTelemetry();
otel.ConfigureResource(resource => resource.AddService("MilkWorker"));
otel.WithMetrics(metrics => metrics
    .AddHttpClientInstrumentation()
    .AddOtlpExporter());
otel.WithTracing(tracing => tracing
    .AddHttpClientInstrumentation()
    .AddEntityFrameworkCoreInstrumentation()
    .AddOtlpExporter());

// Register DbContext
var isTestMode = Environment.GetEnvironmentVariable("USE_TEST_MODE") == "true";
if (isTestMode)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MilkApiManagerTestDb"));
    builder.Services.AddDbContext<AuditContext>(options =>
        options.UseInMemoryDatabase("AuditLogTestDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddDbContext<AuditContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register Services
if (isTestMode)
{
    builder.Services.AddHttpClient<ApisixClient, MockApisixClient>().AddStandardResilienceHandler();
}
else
{
    builder.Services.AddHttpClient<ApisixClient>().AddStandardResilienceHandler();
}

builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddHttpClient<NotificationService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<PrometheusService>().AddStandardResilienceHandler();

// Register Background Services
builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

builder.Services.AddHostedService<AlertMonitoringService>();
builder.Services.AddHostedService<AutoBlockWorker>();
builder.Services.AddHostedService<ApisixRouteSyncService>();
builder.Services.AddHostedService<KeyRotationBackgroundService>();

var host = builder.Build();
host.Run();
