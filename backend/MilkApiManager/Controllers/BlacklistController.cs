using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class BlacklistController : ControllerBase
    {
        private readonly IApisixClient _apisixClient;
        private readonly ILogger<BlacklistController> _logger;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLog;
        private readonly ApisixSyncOutboxService _outboxService;

        public BlacklistController(IApisixClient apisixClient, ILogger<BlacklistController> logger, AppDbContext db, IConfiguration config, IAuditLogService auditLog, ApisixSyncOutboxService outboxService)
        {
            _apisixClient = apisixClient;
            _logger = logger;
            _db = db;
            _config = config;
            _auditLog = auditLog;
            _outboxService = outboxService;
        }

        /// <summary>
        /// Retrieves the current IP blacklist.
        /// </summary>
        /// <returns>A list of blacklisted IPs or CIDR blocks.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BlacklistEntry>))]
        public async Task<IActionResult> GetBlacklist()
        {
            try
            {
                var persist = _config.GetValue<bool>("Blacklist:PersistToDatabase");
                if (persist)
                {
                    var entries = await _db.BlacklistEntries.OrderByDescending(e => e.AddedAt).ToListAsync();
                    return Ok(entries);
                }
                else
                {
                    var blacklist = await _apisixClient.GetBlacklistAsync();
                    return Ok(blacklist.Select(ip => new BlacklistEntry { IpOrCidr = ip }).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blacklist");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Adds or removes an IP/CIDR to/from the blacklist.
        /// </summary>
        /// <param name="request">The blacklist update instruction.</param>
        /// <returns>A status message indicating success.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateBlacklist([FromBody] BlacklistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Ip))
            {
                return BadRequest("IP is required");
            }

            if (!IsValidIpOrCidr(request.Ip))
            {
                return BadRequest("Invalid IP or CIDR format");
            }

            try
            {
                var blacklist = await _apisixClient.GetBlacklistAsync();
                var blacklistSet = new HashSet<string>(blacklist);

                if (request.Action == "add")
                {
                    blacklistSet.Add(request.Ip);
                    // persist to DB if enabled
                    if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
                    {
                        var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == request.Ip);
                        if (exists == null)
                        {
                            var entry = new BlacklistEntry
                            {
                                IpOrCidr = request.Ip,
                                Reason = request.Reason,
                                AddedBy = request.AddedBy,
                                ExpiresAt = request.ExpiresAt,
                                AddedAt = DateTime.UtcNow
                            };
                            _db.BlacklistEntries.Add(entry);
                            await _db.SaveChangesAsync();

                            // Audit log for blacklist add
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Blacklist.Add",
                                    Resource = "Blacklist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = new { Ip = request.Ip, Reason = request.Reason, ExpiresAt = request.ExpiresAt }
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for blacklist add {Ip}", request.Ip);
                            }
                        }
                    }
                }
                else if (request.Action == "remove")
                {
                    blacklistSet.Remove(request.Ip);
                    if (_config.GetValue<bool>("Blacklist:PersistToDatabase"))
                    {
                        var exists = await _db.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == request.Ip);
                        if (exists != null)
                        {
                            // capture details before removal
                            var details = new { Ip = exists.IpOrCidr, Reason = exists.Reason, ExpiresAt = exists.ExpiresAt };

                            _db.BlacklistEntries.Remove(exists);
                            await _db.SaveChangesAsync();

                            // Audit log for blacklist remove
                            try
                            {
                                await _auditLog.LogAsync(new Models.AuditLogEntry
                                {
                                    Action = "Blacklist.Remove",
                                    Resource = "Blacklist",
                                    User = request.AddedBy ?? "Unknown",
                                    Details = details
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write audit log for blacklist remove {Ip}", request.Ip);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Invalid action. Use 'add' or 'remove'.");
                }

                if (_config.GetValue<bool>("Sync:Blacklist:UseOutbox"))
                {
                    await _outboxService.EnqueueBlacklistSyncAsync(blacklistSet.ToList());
                }
                else
                {
                    await _apisixClient.UpdateBlacklistAsync(blacklistSet.ToList());
                }

                return Ok(new { message = $"IP {request.Ip} {request.Action}ed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blacklist for IP {Ip}", request.Ip);
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
    }
}
