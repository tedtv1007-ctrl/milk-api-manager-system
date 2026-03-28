using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using System.Collections.Concurrent;
using System.Net;

namespace MilkApiManager.Services;

public class AutoBlockWorker : BackgroundService
{
    private readonly ILogger<AutoBlockWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private const int THRESHOLD_ERRORS_PER_MIN = 50;

    // Cache to prevent re-blocking the same IP repeatedly in short bursts
    private readonly ConcurrentDictionary<string, DateTime> _recentlyBlockedIps = new();

    public AutoBlockWorker(IServiceProvider serviceProvider, ILogger<AutoBlockWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Blocking Shield Activated. Monitoring for high error rates...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAndBlockAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoBlockWorker loop");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task MonitorAndBlockAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var promService = scope.ServiceProvider.GetRequiredService<IPrometheusService>();
        var apisixClient = scope.ServiceProvider.GetRequiredService<IApisixClient>();
        var notifyService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Query: Sum of 401/403 errors in the last 1 minute, grouped by client IP
        // Note: 'apisix_http_status' metric must have 'client_ip' label enabled in APISIX prometheus plugin config.
        // If not available, we might fall back to 'remote_addr' or route granularity.
        // For simulation/robustness, we query specific error codes.
        var query = "sum(increase(apisix_http_status{code=~\"401|403\"}[1m])) by (client_ip)";
        
        var suspiciousIps = await promService.QueryVectorAsync(query);

        foreach (var kvp in suspiciousIps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ip = kvp.Key;
            var errorCount = kvp.Value;

            if (ip == "unknown" || ip == "127.0.0.1") continue; // Skip localhost or unknown

            if (errorCount > THRESHOLD_ERRORS_PER_MIN)
            {
                if (_recentlyBlockedIps.TryGetValue(ip, out var blockedTime))
                {
                    if ((DateTime.UtcNow - blockedTime).TotalMinutes < 10) continue; // Already handled recently
                }

                _logger.LogWarning("[SECURITY ALERT] IP {Ip} triggered {ErrorCount} auth errors in 1min. Initiating BLOCK.", ip, errorCount);

                // 1. Add to DB and APISIX via Controller logic (reusing existing logic to ensure consistency)
                // We construct a BlacklistEntry
                var ipAddress = ip;
                var entry = new BlacklistEntry
                {
                    IpOrCidr = ipAddress,
                    Reason = $"Auto-blocked: {errorCount} auth errors/min",
                    AddedBy = "System:AutoBlockWorker",
                    AddedAt = DateTime.UtcNow
                };

                if (!await dbContext.BlacklistEntries.AnyAsync(b => b.IpOrCidr == ipAddress, cancellationToken))
                {
                    dbContext.BlacklistEntries.Add(entry);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    // Sync to APISIX
                    var currentList = await dbContext.BlacklistEntries.Select(e => e.IpOrCidr).ToListAsync(cancellationToken);
                    await apisixClient.UpdateBlacklistAsync(currentList);
                    
                    _recentlyBlockedIps[ip] = DateTime.UtcNow;
                    _logger.LogInformation("[BLOCKED] IP {Ip} has been successfully banned.", ip);

                    await notifyService.AlertAsync(
                        "Active Defense Triggered", 
                        $"IP `{ip}` has been **BLOCKED** due to high error rate ({errorCount}/min).", 
                        isCritical: true
                    );
                }
            }
        }
        
        // Cleanup old cache
        foreach (var key in _recentlyBlockedIps.Keys)
        {
            if ((DateTime.UtcNow - _recentlyBlockedIps[key]).TotalMinutes > 60)
                _recentlyBlockedIps.TryRemove(key, out _);
        }
    }
}
