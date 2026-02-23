using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AccessRequestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ApisixClient _apisixClient;
    private readonly NotificationService _notificationService;
    private readonly ILogger<AccessRequestController> _logger;

    public AccessRequestController(AppDbContext context, ApisixClient apisixClient, NotificationService notificationService, ILogger<AccessRequestController> logger)
    {
        _context = context;
        _apisixClient = apisixClient;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccessRequest>>> GetRequests()
    {
        return await _context.AccessRequests.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    [HttpPost("submit")]
    public async Task<ActionResult<AccessRequest>> SubmitRequest(AccessRequest request)
    {
        request.Status = RequestStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        await _notificationService.AlertAsync("New Access Request", $"Project `{request.ProjectName}` requested `{request.RequestedTier}` access.");
        return Ok(request);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveRequest(int id, [FromQuery] string comment = "")
    {
        var request = await _context.AccessRequests.FindAsync(id);
        if (request == null || request.Status != RequestStatus.Pending) return BadRequest();

        // 1. Update DB Status
        request.Status = RequestStatus.Approved;
        request.AdminComment = comment;
        request.ProcessedAt = DateTime.UtcNow;

        // 2. Auto-Provision in APISIX
        try
        {
            var consumerName = $"internal-{request.ProjectName.Replace(" ", "-").ToLower()}";
            var apisixConsumer = new Models.Apisix.Consumer
            {
                Username = consumerName,
                GroupId = request.RequestedTier.ToLower(), // Free, Silver, Gold groups must exist
                Plugins = new Dictionary<string, object>
                {
                    ["key-auth"] = new { key = Guid.NewGuid().ToString("N") } // Generate initial API Key
                }
            };

            await _apisixClient.CreateConsumerAsync(consumerName, apisixConsumer);
            _logger.LogInformation($"Auto-provisioned consumer {consumerName} for request {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-provision APISIX consumer during approval");
            return StatusCode(500, "Approved in DB but failed to sync with APISIX");
        }

        await _context.SaveChangesAsync();
        await _notificationService.AlertAsync("Request Approved", $"Access for `{request.ProjectName}` has been granted.");
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectRequest(int id, [FromQuery] string reason = "")
    {
        var request = await _context.AccessRequests.FindAsync(id);
        if (request == null || request.Status != RequestStatus.Pending) return BadRequest();

        request.Status = RequestStatus.Rejected;
        request.AdminComment = reason;
        request.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }
}
