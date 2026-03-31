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

public class TransformControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<TransformController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly TransformController _controller;

    public TransformControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TransformTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mockLogger = new Mock<ILogger<TransformController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new TransformController(
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
        var items = Assert.IsAssignableFrom<IEnumerable<RequestTransformRule>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetByRouteId_ReturnsOk_WithRules()
    {
        _dbContext.RequestTransformRules.Add(new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-Custom", Value = "test" });
        _dbContext.RequestTransformRules.Add(new RequestTransformRule { RouteId = "r1", Phase = "response", OperationType = "remove_header", Key = "Server" });
        _dbContext.RequestTransformRules.Add(new RequestTransformRule { RouteId = "r2", Phase = "request", OperationType = "add_header", Key = "X-Other", Value = "other" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByRouteId("r1", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<RequestTransformRule>>(okResult.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetByRouteId_ReturnsOk_EmptyWhenNoRules()
    {
        var result = await _controller.GetByRouteId("nonexistent", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<RequestTransformRule>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Create_ValidRule_ReturnsCreated()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var rule = new RequestTransformRule { RouteId = "r-new", Phase = "request", OperationType = "add_header", Key = "X-Test", Value = "val" };

        var result = await _controller.Create(rule, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var created = Assert.IsType<RequestTransformRule>(createdResult.Value);
        Assert.Equal("r-new", created.RouteId);
        Assert.Equal("add_header", created.OperationType);
    }

    [Fact]
    public async Task Create_NullRule_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidPhase_ReturnsBadRequest()
    {
        var rule = new RequestTransformRule { RouteId = "r1", Phase = "invalid_phase", OperationType = "add_header", Key = "X-Test" };
        var result = await _controller.Create(rule, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidOperation_ReturnsBadRequest()
    {
        var rule = new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "invalid_op", Key = "X-Test" };
        var result = await _controller.Create(rule, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingRule_ReturnsOk()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-Old", Value = "old" };
        _dbContext.RequestTransformRules.Add(entity);
        await _dbContext.SaveChangesAsync();

        var update = new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-New", Value = "new" };
        var result = await _controller.Update(entity.Id, update, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<RequestTransformRule>(okResult.Value);
        Assert.Equal("X-New", updated.Key);
        Assert.Equal("new", updated.Value);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsNotFound()
    {
        var update = new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-Test" };
        var result = await _controller.Update(9999, update, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingRule_ReturnsNoContent()
    {
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var entity = new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-Del" };
        _dbContext.RequestTransformRules.Add(entity);
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
    public async Task GetByRouteId_ReturnsOrderedByPriority()
    {
        _dbContext.RequestTransformRules.Add(new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-Second", Priority = 20 });
        _dbContext.RequestTransformRules.Add(new RequestTransformRule { RouteId = "r1", Phase = "request", OperationType = "add_header", Key = "X-First", Priority = 10 });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetByRouteId("r1", CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<RequestTransformRule>>(okResult.Value).ToList();
        Assert.Equal("X-First", items[0].Key);
        Assert.Equal("X-Second", items[1].Key);
    }
}
