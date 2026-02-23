using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Milk API Manager",
        Version = "v1",
        Description = "Enterprise API Management & Security Governance Platform"
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS — allow Blazor admin UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5000", "http://milk-admin-ui:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// JWT Bearer Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? builder.Configuration["Jwt:Secret"] 
    ?? "milk-api-default-jwt-secret-change-in-production-32chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MilkApiManager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MilkApiClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

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

// Register Background Services
builder.Services.AddHostedService<AlertMonitoringService>();
builder.Services.AddHostedService<AutoBlockWorker>();
builder.Services.AddHostedService<ApisixRouteSyncService>();

// Register AuthService
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// ===== HTTP Request Pipeline =====

// 1. Security Headers (first in pipeline, applies to ALL responses)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Server", "MilkApiManager");
    await next();
});

// 2. CORS
app.UseCors();

// 3. Swagger (always enabled, gated by configuration if needed)
app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// 4. Health Check endpoint (no auth required)
app.MapHealthChecks("/health");

// 5. Authentication (JWT Bearer)
app.UseAuthentication();

// 6. API Key / JWT dual Authentication middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

// 7. Authorization (RBAC)
app.UseAuthorization();

// 7. Controllers
app.MapControllers();

// Auto-migrate / ensure created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();
    
    try
    {
        if (isTestMode)
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Test database initialized with EnsureCreated.");
        }
        else
        {
            db.Database.Migrate();
            logger.LogInformation("Database migrated successfully.");
        }

        // On startup: sync DB entries to APISIX
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

app.Run();
