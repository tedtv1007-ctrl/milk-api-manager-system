using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;
using MilkApiManager.Data;

namespace MilkApiManager.Services;

public class AuditLogService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditLogService(HttpClient httpClient, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    public virtual async Task LogAsync(AuditLogEntry entry)
    {
        // 1. Console Logging for Fluentd/Logstash
        if (entry.Timestamp.Kind != DateTimeKind.Utc)
        {
            entry.Timestamp = entry.Timestamp.ToUniversalTime();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(entry, options);
        Console.WriteLine(json);

        // 2. Database Logging (Configurable)
        bool enableDb = _configuration.GetValue<bool>("AuditLog:EnableDatabaseWrite");
        if (enableDb)
        {
            // Serialize Details object to string for DB storage
            if (entry.Details != null && string.IsNullOrEmpty(entry.DetailsJson))
            {
                entry.DetailsJson = JsonSerializer.Serialize(entry.Details);
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Ensure EF Core doesn't track the ID if it's 0 (insert)
                entry.Id = 0; 
                
                dbContext.AuditLogs.Add(entry);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public virtual async Task<List<AuditLogEntry>> GetLogsAsync(int limit = 100)
    {
        bool enableDb = _configuration.GetValue<bool>("AuditLog:EnableDatabaseWrite");
        if (!enableDb)
        {
            return new List<AuditLogEntry>();
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }

    public virtual async Task ShipLogsToSIEM(AuditLogEntry entry)
    {
        // Placeholder for SIEM shipping logic (e.g., Splunk, Azure Monitor)
        // For now, it's handled by Console output which is collected by Fluentd
        await Task.CompletedTask;
    }
}
