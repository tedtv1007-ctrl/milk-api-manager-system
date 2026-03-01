using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

/// <summary>
/// T-1: Unit tests for BlacklistService (extracted from controller logic).
/// </summary>
public class BlacklistServiceTests : IDisposable
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly AppDbContext _dbContext;
    private readonly Mock<IAuditLogService> _mockAuditLog;
    private readonly Mock<ApisixSyncOutboxService> _mockOutboxService;
    private readonly Mock<ILogger<BlacklistService>> _mockLogger;

    public BlacklistServiceTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockAuditLog = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<BlacklistService>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _mockOutboxService = new Mock<ApisixSyncOutboxService>(
            _dbContext,
            Mock.Of<ILogger<ApisixSyncOutboxService>>());
    }

    private BlacklistService CreateService(bool persistToDb = true, bool useOutbox = false)
    {
        var configData = new Dictionary<string, string?>
        {
            { "Blacklist:PersistToDatabase", persistToDb.ToString() },
            { "Sync:Blacklist:UseOutbox", useOutbox.ToString() }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new BlacklistService(
            _mockApisixClient.Object,
            _dbContext,
            config,
            _mockAuditLog.Object,
            _mockOutboxService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetBlacklistAsync_WithDbPersistence_ReturnsDbEntries()
    {
        _dbContext.BlacklistEntries.Add(new BlacklistEntry
        {
            IpOrCidr = "192.168.1.100",
            Reason = "Test",
            AddedBy = "UnitTest",
            AddedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var service = CreateService(persistToDb: true);
        var result = await service.GetBlacklistAsync();

        Assert.Single(result);
        Assert.Equal("192.168.1.100", result[0].IpOrCidr);
    }

    [Fact]
    public async Task GetBlacklistAsync_WithoutDbPersistence_ReturnsApisixData()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string> { "10.0.0.1", "10.0.0.2" });

        var service = CreateService(persistToDb: false);
        var result = await service.GetBlacklistAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("10.0.0.1", result[0].IpOrCidr);
    }

    [Fact]
    public async Task AddAsync_PersistsToDb_AndSyncsToGateway()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        _mockAuditLog.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        var service = CreateService(persistToDb: true);
        var request = new BlacklistUpdateRequest
        {
            Ip = "192.168.1.50",
            Action = "add",
            Reason = "Unit test",
            AddedBy = "tester"
        };

        var result = await service.AddAsync(request);

        Assert.Contains("192.168.1.50", result);
        Assert.Contains("added", result);

        var entry = await _dbContext.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == "192.168.1.50");
        Assert.NotNull(entry);
        Assert.Equal("Unit test", entry.Reason);

        _mockApisixClient.Verify(c => c.UpdateBlacklistAsync(
            It.Is<List<string>>(l => l.Contains("192.168.1.50"))), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DuplicateIp_DoesNotDuplicateInDb()
    {
        _dbContext.BlacklistEntries.Add(new BlacklistEntry
        {
            IpOrCidr = "10.0.0.1",
            AddedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "10.0.0.1" });
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);

        var service = CreateService(persistToDb: true);
        var request = new BlacklistUpdateRequest { Ip = "10.0.0.1", Action = "add" };

        await service.AddAsync(request);

        var count = await _dbContext.BlacklistEntries.CountAsync(b => b.IpOrCidr == "10.0.0.1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromDb_AndSyncsToGateway()
    {
        _dbContext.BlacklistEntries.Add(new BlacklistEntry
        {
            IpOrCidr = "10.0.0.5",
            Reason = "To be removed",
            AddedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "10.0.0.5" });
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        _mockAuditLog.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        var service = CreateService(persistToDb: true);
        var request = new BlacklistUpdateRequest { Ip = "10.0.0.5", Action = "remove" };

        var result = await service.RemoveAsync(request);

        Assert.Contains("removed", result);
        var entry = await _dbContext.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == "10.0.0.5");
        Assert.Null(entry);

        _mockApisixClient.Verify(c => c.UpdateBlacklistAsync(
            It.Is<List<string>>(l => !l.Contains("10.0.0.5"))), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithOutbox_EnqueuesInsteadOfDirectSync()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());
        _mockOutboxService.Setup(o => o.EnqueueBlacklistSyncAsync(
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockAuditLog.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        var service = CreateService(persistToDb: true, useOutbox: true);
        var request = new BlacklistUpdateRequest { Ip = "172.16.1.10", Action = "add", AddedBy = "tester" };

        await service.AddAsync(request);

        _mockOutboxService.Verify(o => o.EnqueueBlacklistSyncAsync(
            It.Is<List<string>>(l => l.Contains("172.16.1.10")),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockApisixClient.Verify(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_AuditLogFailure_DoesNotBlockOperation()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        _mockAuditLog.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .ThrowsAsync(new Exception("Audit log down"));

        var service = CreateService(persistToDb: true);
        var request = new BlacklistUpdateRequest { Ip = "1.2.3.4", Action = "add", AddedBy = "admin" };

        // Should not throw even if audit log fails
        var result = await service.AddAsync(request);
        Assert.Contains("added", result);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentIp_CompletesWithoutError()
    {
        _mockApisixClient.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "10.0.0.1" });
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);

        var service = CreateService(persistToDb: true);
        var request = new BlacklistUpdateRequest { Ip = "99.99.99.99", Action = "remove" };

        var result = await service.RemoveAsync(request);
        Assert.Contains("removed", result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
