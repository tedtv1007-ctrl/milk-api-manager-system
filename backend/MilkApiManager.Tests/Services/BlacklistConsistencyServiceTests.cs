using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class BlacklistConsistencyServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly BlacklistConsistencyService _service;

    public BlacklistConsistencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _mockApisixClient = new Mock<IApisixClient>();
        _service = new BlacklistConsistencyService(_dbContext, _mockApisixClient.Object, Mock.Of<ILogger<BlacklistConsistencyService>>());
    }

    [Fact]
    public async Task GetBlacklistDriftReportAsync_ReturnsDatabaseOnlyAndGatewayOnly()
    {
        _dbContext.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = "10.0.0.1", AddedAt = DateTime.UtcNow });
        _dbContext.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = "10.0.0.2", AddedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string> { "10.0.0.2", "10.0.0.3" });

        var report = await _service.GetBlacklistDriftReportAsync();

        Assert.False(report.IsInSync);
        Assert.Single(report.DatabaseOnly);
        Assert.Contains("10.0.0.1", report.DatabaseOnly);
        Assert.Single(report.GatewayOnly);
        Assert.Contains("10.0.0.3", report.GatewayOnly);
    }

    [Fact]
    public async Task ReconcileDatabaseToGatewayAsync_PushesDbStateAndReturnsInSync()
    {
        _dbContext.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = "192.168.1.1", AddedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.Is<List<string>>(l => l.SequenceEqual(new[] { "192.168.1.1" }))))
            .Returns(Task.CompletedTask);
        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string> { "192.168.1.1" });

        var report = await _service.ReconcileDatabaseToGatewayAsync();

        Assert.True(report.IsInSync);
        _mockApisixClient.Verify(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>()), Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
