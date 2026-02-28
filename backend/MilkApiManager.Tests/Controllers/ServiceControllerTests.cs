using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class ServiceControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<ServiceController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly ServiceController _controller;

    public ServiceControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<ServiceController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new ServiceController(
            _mockApisixClient.Object,
            _mockLogger.Object,
            _mockAuditLogService.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ============================================================
    // GET /api/Service (List)
    // ============================================================

    [Fact]
    public async Task GetServices_ReturnsOk_WithServiceList()
    {
        var services = new List<Service>
        {
            new Service { Id = "svc-1", Name = "TestService", Upstream = new Upstream { Nodes = new Dictionary<string, int> { { "127.0.0.1:80", 1 } } } }
        };

        _mockApisixClient.Setup(c => c.GetServicesTypedAsync())
            .ReturnsAsync(services);

        var result = await _controller.GetServices();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<Service>>(okResult.Value);
        Assert.Single(returned);
        Assert.Equal("svc-1", returned[0].Id);
    }

    [Fact]
    public async Task GetServices_EmptyList_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetServicesTypedAsync())
            .ReturnsAsync(new List<Service>());

        var result = await _controller.GetServices();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<Service>>(okResult.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetServices_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetServicesTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetServices();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // GET /api/Service/{id}
    // ============================================================

    [Fact]
    public async Task GetService_ExistingId_ReturnsOk()
    {
        var service = new Service
        {
            Id = "svc-1",
            Name = "TestService",
            Upstream = new Upstream { Nodes = new Dictionary<string, int> { { "127.0.0.1:80", 1 } } }
        };

        _mockApisixClient.Setup(c => c.GetServiceAsync("svc-1"))
            .ReturnsAsync(service);

        var result = await _controller.GetService("svc-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<Service>(okResult.Value);
        Assert.Equal("svc-1", returned.Id);
        Assert.Equal("TestService", returned.Name);
    }

    [Fact]
    public async Task GetService_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetServiceAsync("nonexistent"))
            .ReturnsAsync((Service?)null);

        var result = await _controller.GetService("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetService_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetServiceAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.GetService("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // PUT /api/Service/{id} (CreateOrUpdate)
    // ============================================================

    [Fact]
    public async Task CreateOrUpdateService_ValidConfig_ReturnsOk()
    {
        var serviceConfig = new Service
        {
            Id = "svc-1",
            Name = "NewService",
            Upstream = new Upstream { Nodes = new Dictionary<string, int> { { "10.0.0.1:80", 1 } } }
        };

        _mockApisixClient.Setup(c => c.CreateServiceAsync("svc-1", serviceConfig))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateOrUpdateService("svc-1", serviceConfig);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "CreateOrUpdate" && e.Resource == "Service")),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateService_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.CreateOrUpdateService("svc-1", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateService_OnException_Returns500()
    {
        var serviceConfig = new Service
        {
            Name = "FailService",
            Upstream = new Upstream { Nodes = new Dictionary<string, int> { { "10.0.0.1:80", 1 } } }
        };

        _mockApisixClient.Setup(c => c.CreateServiceAsync("svc-err", serviceConfig))
            .ThrowsAsync(new Exception("APISIX error"));

        var result = await _controller.CreateOrUpdateService("svc-err", serviceConfig);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // DELETE /api/Service/{id}
    // ============================================================

    [Fact]
    public async Task DeleteService_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteServiceAsync("svc-del"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteService("svc-del");

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Delete" && e.Resource == "Service")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteService_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteServiceAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteService("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
