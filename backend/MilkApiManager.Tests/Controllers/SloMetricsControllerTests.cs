using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Tests.Controllers;

public class SloMetricsControllerTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<BlacklistConsistencyService> _mockConsistencyService;

    public SloMetricsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        var apisixClient = new Mock<IApisixClient>();
        _mockConsistencyService = new Mock<BlacklistConsistencyService>(
            _dbContext,
            apisixClient.Object,
            Mock.Of<ILogger<BlacklistConsistencyService>>());
    }

    [Fact]
    public async Task Get_ReturnsPrometheusFormattedMetrics()
    {
        _dbContext.AuditLogs.AddRange(
            new AuditLogEntry { Timestamp = DateTime.UtcNow.AddMinutes(-1), StatusCode = 200, Action = "Read", Resource = "Route", User = "u1" },
            new AuditLogEntry { Timestamp = DateTime.UtcNow.AddMinutes(-1), StatusCode = 500, Action = "Update", Resource = "Route", User = "u2" });

        _dbContext.SyncOutboxEntries.AddRange(
            new SyncOutboxEntry { CreatedAt = DateTime.UtcNow.AddSeconds(-12), ProcessedAt = DateTime.UtcNow.AddSeconds(-2), EventType = SyncOutboxEventType.BlacklistSync, Status = SyncOutboxStatus.Completed },
            new SyncOutboxEntry { CreatedAt = DateTime.UtcNow.AddSeconds(-20), ProcessedAt = DateTime.UtcNow.AddSeconds(-5), EventType = SyncOutboxEventType.AuditLogShip, Status = SyncOutboxStatus.Completed });

        await _dbContext.SaveChangesAsync();

        _mockConsistencyService
            .Setup(s => s.GetBlacklistDriftReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlacklistDriftReport
            {
                DatabaseOnly = new List<string> { "10.0.0.1" },
                GatewayOnly = new List<string> { "10.0.0.2", "10.0.0.3" }
            });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Slo:WindowMinutes"] = "60" })
            .Build();

        var controller = new SloMetricsController(
            _dbContext,
            _mockConsistencyService.Object,
            config,
            Mock.Of<ILogger<SloMetricsController>>());

        var result = await controller.Get(CancellationToken.None);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/plain; version=0.0.4", contentResult.ContentType);
        Assert.NotNull(contentResult.Content);
        Assert.Contains("milk_control_plane_success_rate_percent", contentResult.Content);
        Assert.Contains("milk_sync_latency_p95_seconds", contentResult.Content);
        Assert.Contains("milk_blacklist_drift_count 3", contentResult.Content);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
