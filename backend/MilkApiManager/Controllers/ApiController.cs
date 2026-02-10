using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MilkApiManager.Services;
using MilkApiManager.Models.Apisix;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApiController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ApisixClient apisixClient, ILogger<ApiController> logger)
        {
            _apisixClient = apisixClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetApis()
        {
            try
            {
                var servicesJson = await _apisixClient.GetServicesAsync();
                return Ok(servicesJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving APIs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetApi(string id)
        {
            try
            {
                var service = await _apisixClient.GetServiceAsync(id);
                if (service == null)
                {
                    return NotFound();
                }
                return Ok(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateApi([FromBody] Service serviceConfig)
        {
            if (serviceConfig == null || string.IsNullOrEmpty(serviceConfig.Id))
            {
                return BadRequest("Invalid service configuration");
            }

            try
            {
                await _apisixClient.CreateServiceAsync(serviceConfig.Id, serviceConfig);
                return CreatedAtAction(nameof(GetApi), new { id = serviceConfig.Id }, serviceConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API {Id}", serviceConfig.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApi(string id, [FromBody] Service serviceConfig)
        {
            if (serviceConfig == null)
            {
                return BadRequest("Invalid service configuration");
            }

            try
            {
                await _apisixClient.UpdateServiceAsync(id, serviceConfig);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApi(string id)
        {
            try
            {
                await _apisixClient.DeleteServiceAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}