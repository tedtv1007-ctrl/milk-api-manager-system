using MilkDemo.Shared.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace MilkDemo.WebApp.Services;

public interface IGatewayApiService
{
    Task<GatewayStatusDto?> GetStatusAsync();
    Task<string?> GetRoutesAsync();
    Task<string?> GetAuditLogsAsync(int count = 20);
    Task<string?> GetBlacklistAsync();
}

public class GatewayApiService : IGatewayApiService
{
    private readonly HttpClient _httpClient;

    public GatewayApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GatewayStatusDto?> GetStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<GatewayStatusDto>("api/gateway/status");
        }
        catch
        {
            return new GatewayStatusDto { IsGatewayOnline = false, IsBackendOnline = false };
        }
    }

    public async Task<string?> GetRoutesAsync()
    {
        try
        {
            return await _httpClient.GetStringAsync("api/gateway/routes");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetAuditLogsAsync(int count = 20)
    {
        try
        {
            return await _httpClient.GetStringAsync($"api/gateway/audit-logs?count={count}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetBlacklistAsync()
    {
        try
        {
            return await _httpClient.GetStringAsync("api/gateway/blacklist");
        }
        catch
        {
            return null;
        }
    }
}
