using System.Text.Json;

namespace Milk.ApiManager.Services;

public class AuditLogService
{
    private readonly HttpClient _httpClient;
    private readonly string _logstashEndpoint = "http://logstash-svc:8080/apisix/logs";

    public AuditLogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 對齊稽核要求 Q7: 實作 API 呼叫紀錄全量收容
    /// </summary>
    public async Task ShipLogsToSIEM(object logPayload)
    {
        var json = JsonSerializer.Serialize(logPayload);
        await _httpClient.PostAsync(_logstashEndpoint, new StringContent(json));
        Console.WriteLine($"[AUDIT] Log shipped to SIEM: {DateTime.Now}");
    }

    /// <summary>
    /// 定時產出 24h 稽核報表 (Mock 邏輯)
    /// </summary>
    public string GenerateComplianceReport()
    {
        return "Daily API Compliance Report - Verified by Milk AI";
    }
}
