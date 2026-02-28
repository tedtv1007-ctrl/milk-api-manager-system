using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using System.Text.Json;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class ConsumerController : ControllerBase
    {
        private readonly IApisixClient _apisixClient;
        private readonly ILogger<ConsumerController> _logger;

        public ConsumerController(IApisixClient apisixClient, ILogger<ConsumerController> logger)
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
                        
                        var consumerObj = ParseConsumerFromApisix(value, username!);
                        consumers.Add(consumerObj);
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

        /// <summary>
        /// 取得單一 Consumer 明細（含 quota 與 rate_limit 設定）
        /// </summary>
        [HttpGet("{username}")]
        public async Task<IActionResult> GetConsumer(string username)
        {
            try
            {
                var consumer = await _apisixClient.GetConsumerAsync(username);
                if (consumer == null)
                {
                    return NotFound(new { Error = $"Consumer '{username}' not found." });
                }

                // 構建回傳物件
                var quota = new { count = 1000, time_window = 3600, rejected_code = 429, rejected_msg = "API quota exceeded. Please contact support." };
                var rateLimit = new { rate = 0, burst = 0, rejected_code = 503, key = "remote_addr" };

                if (consumer.Plugins != null)
                {
                    if (consumer.Plugins.TryGetValue("limit-count", out var limitCountObj))
                    {
                        var lc = JsonSerializer.SerializeToElement(limitCountObj);
                        quota = new
                        {
                            count = lc.TryGetProperty("count", out var c) ? c.GetInt32() : 1000,
                            time_window = lc.TryGetProperty("time_window", out var tw) ? tw.GetInt32() : 3600,
                            rejected_code = lc.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 429,
                            rejected_msg = lc.TryGetProperty("rejected_msg", out var rm) ? rm.GetString() ?? "API quota exceeded." : "API quota exceeded. Please contact support."
                        };
                    }

                    if (consumer.Plugins.TryGetValue("limit-req", out var limitReqObj))
                    {
                        var lr = JsonSerializer.SerializeToElement(limitReqObj);
                        rateLimit = new
                        {
                            rate = lr.TryGetProperty("rate", out var r) ? r.GetInt32() : 0,
                            burst = lr.TryGetProperty("burst", out var b) ? b.GetInt32() : 0,
                            rejected_code = lr.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 503,
                            key = lr.TryGetProperty("key", out var k) ? k.GetString() ?? "remote_addr" : "remote_addr"
                        };
                    }
                }

                return Ok(new
                {
                    username = consumer.Username,
                    quota = quota,
                    rate_limit = rateLimit
                });
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { Error = $"Consumer '{username}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consumer {Username}", username);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
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
                var plugins = new Dictionary<string, object>();

                // Add Quota plugin (limit-count) if present
                if (consumerData.TryGetProperty("quota", out var quota))
                {
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

                // Add Rate Limit plugin (limit-req) if present
                if (consumerData.TryGetProperty("rate_limit", out var rateLimit))
                {
                    plugins["limit-req"] = new
                    {
                        rate = rateLimit.GetProperty("rate").GetInt32(),
                        burst = rateLimit.GetProperty("burst").GetInt32(),
                        rejected_code = rateLimit.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 503,
                        key = rateLimit.TryGetProperty("key", out var k) ? k.GetString() : "remote_addr"
                    };
                }

                var apisixFormat = new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["plugins"] = plugins
                };

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
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
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

        /// <summary>
        /// 從 APISIX Consumer 原始資料中解析出 quota 和 rate_limit
        /// </summary>
        private static object ParseConsumerFromApisix(JsonElement value, string username)
        {
            var quota = new { count = 1000, time_window = 3600, rejected_code = 429, rejected_msg = "API quota exceeded. Please contact support." };
            var rateLimit = new { rate = 0, burst = 0, rejected_code = 503, key = "remote_addr" };

            if (value.TryGetProperty("plugins", out var plugins))
            {
                if (plugins.TryGetProperty("limit-count", out var limitCount))
                {
                    quota = new
                    {
                        count = limitCount.TryGetProperty("count", out var c) ? c.GetInt32() : 1000,
                        time_window = limitCount.TryGetProperty("time_window", out var tw) ? tw.GetInt32() : 3600,
                        rejected_code = limitCount.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 429,
                        rejected_msg = limitCount.TryGetProperty("rejected_msg", out var rm) ? rm.GetString() ?? "API quota exceeded." : "API quota exceeded. Please contact support."
                    };
                }

                if (plugins.TryGetProperty("limit-req", out var limitReq))
                {
                    rateLimit = new
                    {
                        rate = limitReq.TryGetProperty("rate", out var r) ? r.GetInt32() : 0,
                        burst = limitReq.TryGetProperty("burst", out var b) ? b.GetInt32() : 0,
                        rejected_code = limitReq.TryGetProperty("rejected_code", out var rc) ? rc.GetInt32() : 503,
                        key = limitReq.TryGetProperty("key", out var k) ? k.GetString() ?? "remote_addr" : "remote_addr"
                    };
                }
            }

            return new
            {
                username = username,
                desc = "",
                labels = new List<string>(),
                quota = quota,
                rate_limit = rateLimit
            };
        }
    }
}
