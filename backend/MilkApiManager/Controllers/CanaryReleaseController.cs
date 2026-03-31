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
public class CanaryReleaseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CanaryReleaseController> _logger;
    private readonly IAuditLogService _auditLogService;

    public CanaryReleaseController(AppDbContext db, ILogger<CanaryReleaseController> logger, IAuditLogService auditLogService)
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
            var releases = await _db.CanaryReleases.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(releases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving canary releases");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var release = await _db.CanaryReleases.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (release == null)
                return NotFound(new ApiError("NotFound", $"Canary release with ID {id} not found."));
            return Ok(release);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving canary release {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] CanaryRelease release, CancellationToken cancellationToken)
    {
        if (release == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        if (release.StableWeight + release.CanaryWeight != 100)
            return BadRequest(new ApiError("ValidationError", "StableWeight + CanaryWeight must equal 100."));

        try
        {
            release.Status = "active";
            release.CreatedAt = DateTime.UtcNow;
            release.UpdatedAt = DateTime.UtcNow;
            release.CreatedBy = User.Identity?.Name ?? "System";

            _db.CanaryReleases.Add(release);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "CanaryRelease",
                User = release.CreatedBy,
                Details = new { release.RouteId, release.Name, release.StableWeight, release.CanaryWeight }
            });

            return CreatedAtAction(nameof(GetById), new { id = release.Id }, release);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating canary release for route {RouteId}", release.RouteId);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(int id, [FromBody] CanaryRelease update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CanaryReleases.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Canary release with ID {id} not found."));

            existing.StableWeight = update.StableWeight;
            existing.CanaryWeight = update.CanaryWeight;
            existing.MatchRulesJson = update.MatchRulesJson;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "CanaryRelease",
                User = User.Identity?.Name ?? "System",
                Details = new { id, update.StableWeight, update.CanaryWeight }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating canary release {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost("{id:int}/rollback")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Rollback(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CanaryReleases.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Canary release with ID {id} not found."));

            existing.Status = "rolled_back";
            existing.StableWeight = 100;
            existing.CanaryWeight = 0;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Rollback",
                Resource = "CanaryRelease",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.Name }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back canary release {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost("{id:int}/promote")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Promote(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CanaryReleases.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Canary release with ID {id} not found."));

            existing.Status = "completed";
            existing.StableWeight = 0;
            existing.CanaryWeight = 100;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Promote",
                Resource = "CanaryRelease",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.Name }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting canary release {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.CanaryReleases.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"Canary release with ID {id} not found."));

            _db.CanaryReleases.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "CanaryRelease",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.Name }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting canary release {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
