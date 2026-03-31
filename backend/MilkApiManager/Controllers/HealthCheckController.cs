using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Auth;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class HealthCheckController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<HealthCheckController> _logger;
    private readonly IAuditLogService _auditLogService;

    public HealthCheckController(AppDbContext db, ILogger<HealthCheckController> logger, IAuditLogService auditLogService)
    {
        _db = db;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var configs = await _db.HealthCheckConfigs.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check configs");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{upstreamId}")]
    public async Task<IActionResult> GetByUpstreamId(string upstreamId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _db.HealthCheckConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UpstreamId == upstreamId, cancellationToken);
            if (config == null)
                return NotFound(new ApiError("NotFound", $"Health check config for upstream '{upstreamId}' not found."));
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check config for upstream {UpstreamId}", upstreamId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] HealthCheckConfig config, CancellationToken cancellationToken)
    {
        if (config == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        if (config.ActiveIntervalSeconds <= 0)
            return BadRequest(new ApiError("ValidationError", "ActiveIntervalSeconds must be greater than 0."));

        try
        {
            var exists = await _db.HealthCheckConfigs.AnyAsync(c => c.UpstreamId == config.UpstreamId, cancellationToken);
            if (exists)
                return Conflict(new ApiError("Conflict", $"Health check config for upstream '{config.UpstreamId}' already exists."));

            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            config.CreatedBy = User.Identity?.Name ?? "System";

            _db.HealthCheckConfigs.Add(config);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "HealthCheckConfig",
                User = config.CreatedBy,
                Details = new { config.UpstreamId, config.ActiveHttpPath, config.ActiveIntervalSeconds }
            });

            return CreatedAtAction(nameof(GetByUpstreamId), new { upstreamId = config.UpstreamId }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating health check config for upstream {UpstreamId}", config.UpstreamId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{upstreamId}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(string upstreamId, [FromBody] HealthCheckConfig update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.HealthCheckConfigs.FirstOrDefaultAsync(c => c.UpstreamId == upstreamId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Health check config for upstream '{upstreamId}' not found."));

            existing.ActiveEnabled = update.ActiveEnabled;
            existing.ActiveHttpPath = update.ActiveHttpPath;
            existing.ActiveIntervalSeconds = update.ActiveIntervalSeconds;
            existing.ActiveHealthySuccesses = update.ActiveHealthySuccesses;
            existing.ActiveUnhealthyFailures = update.ActiveUnhealthyFailures;
            existing.ActiveHealthyStatuses = update.ActiveHealthyStatuses;
            existing.ActiveUnhealthyStatuses = update.ActiveUnhealthyStatuses;
            existing.ActiveTimeoutSeconds = update.ActiveTimeoutSeconds;
            existing.PassiveEnabled = update.PassiveEnabled;
            existing.PassiveHealthyStatuses = update.PassiveHealthyStatuses;
            existing.PassiveUnhealthyStatuses = update.PassiveUnhealthyStatuses;
            existing.PassiveUnhealthyTimeouts = update.PassiveUnhealthyTimeouts;
            existing.IsEnabled = update.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "HealthCheckConfig",
                User = User.Identity?.Name ?? "System",
                Details = new { upstreamId, update.ActiveHttpPath, update.ActiveIntervalSeconds }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health check config for upstream {UpstreamId}", upstreamId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{upstreamId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(string upstreamId, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.HealthCheckConfigs.FirstOrDefaultAsync(c => c.UpstreamId == upstreamId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Health check config for upstream '{upstreamId}' not found."));

            _db.HealthCheckConfigs.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "HealthCheckConfig",
                User = User.Identity?.Name ?? "System",
                Details = new { upstreamId }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting health check config for upstream {UpstreamId}", upstreamId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
