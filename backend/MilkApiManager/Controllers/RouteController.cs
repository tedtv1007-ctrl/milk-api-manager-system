using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using System.Text.Json;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<RouteController> _logger;
        private readonly AuditLogService _auditLogService;

        public RouteController(ApisixClient apisixClient, ILogger<RouteController> logger, AuditLogService auditLogService)
        {
            _apisixClient = apisixClient;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoutes()
        {
            try
            {
                var routesJson = await _apisixClient.GetRoutesAsync();
                return Ok(routesJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving routes");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoute(string id)
        {
            try
            {
                var route = await _apisixClient.GetRouteAsync(id);
                if (route == null)
                {
                    return NotFound();
                }
                return Ok(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoute([FromBody] ApisixRoute routeConfig)
        {
            if (routeConfig == null || string.IsNullOrEmpty(routeConfig.Id))
            {
                return BadRequest("Invalid route configuration");
            }

            try
            {
                var currentUser = User.Identity?.Name ?? "Anonymous";

                await _apisixClient.CreateRouteAsync(routeConfig.Id, routeConfig);

                // Audit Log: Create
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "Create",
                    Resource = "Route",
                    User = currentUser,
                    Details = new { RouteId = routeConfig.Id, Config = routeConfig }
                });

                return CreatedAtAction(nameof(GetRoute), new { id = routeConfig.Id }, routeConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route {Id}", routeConfig.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(string id, [FromBody] ApisixRoute routeConfig)
        {
            if (routeConfig == null)
            {
                return BadRequest("Invalid route configuration");
            }

            try
            {
                var currentUser = User.Identity?.Name ?? "Anonymous";

                await _apisixClient.UpdateRouteAsync(id, routeConfig);

                // Audit Log: Update
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "Update",
                    Resource = "Route",
                    User = currentUser,
                    Details = new { RouteId = id, NewConfig = routeConfig }
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating route {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(string id)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "Anonymous";

                await _apisixClient.DeleteRouteAsync(id);

                // Audit Log: Delete
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "Delete",
                    Resource = "Route",
                    User = currentUser,
                    Details = new { RouteId = id }
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting route {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
