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

public class CircuitBreakerControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<CircuitBreakerController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly CircuitBreakerController _controller;

    public CircuitBreakerControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CircuitBreakerTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<CircuitBreakerController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new CircuitBreakerController(
            _dbContext,
            _mockApisixClient.Object,
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
        var items = Assert.IsAssignableFrom<IEnumerable<CircuitBreakerConfig>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithConfigs()
    {
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-1", BreakResponseCode = 502 });
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-2", BreakResponseCode = 503 });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<CircuitBreakerConfig>>(okResult.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetByRouteId_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetByRouteId("nonexistent", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetByRouteId_ReturnsOk_WhenExists()
    {
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-1", BreakResponseCode = 502 });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByRouteId("route-1", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var config = Assert.IsType<CircuitBreakerConfig>(okResult.Value);
        Assert.Equal("route-1", config.RouteId);
    }

    [Fact]
    public async Task Create_ValidConfig_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var config = new CircuitBreakerConfig { RouteId = "route-new", BreakResponseCode = 503, UnhealthyFailures = 5 };

        var result = await _controller.Create(config, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<CircuitBreakerConfig>(createdResult.Value);
        Assert.Equal("route-new", created.RouteId);
        Assert.Equal(503, created.BreakResponseCode);
    }

    [Fact]
    public async Task Create_DuplicateRouteId_ReturnsConflict()
    {
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-dup" });
        await _dbContext.SaveChangesAsync();

        var config = new CircuitBreakerConfig { RouteId = "route-dup" };
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
    public async Task Update_ExistingConfig_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-upd", BreakResponseCode = 502 });
        await _dbContext.SaveChangesAsync();

        var update = new CircuitBreakerConfig { RouteId = "route-upd", BreakResponseCode = 503, MaxBreakerSec = 600 };
        var result = await _controller.Update("route-upd", update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<CircuitBreakerConfig>(okResult.Value);
        Assert.Equal(503, updated.BreakResponseCode);
        Assert.Equal(600, updated.MaxBreakerSec);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new CircuitBreakerConfig { RouteId = "missing", BreakResponseCode = 503 };
        var result = await _controller.Update("missing", update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingConfig_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.CircuitBreakerConfigs.Add(new CircuitBreakerConfig { RouteId = "route-del" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete("route-del", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await _dbContext.CircuitBreakerConfigs.FirstOrDefaultAsync(c => c.RouteId == "route-del"));
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Delete("missing", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
