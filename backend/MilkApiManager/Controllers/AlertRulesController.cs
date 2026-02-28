using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Models;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class AlertRulesController : ControllerBase
    {
        // For demonstration, using a static list. In production, use a DB.
        private static readonly List<AlertRule> _rules = new List<AlertRule>
        {
            new AlertRule { 
                Name = "5xx Error Spike", 
                MetricName = "apisix_http_status", 
                Threshold = 10, 
                Duration = "1m",
                NotificationChannels = new List<string> { "Mattermost" }
            },
            new AlertRule { 
                Name = "High Frequency IP Access", 
                MetricName = "apisix_http_requests_total", 
                Threshold = 100, 
                Duration = "1m",
                NotificationChannels = new List<string> { "Email", "Mattermost" }
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<AlertRule>> GetRules()
        {
            return Ok(_rules);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public ActionResult<AlertRule> CreateRule(AlertRule rule)
        {
            _rules.Add(rule);
            return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public IActionResult DeleteRule(string id)
        {
            var rule = _rules.FirstOrDefault(r => r.Id == id);
            if (rule == null) return NotFound();
            _rules.Remove(rule);
            return NoContent();
        }

        [HttpPut("{id}/toggle")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public IActionResult ToggleRule(string id)
        {
            var rule = _rules.FirstOrDefault(r => r.Id == id);
            if (rule == null) return NotFound();
            rule.IsEnabled = !rule.IsEnabled;
            return Ok(rule);
        }
    }
}
