using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using System.Text.Json;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class RouteController : ControllerBase
    {
        private readonly IApisixClient _apisixClient;
        private readonly ILogger<RouteController> _logger;
        private readonly IAuditLogService _auditLogService;

        public RouteController(IApisixClient apisixClient, ILogger<RouteController> logger, IAuditLogService auditLogService)
        {
            _apisixClient = apisixClient;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoutes(CancellationToken cancellationToken)
        {
            try
            {
                var routes = await _apisixClient.GetRoutesTypedAsync();
                return Ok(routes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving routes");
                return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred while retrieving routes."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoute(string id, CancellationToken cancellationToken)
        {
            try
            {
                var route = await _apisixClient.GetRouteAsync(id);
                if (route == null)
                {
                    return NotFound(new ApiError("NotFound", $"Route with ID '{id}' was not found."));
                }
                return Ok(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route {RouteId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while retrieving route '{id}'."));
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> CreateRoute([FromBody] ApisixRoute routeConfig, CancellationToken cancellationToken)
        {
            if (routeConfig == null || string.IsNullOrEmpty(routeConfig.Id))
            {
                return BadRequest(new ApiError("ValidationError", "Invalid route configuration: ID is required."));
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
                _logger.LogError(ex, "Error creating route {RouteId}", routeConfig.Id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while creating route '{routeConfig.Id}'."));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> UpdateRoute(string id, [FromBody] ApisixRoute routeConfig, CancellationToken cancellationToken)
        {
            if (routeConfig == null)
            {
                return BadRequest(new ApiError("ValidationError", "Invalid route configuration."));
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
                _logger.LogError(ex, "Error updating route {RouteId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while updating route '{id}'."));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> DeleteRoute(string id, CancellationToken cancellationToken)
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
                _logger.LogError(ex, "Error deleting route {RouteId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while deleting route '{id}'."));
            }
        }
    }
}
