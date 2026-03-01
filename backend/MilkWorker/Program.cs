using MilkApiManager.Data;
using MilkApiManager.Services;
using MilkApiManager.Options;
using MilkApiManager.Workers;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);

var isTestMode = Environment.GetEnvironmentVariable("USE_TEST_MODE") == "true";

if (!isTestMode)
{
    ProductionStartupGuardrails.ValidateForWorker(builder.Configuration, builder.Environment);
}

// ===== Strongly-typed Options =====
builder.Services.Configure<ApisixOptions>(builder.Configuration.GetSection(ApisixOptions.SectionName));
builder.Services.PostConfigure<ApisixOptions>(options =>
{
    var envUrl = Environment.GetEnvironmentVariable("APISIX_ADMIN_URL");
    if (!string.IsNullOrEmpty(envUrl)) options.AdminUrl = envUrl;
    var envKey = Environment.GetEnvironmentVariable("APISIX_ADMIN_KEY");
    if (!string.IsNullOrEmpty(envKey)) options.AdminKey = envKey;
    var envPublic = Environment.GetEnvironmentVariable("APISIX_PUBLIC_URL");
    if (!string.IsNullOrEmpty(envPublic)) options.PublicUrl = envPublic;
});
builder.Services.Configure<PrometheusOptions>(builder.Configuration.GetSection(PrometheusOptions.SectionName));
builder.Services.PostConfigure<PrometheusOptions>(options =>
{
    var envUrl = Environment.GetEnvironmentVariable("PROMETHEUS_URL");
    if (!string.IsNullOrEmpty(envUrl)) options.Url = envUrl;
});

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

// Register DbContext (P1-4: AuditContext removed — AppDbContext handles all entities)
if (isTestMode)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MilkApiManagerTestDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register Services (P1-1: Interface-based DI)
if (isTestMode)
{
    builder.Services.AddHttpClient<IApisixClient, MockApisixClient>().AddStandardResilienceHandler();
}
else
{
    builder.Services.AddHttpClient<IApisixClient, ApisixClient>().AddStandardResilienceHandler();
}

builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<ApisixSyncOutboxService>();
builder.Services.AddScoped<ApisixSyncOutboxProcessor>();
builder.Services.AddScoped<BlacklistConsistencyService>();
builder.Services.AddHttpClient<AuditLogShippingOutboxProcessor>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<INotificationService, NotificationService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<IPrometheusService, PrometheusService>().AddStandardResilienceHandler();
builder.Services.AddScoped<ISecurityAutomationService, SecurityAutomationService>();
builder.Services.AddScoped<IDistributedLock, PostgresAdvisoryLock>();

// Register Background Services
builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

builder.Services.AddHostedService<AlertMonitoringService>();
builder.Services.AddHostedService<AutoBlockWorker>();
builder.Services.AddHostedService<ApisixRouteSyncService>();
builder.Services.AddHostedService<KeyRotationBackgroundService>();

if (builder.Configuration.GetValue<bool>("Sync:Blacklist:UseOutbox"))
{
    builder.Services.AddHostedService<ApisixSyncOutboxWorker>();
}

if (builder.Configuration.GetValue<bool>("Sync:Blacklist:EnableReconcile"))
{
    builder.Services.AddHostedService<BlacklistReconcileWorker>();
}

if (builder.Configuration.GetValue<bool?>("AuditLog:UseDurableShipping") ?? true)
{
    builder.Services.AddHostedService<AuditLogShippingOutboxWorker>();
}

var host = builder.Build();
host.Run();
