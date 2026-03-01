using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class PiiMaskingControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<PiiMaskingController>> _mockLogger;
    private readonly PiiMaskingController _controller;

    public PiiMaskingControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<PiiMaskingController>>();
        _controller = new PiiMaskingController(_context, _mockApisixClient.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetRules_ReturnsAllRules()
    {
        _context.PiiMaskingRules.AddRange(
            new PiiMaskingRule { Id = 1, RouteId = "r1", FieldPath = "email", RegexPattern = ".*@.*" },
            new PiiMaskingRule { Id = 2, RouteId = "r2", FieldPath = "phone", RegexPattern = "\\d+" });
        await _context.SaveChangesAsync();

        var result = await _controller.GetRules();

        var okResult = Assert.IsType<ActionResult<IEnumerable<PiiMaskingRule>>>(result);
        var rules = Assert.IsAssignableFrom<IEnumerable<PiiMaskingRule>>(okResult.Value);
        Assert.Equal(2, rules.Count());
    }

    [Fact]
    public async Task GetRules_EmptyDb_ReturnsEmptyList()
    {
        var result = await _controller.GetRules();

        var okResult = Assert.IsType<ActionResult<IEnumerable<PiiMaskingRule>>>(result);
        var rules = Assert.IsAssignableFrom<IEnumerable<PiiMaskingRule>>(okResult.Value);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task CreateRule_ValidRule_ReturnsCreatedAtAction()
    {
        var rule = new PiiMaskingRule
        {
            RouteId = "route-1",
            FieldPath = "user.email",
            RegexPattern = @"(.{3}).*(@.*)",
            ReplacePattern = "$1***$2"
        };

        // Mock GetRouteAsync returns null so SyncToApisix is a no-op
        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var result = await _controller.CreateRule(rule);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdRule = Assert.IsType<PiiMaskingRule>(createdResult.Value);
        Assert.Equal("route-1", createdRule.RouteId);
        Assert.Single(_context.PiiMaskingRules);
    }

    [Fact]
    public async Task CreateRule_EmptyRegex_ReturnsBadRequest()
    {
        var rule = new PiiMaskingRule
        {
            RouteId = "route-1",
            FieldPath = "email",
            RegexPattern = ""
        };

        var result = await _controller.CreateRule(rule);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateRule_InvalidRegex_ReturnsBadRequest()
    {
        var rule = new PiiMaskingRule
        {
            RouteId = "route-1",
            FieldPath = "email",
            RegexPattern = "[invalid("
        };

        var result = await _controller.CreateRule(rule);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRule_ValidUpdate_ReturnsNoContent()
    {
        var existing = new PiiMaskingRule
        {
            Id = 1,
            RouteId = "route-1",
            FieldPath = "email",
            RegexPattern = ".*@.*",
            ReplacePattern = "***"
        };
        _context.PiiMaskingRules.Add(existing);
        await _context.SaveChangesAsync();
        _context.Entry(existing).State = EntityState.Detached;

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var updated = new PiiMaskingRule
        {
            Id = 1,
            RouteId = "route-1",
            FieldPath = "email",
            RegexPattern = @"\w+@\w+",
            ReplacePattern = "[REDACTED]"
        };

        var result = await _controller.UpdateRule(1, updated);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateRule_IdMismatch_ReturnsBadRequest()
    {
        var rule = new PiiMaskingRule { Id = 2, RouteId = "route-1", FieldPath = "email", RegexPattern = ".*" };

        var result = await _controller.UpdateRule(1, rule);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task UpdateRule_EmptyRegex_ReturnsBadRequest()
    {
        var rule = new PiiMaskingRule { Id = 1, RouteId = "route-1", FieldPath = "email", RegexPattern = "" };

        var result = await _controller.UpdateRule(1, rule);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRule_ExistingRule_ReturnsNoContent()
    {
        var rule = new PiiMaskingRule
        {
            Id = 1,
            RouteId = "route-1",
            FieldPath = "email",
            RegexPattern = ".*@.*"
        };
        _context.PiiMaskingRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var result = await _controller.DeleteRule(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(_context.PiiMaskingRules);
    }

    [Fact]
    public async Task DeleteRule_NonExistent_ReturnsNotFound()
    {
        var result = await _controller.DeleteRule(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateRule_SyncsToApisixWithActiveRules()
    {
        var route = new Models.Apisix.Route
        {
            Id = "route-1",
            Name = "test-route",
            Uri = "/test",
            Plugins = new Dictionary<string, object>()
        };

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1")).ReturnsAsync(route);
        _mockApisixClient.Setup(c => c.UpdateRouteAsync("route-1", It.IsAny<Models.Apisix.Route>()))
            .Returns(Task.CompletedTask);

        var rule = new PiiMaskingRule
        {
            RouteId = "route-1",
            FieldPath = "user.email",
            RegexPattern = @"(.{3}).*(@.*)",
            ReplacePattern = "$1***$2",
            IsActive = true
        };

        await _controller.CreateRule(rule);

        _mockApisixClient.Verify(c => c.UpdateRouteAsync("route-1", It.IsAny<Models.Apisix.Route>()), Times.Once);
    }
}
