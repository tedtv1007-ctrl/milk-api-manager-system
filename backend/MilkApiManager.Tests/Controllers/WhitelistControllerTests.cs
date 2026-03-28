using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class WhitelistControllerTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IApisixClient> _mockApisix;
    private readonly Mock<ILogger<WhitelistController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAudit;
    private readonly IConfiguration _config;
    private readonly WhitelistController _controller;

    public WhitelistControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _mockApisix = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<WhitelistController>>();
        _mockAudit = new Mock<IAuditLogService>();

        var inMemoryConfig = new Dictionary<string, string?> {
            {"Whitelist:PersistToDatabase", "true"}
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        _controller = new WhitelistController(_mockApisix.Object, _mockLogger.Object, _db, _config, _mockAudit.Object);
    }

    [Fact]
    public async Task GetWhitelistForRoute_ReturnsOk()
    {
        var routeId = "r1";
        _db.WhitelistEntries.Add(new WhitelistEntry { RouteId = routeId, IpCidr = "10.0.0.1" });
        await _db.SaveChangesAsync();

        var result = await _controller.GetWhitelistForRoute(routeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var entries = Assert.IsType<List<WhitelistEntry>>(okResult.Value);
        Assert.Single(entries);
    }

    [Fact]
    public async Task AddWhitelistEntry_ValidRequest_ReturnsOk()
    {
        var routeId = "r1";
        var request = new WhitelistUpdateRequest { IpCidr = "192.168.1.1", Action = "add" };
        _mockApisix.Setup(c => c.UpdateWhitelistForRouteAsync(routeId, It.IsAny<List<string>>())).Returns(Task.CompletedTask);

        var result = await _controller.AddWhitelistEntry(routeId, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var entry = await _db.WhitelistEntries.FirstOrDefaultAsync(w => w.RouteId == routeId && w.IpCidr == "192.168.1.1");
        Assert.NotNull(entry);
    }
}
