using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register DbContext
// Check both connection string paths just in case
// Check for Test Mode
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
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddDbContext<AuditContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register Services
if (isTestMode)
{
    builder.Services.AddHttpClient<ApisixClient, MockApisixClient>();
}
else
{
    builder.Services.AddHttpClient<ApisixClient>();
}
builder.Services.AddHttpClient<AuditLogService>();
builder.Services.AddHttpClient<PrometheusService>();
builder.Services.AddSingleton<LoadTestService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

builder.Services.AddSingleton<AdGroupSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdGroupSyncService>());

// Register NotificationService for AlertMonitoringService
builder.Services.AddHttpClient<NotificationService>();

// Register AlertMonitoringService as Background Service
builder.Services.AddHostedService<AlertMonitoringService>();
// Register Auto-Blocking Security Worker
builder.Services.AddHostedService<AutoBlockWorker>();
// Register Code-First Route Sync
builder.Services.AddHostedService<ApisixRouteSyncService>();

// AuditContext registered above with AppDbContext logic

var app = builder.Build();

// Auto-migrate/ensure created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();
    
    try
    {
        // For production, consider using db.Database.Migrate() instead of EnsureCreated
        db.Database.EnsureCreated();
        logger.LogInformation("Database initialized and ensured created.");

        // On startup: if configured to persist blacklist to DB, sync DB entries to APISIX
        if (!isTestMode)
        {
            await InitializeSystemState(services, logger);
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred during startup initialization.");
    }
}

async Task InitializeSystemState(IServiceProvider services, ILogger logger)
{
    var db = services.GetRequiredService<AppDbContext>();
    var config = services.GetRequiredService<IConfiguration>();
    var apisix = services.GetRequiredService<ApisixClient>();
    
    // 1. Sync Blacklist
    var persistBlacklist = config.GetValue<bool>("Blacklist:PersistToDatabase");
    if (persistBlacklist)
    {
        try
        {
            var entries = await db.BlacklistEntries.Select(e => e.IpOrCidr).ToListAsync();
            if (entries.Any())
            {
                await apisix.UpdateBlacklistAsync(entries);
                logger.LogInformation("Synced {Count} blacklist entries to APISIX.", entries.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync blacklist to APISIX on startup.");
        }
    }

    // 2. Register this service in API Catalog and seed default data
    try 
    {
        var coreService = await db.ApiServices.FirstOrDefaultAsync(s => s.Name == "Milk Manager Core");
        
        if (coreService == null)
        {
            coreService = new ApiServiceMetadata {
                Name = "Milk Manager Core",
                Description = "Central API Management Control Plane",
                BasePath = "/api",
                OpenApiUrl = "http://localhost:5001/swagger/v1/swagger.json",
                OwnerTeam = "Platform Team"
            };
            db.ApiServices.Add(coreService);
            await db.SaveChangesAsync();
            logger.LogInformation("Registered Milk Manager Core in API Catalog.");
        }

        // Seed a default test scenario
        if (!await db.ApiTestScenarios.AnyAsync(s => s.ServiceId == coreService.Id))
        {
            db.ApiTestScenarios.Add(new ApiTestScenario {
                ServiceId = coreService.Id,
                Name = "Health Check",
                Endpoint = "/AuditLogs/stats",
                HttpMethod = "GET",
                ExpectedStatusCode = 200
            });
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to register service in API Catalog on startup.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disable for local testing dev

app.UseAuthorization();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Server", "MilkApiManager"); // Explicitly set or hide in Kestrel
    await next();
});

app.MapControllers();

app.Run();
