using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("metrics/slo")]
public class SloMetricsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly BlacklistConsistencyService _blacklistConsistencyService;
    private readonly ILogger<SloMetricsController> _logger;
    private readonly int _windowMinutes;

    public SloMetricsController(
        AppDbContext dbContext,
        BlacklistConsistencyService blacklistConsistencyService,
        IConfiguration configuration,
        ILogger<SloMetricsController> logger)
    {
        _dbContext = dbContext;
        _blacklistConsistencyService = blacklistConsistencyService;
        _logger = logger;
        _windowMinutes = Math.Max(configuration.GetValue<int?>("Slo:WindowMinutes") ?? 15, 1);
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces("text/plain")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-_windowMinutes);

        var totalCount = await _dbContext.AuditLogs
            .Where(x => x.Timestamp >= windowStart)
            .CountAsync(cancellationToken);

        var successCount = await _dbContext.AuditLogs
            .Where(x => x.Timestamp >= windowStart)
            .Where(x => x.StatusCode >= 200 && x.StatusCode < 400)
            .CountAsync(cancellationToken);

        var successRatePercent = totalCount == 0
            ? 100d
            : (double)successCount / totalCount * 100d;

        var latencies = await _dbContext.SyncOutboxEntries
            .Where(x => x.ProcessedAt != null)
            .Where(x => x.ProcessedAt >= windowStart)
            .Select(x => (x.ProcessedAt!.Value - x.CreatedAt).TotalSeconds)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var p95Seconds = Percentile(latencies, 0.95);

        double driftTotal = 0;
        double driftDatabaseOnly = 0;
        double driftGatewayOnly = 0;

        try
        {
            var report = await _blacklistConsistencyService.GetBlacklistDriftReportAsync(cancellationToken);
            driftDatabaseOnly = report.DatabaseOnly.Count;
            driftGatewayOnly = report.GatewayOnly.Count;
            driftTotal = driftDatabaseOnly + driftGatewayOnly;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect blacklist drift for SLO metrics endpoint.");
        }

        var payload = string.Join("\n", new[]
        {
            "# HELP milk_control_plane_success_rate_percent Control-plane success rate percentage over recent audit log window.",
            "# TYPE milk_control_plane_success_rate_percent gauge",
            $"milk_control_plane_success_rate_percent {Format(successRatePercent)}",
            "# HELP milk_sync_latency_p95_seconds P95 outbox sync latency in seconds over recent processed outbox window.",
            "# TYPE milk_sync_latency_p95_seconds gauge",
            $"milk_sync_latency_p95_seconds {Format(p95Seconds)}",
            "# HELP milk_blacklist_drift_count Total blacklist drift count between database and gateway.",
            "# TYPE milk_blacklist_drift_count gauge",
            $"milk_blacklist_drift_count {Format(driftTotal)}",
            "# HELP milk_blacklist_drift_database_only_count Blacklist entries present only in database.",
            "# TYPE milk_blacklist_drift_database_only_count gauge",
            $"milk_blacklist_drift_database_only_count {Format(driftDatabaseOnly)}",
            "# HELP milk_blacklist_drift_gateway_only_count Blacklist entries present only in gateway.",
            "# TYPE milk_blacklist_drift_gateway_only_count gauge",
            $"milk_blacklist_drift_gateway_only_count {Format(driftGatewayOnly)}"
        }) + "\n";

        return Content(payload, "text/plain; version=0.0.4");
    }

    private static double Percentile(IReadOnlyList<double> values, double p)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(values.Count * p) - 1;
        index = Math.Clamp(index, 0, values.Count - 1);
        return values[index];
    }

    private static string Format(double value)
    {
        return value.ToString("0.########", CultureInfo.InvariantCulture);
    }
}
