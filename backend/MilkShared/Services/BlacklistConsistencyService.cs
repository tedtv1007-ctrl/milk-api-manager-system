using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;

namespace MilkApiManager.Services;

public class BlacklistConsistencyService
{
    private readonly AppDbContext _dbContext;
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<BlacklistConsistencyService> _logger;

    public BlacklistConsistencyService(AppDbContext dbContext, IApisixClient apisixClient, ILogger<BlacklistConsistencyService> logger)
    {
        _dbContext = dbContext;
        _apisixClient = apisixClient;
        _logger = logger;
    }

    public virtual async Task<BlacklistDriftReport> GetBlacklistDriftReportAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var dbEntries = await _dbContext.BlacklistEntries
            .Where(e => e.ExpiresAt == null || e.ExpiresAt > utcNow)
            .Select(e => e.IpOrCidr.ToString())
            .ToListAsync(cancellationToken);

        var gatewayEntries = await _apisixClient.GetBlacklistAsync();

        var dbSet = new HashSet<string>(dbEntries);
        var gatewaySet = new HashSet<string>(gatewayEntries);

        return new BlacklistDriftReport
        {
            DatabaseOnly = dbSet.Except(gatewaySet).OrderBy(x => x).ToList(),
            GatewayOnly = gatewaySet.Except(dbSet).OrderBy(x => x).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public virtual async Task<BlacklistDriftReport> ReconcileDatabaseToGatewayAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var dbEntries = await _dbContext.BlacklistEntries
            .Where(e => e.ExpiresAt == null || e.ExpiresAt > utcNow)
            .Select(e => e.IpOrCidr.ToString())
            .ToListAsync(cancellationToken);

        await _apisixClient.UpdateBlacklistAsync(dbEntries);
        _logger.LogInformation("Reconciled APISIX blacklist from database with {Count} entries", dbEntries.Count);

        return await GetBlacklistDriftReportAsync(cancellationToken);
    }
}
