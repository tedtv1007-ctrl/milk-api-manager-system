using MilkApiManager.Models;

namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for audit log operations (write, query, export, SIEM shipping).
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry);
    Task<List<AuditLogEntry>> GetLogsAsync(int limit = 100);
    Task<Dictionary<string, int>> GetAuditStatsAsync();
    Task<string> GetLogsCsvAsync(int limit = 1000);
    Task ShipLogsToSIEM(AuditLogEntry entry);
}
