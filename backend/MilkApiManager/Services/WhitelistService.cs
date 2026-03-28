using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using System.Net;

namespace MilkApiManager.Services;

/// <summary>
/// Domain service encapsulating whitelist business logic.
/// Extracted from WhitelistController to achieve SRP and testability.
/// </summary>
public interface IWhitelistService
{
    Task<List<WhitelistEntry>> GetWhitelistForRouteAsync(string routeId);
    Task<string> AddAsync(string routeId, WhitelistUpdateRequest request);
    Task<string> RemoveAsync(string routeId, WhitelistUpdateRequest request);
}

public class WhitelistService : IWhitelistService
{
    private readonly IApisixClient _apisixClient;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<WhitelistService> _logger;

    public WhitelistService(
        IApisixClient apisixClient,
        AppDbContext db,
        IConfiguration config,
        IAuditLogService auditLog,
        ILogger<WhitelistService> logger)
    {
        _apisixClient = apisixClient;
        _db = db;
        _config = config;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<List<WhitelistEntry>> GetWhitelistForRouteAsync(string routeId)
    {
        var persist = _config.GetValue<bool>("Whitelist:PersistToDatabase");
        if (persist)
        {
            return await _db.WhitelistEntries
                .Where(w => w.RouteId == routeId)
                .Where(w => w.ExpiresAt == null || w.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.AddedAt)
                .ToListAsync();
        }

        var ips = await _apisixClient.GetWhitelistForRouteAsync(routeId);
        return ips.Select(ip => new WhitelistEntry { IpCidr = IPAddress.Parse(ip), RouteId = routeId }).ToList();
    }

    public async Task<string> AddAsync(string routeId, WhitelistUpdateRequest request)
    {
        var ipAddress = IPAddress.Parse(request.IpCidr);
        if (_config.GetValue<bool>("Whitelist:PersistToDatabase"))
        {
            var exists = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == ipAddress);
            if (exists == null)
            {
                var entry = new WhitelistEntry
                {
                    RouteId = routeId,
                    IpCidr = ipAddress,
                    Reason = request.Reason,
                    AddedBy = request.AddedBy,
                    ExpiresAt = request.ExpiresAt,
                    AddedAt = DateTime.UtcNow
                };
                _db.WhitelistEntries.Add(entry);
                await _db.SaveChangesAsync();

                await SafeAuditLog("Create", "Whitelist", request.AddedBy,
                    new { RouteId = routeId, IpCidr = request.IpCidr, Reason = request.Reason });
            }
        }

        await SyncWhitelistToApisix(routeId);
        return $"IP {request.IpCidr} added successfully";
    }

    public async Task<string> RemoveAsync(string routeId, WhitelistUpdateRequest request)
    {
        var ipAddress = IPAddress.Parse(request.IpCidr);
        if (_config.GetValue<bool>("Whitelist:PersistToDatabase"))
        {
            var exists = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == ipAddress);
            if (exists != null)
            {
                _db.WhitelistEntries.Remove(exists);
                await _db.SaveChangesAsync();

                await SafeAuditLog("Delete", "Whitelist", request.AddedBy,
                    new { RouteId = routeId, IpCidr = request.IpCidr });
            }
        }

        await SyncWhitelistToApisix(routeId);
        return $"IP {request.IpCidr} removed successfully";
    }

    private async Task SyncWhitelistToApisix(string routeId)
    {
        var entries = await _db.WhitelistEntries
            .Where(w => w.RouteId == routeId)
            .Where(w => w.ExpiresAt == null || w.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var ipList = entries.Select(e => e.IpCidr.ToString()).Distinct().ToList();
        await _apisixClient.UpdateWhitelistForRouteAsync(routeId, ipList);
        _logger.LogInformation("Synced {Count} whitelist entries to APISIX for route {RouteId}", ipList.Count, routeId);
    }

    private async Task SafeAuditLog(string action, string resource, string? user, object details)
    {
        try
        {
            await _auditLog.LogAsync(new AuditLogEntry
            {
                Action = action,
                Resource = resource,
                User = user ?? "Unknown",
                Details = details
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log for {Action}", action);
        }
    }
}

public class WhitelistUpdateRequest
{
    public required string IpCidr { get; set; }
    public string Action { get; set; } = "add";
    public string? Reason { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
