using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class HealthCheckControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<HealthCheckController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly HealthCheckController _controller;

    public HealthCheckControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"HealthCheckTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockLogger = new Mock<ILogger<HealthCheckController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new HealthCheckController(
            _dbContext,
            _mockLogger.Object,
            _mockAuditLogService.Object
        );
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEmptyList()
    {
        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<HealthCheckConfig>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithConfigs()
    {
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-1" });
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-2" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<HealthCheckConfig>>(okResult.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetByUpstreamId_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetByUpstreamId("nonexistent", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetByUpstreamId_ReturnsOk_WhenExists()
    {
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-1", ActiveHttpPath = "/status" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByUpstreamId("ups-1", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var config = Assert.IsType<HealthCheckConfig>(okResult.Value);
        Assert.Equal("ups-1", config.UpstreamId);
        Assert.Equal("/status", config.ActiveHttpPath);
    }

    [Fact]
    public async Task Create_ValidConfig_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var config = new HealthCheckConfig { UpstreamId = "ups-new", ActiveIntervalSeconds = 30, ActiveHttpPath = "/ping" };

        var result = await _controller.Create(config, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<HealthCheckConfig>(createdResult.Value);
        Assert.Equal("ups-new", created.UpstreamId);
        Assert.Equal(30, created.ActiveIntervalSeconds);
    }

    [Fact]
    public async Task Create_DuplicateUpstreamId_ReturnsConflict()
    {
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-dup" });
        await _dbContext.SaveChangesAsync();

        var config = new HealthCheckConfig { UpstreamId = "ups-dup" };
        var result = await _controller.Create(config, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidInterval_ReturnsBadRequest()
    {
        var config = new HealthCheckConfig { UpstreamId = "ups-bad", ActiveIntervalSeconds = 0 };
        var result = await _controller.Create(config, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingConfig_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-upd", ActiveIntervalSeconds = 10 });
        await _dbContext.SaveChangesAsync();

        var update = new HealthCheckConfig { UpstreamId = "ups-upd", ActiveIntervalSeconds = 30, ActiveHttpPath = "/ready" };
        var result = await _controller.Update("ups-upd", update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<HealthCheckConfig>(okResult.Value);
        Assert.Equal(30, updated.ActiveIntervalSeconds);
        Assert.Equal("/ready", updated.ActiveHttpPath);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new HealthCheckConfig { UpstreamId = "missing" };
        var result = await _controller.Update("missing", update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingConfig_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.HealthCheckConfigs.Add(new HealthCheckConfig { UpstreamId = "ups-del" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete("ups-del", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Delete("missing", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
