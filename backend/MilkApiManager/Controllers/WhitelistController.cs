using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhitelistController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<WhitelistController> _logger;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly AuditLogService _auditLog;

        public WhitelistController(ApisixClient apisixClient, ILogger<WhitelistController> logger, AppDbContext db, IConfiguration config, AuditLogService auditLog)
        {
            _apisixClient = apisixClient;
            _logger = logger;
            _db = db;
            _config = config;
            _auditLog = auditLog;
        }

        [HttpGet("route/{routeId}")]
        public async Task<IActionResult> GetWhitelistForRoute(string routeId)
        {
            try
            {
                var persist = _config.GetValue<bool>("Whitelist:PersistToDatabase");
                if (persist)
                {
                    var entries = await _db.WhitelistEntries.Where(w => w.RouteId == routeId)
                        .Where(w => w.ExpiresAt == null || w.ExpiresAt > DateTime.UtcNow)
                        .OrderByDescending(e => e.AddedAt).ToListAsync();
                    return Ok(entries);
                }
                else
                {
                    // fallback to apisix plugin config
                    var ips = await _apisixClient.GetWhitelistForRouteAsync(routeId);
                    return Ok(ips.Select(ip => new WhitelistEntry { IpCidr = ip, RouteId = routeId }).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving whitelist for route {RouteId}", routeId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("route/{routeId}")]
        public async Task<IActionResult> AddWhitelistEntry(string routeId, [FromBody] WhitelistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.IpCidr))
            {
                return BadRequest("IpCidr is required");
            }

            try
            {
                var persist = _config.GetValue<bool>("Whitelist:PersistToDatabase");

                if (request.Action == "add")
                {
                    if (persist)
                    {
                        var exists = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == request.IpCidr);
                        if (exists == null)
                        {
                            var entry = new WhitelistEntry
                            {
                                RouteId = routeId,
                                IpCidr = request.IpCidr,
                                Reason = request.Reason,
                                AddedBy = request.AddedBy,
                                ExpiresAt = request.ExpiresAt,
                                AddedAt = DateTime.UtcNow
                            };
                            _db.WhitelistEntries.Add(entry);
                            await _db.SaveChangesAsync();
                        }
                    }
                }
                else if (request.Action == "remove")
                {
                    if (persist)
                    {
                        var exists = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == request.IpCidr);
                        if (exists != null)
                        {
                            _db.WhitelistEntries.Remove(exists);
                            await _db.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid action. Use 'add' or 'remove'.");
                }

                await SyncWhitelistToApisix(routeId);

                return Ok(new { message = $"IP {request.IpCidr} {request.Action}ed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating whitelist for route {RouteId} {Ip}", routeId, request.IpCidr);
                return StatusCode(500, "Internal server error");
            }
        }

        // Sync current valid whitelist entries for a route to APISIX plugin
        public async Task SyncWhitelistToApisix(string routeId)
        {
            // gather valid entries
            var entries = await _db.WhitelistEntries.Where(w => w.RouteId == routeId)
                .Where(w => w.ExpiresAt == null || w.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            var ipList = entries.Select(e => e.IpCidr).Distinct().ToList();

            // call apisix client to update plugin config for the route
            await _apisixClient.UpdateWhitelistForRouteAsync(routeId, ipList);

            _logger.LogInformation("Synced {Count} whitelist entries to APISIX for route {RouteId}", ipList.Count, routeId);
        }
    }

    public class WhitelistUpdateRequest
    {
        public string IpCidr { get; set; }
        public string Action { get; set; } = "add"; // add | remove
        public string? Reason { get; set; }
        public string? AddedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}