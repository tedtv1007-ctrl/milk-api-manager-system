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
public class CircuitBreakerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<CircuitBreakerController> _logger;
    private readonly IAuditLogService _auditLogService;

    public CircuitBreakerController(AppDbContext db, IApisixClient apisixClient, ILogger<CircuitBreakerController> logger, IAuditLogService auditLogService)
    {
        _db = db;
        _apisixClient = apisixClient;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var configs = await _db.CircuitBreakerConfigs.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving circuit breaker configs");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{routeId}")]
    public async Task<IActionResult> GetByRouteId(string routeId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _db.CircuitBreakerConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (config == null)
                return NotFound(new ApiError("NotFound", $"Circuit breaker config for route '{routeId}' not found."));
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving circuit breaker config for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] CircuitBreakerConfig config, CancellationToken cancellationToken)
    {
        if (config == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        try
        {
            var exists = await _db.CircuitBreakerConfigs.AnyAsync(c => c.RouteId == config.RouteId, cancellationToken);
            if (exists)
                return Conflict(new ApiError("Conflict", $"Circuit breaker config for route '{config.RouteId}' already exists."));

            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            config.CreatedBy = User.Identity?.Name ?? "System";

            _db.CircuitBreakerConfigs.Add(config);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "CircuitBreaker",
                User = config.CreatedBy,
                Details = new { config.RouteId, config.BreakResponseCode, config.UnhealthyFailures }
            });

            return CreatedAtAction(nameof(GetByRouteId), new { routeId = config.RouteId }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating circuit breaker config for route {RouteId}", config.RouteId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{routeId}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(string routeId, [FromBody] CircuitBreakerConfig update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CircuitBreakerConfigs.FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Circuit breaker config for route '{routeId}' not found."));

            existing.BreakResponseCode = update.BreakResponseCode;
            existing.BreakResponseBody = update.BreakResponseBody;
            existing.MaxBreakerSec = update.MaxBreakerSec;
            existing.UnhealthyHttpStatuses = update.UnhealthyHttpStatuses;
            existing.UnhealthyFailures = update.UnhealthyFailures;
            existing.HealthyHttpStatuses = update.HealthyHttpStatuses;
            existing.HealthySuccesses = update.HealthySuccesses;
            existing.IsEnabled = update.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "CircuitBreaker",
                User = User.Identity?.Name ?? "System",
                Details = new { routeId, update.BreakResponseCode, update.UnhealthyFailures }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating circuit breaker config for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{routeId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(string routeId, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CircuitBreakerConfigs.FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Circuit breaker config for route '{routeId}' not found."));

            _db.CircuitBreakerConfigs.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "CircuitBreaker",
                User = User.Identity?.Name ?? "System",
                Details = new { routeId }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting circuit breaker config for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
