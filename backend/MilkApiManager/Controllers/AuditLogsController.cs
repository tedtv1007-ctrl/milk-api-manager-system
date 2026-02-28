using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogEntry>>> Get([FromQuery] int limit = 100)
    {
        var logs = await _auditLogService.GetLogsAsync(limit);
        return Ok(logs);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<Dictionary<string, int>>> GetStats()
    {
        var stats = await _auditLogService.GetAuditStatsAsync();
        return Ok(stats);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var csv = await _auditLogService.GetLogsCsvAsync(1000);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"audit_logs_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
