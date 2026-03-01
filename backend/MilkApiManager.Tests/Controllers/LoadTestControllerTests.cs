using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class LoadTestControllerTests
{
    private readonly Mock<ILoadTestService> _mockLoadTestService;
    private readonly LoadTestController _controller;

    public LoadTestControllerTests()
    {
        _mockLoadTestService = new Mock<ILoadTestService>();
        _controller = new LoadTestController(_mockLoadTestService.Object);
    }

    [Fact]
    public async Task RunTest_ValidParameters_ReturnsOkWithReport()
    {
        _mockLoadTestService.Setup(s => s.RunTestAsync("http://example.com/api", 10, 30))
            .ReturnsAsync("Test completed: 100 requests, avg 50ms");

        var result = await _controller.RunTest("http://example.com/api", 10, 30);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RunTest_DefaultParameters_Uses10Vus30Duration()
    {
        _mockLoadTestService.Setup(s => s.RunTestAsync("http://example.com", 10, 30))
            .ReturnsAsync("Default run complete");

        var result = await _controller.RunTest("http://example.com");

        _mockLoadTestService.Verify(s => s.RunTestAsync("http://example.com", 10, 30), Times.Once);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunTest_CustomParameters_PassedCorrectly()
    {
        _mockLoadTestService.Setup(s => s.RunTestAsync("http://api.test/v1", 50, 120))
            .ReturnsAsync("High load test done");

        var result = await _controller.RunTest("http://api.test/v1", 50, 120);

        _mockLoadTestService.Verify(s => s.RunTestAsync("http://api.test/v1", 50, 120), Times.Once);
    }

    [Fact]
    public async Task RunTest_ServiceThrows_PropagatesException()
    {
        _mockLoadTestService.Setup(s => s.RunTestAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("k6 not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.RunTest("http://example.com"));
    }

    [Fact]
    public async Task RunTest_ReturnsReportInExpectedFormat()
    {
        var reportContent = "Checks: 100% passed\nRequests: 500\nAvg Duration: 45ms";
        _mockLoadTestService.Setup(s => s.RunTestAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(reportContent);

        var result = await _controller.RunTest("http://example.com");

        var okResult = Assert.IsType<OkObjectResult>(result);
        // The controller wraps result in { report = ... }
        Assert.NotNull(okResult.Value);
        var reportProp = okResult.Value.GetType().GetProperty("report");
        Assert.NotNull(reportProp);
        Assert.Equal(reportContent, reportProp.GetValue(okResult.Value));
    }
}
