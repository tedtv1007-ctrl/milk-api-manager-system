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

public class CanaryReleaseControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<CanaryReleaseController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly CanaryReleaseController _controller;

    public CanaryReleaseControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CanaryReleaseTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockLogger = new Mock<ILogger<CanaryReleaseController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new CanaryReleaseController(
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
        var items = Assert.IsAssignableFrom<IEnumerable<CanaryRelease>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithReleases()
    {
        _dbContext.CanaryReleases.Add(new CanaryRelease { RouteId = "r1", Name = "canary-1", StableUpstreamId = "s1", CanaryUpstreamId = "c1" });
        _dbContext.CanaryReleases.Add(new CanaryRelease { RouteId = "r2", Name = "canary-2", StableUpstreamId = "s2", CanaryUpstreamId = "c2" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<CanaryRelease>>(okResult.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _controller.GetById(9999, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenExists()
    {
        var entity = new CanaryRelease { RouteId = "r1", Name = "canary-test", StableUpstreamId = "s1", CanaryUpstreamId = "c1", StableWeight = 80, CanaryWeight = 20 };
        _dbContext.CanaryReleases.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetById(entity.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var release = Assert.IsType<CanaryRelease>(okResult.Value);
        Assert.Equal("canary-test", release.Name);
        Assert.Equal(80, release.StableWeight);
        Assert.Equal(20, release.CanaryWeight);
    }

    [Fact]
    public async Task Create_ValidRelease_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var release = new CanaryRelease
        {
            RouteId = "r-new",
            Name = "v2-canary",
            StableUpstreamId = "stable-1",
            CanaryUpstreamId = "canary-1",
            StableWeight = 90,
            CanaryWeight = 10
        };

        var result = await _controller.Create(release, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<CanaryRelease>(createdResult.Value);
        Assert.Equal("v2-canary", created.Name);
        Assert.Equal("active", created.Status);
    }

    [Fact]
    public async Task Create_NullRelease_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidWeights_ReturnsBadRequest()
    {
        var release = new CanaryRelease
        {
            RouteId = "r1", Name = "bad", StableUpstreamId = "s1", CanaryUpstreamId = "c1",
            StableWeight = 60, CanaryWeight = 60
        };
        var result = await _controller.Create(release, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingRelease_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new CanaryRelease { RouteId = "r1", Name = "canary-upd", StableUpstreamId = "s1", CanaryUpstreamId = "c1", StableWeight = 90, CanaryWeight = 10 };
        _dbContext.CanaryReleases.Add(entity);
        await _dbContext.SaveChangesAsync();

        var update = new CanaryRelease
        {
            RouteId = "r1", Name = "canary-upd", StableUpstreamId = "s1", CanaryUpstreamId = "c1",
            StableWeight = 50, CanaryWeight = 50
        };
        var result = await _controller.Update(entity.Id, update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<CanaryRelease>(okResult.Value);
        Assert.Equal(50, updated.StableWeight);
        Assert.Equal(50, updated.CanaryWeight);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new CanaryRelease { RouteId = "r1", Name = "nope", StableUpstreamId = "s1", CanaryUpstreamId = "c1" };
        var result = await _controller.Update(9999, update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Rollback_ActiveRelease_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new CanaryRelease { RouteId = "r1", Name = "canary-rb", StableUpstreamId = "s1", CanaryUpstreamId = "c1", Status = "active" };
        _dbContext.CanaryReleases.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Rollback(entity.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var rolledBack = Assert.IsType<CanaryRelease>(okResult.Value);
        Assert.Equal("rolled_back", rolledBack.Status);
        Assert.Equal(100, rolledBack.StableWeight);
        Assert.Equal(0, rolledBack.CanaryWeight);
    }

    [Fact]
    public async Task Rollback_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Rollback(9999, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Promote_ActiveRelease_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new CanaryRelease { RouteId = "r1", Name = "canary-promo", StableUpstreamId = "s1", CanaryUpstreamId = "c1", Status = "active" };
        _dbContext.CanaryReleases.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Promote(entity.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var promoted = Assert.IsType<CanaryRelease>(okResult.Value);
        Assert.Equal("completed", promoted.Status);
        Assert.Equal(0, promoted.StableWeight);
        Assert.Equal(100, promoted.CanaryWeight);
    }

    [Fact]
    public async Task Delete_ExistingRelease_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new CanaryRelease { RouteId = "r1", Name = "canary-del", StableUpstreamId = "s1", CanaryUpstreamId = "c1" };
        _dbContext.CanaryReleases.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Delete(entity.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Delete(9999, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
