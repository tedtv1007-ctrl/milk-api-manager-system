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
public class ApiLifecycleController : ControllerBase
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "planning", "active", "deprecated", "retired"
    };

    private readonly AppDbContext _db;
    private readonly ILogger<ApiLifecycleController> _logger;
    private readonly IAuditLogService _auditLogService;

    public ApiLifecycleController(AppDbContext db, ILogger<ApiLifecycleController> logger, IAuditLogService auditLogService)
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
            var entries = await _db.ApiLifecycleEntries.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API lifecycle entries");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("api/{apiIdentifier}")]
    public async Task<IActionResult> GetByApiIdentifier(string apiIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _db.ApiLifecycleEntries.AsNoTracking()
                .Where(e => e.ApiIdentifier == apiIdentifier)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lifecycle entries for API {ApiIdentifier}", apiIdentifier);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _db.ApiLifecycleEntries.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entry == null)
                return NotFound(new ApiError("NotFound", $"API lifecycle entry with ID {id} not found."));
            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API lifecycle entry {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("deprecated")]
    public async Task<IActionResult> GetDeprecated(CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _db.ApiLifecycleEntries.AsNoTracking()
                .Where(e => e.Status == "deprecated")
                .ToListAsync(cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deprecated API lifecycle entries");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Create([FromBody] ApiLifecycleEntry entry, CancellationToken cancellationToken)
    {
        if (entry == null)
            return BadRequest(new ApiError("ValidationError", "Request body is required."));

        if (!ValidStatuses.Contains(entry.Status))
            return BadRequest(new ApiError("ValidationError", $"Status must be one of: {string.Join(", ", ValidStatuses)}"));

        try
        {
            var exists = await _db.ApiLifecycleEntries
                .AnyAsync(e => e.ApiIdentifier == entry.ApiIdentifier && e.Version == entry.Version, cancellationToken);
            if (exists)
                return Conflict(new ApiError("Conflict", $"API lifecycle entry for '{entry.ApiIdentifier}' version '{entry.Version}' already exists."));

            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;
            entry.CreatedBy = User.Identity?.Name ?? "System";

            if (entry.Status == "active")
                entry.PublishedAt = DateTime.UtcNow;

            _db.ApiLifecycleEntries.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Create",
                Resource = "ApiLifecycle",
                User = entry.CreatedBy,
                Details = new { entry.ApiIdentifier, entry.Version, entry.Status }
            });

            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API lifecycle entry for {ApiIdentifier} {Version}", entry.ApiIdentifier, entry.Version);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Update(int id, [FromBody] ApiLifecycleEntry update, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.ApiLifecycleEntries.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"API lifecycle entry with ID {id} not found."));

            existing.Status = update.Status;
            existing.OwnerTeam = update.OwnerTeam;
            existing.SuccessorUrl = update.SuccessorUrl;
            existing.DeprecationNotice = update.DeprecationNotice;
            existing.SunsetAt = update.SunsetAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                Resource = "ApiLifecycle",
                User = User.Identity?.Name ?? "System",
                Details = new { id, update.ApiIdentifier, update.Version, update.Status }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API lifecycle entry {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost("{id:int}/deprecate")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> Deprecate(int id, [FromQuery] string notice, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.ApiLifecycleEntries.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"API lifecycle entry with ID {id} not found."));

            existing.Status = "deprecated";
            existing.DeprecatedAt = DateTime.UtcNow;
            existing.DeprecationNotice = notice;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Deprecate",
                Resource = "ApiLifecycle",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.ApiIdentifier, existing.Version, notice }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deprecating API lifecycle entry {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPost("{id:int}/retire")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Retire(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.ApiLifecycleEntries.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"API lifecycle entry with ID {id} not found."));

            existing.Status = "retired";
            existing.RetiredAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Retire",
                Resource = "ApiLifecycle",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.ApiIdentifier, existing.Version }
            });

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retiring API lifecycle entry {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _db.ApiLifecycleEntries.FindAsync(new object[] { id }, cancellationToken);
            if (existing == null)
                return NotFound(new ApiError("NotFound", $"API lifecycle entry with ID {id} not found."));

            _db.ApiLifecycleEntries.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "ApiLifecycle",
                User = User.Identity?.Name ?? "System",
                Details = new { id, existing.ApiIdentifier, existing.Version }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API lifecycle entry {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
