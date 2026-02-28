using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
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

        /// <summary>
        /// Retrieves the IP whitelist for a specific route.
        /// </summary>
        /// <param name="routeId">The target APISIX route ID.</param>
        /// <returns>A list of whitelisted IPs or CIDR blocks.</returns>
        [HttpGet("route/{routeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WhitelistEntry>))]
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

        /// <summary>
        /// Adds or removes an IP/CIDR to/from a route's whitelist.
        /// </summary>
        /// <param name="routeId">The target APISIX route ID.</param>
        /// <param name="request">The whitelist update instruction.</param>
        /// <returns>A status message indicating success.</returns>
        [HttpPost("route/{routeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddWhitelistEntry(string routeId, [FromBody] WhitelistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.IpCidr))
            {
                return BadRequest("IpCidr is required");
            }

            if (!IsValidIpOrCidr(request.IpCidr))
            {
                return BadRequest("Invalid IP or CIDR format");
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

                            // Audit log for add
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Create",
                                    Resource = "Whitelist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = new { RouteId = routeId, IpCidr = request.IpCidr, Reason = request.Reason }
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for whitelist add {Route} {Ip}", routeId, request.IpCidr);
                            }
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

                            // Audit log for remove
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Delete",
                                    Resource = "Whitelist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = new { RouteId = routeId, IpCidr = request.IpCidr }
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for whitelist remove {Route} {Ip}", routeId, request.IpCidr);
                            }
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

        private bool IsValidIpOrCidr(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            if (value.Contains('/'))
            {
                var parts = value.Split('/');
                if (parts.Length != 2) return false;
                if (!System.Net.IPAddress.TryParse(parts[0], out _)) return false;
                if (!int.TryParse(parts[1], out int mask) || mask < 0 || mask > 128) return false;
                return true;
            }

            return System.Net.IPAddress.TryParse(value, out _);
        }

        // Sync current valid whitelist entries for a route to APISIX plugin
        private async Task SyncWhitelistToApisix(string routeId)
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
        public required string IpCidr { get; set; }
        public string Action { get; set; } = "add"; // add | remove
        public string? Reason { get; set; }
        public string? AddedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}