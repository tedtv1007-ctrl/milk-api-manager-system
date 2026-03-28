using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using ApisixRoute = MilkApiManager.Models.Apisix.Route;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class RouteControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<RouteController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly RouteController _controller;

    public RouteControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<RouteController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new RouteController(
            _mockApisixClient.Object,
            _mockLogger.Object,
            _mockAuditLogService.Object
        );

        // Setup ControllerContext for User.Identity
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetRoutes_ReturnsOk()
    {
        var routes = new List<ApisixRoute> { new ApisixRoute { Id = "r1", Name = "Route 1", Uri = "/r1" } };
        _mockApisixClient.Setup(c => c.GetRoutesTypedAsync())
            .ReturnsAsync(routes);

        var result = await _controller.GetRoutes(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRoutes = Assert.IsType<List<ApisixRoute>>(okResult.Value);
        Assert.Single(returnedRoutes);
        Assert.Equal("r1", returnedRoutes[0].Id);
    }

    [Fact]
    public async Task GetRoutes_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetRoutesTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetRoutes(CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetRoute_ExistingId_ReturnsOk()
    {
        var route = new ApisixRoute { Id = "test-1", Name = "TestRoute", Uri = "/test" };
        _mockApisixClient.Setup(c => c.GetRouteAsync("test-1"))
            .ReturnsAsync(route);

        var result = await _controller.GetRoute("test-1", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRoute = Assert.IsType<ApisixRoute>(okResult.Value);
        Assert.Equal("test-1", returnedRoute.Id);
    }

    [Fact]
    public async Task GetRoute_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetRouteAsync("nonexistent"))
            .ReturnsAsync((ApisixRoute?)null);

        var result = await _controller.GetRoute("nonexistent", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateRoute_ValidConfig_Returns201()
    {
        var routeConfig = new ApisixRoute { Id = "new-route", Name = "NewRoute", Uri = "/new" };
        _mockApisixClient.Setup(c => c.CreateRouteAsync("new-route", routeConfig))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateRoute(routeConfig, CancellationToken.None);

        // CreatedAtAction returns CreatedAtActionResult (201)
        Assert.NotNull(result);
        var actionResult = result as CreatedAtActionResult;
        Assert.NotNull(actionResult);
        Assert.Equal(201, actionResult.StatusCode);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Create" && e.Resource == "Route")),
            Times.Once);
    }

    [Fact]
    public async Task CreateRoute_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.CreateRoute(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRoute_EmptyId_ReturnsBadRequest()
    {
        var routeConfig = new ApisixRoute { Id = "", Name = "Test", Uri = "/test" };

        var result = await _controller.CreateRoute(routeConfig, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRoute_ValidConfig_Returns204()
    {
        var routeConfig = new ApisixRoute { Id = "update-1", Name = "Updated", Uri = "/updated" };
        _mockApisixClient.Setup(c => c.UpdateRouteAsync("update-1", routeConfig))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateRoute("update-1", routeConfig, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Update")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRoute_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.UpdateRoute("id", null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRoute_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteRouteAsync("del-1"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteRoute("del-1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Delete")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRoute_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteRouteAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteRoute("err", CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
