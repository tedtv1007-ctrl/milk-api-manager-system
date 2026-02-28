using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class ServerInfoController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<ServerInfoController> _logger;

    public ServerInfoController(IApisixClient apisixClient, ILogger<ServerInfoController> logger)
    {
        _apisixClient = apisixClient;
        _logger = logger;
    }

    /// <summary>
    /// Get APISIX server info (version, hostname, boot_time etc.)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServerInfo()
    {
        try
        {
            var json = await _apisixClient.GetServerInfoAsync();
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving APISIX server info");
            return StatusCode(500, new { error = "Failed to retrieve server info" });
        }
    }

    /// <summary>
    /// Get aggregated dashboard stats: route/service/upstream/consumer/ssl counts
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var routes = await _apisixClient.GetRoutesTypedAsync();
            var services = await _apisixClient.GetServicesTypedAsync();
            var upstreams = await _apisixClient.GetUpstreamsTypedAsync();
            var consumers = await _apisixClient.GetConsumersTypedAsync();
            var ssls = await _apisixClient.GetSslsTypedAsync();
            var globalRules = await _apisixClient.GetGlobalRulesTypedAsync();

            return Ok(new
            {
                routeCount = routes.Count,
                serviceCount = services.Count,
                upstreamCount = upstreams.Count,
                consumerCount = consumers.Count,
                sslCount = ssls.Count,
                globalRuleCount = globalRules.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return Ok(new
            {
                routeCount = 0,
                serviceCount = 0,
                upstreamCount = 0,
                consumerCount = 0,
                sslCount = 0,
                globalRuleCount = 0,
                error = ex.Message
            });
        }
    }
}
