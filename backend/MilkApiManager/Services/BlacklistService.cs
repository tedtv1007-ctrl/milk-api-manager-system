using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using System.Net;

namespace MilkApiManager.Services;

/// <summary>
/// Domain service encapsulating blacklist business logic.
/// Extracted from BlacklistController to achieve SRP and testability.
/// </summary>
public interface IBlacklistService
{
    Task<List<BlacklistEntry>> GetBlacklistAsync();
    Task<string> AddAsync(BlacklistUpdateRequest request);
    Task<string> RemoveAsync(BlacklistUpdateRequest request);
}

public class BlacklistService : IBlacklistService
{
    private readonly IApisixClient _apisixClient;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditLogService _auditLog;
    private readonly ApisixSyncOutboxService _outboxService;
    private readonly ILogger<BlacklistService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1); // E-6: prevent race condition

    public BlacklistService(
        IApisixClient apisixClient,
        AppDbContext db,
        IConfiguration config,
        IAuditLogService auditLog,
        ApisixSyncOutboxService outboxService,
        ILogger<BlacklistService> logger)
    {
        _apisixClient = apisixClient;
        _db = db;
        _config = config;
        _auditLog = auditLog;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task<List<BlacklistEntry>> GetBlacklistAsync()
    {
        var persist = _config.GetValue<bool>("Blacklist:PersistToDatabase");
        if (persist)
        {
            return await _db.BlacklistEntries.OrderByDescending(e => e.AddedAt).ToListAsync();
        }

        var blacklist = await _apisixClient.GetBlacklistAsync();
        return blacklist.Select(ip => new BlacklistEntry { IpOrCidr = ip }).ToList();
    }

    public async Task<string> AddAsync(BlacklistUpdateRequest request)
    {
        if (!IsValidIpOrCidr(request.Ip))
        {
            throw new ArgumentException("Invalid IP or CIDR format");
        }

        await _lock.WaitAsync();
        try
        {
            var ipAddress = IPAddress.Parse(request.Ip.Split('/')[0]); // Handle CIDR if needed for parsing, though EF uses converter
            // For simple IPAddress.Parse, CIDR needs special handling. 
            // Our converter handles parsing from string.
            var searchIp = request.Ip; // Use string for set comparison since gateway uses string
            
            var blacklist = await _apisixClient.GetBlacklistAsync();
            var blacklistSet = new HashSet<string>(blacklist);
            blacklistSet.Add(request.Ip);

            if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
            {
                var targetIp = request.Ip;
                var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == targetIp);
                if (exists == null)
                {
                    var entry = new BlacklistEntry
                    {
                        IpOrCidr = targetIp,
                        Reason = request.Reason,
                        AddedBy = request.AddedBy,
                        ExpiresAt = request.ExpiresAt,
                        AddedAt = DateTime.UtcNow
                    };
                    _db.BlacklistEntries.Add(entry);
                    await _db.SaveChangesAsync();

                    await SafeAuditLog("Blacklist.Add", request.AddedBy, new { Ip = request.Ip, Reason = request.Reason, ExpiresAt = request.ExpiresAt });
                }
            }

            await SyncToGateway(blacklistSet);
            return $"IP {request.Ip} added successfully";
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> RemoveAsync(BlacklistUpdateRequest request)
    {
        if (!IsValidIpOrCidr(request.Ip))
        {
            throw new ArgumentException("Invalid IP or CIDR format");
        }

        await _lock.WaitAsync();
        try
        {
            var targetIp = request.Ip;
            var blacklist = await _apisixClient.GetBlacklistAsync();
            var blacklistSet = new HashSet<string>(blacklist);
            blacklistSet.Remove(request.Ip);

            if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
            {
                var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == targetIp);
                if (exists != null)
                {
                    _db.BlacklistEntries.Remove(exists);
                    await _db.SaveChangesAsync();

                    await SafeAuditLog("Blacklist.Remove", request.AddedBy, new { Ip = exists.IpOrCidr, Reason = exists.Reason, ExpiresAt = exists.ExpiresAt });
                }
            }

            await SyncToGateway(blacklistSet);
            return $"IP {request.Ip} removed successfully";
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsValidIpOrCidr(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        if (value.Contains('/'))
        {
            var parts = value.Split('/');
            if (parts.Length != 2) return false;
            if (!IPAddress.TryParse(parts[0], out _)) return false;
            if (!int.TryParse(parts[1], out int mask) || mask < 0 || mask > 128) return false;
            return true;
        }

        return IPAddress.TryParse(value, out _);
    }

    private async Task SyncToGateway(HashSet<string> blacklistSet)
    {
        if (_config.GetValue<bool>("Sync:Blacklist:UseOutbox"))
        {
            await _outboxService.EnqueueBlacklistSyncAsync(blacklistSet.ToList());
        }
        else
        {
            await _apisixClient.UpdateBlacklistAsync(blacklistSet.ToList());
        }
    }

    private async Task SafeAuditLog(string action, string? user, object details)
    {
        try
        {
            await _auditLog.LogAsync(new AuditLogEntry
            {
                Action = action,
                Resource = "Blacklist",
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

public class BlacklistUpdateRequest
{
    public required string Ip { get; set; }
    public string Action { get; set; } = "add";
    public string? Reason { get; set; }
    public string? AddedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
