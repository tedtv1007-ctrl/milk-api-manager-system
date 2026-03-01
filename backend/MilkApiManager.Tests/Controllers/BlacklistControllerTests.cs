using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

/// <summary>
/// Updated tests for BlacklistController after A-1 refactor (delegates to IBlacklistService).
/// </summary>
public class BlacklistControllerTests
{
    private readonly Mock<IBlacklistService> _mockBlacklistService;
    private readonly Mock<ILogger<BlacklistController>> _mockLogger;

    public BlacklistControllerTests()
    {
        _mockBlacklistService = new Mock<IBlacklistService>();
        _mockLogger = new Mock<ILogger<BlacklistController>>();
    }

    private BlacklistController CreateController()
    {
        return new BlacklistController(_mockBlacklistService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBlacklist_ReturnsServiceEntries()
    {
        var entries = new List<BlacklistEntry>
        {
            new BlacklistEntry { IpOrCidr = "192.168.1.100", Reason = "Test", AddedBy = "UnitTest", AddedAt = DateTime.UtcNow }
        };
        _mockBlacklistService.Setup(s => s.GetBlacklistAsync()).ReturnsAsync(entries);

        var controller = CreateController();
        var result = await controller.GetBlacklist();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedEntries = Assert.IsAssignableFrom<List<BlacklistEntry>>(okResult.Value);
        Assert.Single(returnedEntries);
        Assert.Equal("192.168.1.100", returnedEntries[0].IpOrCidr);
    }

    [Fact]
    public async Task GetBlacklist_WhenServiceThrows_Returns500()
    {
        _mockBlacklistService.Setup(s => s.GetBlacklistAsync()).ThrowsAsync(new Exception("DB failure"));

        var controller = CreateController();
        var result = await controller.GetBlacklist();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateBlacklist_AddIp_Success()
    {
        _mockBlacklistService
            .Setup(s => s.AddAsync(It.Is<BlacklistUpdateRequest>(r => r.Ip == "192.168.1.200")))
            .ReturnsAsync("IP 192.168.1.200 added successfully");

        var controller = CreateController();
        var request = new BlacklistUpdateRequest
        {
            Ip = "192.168.1.200",
            Action = "add",
            Reason = "Suspicious activity",
            AddedBy = "admin"
        };

        var result = await controller.UpdateBlacklist(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockBlacklistService.Verify(s => s.AddAsync(It.Is<BlacklistUpdateRequest>(r => r.Ip == "192.168.1.200")), Times.Once);
    }

    [Fact]
    public async Task UpdateBlacklist_RemoveIp_Success()
    {
        _mockBlacklistService
            .Setup(s => s.RemoveAsync(It.Is<BlacklistUpdateRequest>(r => r.Ip == "10.0.0.5")))
            .ReturnsAsync("IP 10.0.0.5 removed successfully");

        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "10.0.0.5", Action = "remove" };

        var result = await controller.UpdateBlacklist(request);

        Assert.IsType<OkObjectResult>(result);
        _mockBlacklistService.Verify(s => s.RemoveAsync(It.Is<BlacklistUpdateRequest>(r => r.Ip == "10.0.0.5")), Times.Once);
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
        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "1.2.3.4", Action = "invalid" };

        var result = await controller.UpdateBlacklist(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_InvalidIpFormat_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "not-an-ip", Action = "add" };

        var result = await controller.UpdateBlacklist(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_ValidCidr_Succeeds()
    {
        _mockBlacklistService
            .Setup(s => s.AddAsync(It.IsAny<BlacklistUpdateRequest>()))
            .ReturnsAsync("IP 10.0.0.0/24 added successfully");

        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "10.0.0.0/24", Action = "add" };

        var result = await controller.UpdateBlacklist(request);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_ServiceThrows_Returns500()
    {
        _mockBlacklistService
            .Setup(s => s.AddAsync(It.IsAny<BlacklistUpdateRequest>()))
            .ThrowsAsync(new Exception("Gateway unreachable"));

        var controller = CreateController();
        var request = new BlacklistUpdateRequest { Ip = "1.2.3.4", Action = "add" };

        var result = await controller.UpdateBlacklist(request);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
