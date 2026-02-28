using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Auth;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using System.Text.Json;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class MockController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ApisixClient _apisixClient;
    private readonly ILogger<MockController> _logger;

    public MockController(AppDbContext context, ApisixClient apisixClient, ILogger<MockController> logger)
    {
        _context = context;
        _apisixClient = apisixClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MockRule>>> GetRules()
    {
        return await _context.MockRules.ToListAsync();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<ActionResult<MockRule>> CreateRule(MockRule rule)
    {
        _context.MockRules.Add(rule);
        await _context.SaveChangesAsync();
        await SyncMockToApisix(rule.RouteId);
        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> UpdateRule(int id, MockRule rule)
    {
        if (id != rule.Id) return BadRequest();
        _context.Entry(rule).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await SyncMockToApisix(rule.RouteId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.MockRules.FindAsync(id);
        if (rule == null) return NotFound();
        var routeId = rule.RouteId;
        _context.MockRules.Remove(rule);
        await _context.SaveChangesAsync();
        await SyncMockToApisix(routeId);
        return NoContent();
    }

    private async Task SyncMockToApisix(string routeId)
    {
        try
        {
            var rule = await _context.MockRules.FirstOrDefaultAsync(r => r.RouteId == routeId && r.IsEnabled);
            var route = await _apisixClient.GetRouteAsync(routeId);
            if (route == null) return;

            var plugins = route.Plugins ?? new Dictionary<string, object>();

            if (rule != null)
            {
                plugins["mocking"] = new
                {
                    response_status = rule.ResponseStatusCode,
                    response_schema = new { type = "object" }, // Basic schema
                    content_type = rule.ContentType,
                    response_example = rule.ResponseBody
                };
            }
            else
            {
                plugins.Remove("mocking");
            }

            route.Plugins = plugins;
            await _apisixClient.UpdateRouteAsync(routeId, route);
            _logger.LogInformation($"Synced Mocking plugin for route {routeId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing Mocking for route {routeId}");
        }
    }
}
