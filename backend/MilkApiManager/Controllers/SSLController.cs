using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class SSLController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<SSLController> _logger;
    private readonly IAuditLogService _auditLogService;

    public SSLController(IApisixClient apisixClient, ILogger<SSLController> logger, IAuditLogService auditLogService)
    {
        _apisixClient = apisixClient;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSsls()
    {
        try
        {
            var ssls = await _apisixClient.GetSslsTypedAsync();
            // Strip cert/key content for list view (security)
            var safeSsls = ssls.Select(s => new
            {
                s.Id,
                s.Snis,
                s.Status,
                s.ValidityStart,
                s.ValidityEnd,
                HasCert = !string.IsNullOrEmpty(s.Cert),
                HasKey = !string.IsNullOrEmpty(s.Key)
            });
            return Ok(safeSsls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SSL certificates");
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSsl(string id)
    {
        try
        {
            var ssl = await _apisixClient.GetSslAsync(id);
            if (ssl == null) return NotFound();
            return Ok(ssl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SSL certificate {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> CreateOrUpdateSsl(string id, [FromBody] SslCertificate sslConfig)
    {
        if (sslConfig == null || string.IsNullOrEmpty(sslConfig.Cert) || string.IsNullOrEmpty(sslConfig.Key))
            return BadRequest(new ApiError("ValidationError", "Certificate and key are required"));

        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            sslConfig.Id = id;
            await _apisixClient.CreateSslAsync(id, sslConfig);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CreateOrUpdate",
                Resource = "SSL",
                User = currentUser,
                Details = new { SslId = id, Snis = sslConfig.Snis }
            });

            return Ok(new { id, snis = sslConfig.Snis, status = sslConfig.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating SSL certificate {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteSsl(string id)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Anonymous";
            await _apisixClient.DeleteSslAsync(id);

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                Resource = "SSL",
                User = currentUser,
                Details = new { SslId = id }
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SSL certificate {Id}", id);
            return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred."));
        }
    }
}
