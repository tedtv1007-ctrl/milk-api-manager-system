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

public class BlacklistServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IApisixClient> _mockApisix;
    private readonly Mock<IAuditLogService> _mockAudit;
    private readonly Mock<ILogger<BlacklistService>> _mockLogger;
    private readonly IConfiguration _config;
    private readonly BlacklistService _service;

    public BlacklistServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        
        _mockApisix = new Mock<IApisixClient>();
        _mockAudit = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<BlacklistService>>();
        
        // Setup config
        var inMemoryConfig = new Dictionary<string, string?> {
            {"Blacklist:PersistToDatabase", "true"},
            {"Sync:Blacklist:UseOutbox", "false"}
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        // For simplicity in unit tests, we pass null for outbox if not used, 
        // or a mock if we want to test that path.
        _service = new BlacklistService(_mockApisix.Object, _db, _config, _mockAudit.Object, null!, _mockLogger.Object);
    }

    [Fact]
    public async Task AddAsync_ValidIp_AddsToDbAndGateway()
    {
        var request = new BlacklistUpdateRequest { Ip = "1.2.3.4", Action = "add", AddedBy = "test" };
        _mockApisix.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string>());

        await _service.AddAsync(request);

        var entry = await _db.BlacklistEntries.FirstOrDefaultAsync(e => e.IpOrCidr == "1.2.3.4");
        Assert.NotNull(entry);
        _mockApisix.Verify(c => c.UpdateBlacklistAsync(It.Is<List<string>>(l => l.Contains("1.2.3.4"))), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ExistingIp_RemovesFromDbAndGateway()
    {
        var ip = "5.5.5.5";
        _db.BlacklistEntries.Add(new BlacklistEntry { IpOrCidr = ip });
        await _db.SaveChangesAsync();

        _mockApisix.Setup(c => c.GetBlacklistAsync()).ReturnsAsync(new List<string> { "5.5.5.5" });

        var request = new BlacklistUpdateRequest { Ip = "5.5.5.5", Action = "remove" };
        await _service.RemoveAsync(request);

        var entry = await _db.BlacklistEntries.FirstOrDefaultAsync(e => e.IpOrCidr == ip);
        Assert.Null(entry);
        _mockApisix.Verify(c => c.UpdateBlacklistAsync(It.Is<List<string>>(l => !l.Contains("5.5.5.5"))), Times.Once);
    }
}
