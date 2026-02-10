using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;
using System.Text.Json;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsumerController : ControllerBase
    {
        private readonly ApisixClient _apisixClient;
        private readonly ILogger<ConsumerController> _logger;

        public ConsumerController(ApisixClient apisixClient, ILogger<ConsumerController> logger)
        {
            _apisixClient = apisixClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetConsumers()
        {
            try
            {
                var consumers = await _apisixClient.GetConsumersAsync();
                return Ok(consumers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consumers");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConsumer([FromBody] JsonElement consumerData)
        {
            try
            {
                string username = consumerData.GetProperty("username").GetString();
                await _apisixClient.UpdateConsumerAsync(username, consumerData);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consumer");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteConsumer(string username)
        {
            try
            {
                await _apisixClient.DeleteConsumerAsync(username);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting consumer {Username}", username);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
