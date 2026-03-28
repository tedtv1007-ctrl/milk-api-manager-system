using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

/// <summary>
/// TDD tests for AlertRulesController — validates CRUD for alert rule management.
/// Note: Uses static list for demonstration; tests must account for shared state.
/// </summary>
public class AlertRulesControllerTests
{
    private readonly AlertRulesController _controller;

    public AlertRulesControllerTests()
    {
        _controller = new AlertRulesController();
    }

    [Fact]
    public void GetRules_ReturnsOkWithDefaultRules()
    {
        var result = _controller.GetRules();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var rules = Assert.IsAssignableFrom<IEnumerable<AlertRule>>(okResult.Value);
        Assert.NotEmpty(rules);
    }

    [Fact]
    public void CreateRule_ValidRule_ReturnsCreated()
    {
        var newRule = new AlertRule
        {
            Name = "Test Alert",
            MetricName = "test_metric",
            Threshold = 5,
            Duration = "5m",
            NotificationChannels = new List<string> { "Email" }
        };

        var result = _controller.CreateRule(newRule);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var rule = Assert.IsType<AlertRule>(createdResult.Value);
        Assert.Equal("Test Alert", rule.Name);
    }

    [Fact]
    public void DeleteRule_NonExistent_ReturnsNotFound()
    {
        var result = _controller.DeleteRule("non-existent-id-xyz");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void ToggleRule_NonExistent_ReturnsNotFound()
    {
        var result = _controller.ToggleRule("non-existent-toggle-xyz");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void ToggleRule_Existing_TogglesIsEnabled()
    {
        // First, create a rule to toggle
        var newRule = new AlertRule
        {
            Name = "Toggle Test",
            MetricName = "toggle_metric",
            Threshold = 1,
            Duration = "1m",
            IsEnabled = true
        };
        _controller.CreateRule(newRule);

        // Toggle it
        var result = _controller.ToggleRule(newRule.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var toggled = Assert.IsType<AlertRule>(okResult.Value);
        Assert.False(toggled.IsEnabled);

        // Toggle again
        var result2 = _controller.ToggleRule(newRule.Id);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);
        var toggledBack = Assert.IsType<AlertRule>(okResult2.Value);
        Assert.True(toggledBack.IsEnabled);
    }

    [Fact]
    public void DeleteRule_Existing_ReturnsNoContent()
    {
        // Create a specific rule to delete
        var rule = new AlertRule
        {
            Name = "Delete Me",
            MetricName = "del_metric",
            Threshold = 99,
            Duration = "10m"
        };
        _controller.CreateRule(rule);

        var result = _controller.DeleteRule(rule.Id);

        Assert.IsType<NoContentResult>(result);

        // Verify it's gone
        var deleteAgain = _controller.DeleteRule(rule.Id);
        Assert.IsType<NotFoundResult>(deleteAgain);
    }
}
