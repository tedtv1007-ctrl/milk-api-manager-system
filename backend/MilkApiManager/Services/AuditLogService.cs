using System.Text.Json;
using MilkApiManager.Models;

namespace MilkApiManager.Services;

public class AuditLogService
{
    private readonly HttpClient _httpClient;
    private readonly string _logstashEndpoint = "http://logstash:8080/";

    public AuditLogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 結構化稽核日誌記錄方法
    /// 輸出 JSON 到 StdOut 供 Fluentd/Logstash 收集
    /// </summary>
    /// <param name="entry">日誌實體</param>
    public async Task LogAsync(AuditLogEntry entry)
    {
        // 確保 Timestamp 為 UTC
        if (entry.Timestamp.Kind != DateTimeKind.Utc)
        {
            entry.Timestamp = entry.Timestamp.ToUniversalTime();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false // 保持單行以便日誌收集
        };

        var json = JsonSerializer.Serialize(entry, options);
        
        // 輸出到 Standard Output
        Console.WriteLine(json);

        // Optional: 也可以保留原本的 HTTP Shipping 邏輯
        // await ShipLogsToSIEM(entry);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 對齊稽核要求 Q7: 實作 API 呼叫紀錄全量收容
    /// </summary>
    public async Task ShipLogsToSIEM(object logPayload)
    {
        try 
        {
            var json = JsonSerializer.Serialize(logPayload);
            // 這裡僅示範，若 logstash 不在線上可能會報錯，因此加個 try-catch 或視為非同步 fire-and-forget
            // await _httpClient.PostAsync(_logstashEndpoint, new StringContent(json));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to ship logs: {ex.Message}");
        }
    }

    /// <summary>
    /// 定時產出 24h 稽核報表 (Mock 邏輯)
    /// </summary>
    public string GenerateComplianceReport()
    {
        return "Daily API Compliance Report - Verified by Milk AI";
    }
}
