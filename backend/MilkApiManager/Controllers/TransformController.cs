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
public class TransformController : ControllerBase
{
    private static readonly HashSet<string> ValidPhases = new(StringComparer.OrdinalIgnoreCase) { "request", "response" };
    private static readonly HashSet<string> ValidOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "add_header", "remove_header", "rename_header", "rewrite_uri", "rewrite_host"
    };

    private readonly AppDbContext _db;
    private readonly ILogger<TransformController> _logger;
    private readonly IAuditLogService _auditLogService;

    public TransformController(AppDbContext db, ILogger<TransformController> logger, IAuditLogService auditLogService)
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
            var rules = await _db.RequestTransformRules.AsNoTracking().OrderBy(r => r.Priority).ToListAsync(cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transform rules");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("route/{routeId}")]
    public async Task<IActionResult> GetByRouteId(string routeId, CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _db.RequestTransformRules.AsNoTracking()
                .Where(r => r.RouteId == routeId)
                .OrderBy(r => r.Priority)
                .ToListAsync(cancellationToken);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transform rules for route {RouteId}", routeId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] RequestTransformRule rule, CancellationToken cancellationToken)
    {
        if (rule == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        if (!ValidPhases.Contains(rule.Phase))
            return BadRequest(new ApiError("ValidationError", $"Phase must be one of: {string.Join(", ", ValidPhases)}"));

        if (!ValidOperations.Contains(rule.OperationType))
            return BadRequest(new ApiError("ValidationError", $"OperationType must be one of: {string.Join(", ", ValidOperations)}"));

        try
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.CreatedBy = User.Identity?.Name ?? "System";

            _db.RequestTransformRules.Add(rule);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "TransformRule",
                User = rule.CreatedBy,
                Details = new { rule.RouteId, rule.Phase, rule.OperationType, rule.Key }
            });

            return CreatedAtAction(nameof(GetByRouteId), new { routeId = rule.RouteId }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transform rule for route {RouteId}", rule.RouteId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(int id, [FromBody] RequestTransformRule update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.RequestTransformRules.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Transform rule with ID {id} not found."));

            existing.Phase = update.Phase;
            existing.OperationType = update.OperationType;
            existing.Key = update.Key;
            existing.Value = update.Value;
            existing.Priority = update.Priority;
            existing.IsEnabled = update.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "TransformRule",
                User = User.Identity?.Name ?? "System",
                Details = new { id, update.RouteId, update.Phase, update.OperationType, update.Key }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transform rule {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.RequestTransformRules.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Transform rule with ID {id} not found."));

            _db.RequestTransformRules.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "TransformRule",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.RouteId }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transform rule {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
