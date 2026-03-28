using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using System.Net;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class BlacklistControllerTests
{
    private readonly Mock<IBlacklistService> _mockService;
    private readonly Mock<ILogger<BlacklistController>> _mockLogger;
    private readonly BlacklistController _controller;

    public BlacklistControllerTests()
    {
        _mockService = new Mock<IBlacklistService>();
        _mockLogger = new Mock<ILogger<BlacklistController>>();
        _controller = new BlacklistController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBlacklist_ReturnsOk()
    {
        var entries = new List<BlacklistEntry>
        {
            new BlacklistEntry { IpOrCidr = "1.2.3.4", Reason = "Test" }
        };
        _mockService.Setup(s => s.GetBlacklistAsync()).ReturnsAsync(entries);

        var result = await _controller.GetBlacklist();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<BlacklistEntry>>(okResult.Value);
        Assert.Single(returned);
        Assert.Equal("1.2.3.4", returned[0].IpOrCidr);
    }

    [Fact]
    public async Task UpdateBlacklist_Add_ReturnsOk()
    {
        var request = new BlacklistUpdateRequest { Ip = "1.1.1.1", Action = "add" };
        _mockService.Setup(s => s.AddAsync(request)).ReturnsAsync("added");

        var result = await _controller.UpdateBlacklist(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBlacklist_Remove_ReturnsOk()
    {
        var request = new BlacklistUpdateRequest { Ip = "1.1.1.1", Action = "remove" };
        _mockService.Setup(s => s.RemoveAsync(request)).ReturnsAsync("removed");

        var result = await _controller.UpdateBlacklist(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
    }
}
