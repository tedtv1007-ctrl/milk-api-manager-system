using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Auth;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using System.Text.Json;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
public class PiiMaskingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<PiiMaskingController> _logger;

    public PiiMaskingController(AppDbContext context, IApisixClient apisixClient, ILogger<PiiMaskingController> logger)
    {
        _context = context;
        _apisixClient = apisixClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all PII masking rules.
    /// </summary>
    /// <returns>A list of PII masking rules.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PiiMaskingRule>))]
    public async Task<ActionResult<IEnumerable<PiiMaskingRule>>> GetRules()
    {
        return await _context.PiiMaskingRules.ToListAsync();
    }

    /// <summary>
    /// Creates a new PII masking rule governing APISIX traffic.
    /// </summary>
    /// <param name="rule">The rule definition.</param>
    /// <returns>The created rule.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PiiMaskingRule))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PiiMaskingRule>> CreateRule(PiiMaskingRule rule)
    {
        if (string.IsNullOrEmpty(rule.RegexPattern))
        {
            return BadRequest(new ApiError("ValidationError", "Regex pattern is required"));
        }

        try
        {
            // Validate regex format
            _ = new System.Text.RegularExpressions.Regex(rule.RegexPattern);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ApiError("ValidationError", "Invalid Regex pattern"));
        }

        rule.UpdatedAt = DateTime.UtcNow;
        _context.PiiMaskingRules.Add(rule);
        await _context.SaveChangesAsync();

        await SyncToApisix(rule.RouteId);
        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    /// <summary>
    /// Updates an existing PII masking rule.
    /// </summary>
    /// <param name="id">The rule ID to update.</param>
    /// <param name="rule">The new rule content.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRule(int id, PiiMaskingRule rule)
    {
        if (id != rule.Id) return BadRequest();

        if (string.IsNullOrEmpty(rule.RegexPattern))
        {
            return BadRequest(new ApiError("ValidationError", "Regex pattern is required"));
        }

        try
        {
            // Validate regex format
            _ = new System.Text.RegularExpressions.Regex(rule.RegexPattern);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ApiError("ValidationError", "Invalid Regex pattern"));
        }

        rule.UpdatedAt = DateTime.UtcNow;
        _context.Entry(rule).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            await SyncToApisix(rule.RouteId);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RuleExists(id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a PII masking rule from the system and APISIX.
    /// </summary>
    /// <param name="id">The rule ID to delete.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.PiiMaskingRules.FindAsync(id);
        if (rule == null) return NotFound();

        var routeId = rule.RouteId;
        _context.PiiMaskingRules.Remove(rule);
        await _context.SaveChangesAsync();

        await SyncToApisix(routeId);
        return NoContent();
    }

    private async Task SyncToApisix(string routeId)
    {
        try
        {
            var activeRules = await _context.PiiMaskingRules
                .Where(r => r.RouteId == routeId && r.IsActive)
                .ToListAsync();

            var route = await _apisixClient.GetRouteAsync(routeId);
            if (route == null)
            {
                _logger.LogWarning("Route {RouteId} not found in APISIX. Skipping PII sync.", routeId);
                return;
            }

            if (activeRules.Count == 0)
            {
                // Remove the plugin if no active rules
                if (route.Plugins != null)
                {
                    var pluginsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(route.Plugins));
                    if (pluginsDict != null && pluginsDict.Remove("pii-masker"))
                    {
                        route.Plugins = pluginsDict;
                        await _apisixClient.UpdateRouteAsync(routeId, route);
                    }
                }
                return;
            }

            // Construct plugin config
            var piiConfig = new
            {
                rules = activeRules.Select(r => new
                {
                    field_name = r.FieldPath,
                    regex = r.RegexPattern,
                    replace = r.ReplacePattern
                }).ToList()
            };

            // Inject into route plugins
            var currentPlugins = route.Plugins != null 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(route.Plugins)) 
                : new Dictionary<string, object>();
            
            if (currentPlugins != null)
            {
                currentPlugins["pii-masker"] = piiConfig;
                route.Plugins = currentPlugins;
                await _apisixClient.UpdateRouteAsync(routeId, route);
                _logger.LogInformation("Successfully synced {RuleCount} PII rules to route {RouteId}", activeRules.Count, routeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing PII rules for route {routeId}");
        }
    }

    private bool RuleExists(int id) => _context.PiiMaskingRules.Any(e => e.Id == id);
}
