using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using System.Text.Json;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class ServiceController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<ServiceController> _logger;
    private readonly IAuditLogService _auditLogService;

    public ServiceController(IApisixClient apisixClient, ILogger<ServiceController> logger, IAuditLogService auditLogService)
    {
        _apisixClient = apisixClient;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        try
        {
            var services = await _apisixClient.GetServicesTypedAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving services");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetService(string id)
    {
        try
        {
            var service = await _apisixClient.GetServiceAsync(id);
            if (service == null) return NotFound();
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> CreateOrUpdateService(string id, [FromBody] Service serviceConfig)
    {
        if (serviceConfig == null) return BadRequest("Invalid service configuration");

        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            await _apisixClient.CreateServiceAsync(id, serviceConfig);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CreateOrUpdate",
                Resource = "Service",
                User = currentUser,
                Details = new { ServiceId = id, Config = serviceConfig }
            });

            return Ok(serviceConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating service {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteService(string id)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            await _apisixClient.DeleteServiceAsync(id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "Service",
                User = currentUser,
                Details = new { ServiceId = id }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
