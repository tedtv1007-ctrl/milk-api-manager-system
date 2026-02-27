using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MilkApiManager.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

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

// CORS — allow Blazor admin UI dynamically
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
// PostgreSQL health check will be added after connection string is resolved below

// OpenTelemetry Observability
var otel = builder.Services.AddOpenTelemetry();
otel.ConfigureResource(resource => resource.AddService("MilkApiManager"));
otel.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter());
otel.WithTracing(tracing => tracing
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddEntityFrameworkCoreInstrumentation()
    .AddOtlpExporter());

// JWT Bearer Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? builder.Configuration["Jwt:Secret"] 
    ?? "milk-api-default-jwt-secret-change-in-production-32chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MilkApiManager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MilkApiClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "BearerOrApiKey";
    options.DefaultChallengeScheme = "BearerOrApiKey";
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
})
.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, null)
.AddPolicyScheme("BearerOrApiKey", "BearerOrApiKey", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        if (context.Request.Headers.ContainsKey("X-API-KEY"))
            return ApiKeyAuthenticationOptions.DefaultScheme;
        
        return JwtBearerDefaults.AuthenticationScheme;
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
    
    // Add PostgreSQL health check for deep /health/ready validation
    healthChecksBuilder.AddNpgSql(connectionString, name: "postgresql", tags: new[] { "ready" });
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
builder.Services.AddHttpClient<AuditLogService>().AddStandardResilienceHandler();
builder.Services.AddHttpClient<PrometheusService>().AddStandardResilienceHandler();
builder.Services.AddSingleton<LoadTestService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<SecurityAutomationService>();

// Register Background Services
// Moved to MilkWorker: AlertMonitoringService, AutoBlockWorker, ApisixRouteSyncService, KeyRotationBackgroundService

// Register AuthService
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// ===== HTTP Request Pipeline =====

// 0. Global Exception Handler (catches unhandled exceptions, prevents stack trace leaks)
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exceptionFeature != null)
        {
            logger.LogError(exceptionFeature.Error, "Unhandled exception on {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
    });
});

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

// 4. Health Check endpoints (no auth required)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

// 5. Authentication (JWT Bearer)
app.UseAuthentication();

// 6. Authorization (RBAC)
app.UseAuthorization();

// 7. Controllers
app.MapControllers();

// Auto-migrate / ensure created
var isMigrateOnly = args.Contains("--migrate-only");

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
        else if (isMigrateOnly || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            // Only migrate if explicit or in dev
            db.Database.Migrate();
            logger.LogInformation("Database migrated successfully.");
        }

        // On startup: sync DB entries to APISIX
        if (!isTestMode && isMigrateOnly)
        {
            await InitializeSystemState(services, logger);
            logger.LogInformation("Migration and Seeding complete. Exiting (--migrate-only).");
            return; // Exit here for Init Container
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred during startup initialization.");
        if (isMigrateOnly)
        {
            Environment.Exit(1);
        }
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

if (!isMigrateOnly)
{
    app.Run();
}
