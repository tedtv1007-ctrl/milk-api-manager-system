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

public class CachePolicyControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<CachePolicyController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly CachePolicyController _controller;

    public CachePolicyControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CachePolicyTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockLogger = new Mock<ILogger<CachePolicyController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new CachePolicyController(
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
        var items = Assert.IsAssignableFrom<IEnumerable<CachePolicy>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPolicies()
    {
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r1", CacheTtlSeconds = 300 });
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r2", CacheTtlSeconds = 600 });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<CachePolicy>>(okResult.Value);
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
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r1", CacheTtlSeconds = 300 });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByRouteId("r1", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var policy = Assert.IsType<CachePolicy>(okResult.Value);
        Assert.Equal("r1", policy.RouteId);
        Assert.Equal(300, policy.CacheTtlSeconds);
    }

    [Fact]
    public async Task Create_ValidPolicy_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var policy = new CachePolicy { RouteId = "r-new", CacheTtlSeconds = 120, CacheStrategy = "disk" };

        var result = await _controller.Create(policy, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<CachePolicy>(createdResult.Value);
        Assert.Equal("r-new", created.RouteId);
        Assert.Equal(120, created.CacheTtlSeconds);
    }

    [Fact]
    public async Task Create_DuplicateRouteId_ReturnsConflict()
    {
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r-dup" });
        await _dbContext.SaveChangesAsync();

        var policy = new CachePolicy { RouteId = "r-dup" };
        var result = await _controller.Create(policy, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_NullPolicy_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidTtl_ReturnsBadRequest()
    {
        var policy = new CachePolicy { RouteId = "r-bad", CacheTtlSeconds = -1 };
        var result = await _controller.Create(policy, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingPolicy_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r-upd", CacheTtlSeconds = 300 });
        await _dbContext.SaveChangesAsync();

        var update = new CachePolicy { RouteId = "r-upd", CacheTtlSeconds = 600, CacheStrategy = "disk" };
        var result = await _controller.Update("r-upd", update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<CachePolicy>(okResult.Value);
        Assert.Equal(600, updated.CacheTtlSeconds);
        Assert.Equal("disk", updated.CacheStrategy);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new CachePolicy { RouteId = "missing" };
        var result = await _controller.Update("missing", update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingPolicy_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        _dbContext.CachePolicies.Add(new CachePolicy { RouteId = "r-del" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete("r-del", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Delete("missing", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
