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
    /// <summary>
    /// Blacklist management controller — delegates to IBlacklistService (A-1 fix).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class BlacklistController : ControllerBase
    {
        private readonly IBlacklistService _blacklistService;
        private readonly ILogger<BlacklistController> _logger;

        public BlacklistController(IBlacklistService blacklistService, ILogger<BlacklistController> logger)
        {
            _blacklistService = blacklistService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the current IP blacklist.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BlacklistEntry>))]
        public async Task<IActionResult> GetBlacklist()
        {
            try
            {
                var entries = await _blacklistService.GetBlacklistAsync();
                return Ok(entries);
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
                string message;
                if (request.Action == "add")
                {
                    message = await _blacklistService.AddAsync(request);
                }
                else if (request.Action == "remove")
                {
                    message = await _blacklistService.RemoveAsync(request);
                }
                else
                {
                    return BadRequest("Invalid action. Use 'add' or 'remove'.");
                }

                return Ok(new { message });
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
