using Microsoft.AspNetCore.Mvc;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<PrometheusService> _mockPrometheusService;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _mockPrometheusService = new Mock<PrometheusService>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<PrometheusService>>()
        );

        _controller = new AnalyticsController(_mockPrometheusService.Object);
    }

    [Fact]
    public async Task GetRequests_DefaultQuery_ReturnsOk()
    {
        // Arrange
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery();

        // Act
        var result = await _controller.GetRequests(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetRequests_WithConsumerFilter_PassesCorrectQuery()
    {
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.Is<string>(s => s.Contains("consumer=\"test\"")),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery { Consumer = "test" };

        var result = await _controller.GetRequests(query);

        Assert.IsType<OkObjectResult>(result);
        _mockPrometheusService.Verify(p => p.GetMetricAsync(
            It.Is<string>(s => s.Contains("consumer=\"test\"")),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetLatency_ReturnsOk()
    {
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery();

        var result = await _controller.GetLatency(query);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetErrors_ReturnsOk()
    {
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery();

        var result = await _controller.GetErrors(query);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetRequests_30MinRange_Uses1mStep()
    {
        // 30-minute time range → should use "1m" step
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.Is<string>(s => s == "1m")))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery
        {
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow
        };

        var result = await _controller.GetRequests(query);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetRequests_12HourRange_Uses10mStep()
    {
        // 12-hour time range → should use "10m" step
        _mockPrometheusService.Setup(p => p.GetMetricAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.Is<string>(s => s == "10m")))
            .ReturnsAsync(new List<AnalyticsResult>());

        var query = new AnalyticsQuery
        {
            StartTime = DateTime.UtcNow.AddHours(-12),
            EndTime = DateTime.UtcNow
        };

        var result = await _controller.GetRequests(query);

        Assert.IsType<OkObjectResult>(result);
    }
}
