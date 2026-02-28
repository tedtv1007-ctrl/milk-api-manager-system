using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Auth;
using MilkApiManager.Data;
using MilkApiManager.Models;
using System.Diagnostics;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class TestExecutionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TestExecutionController> _logger;

    public TestExecutionController(AppDbContext context, HttpClient httpClient, ILogger<TestExecutionController> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpGet("scenarios/{serviceId}")]
    public async Task<ActionResult<IEnumerable<ApiTestScenario>>> GetScenarios(int serviceId)
    {
        return await _context.ApiTestScenarios
            .Where(s => s.ServiceId == serviceId)
            .ToListAsync();
    }

    [HttpPost("scenarios")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<ActionResult<ApiTestScenario>> CreateScenario(ApiTestScenario scenario)
    {
        _context.ApiTestScenarios.Add(scenario);
        await _context.SaveChangesAsync();
        return Ok(scenario);
    }

    [HttpPost("run/{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> RunTest(int id)
    {
        var scenario = await _context.ApiTestScenarios.FindAsync(id);
        if (scenario == null) return NotFound();

        var service = await _context.ApiServices.FindAsync(scenario.ServiceId);
        if (service == null) return BadRequest("Service metadata not found.");

        // Construct target URL (through APISIX)
        // APISIX is at http://apisix:9080, and BasePath is e.g. /api
        var apisixUrl = Environment.GetEnvironmentVariable("APISIX_PUBLIC_URL") ?? "http://apisix:9080";
        var targetUrl = $"{apisixUrl.TrimEnd('/')}{service.BasePath.TrimEnd('/')}/{scenario.Endpoint.TrimStart('/')}";

        _logger.LogInformation($"Executing API Test: {scenario.Name} -> {targetUrl}");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var request = new HttpRequestMessage(new HttpMethod(scenario.HttpMethod), targetUrl);
            var response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            var success = (int)response.StatusCode == scenario.ExpectedStatusCode;
            scenario.LastResult = success ? "PASS" : $"FAIL ({(int)response.StatusCode})";
            scenario.LastRunAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                status = scenario.LastResult, 
                latencyMs = stopwatch.ElapsedMilliseconds,
                statusCode = (int)response.StatusCode
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            scenario.LastResult = $"ERR: {ex.Message}";
            scenario.LastRunAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { status = "ERROR", error = ex.Message });
        }
    }
}
