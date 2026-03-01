using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;

namespace MilkApiManager.Services;

/// <summary>
/// 背景服務：定期檢查即將到期與已過期的 API 金鑰，
/// 自動停用過期金鑰並發送通知提醒。
/// </summary>
public class KeyRotationBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeyRotationBackgroundService> _logger;
    private Timer? _timer;

    /// <summary>
    /// 檢查間隔（預設每小時）
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 到期前幾天開始發送提醒（預設 7 天）
    /// </summary>
    public int WarningDaysBeforeExpiry { get; set; } = 7;

    public KeyRotationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<KeyRotationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KeyRotationBackgroundService started. Check interval: {Interval}", CheckInterval);
        _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(30), CheckInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KeyRotationBackgroundService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            await CheckAndProcessKeysAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in KeyRotationBackgroundService.DoWork");
        }
    }

    /// <summary>
    /// 核心邏輯：檢查即將到期與已過期的金鑰
    /// </summary>
    public async Task CheckAndProcessKeysAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        var now = DateTime.UtcNow;
        var warningThreshold = now.AddDays(WarningDaysBeforeExpiry);

        // 1. 自動停用已過期的金鑰
        var expiredKeys = await dbContext.ApiKeys
            .Where(k => k.IsActive && k.ExpiresAt <= now)
            .ToListAsync();

        foreach (var key in expiredKeys)
        {
            key.IsActive = false;
            _logger.LogWarning("API Key for {Owner} (ID: {Id}) has expired and been deactivated.", key.Owner, key.Id);
        }

        if (expiredKeys.Any())
        {
            await dbContext.SaveChangesAsync();

            if (notificationService != null)
            {
                await notificationService.AlertAsync(
                    "🔑 API 金鑰已過期停用",
                    $"共 {expiredKeys.Count} 組金鑰已過期並自動停用：\n" +
                    string.Join("\n", expiredKeys.Select(k => $"- {k.Owner} (到期: {k.ExpiresAt:yyyy-MM-dd})")),
                    isCritical: true);
            }
        }

        // 2. 發送即將到期提醒（7 天內到期且仍為有效的金鑰）
        var soonToExpireKeys = await dbContext.ApiKeys
            .Where(k => k.IsActive && k.ExpiresAt > now && k.ExpiresAt <= warningThreshold)
            .ToListAsync();

        if (soonToExpireKeys.Any() && notificationService != null)
        {
            await notificationService.AlertAsync(
                "⏰ API 金鑰即將到期提醒",
                $"以下 {soonToExpireKeys.Count} 組金鑰將在 {WarningDaysBeforeExpiry} 天內到期，請及時輪替：\n" +
                string.Join("\n", soonToExpireKeys.Select(k =>
                    $"- {k.Owner} (到期: {k.ExpiresAt:yyyy-MM-dd}, 剩餘 {(int)(k.ExpiresAt - now).TotalDays} 天)")),
                isCritical: false);
        }

        _logger.LogInformation(
            "Key rotation check completed. Expired: {Expired}, Soon-to-expire: {Warning}",
            expiredKeys.Count, soonToExpireKeys.Count);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
