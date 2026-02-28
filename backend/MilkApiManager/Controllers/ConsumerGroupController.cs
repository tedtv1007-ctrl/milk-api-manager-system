using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using System.Text.Json;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
public class ConsumerGroupController : ControllerBase
{
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<ConsumerGroupController> _logger;

    public ConsumerGroupController(IApisixClient apisixClient, ILogger<ConsumerGroupController> logger)
    {
        _apisixClient = apisixClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        try
        {
            var json = await _apisixClient.GetConsumerGroupsAsync();
            var doc = JsonDocument.Parse(json);
            var groups = new List<ConsumerGroup>();

            if (doc.RootElement.TryGetProperty("list", out var list))
            {
                foreach (var item in list.EnumerateArray())
                {
                    if (item.TryGetProperty("value", out var val))
                    {
                        var group = JsonSerializer.Deserialize<ConsumerGroup>(val.GetRawText());
                        if (group != null) groups.Add(group);
                    }
                }
            }
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consumer groups");
            return StatusCode(500);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] ConsumerGroup groupData)
    {
        try
        {
            if (groupData == null) return BadRequest();
            
            // Ensure ID matches
            groupData.Id = id;

            // Send to APISIX
            await _apisixClient.CreateConsumerGroupAsync(id, groupData);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating consumer group {Id}", id);
            return StatusCode(500);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        try
        {
            await _apisixClient.DeleteConsumerGroupAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting consumer group {Id}", id);
            return StatusCode(500);
        }
    }
}
