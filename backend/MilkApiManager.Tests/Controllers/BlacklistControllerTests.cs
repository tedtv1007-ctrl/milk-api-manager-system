using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class BlacklistControllerTests : IDisposable
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<BlacklistController>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public BlacklistControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        Environment.SetEnvironmentVariable("APISIX_ADMIN_URL", "http://localhost:9180/apisix/admin/");
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );
        _mockLogger = new Mock<ILogger<BlacklistController>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
    }

    private BlacklistController CreateController(bool persistToDb = true)
    {
        var configData = new Dictionary<string, string?>
        {
            { "Blacklist:PersistToDatabase", persistToDb.ToString() }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new BlacklistController(_mockApisixClient.Object, _mockLogger.Object, _dbContext, config);
    }

    [Fact]
    public async Task GetBlacklist_WithDbPersistence_ReturnsDbEntries()
    {
        // Arrange
        _dbContext.BlacklistEntries.Add(new BlacklistEntry
        {
            IpOrCidr = "192.168.1.100",
            Reason = "Test",
            AddedBy = "UnitTest",
            AddedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var controller = CreateController(persistToDb: true);

        // Act
        var result = await controller.GetBlacklist();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var entries = Assert.IsAssignableFrom<List<BlacklistEntry>>(okResult.Value);
        Assert.Single(entries);
        Assert.Equal("192.168.1.100", entries[0].IpOrCidr);
    }

    [Fact]
    public async Task GetBlacklist_WithoutDbPersistence_ReturnsApisixData()
    {
        // Arrange
        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string> { "10.0.0.1", "10.0.0.2" });

        var controller = CreateController(persistToDb: false);

        // Act
        var result = await controller.GetBlacklist();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var entries = Assert.IsAssignableFrom<List<BlacklistEntry>>(okResult.Value);
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task UpdateBlacklist_AddIp_Success()
    {
        // Arrange
        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string>());
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(persistToDb: true);
        var request = new BlacklistUpdateRequest
        {
            Ip = "192.168.1.200",
            Action = "add",
            Reason = "Suspicious activity",
            AddedBy = "admin"
        };

        // Act
        var result = await controller.UpdateBlacklist(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockApisixClient.Verify(c => c.UpdateBlacklistAsync(
            It.Is<List<string>>(l => l.Contains("192.168.1.200"))), Times.Once);

        // Verify DB persistence
        var entry = await _dbContext.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == "192.168.1.200");
        Assert.NotNull(entry);
        Assert.Equal("Suspicious activity", entry.Reason);
    }

    [Fact]
    public async Task UpdateBlacklist_RemoveIp_Success()
    {
        // Arrange
        _dbContext.BlacklistEntries.Add(new BlacklistEntry
        {
            IpOrCidr = "10.0.0.5",
            AddedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string> { "10.0.0.5" });
        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(persistToDb: true);
        var request = new BlacklistUpdateRequest { Ip = "10.0.0.5", Action = "remove" };

        // Act
        var result = await controller.UpdateBlacklist(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var entry = await _dbContext.BlacklistEntries.FirstOrDefaultAsync(b => b.IpOrCidr == "10.0.0.5");
        Assert.Null(entry);
    }

    [Fact]
    public async Task UpdateBlacklist_NullRequest_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.UpdateBlacklist(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_EmptyIp_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "", Action = "add" };

        var result = await controller.UpdateBlacklist(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_InvalidAction_ReturnsBadRequest()
    {
        // Arrange
        _mockApisixClient.Setup(c => c.GetBlacklistAsync())
            .ReturnsAsync(new List<string>());

        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "1.2.3.4", Action = "invalid" };

        // Act
        var result = await controller.UpdateBlacklist(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
