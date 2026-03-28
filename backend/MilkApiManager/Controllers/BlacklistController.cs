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
    [Authorize]
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
        [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
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
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateBlacklist([FromBody] BlacklistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Ip))
            {
                return BadRequest("IP is required");
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blacklist for IP {Ip}", request.Ip);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
