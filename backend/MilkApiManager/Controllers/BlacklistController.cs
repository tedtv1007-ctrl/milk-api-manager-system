using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlacklistController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<BlacklistController> _logger;

        public BlacklistController(ApisixClient apisixClient, ILogger<BlacklistController> logger)
        {
            _apisixClient = apisixClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlacklist()
        {
            try
            {
                var blacklist = await _apisixClient.GetBlacklistAsync();
                return Ok(blacklist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blacklist");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBlacklist([FromBody] BlacklistUpdateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Ip))
            {
                return BadRequest("IP is required");
            }

            try
            {
                var blacklist = await _apisixClient.GetBlacklistAsync();
                var blacklistSet = new HashSet<string>(blacklist);

                if (request.Action == "add")
                {
                    blacklistSet.Add(request.Ip);
                }
                else if (request.Action == "remove")
                {
                    blacklistSet.Remove(request.Ip);
                }
                else
                {
                    return BadRequest("Invalid action. Use 'add' or 'remove'.");
                }

                await _apisixClient.UpdateBlacklistAsync(blacklistSet.ToList());
                return Ok(new { message = $"IP {request.Ip} {request.Action}ed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blacklist for IP {Ip}", request.Ip);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class BlacklistUpdateRequest
    {
        public string Ip { get; set; }
        public string Action { get; set; } = "add"; // add | remove
    }
}