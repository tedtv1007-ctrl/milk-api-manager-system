using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Auth;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class ApiCatalogController : ControllerBase
{
    private readonly AppDbContext _context;

    public ApiCatalogController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiServiceMetadata>>> GetCatalog()
    {
        return await _context.ApiServices.OrderBy(s => s.Name).ToListAsync();
    }

    [HttpPost("register")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> RegisterService(ApiServiceMetadata metadata)
    {
        var existing = await _context.ApiServices.FirstOrDefaultAsync(s => s.Name == metadata.Name);
        if (existing != null)
        {
            existing.Description = metadata.Description;
            existing.BasePath = metadata.BasePath;
            existing.OpenApiUrl = metadata.OpenApiUrl;
            existing.LastSyncedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ApiServices.Add(metadata);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}
