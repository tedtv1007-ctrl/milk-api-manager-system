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

public class UpstreamControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<UpstreamController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly UpstreamController _controller;

    public UpstreamControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<UpstreamController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new UpstreamController(
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
    // GET /api/Upstream (List)
    // ============================================================

    [Fact]
    public async Task GetUpstreams_ReturnsOk_WithUpstreamList()
    {
        var upstreams = new List<StandaloneUpstream>
        {
            new StandaloneUpstream { Id = "ups-1", Name = "Backend", Type = "roundrobin", Nodes = new Dictionary<string, int> { { "10.0.0.1:8080", 1 } } }
        };

        _mockApisixClient.Setup(c => c.GetUpstreamsTypedAsync())
            .ReturnsAsync(upstreams);

        var result = await _controller.GetUpstreams();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<StandaloneUpstream>>(okResult.Value);
        Assert.Single(returned);
        Assert.Equal("ups-1", returned[0].Id);
    }

    [Fact]
    public async Task GetUpstreams_EmptyList_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetUpstreamsTypedAsync())
            .ReturnsAsync(new List<StandaloneUpstream>());

        var result = await _controller.GetUpstreams();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<StandaloneUpstream>>(okResult.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetUpstreams_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetUpstreamsTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetUpstreams();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // GET /api/Upstream/{id}
    // ============================================================

    [Fact]
    public async Task GetUpstream_ExistingId_ReturnsOk()
    {
        var upstream = new StandaloneUpstream
        {
            Id = "ups-1",
            Name = "Backend",
            Type = "roundrobin",
            Nodes = new Dictionary<string, int> { { "10.0.0.1:8080", 1 } }
        };

        _mockApisixClient.Setup(c => c.GetUpstreamAsync("ups-1"))
            .ReturnsAsync(upstream);

        var result = await _controller.GetUpstream("ups-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<StandaloneUpstream>(okResult.Value);
        Assert.Equal("ups-1", returned.Id);
        Assert.Equal("roundrobin", returned.Type);
    }

    [Fact]
    public async Task GetUpstream_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetUpstreamAsync("nonexistent"))
            .ReturnsAsync((StandaloneUpstream?)null);

        var result = await _controller.GetUpstream("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUpstream_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetUpstreamAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.GetUpstream("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // PUT /api/Upstream/{id} (CreateOrUpdate)
    // ============================================================

    [Fact]
    public async Task CreateOrUpdateUpstream_ValidConfig_ReturnsOk()
    {
        var upstreamConfig = new StandaloneUpstream
        {
            Name = "NewUpstream",
            Type = "roundrobin",
            Nodes = new Dictionary<string, int> { { "10.0.0.2:80", 1 } }
        };

        _mockApisixClient.Setup(c => c.CreateUpstreamAsync("ups-new", It.IsAny<StandaloneUpstream>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateOrUpdateUpstream("ups-new", upstreamConfig);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "CreateOrUpdate" && e.Resource == "Upstream")),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateUpstream_SetsIdFromRoute()
    {
        var upstreamConfig = new StandaloneUpstream
        {
            Name = "Test",
            Type = "roundrobin",
            Nodes = new Dictionary<string, int> { { "10.0.0.1:80", 1 } }
        };

        StandaloneUpstream? captured = null;
        _mockApisixClient.Setup(c => c.CreateUpstreamAsync("ups-id-test", It.IsAny<StandaloneUpstream>()))
            .Callback<string, StandaloneUpstream>((id, config) => captured = config)
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        await _controller.CreateOrUpdateUpstream("ups-id-test", upstreamConfig);

        Assert.NotNull(captured);
        Assert.Equal("ups-id-test", captured!.Id);
    }

    [Fact]
    public async Task CreateOrUpdateUpstream_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.CreateOrUpdateUpstream("ups-1", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateUpstream_OnException_Returns500()
    {
        var upstreamConfig = new StandaloneUpstream
        {
            Name = "Fail",
            Type = "roundrobin",
            Nodes = new Dictionary<string, int> { { "10.0.0.1:80", 1 } }
        };

        _mockApisixClient.Setup(c => c.CreateUpstreamAsync("err", It.IsAny<StandaloneUpstream>()))
            .ThrowsAsync(new Exception("APISIX error"));

        var result = await _controller.CreateOrUpdateUpstream("err", upstreamConfig);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // DELETE /api/Upstream/{id}
    // ============================================================

    [Fact]
    public async Task DeleteUpstream_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteUpstreamAsync("ups-del"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteUpstream("ups-del");

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Delete" && e.Resource == "Upstream")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUpstream_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteUpstreamAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteUpstream("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
