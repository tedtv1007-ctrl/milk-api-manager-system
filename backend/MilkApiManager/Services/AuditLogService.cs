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
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(HttpClient httpClient, IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<AuditLogService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        // Ship to Logstash (fire-and-forget with proper error handling)
        _ = Task.Run(async () =>
        {
            try
            {
                var logstashUrl = Environment.GetEnvironmentVariable("LOGSTASH_URL") ?? "http://logstash:8080";
                await _httpClient.PostAsJsonAsync(logstashUrl, entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ship audit log to Logstash");
            }
        });

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

    public virtual async Task<Dictionary<string, int>> GetAuditStatsAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Count entries by Action in the last 24 hours
            var cutoff = DateTime.UtcNow.AddHours(-24);
            return await dbContext.AuditLogs
                .Where(l => l.Timestamp >= cutoff)
                .GroupBy(l => l.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);
        }
    }

    public virtual async Task<string> GetLogsCsvAsync(int limit = 1000)
    {
        var logs = await GetLogsAsync(limit);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,User,Action,Resource,Status");
        
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Timestamp:O},{log.User},{log.Action},{log.Resource},{log.StatusCode}");
        }
        
        return sb.ToString();
    }

    public virtual async Task ShipLogsToSIEM(AuditLogEntry entry)
    {
        // Placeholder for SIEM shipping logic (e.g., Splunk, Azure Monitor)
        // For now, it's handled by Console output which is collected by Fluentd
        await Task.CompletedTask;
    }
}
