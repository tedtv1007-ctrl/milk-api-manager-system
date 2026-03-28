using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class BlacklistConsistencyServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IApisixClient> _mockApisix;
    private readonly Mock<ILogger<BlacklistConsistencyService>> _mockLogger;
    private readonly BlacklistConsistencyService _service;

    public BlacklistConsistencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _mockApisix = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<BlacklistConsistencyService>>();
        _service = new BlacklistConsistencyService(_db, _mockApisix.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBlacklistDriftReportAsync_NoDrift_ReturnsEmptyReport()
    {
        // Arrange
        var ip = IPAddress.Parse("1.1.1.1");
        _db.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = ip });
        await _db.SaveChangesAsync();

        _mockApisix.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "1.1.1.1" });

        // Act
        var report = await _service.GetBlacklistDriftReportAsync(CancellationToken.None);

        // Assert
        Assert.Empty(report.DatabaseOnly);
        Assert.Empty(report.GatewayOnly);
        Assert.True(report.IsInSync);
    }

    [Fact]
    public async Task GetBlacklistDriftReportAsync_WithDrift_IdentifiesIssues()
    {
        // Arrange
        _db.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = IPAddress.Parse("1.1.1.1") }); // In DB but not APISIX
        await _db.SaveChangesAsync();

        _mockApisix.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "2.2.2.2" }); // In APISIX but not DB

        // Act
        var report = await _service.GetBlacklistDriftReportAsync(CancellationToken.None);

        // Assert
        Assert.Contains("1.1.1.1", report.DatabaseOnly);
        Assert.Contains("2.2.2.2", report.GatewayOnly);
        Assert.False(report.IsInSync);
    }
}
