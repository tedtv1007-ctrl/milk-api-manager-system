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
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<RouteController>> _mockLogger;
    private readonly Mock<AuditLogService> _mockAuditLogService;
    private readonly RouteController _controller;

    public RouteControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );
        _mockLogger = new Mock<ILogger<RouteController>>();
        _mockAuditLogService = new Mock<AuditLogService>(
            Mock.Of<HttpClient>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
        );

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
        _mockApisixClient.Setup(c => c.GetRoutesAsync())
            .ReturnsAsync("{\"list\":[]}");

        var result = await _controller.GetRoutes();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("{\"list\":[]}", okResult.Value);
    }

    [Fact]
    public async Task GetRoutes_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetRoutesAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetRoutes();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetRoute_ExistingId_ReturnsOk()
    {
        var route = new ApisixRoute { Id = "test-1", Name = "TestRoute", Uri = "/test" };
        _mockApisixClient.Setup(c => c.GetRouteAsync("test-1"))
            .ReturnsAsync(route);

        var result = await _controller.GetRoute("test-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRoute = Assert.IsType<ApisixRoute>(okResult.Value);
        Assert.Equal("test-1", returnedRoute.Id);
    }

    [Fact]
    public async Task GetRoute_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetRouteAsync("nonexistent"))
            .ReturnsAsync((ApisixRoute?)null);

        var result = await _controller.GetRoute("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateRoute_ValidConfig_Returns201()
    {
        var routeConfig = new ApisixRoute { Id = "new-route", Name = "NewRoute", Uri = "/new" };
        _mockApisixClient.Setup(c => c.CreateRouteAsync("new-route", routeConfig))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateRoute(routeConfig);

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
        var result = await _controller.CreateRoute(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRoute_EmptyId_ReturnsBadRequest()
    {
        var routeConfig = new ApisixRoute { Id = "", Name = "Test", Uri = "/test" };

        var result = await _controller.CreateRoute(routeConfig);

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

        var result = await _controller.UpdateRoute("update-1", routeConfig);

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Update")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRoute_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.UpdateRoute("id", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRoute_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteRouteAsync("del-1"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteRoute("del-1");

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

        var result = await _controller.DeleteRoute("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
