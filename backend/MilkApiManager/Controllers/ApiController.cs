using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class ApiController : ControllerBase
    {
        private readonly IApisixClient _apisixClient;
        private readonly ILogger<ApiController> _logger;

        public ApiController(IApisixClient apisixClient, ILogger<ApiController> logger)
        {
            _apisixClient = apisixClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetApis(CancellationToken cancellationToken)
        {
            try
            {
                var servicesJson = await _apisixClient.GetServicesAsync();
                return Ok(servicesJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving APIs");
                return StatusCode(500, new ApiError("InternalError", "An unexpected error occurred while retrieving APIs."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetApi(string id, CancellationToken cancellationToken)
        {
            try
            {
                var service = await _apisixClient.GetServiceAsync(id);
                if (service == null)
                {
                    return NotFound(new ApiError("NotFound", $"API with ID '{id}' was not found."));
                }
                return Ok(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API {ApiId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while retrieving API '{id}'."));
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> CreateApi([FromBody] Service serviceConfig, CancellationToken cancellationToken)
        {
            if (serviceConfig == null || string.IsNullOrEmpty(serviceConfig.Id))
            {
                return BadRequest(new ApiError("ValidationError", "Invalid service configuration: ID is required."));
            }

            try
            {
                await _apisixClient.CreateServiceAsync(serviceConfig.Id, serviceConfig);
                return CreatedAtAction(nameof(GetApi), new { id = serviceConfig.Id }, serviceConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API {ApiId}", serviceConfig.Id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while creating API '{serviceConfig.Id}'."));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> UpdateApi(string id, [FromBody] Service serviceConfig, CancellationToken cancellationToken)
        {
            if (serviceConfig == null)
            {
                return BadRequest(new ApiError("ValidationError", "Invalid service configuration."));
            }

            try
            {
                await _apisixClient.UpdateServiceAsync(id, serviceConfig);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API {ApiId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while updating API '{id}'."));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> DeleteApi(string id, CancellationToken cancellationToken)
        {
            try
            {
                await _apisixClient.DeleteServiceAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API {ApiId}", id);
                return StatusCode(500, new ApiError("InternalError", $"An unexpected error occurred while deleting API '{id}'."));
            }
        }
    }
}