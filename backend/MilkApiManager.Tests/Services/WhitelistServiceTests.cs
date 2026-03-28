using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace MilkApiManager.Tests.Services;

/// <summary>
/// TDD tests for WhitelistService — validates business logic for route-scoped IP whitelisting.
/// Covers persistence, APISIX sync, audit logging, and edge cases.
/// </summary>
public class WhitelistServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IApisixClient> _mockApisix;
    private readonly Mock<IAuditLogService> _mockAudit;
    private readonly Mock<ILogger<WhitelistService>> _mockLogger;
    private readonly IConfiguration _config;
    private readonly WhitelistService _service;

    public WhitelistServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"WhitelistTest_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        _mockApisix = new Mock<IApisixClient>();
        _mockAudit = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<WhitelistService>>();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Whitelist:PersistToDatabase", "true" }
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        _service = new WhitelistService(_mockApisix.Object, _db, _config, _mockAudit.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetWhitelistForRouteAsync_WithPersistence_ReturnsDbEntries()
    {
        // Arrange
        var routeId = "route-1";
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = "10.0.0.1",
            AddedAt = DateTime.UtcNow
        });
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = "10.0.0.2",
            AddedAt = DateTime.UtcNow
        });
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = "other-route",
            IpCidr = "10.0.0.3",
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetWhitelistForRouteAsync(routeId);

        // Assert — only entries for the specified route
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(routeId, e.RouteId));
    }

    [Fact]
    public async Task GetWhitelistForRouteAsync_ExcludesExpiredEntries()
    {
        // Arrange
        var routeId = "route-expiry";
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = "10.0.1.1",
            AddedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        });
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = "10.0.1.2",
            AddedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Active
        });
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = "10.0.1.3",
            AddedAt = DateTime.UtcNow,
            ExpiresAt = null // No expiry = always active
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetWhitelistForRouteAsync(routeId);

        // Assert — expired entry should be excluded
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddAsync_NewIp_AddsToDbAndSyncsToApisix()
    {
        // Arrange
        var routeId = "route-add";
        var request = new WhitelistUpdateRequest
        {
            IpCidr = "192.168.1.100",
            Reason = "Trusted partner",
            AddedBy = "admin"
        };

        // Act
        var result = await _service.AddAsync(routeId, request);

        // Assert
        Assert.Contains("added successfully", result);
        var entry = await _db.WhitelistEntries.FirstOrDefaultAsync(w =>
            w.RouteId == routeId && w.IpCidr == "192.168.1.100");
        Assert.NotNull(entry);
        Assert.Equal("Trusted partner", entry.Reason);
        _mockApisix.Verify(a => a.UpdateWhitelistForRouteAsync(routeId, It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DuplicateIp_DoesNotCreateDuplicateEntry()
    {
        // Arrange
        var routeId = "route-dup";
        var ip = "172.16.0.1";
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = ip,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var request = new WhitelistUpdateRequest { IpCidr = "172.16.0.1", AddedBy = "admin" };

        // Act
        await _service.AddAsync(routeId, request);

        // Assert — no duplicates
        var count = await _db.WhitelistEntries.CountAsync(w => w.RouteId == routeId && w.IpCidr == ip);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveAsync_ExistingIp_RemovesFromDbAndSyncsToApisix()
    {
        // Arrange
        var routeId = "route-remove";
        var ip = "10.10.10.10";
        _db.WhitelistEntries.Add(new WhitelistEntry
        {
            RouteId = routeId,
            IpCidr = ip,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var request = new WhitelistUpdateRequest { IpCidr = "10.10.10.10", AddedBy = "admin" };

        // Act
        var result = await _service.RemoveAsync(routeId, request);

        // Assert
        Assert.Contains("removed successfully", result);
        var entry = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == ip);
        Assert.Null(entry);
        _mockApisix.Verify(a => a.UpdateWhitelistForRouteAsync(routeId, It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentIp_DoesNotThrow()
    {
        // Arrange
        var routeId = "route-nonexist";
        var request = new WhitelistUpdateRequest { IpCidr = "99.99.99.99", AddedBy = "admin" };

        // Act & Assert — should not throw
        var result = await _service.RemoveAsync(routeId, request);
        Assert.Contains("removed successfully", result);
    }

    [Fact]
    public async Task AddAsync_WritesAuditLog()
    {
        // Arrange
        var routeId = "route-audit";
        var request = new WhitelistUpdateRequest
        {
            IpCidr = "10.20.30.40",
            Reason = "Audit test",
            AddedBy = "tester"
        };

        // Act
        await _service.AddAsync(routeId, request);

        // Assert — audit log should be called
        _mockAudit.Verify(a => a.LogAsync(It.Is<AuditLogEntry>(e =>
            e.Action == "Create" &&
            e.Resource == "Whitelist"
        )), Times.Once);
    }

    [Fact]
    public async Task GetWhitelistForRouteAsync_WithoutPersistence_QueriesApisixDirectly()
    {
        // Arrange — use config without persistence
        var noPersistConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Whitelist:PersistToDatabase", "false" }
            })
            .Build();
        var service = new WhitelistService(_mockApisix.Object, _db, noPersistConfig, _mockAudit.Object, _mockLogger.Object);

        _mockApisix.Setup(a => a.GetWhitelistForRouteAsync("route-x"))
            .ReturnsAsync(new List<string> { "1.2.3.4", "5.6.7.8" });

        // Act
        var result = await service.GetWhitelistForRouteAsync("route-x");

        // Assert
        Assert.Equal(2, result.Count);
        _mockApisix.Verify(a => a.GetWhitelistForRouteAsync("route-x"), Times.Once);
    }
}
