using Microsoft.Extensions.Logging;
using MilkApiManager.Models;

namespace MilkApiManager.Services;

public class SecurityAutomationService
{
    private readonly ApisixClient _apisixClient;
    private readonly IVaultService _vaultService;
    private readonly ILogger<SecurityAutomationService> _logger;

    public SecurityAutomationService(ApisixClient apisixClient, IVaultService vaultService, ILogger<SecurityAutomationService> logger)
    {
        _apisixClient = apisixClient;
        _vaultService = vaultService;
        _logger = logger;
    }

    /// <summary>
    /// 定時執行 API 密鑰輪轉與通知機制 (Issue #32)
    /// </summary>
    public async Task CheckAndRotateKeys()
    {
        _logger.LogInformation("Starting API Key lifecycle check...");

        // 模擬從資料庫讀取金鑰資訊
        var keys = new List<ApiKey>
        {
            new ApiKey { Owner = "tsp-partner-01", ExpiresAt = DateTime.UtcNow.AddDays(5), ContactEmail = "dev@tsp.com", LastRotatedAt = DateTime.UtcNow.AddDays(-85), KeyHash = "mock_hash_1", Scopes = "read" },
            new ApiKey { Owner = "payment-gateway", ExpiresAt = DateTime.UtcNow.AddDays(-1), ContactEmail = "admin@pay.com", LastRotatedAt = DateTime.UtcNow.AddDays(-91), KeyHash = "mock_hash_2", Scopes = "admin" }
        };

        foreach (var key in keys)
        {
            var daysToExpiry = (key.ExpiresAt - DateTime.UtcNow).TotalDays;

            if (daysToExpiry <= 0)
            {
                // 已過期，執行輪轉
                _logger.LogWarning($"Key for {key.Owner} has expired. Executing auto-rotation.");
                await _vaultService.RotateApiKeyAsync(key.Owner);
                await NotifyMattermost(key.Owner, "API Key has been automatically rotated due to expiration.");
            }
            else if (daysToExpiry <= 7)
            {
                // 即將過期，發送通知
                _logger.LogInformation($"Key for {key.Owner} will expire in {Math.Round(daysToExpiry, 1)} days. Sending notification.");
                await NotifyMattermost(key.Owner, $"API Key will expire in {Math.Round(daysToExpiry, 1)} days. Please prepare for rotation.");
            }
        }
    }

    private async Task NotifyMattermost(string owner, string message)
    {
        // 實作 Webhook 推送到 Mattermost 安全頻道 (Issue #32)
        _logger.LogInformation($"[Notification] To: {owner}, Msg: {message}");
        // 實際程式碼會呼叫 HttpClient 發送 POST 到 Mattermost Hook URL
    }

    /// <summary>
    /// 實作 AIOps 自動阻斷 (Issue #17)
    /// </summary>
    public async Task BlockMaliciousIP(string ip, string reason)
    {
        Console.WriteLine($"[SECURITY] 偵測到異常流量，IP: {ip}, 原因: {reason}. 執行自動阻斷...");
        
        // 呼叫之前龍蝦夥伴實作的 ApisixClient 進行插件更新
        // 這裡示範更新全域 IP 限制名單
        await _apisixClient.UpdateGlobalPlugin("ip-restriction", new {
            blocklist = new[] { ip }
        });

        await NotifyMattermost("SystemAdmin", $"IP {ip} blocked due to {reason}");
    }
}
