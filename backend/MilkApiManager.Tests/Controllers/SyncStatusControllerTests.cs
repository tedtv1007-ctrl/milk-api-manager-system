using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class SyncStatusControllerTests
{
    private readonly Mock<BlacklistConsistencyService> _mockConsistencyService;
    private readonly SyncStatusController _controller;

    public SyncStatusControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");

        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var apisixClient = new Mock<ApisixClient>(Mock.Of<HttpClient>(), Mock.Of<Microsoft.Extensions.Logging.ILogger<ApisixClient>>());

        _mockConsistencyService = new Mock<BlacklistConsistencyService>(
            db,
            apisixClient.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<BlacklistConsistencyService>>()
        );

        _controller = new SyncStatusController(_mockConsistencyService.Object);
    }

    [Fact]
    public void GetStatus_ReturnsOk()
    {
        var result = _controller.GetStatus();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetBlacklistDrift_ReturnsOkWithReport()
    {
        _mockConsistencyService.Setup(s => s.GetBlacklistDriftReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlacklistDriftReport
            {
                DatabaseOnly = new List<string> { "1.1.1.1" },
                GatewayOnly = new List<string>()
            });

        var result = await _controller.GetBlacklistDrift(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task ReconcileBlacklist_ReturnsOkWithMessage()
    {
        _mockConsistencyService.Setup(s => s.ReconcileDatabaseToGatewayAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlacklistDriftReport());

        var result = await _controller.ReconcileBlacklist(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
