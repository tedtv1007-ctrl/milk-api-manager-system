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
                var rawResponse = await _apisixClient.GetConsumersAsync();
                var doc = JsonDocument.Parse(rawResponse);
                var consumers = new List<object>();

                if (doc.RootElement.TryGetProperty("list", out var list))
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        var value = item.GetProperty("value");
                        var username = value.GetProperty("username").GetString();
                        
                        var quota = new { count = 1000, time_window = 3600, rejected_code = 429, rejected_msg = "API quota exceeded. Please contact support." };
                        
                        if (value.TryGetProperty("plugins", out var plugins) && plugins.TryGetProperty("limit-count", out var limitCount))
                        {
                            quota = new
                            {
                                count = limitCount.TryGetProperty("count", out var c) ? c.GetInt32() : 1000,
                                time_window = limitCount.TryGetProperty("time_window", out var tw) ? tw.GetInt32() : 3600,
                                rejected_code = limitCount.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 429,
                                rejected_msg = limitCount.TryGetProperty("rejected_msg", out var rm) ? rm.GetString() : "API quota exceeded. Please contact support."
                            };
                        }

                        consumers.Add(new
                        {
                            username = username,
                            desc = "", // APISIX consumers don't have a native 'desc' field in core
                            labels = new List<string>(),
                            quota = quota
                        });
                    }
                }

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
                if (!consumerData.TryGetProperty("username", out var usernameProp) || 
                    string.IsNullOrWhiteSpace(usernameProp.GetString()))
                {
                    return BadRequest("Username is required.");
                }

                string username = usernameProp.GetString()!;
                
                // Transform internal model to APISIX-compatible format
                var apisixFormat = new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["plugins"] = new Dictionary<string, object>()
                };

                // Add Quota plugin if present
                if (consumerData.TryGetProperty("quota", out var quota))
                {
                    var plugins = (Dictionary<string, object>)apisixFormat["plugins"];
                    plugins["limit-count"] = new
                    {
                        count = quota.GetProperty("count").GetInt32(),
                        time_window = quota.GetProperty("time_window").GetInt32(),
                        rejected_code = quota.GetProperty("rejected_code").GetInt32(),
                        rejected_msg = quota.GetProperty("rejected_msg").GetString(),
                        key = "remote_addr",
                        policy = "local"
                    };
                }

                await _apisixClient.UpdateConsumerAsync(username, apisixFormat);
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
