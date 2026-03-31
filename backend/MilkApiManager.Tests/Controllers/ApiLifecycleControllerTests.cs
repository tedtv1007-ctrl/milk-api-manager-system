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

public class ApiLifecycleControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<ApiLifecycleController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly ApiLifecycleController _controller;

    public ApiLifecycleControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ApiLifecycleTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockLogger = new Mock<ILogger<ApiLifecycleController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new ApiLifecycleController(
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
        var items = Assert.IsAssignableFrom<IEnumerable<ApiLifecycleEntry>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEntries()
    {
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "user-api", Version = "v1", Status = "active" });
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "user-api", Version = "v2", Status = "planning" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAll(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<ApiLifecycleEntry>>(okResult.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetByApiIdentifier_ReturnsOk_WithVersions()
    {
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "order-api", Version = "v1", Status = "deprecated" });
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "order-api", Version = "v2", Status = "active" });
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "other-api", Version = "v1", Status = "active" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByApiIdentifier("order-api", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<ApiLifecycleEntry>>(okResult.Value);
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
        var entity = new ApiLifecycleEntry { ApiIdentifier = "user-api", Version = "v1", Status = "active" };
        _dbContext.ApiLifecycleEntries.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetById(entity.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var entry = Assert.IsType<ApiLifecycleEntry>(okResult.Value);
        Assert.Equal("user-api", entry.ApiIdentifier);
    }

    [Fact]
    public async Task Create_ValidEntry_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entry = new ApiLifecycleEntry { ApiIdentifier = "payment-api", Version = "v1", Status = "active" };

        var result = await _controller.Create(entry, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<ApiLifecycleEntry>(createdResult.Value);
        Assert.Equal("payment-api", created.ApiIdentifier);
        Assert.Equal("v1", created.Version);
    }

    [Fact]
    public async Task Create_NullEntry_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidStatus_ReturnsBadRequest()
    {
        var entry = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "invalid_status" };
        var result = await _controller.Create(entry, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_DuplicateApiVersion_ReturnsConflict()
    {
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "dup-api", Version = "v1", Status = "active" });
        await _dbContext.SaveChangesAsync();

        var entry = new ApiLifecycleEntry { ApiIdentifier = "dup-api", Version = "v1", Status = "planning" };
        var result = await _controller.Create(entry, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Deprecate_ActiveEntry_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "active" };
        _dbContext.ApiLifecycleEntries.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Deprecate(entity.Id, "Use v2 instead", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var deprecated = Assert.IsType<ApiLifecycleEntry>(okResult.Value);
        Assert.Equal("deprecated", deprecated.Status);
        Assert.NotNull(deprecated.DeprecatedAt);
        Assert.Equal("Use v2 instead", deprecated.DeprecationNotice);
    }

    [Fact]
    public async Task Deprecate_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Deprecate(9999, "msg", CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Retire_DeprecatedEntry_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "deprecated" };
        _dbContext.ApiLifecycleEntries.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.Retire(entity.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var retired = Assert.IsType<ApiLifecycleEntry>(okResult.Value);
        Assert.Equal("retired", retired.Status);
        Assert.NotNull(retired.RetiredAt);
    }

    [Fact]
    public async Task Retire_NonExisting_ReturnsNotFound()
    {
        var result = await _controller.Retire(9999, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingEntry_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "active", OwnerTeam = "team-a" };
        _dbContext.ApiLifecycleEntries.Add(entity);
        await _dbContext.SaveChangesAsync();

        var update = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "active", OwnerTeam = "team-b", SuccessorUrl = "/api/v2" };
        var result = await _controller.Update(entity.Id, update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<ApiLifecycleEntry>(okResult.Value);
        Assert.Equal("team-b", updated.OwnerTeam);
        Assert.Equal("/api/v2", updated.SuccessorUrl);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "active" };
        var result = await _controller.Update(9999, update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingEntry_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new ApiLifecycleEntry { ApiIdentifier = "api", Version = "v1", Status = "retired" };
        _dbContext.ApiLifecycleEntries.Add(entity);
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

    [Fact]
    public async Task GetDeprecated_ReturnsOnlyDeprecatedEntries()
    {
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "api-a", Version = "v1", Status = "deprecated" });
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "api-b", Version = "v1", Status = "active" });
        _dbContext.ApiLifecycleEntries.Add(new ApiLifecycleEntry { ApiIdentifier = "api-c", Version = "v1", Status = "deprecated" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetDeprecated(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<ApiLifecycleEntry>>(okResult.Value);
        Assert.Equal(2, items.Count());
        Assert.All(items, i => Assert.Equal("deprecated", i.Status));
    }
}
