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

public class MockControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<MockController>> _mockLogger;
    private readonly MockController _controller;

    public MockControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<MockController>>();
        _controller = new MockController(_context, _mockApisixClient.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetRules_ReturnsAllRules()
    {
        _context.MockRules.AddRange(
            new MockRule { Id = 1, RouteId = "r1", ResponseBody = "{\"msg\":\"hello\"}" },
            new MockRule { Id = 2, RouteId = "r2", ResponseBody = "{\"msg\":\"world\"}" });
        await _context.SaveChangesAsync();

        var result = await _controller.GetRules();

        var rules = Assert.IsAssignableFrom<IEnumerable<MockRule>>(result.Value);
        Assert.Equal(2, rules.Count());
    }

    [Fact]
    public async Task GetRules_EmptyDb_ReturnsEmptyList()
    {
        var result = await _controller.GetRules();

        var rules = Assert.IsAssignableFrom<IEnumerable<MockRule>>(result.Value);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task CreateRule_ValidRule_ReturnsCreatedAtAction()
    {
        var rule = new MockRule
        {
            RouteId = "route-1",
            ResponseStatusCode = 200,
            ResponseBody = "{\"status\":\"ok\"}",
            ContentType = "application/json",
            IsEnabled = true
        };

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var result = await _controller.CreateRule(rule);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var created = Assert.IsType<MockRule>(createdResult.Value);
        Assert.Equal("route-1", created.RouteId);
        Assert.Single(_context.MockRules);
    }

    [Fact]
    public async Task CreateRule_SyncsToApisix()
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

        var rule = new MockRule
        {
            RouteId = "route-1",
            ResponseStatusCode = 200,
            ResponseBody = "{\"mock\":true}",
            IsEnabled = true
        };

        await _controller.CreateRule(rule);

        _mockApisixClient.Verify(c => c.UpdateRouteAsync("route-1", It.IsAny<Models.Apisix.Route>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRule_ValidUpdate_ReturnsNoContent()
    {
        var existing = new MockRule
        {
            Id = 1,
            RouteId = "route-1",
            ResponseBody = "{\"old\":true}"
        };
        _context.MockRules.Add(existing);
        await _context.SaveChangesAsync();
        _context.Entry(existing).State = EntityState.Detached;

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var updated = new MockRule
        {
            Id = 1,
            RouteId = "route-1",
            ResponseBody = "{\"new\":true}"
        };

        var result = await _controller.UpdateRule(1, updated);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateRule_IdMismatch_ReturnsBadRequest()
    {
        var rule = new MockRule { Id = 2, RouteId = "r1" };

        var result = await _controller.UpdateRule(1, rule);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task DeleteRule_ExistingRule_ReturnsNoContent()
    {
        var rule = new MockRule { Id = 1, RouteId = "route-1", ResponseBody = "{}" };
        _context.MockRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1"))
            .ReturnsAsync((Models.Apisix.Route?)null);

        var result = await _controller.DeleteRule(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(_context.MockRules);
    }

    [Fact]
    public async Task DeleteRule_NonExistent_ReturnsNotFound()
    {
        var result = await _controller.DeleteRule(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteRule_RemovesMockingPluginFromApisix()
    {
        // Setup: existing rule that is the only enabled rule for the route
        var rule = new MockRule { Id = 1, RouteId = "route-1", ResponseBody = "{}", IsEnabled = true };
        _context.MockRules.Add(rule);
        await _context.SaveChangesAsync();

        var route = new Models.Apisix.Route
        {
            Id = "route-1",
            Name = "test-route",
            Uri = "/test",
            Plugins = new Dictionary<string, object> { ["mocking"] = new { response_status = 200 } }
        };

        _mockApisixClient.Setup(c => c.GetRouteAsync("route-1")).ReturnsAsync(route);
        _mockApisixClient.Setup(c => c.UpdateRouteAsync("route-1", It.IsAny<Models.Apisix.Route>()))
            .Returns(Task.CompletedTask);

        await _controller.DeleteRule(1);

        _mockApisixClient.Verify(c => c.UpdateRouteAsync("route-1", It.IsAny<Models.Apisix.Route>()), Times.Once);
    }
}
