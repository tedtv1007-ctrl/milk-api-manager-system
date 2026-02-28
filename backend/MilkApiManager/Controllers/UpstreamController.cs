using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class UpstreamController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<UpstreamController> _logger;
    private readonly IAuditLogService _auditLogService;

    public UpstreamController(IApisixClient apisixClient, ILogger<UpstreamController> logger, IAuditLogService auditLogService)
    {
        _apisixClient = apisixClient;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUpstreams()
    {
        try
        {
            var upstreams = await _apisixClient.GetUpstreamsTypedAsync();
            return Ok(upstreams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upstreams");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUpstream(string id)
    {
        try
        {
            var upstream = await _apisixClient.GetUpstreamAsync(id);
            if (upstream == null) return NotFound();
            return Ok(upstream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upstream {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> CreateOrUpdateUpstream(string id, [FromBody] StandaloneUpstream upstreamConfig)
    {
        if (upstreamConfig == null) return BadRequest("Invalid upstream configuration");

        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            upstreamConfig.Id = id;
            await _apisixClient.CreateUpstreamAsync(id, upstreamConfig);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CreateOrUpdate",
                Resource = "Upstream",
                User = currentUser,
                Details = new { UpstreamId = id, Config = upstreamConfig }
            });

            return Ok(upstreamConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating upstream {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteUpstream(string id)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            await _apisixClient.DeleteUpstreamAsync(id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "Upstream",
                User = currentUser,
                Details = new { UpstreamId = id }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting upstream {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
