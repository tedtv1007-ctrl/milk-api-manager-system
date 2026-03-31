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
public class CachePolicyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CachePolicyController> _logger;
    private readonly IAuditLogService _auditLogService;

    public CachePolicyController(AppDbContext db, ILogger<CachePolicyController> logger, IAuditLogService auditLogService)
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
            var policies = await _db.CachePolicies.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache policies");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{routeId}")]
    public async Task<IActionResult> GetByRouteId(string routeId, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _db.CachePolicies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (policy == null)
                return NotFound(new ApiError("NotFound", $"Cache policy for route '{routeId}' not found."));
            return Ok(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache policy for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] CachePolicy policy, CancellationToken cancellationToken)
    {
        if (policy == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        if (policy.CacheTtlSeconds < 0)
            return BadRequest(new ApiError("ValidationError", "CacheTtlSeconds must be non-negative."));

        try
        {
            var exists = await _db.CachePolicies.AnyAsync(c => c.RouteId == policy.RouteId, cancellationToken);
            if (exists)
                return Conflict(new ApiError("Conflict", $"Cache policy for route '{policy.RouteId}' already exists."));

            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;
            policy.CreatedBy = User.Identity?.Name ?? "System";

            _db.CachePolicies.Add(policy);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "CachePolicy",
                User = policy.CreatedBy,
                Details = new { policy.RouteId, policy.CacheTtlSeconds, policy.CacheStrategy }
            });

            return CreatedAtAction(nameof(GetByRouteId), new { routeId = policy.RouteId }, policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cache policy for route {RouteId}", policy.RouteId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{routeId}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(string routeId, [FromBody] CachePolicy update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CachePolicies.FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Cache policy for route '{routeId}' not found."));

            existing.CacheTtlSeconds = update.CacheTtlSeconds;
            existing.CacheHttpMethods = update.CacheHttpMethods;
            existing.CacheHttpStatuses = update.CacheHttpStatuses;
            existing.CacheStrategy = update.CacheStrategy;
            existing.CacheKey = update.CacheKey;
            existing.VaryHeaders = update.VaryHeaders;
            existing.IsEnabled = update.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "CachePolicy",
                User = User.Identity?.Name ?? "System",
                Details = new { routeId, update.CacheTtlSeconds, update.CacheStrategy }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache policy for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{routeId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(string routeId, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CachePolicies.FirstOrDefaultAsync(c => c.RouteId == routeId, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Cache policy for route '{routeId}' not found."));

            _db.CachePolicies.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "CachePolicy",
                User = User.Identity?.Name ?? "System",
                Details = new { routeId }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache policy for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
