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
public class GlobalRuleController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<GlobalRuleController> _logger;
    private readonly IAuditLogService _auditLogService;

    public GlobalRuleController(IApisixClient apisixClient, ILogger<GlobalRuleController> logger, IAuditLogService auditLogService)
    {
        _apisixClient = apisixClient;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGlobalRules()
    {
        try
        {
            var rules = await _apisixClient.GetGlobalRulesTypedAsync();
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global rules");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> CreateOrUpdateGlobalRule(string id, [FromBody] GlobalRule ruleConfig)
    {
        if (ruleConfig == null) return BadRequest("Invalid global rule configuration");

        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            ruleConfig.Id = id;
            await _apisixClient.CreateGlobalRuleAsync(id, ruleConfig);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CreateOrUpdate",
                Resource = "GlobalRule",
                User = currentUser,
                Details = new { RuleId = id, Plugins = ruleConfig.Plugins?.Keys }
            });

            return Ok(ruleConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating global rule {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteGlobalRule(string id)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            await _apisixClient.DeleteGlobalRuleAsync(id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "GlobalRule",
                User = currentUser,
                Details = new { RuleId = id }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting global rule {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
