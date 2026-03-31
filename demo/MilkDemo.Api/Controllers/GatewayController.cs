using MilkDemo.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MilkDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GatewayController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("MilkApiManager");
        _configuration = configuration;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetGatewayStatus()
    {
        var status = new GatewayStatusDto
        {
            CheckedAt = DateTime.UtcNow
        };

        // Check API Manager backend health
        try
        {
            var backendUrl = _configuration["MilkApiManager:BaseUrl"] ?? "http://localhost:5001";
            var response = await _httpClient.GetAsync($"{backendUrl}/health/ready");
            status.IsBackendOnline = response.IsSuccessStatusCode;
        }
        catch
        {
            status.IsBackendOnline = false;
        }

        // Check APISIX gateway health
        try
        {
            var gatewayUrl = _configuration["MilkApiManager:GatewayUrl"] ?? "http://localhost:9080";
            var response = await _httpClient.GetAsync(gatewayUrl);
            status.IsGatewayOnline = true; // If we get any response, gateway is online
        }
        catch
        {
            status.IsGatewayOnline = false;
        }

        return Ok(status);
    }

    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes()
    {
        try
        {
            var backendUrl = _configuration["MilkApiManager:BaseUrl"] ?? "http://localhost:5001";
            var apiKey = _configuration["MilkApiManager:ApiKey"] ?? "";
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{backendUrl}/api/Route");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("X-API-KEY", apiKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(503, new ApiErrorDto { Code = "ServiceUnavailable", Message = $"Cannot reach API Manager: {ex.Message}" });
        }
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int count = 20)
    {
        try
        {
            var backendUrl = _configuration["MilkApiManager:BaseUrl"] ?? "http://localhost:5001";
            var apiKey = _configuration["MilkApiManager:ApiKey"] ?? "";
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{backendUrl}/api/AuditLogs?count={count}");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("X-API-KEY", apiKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(503, new ApiErrorDto { Code = "ServiceUnavailable", Message = $"Cannot reach API Manager: {ex.Message}" });
        }
    }

    [HttpGet("blacklist")]
    public async Task<IActionResult> GetBlacklist()
    {
        try
        {
            var backendUrl = _configuration["MilkApiManager:BaseUrl"] ?? "http://localhost:5001";
            var apiKey = _configuration["MilkApiManager:ApiKey"] ?? "";
            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{backendUrl}/api/Blacklist");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("X-API-KEY", apiKey);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(503, new ApiErrorDto { Code = "ServiceUnavailable", Message = $"Cannot reach API Manager: {ex.Message}" });
        }
    }
}
