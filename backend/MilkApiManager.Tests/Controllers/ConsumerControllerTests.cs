using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using System.Text.Json;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class ConsumerControllerTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<ConsumerController>> _mockLogger;
    private readonly ConsumerController _controller;

    public ConsumerControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );
        _mockLogger = new Mock<ILogger<ConsumerController>>();
        _controller = new ConsumerController(_mockApisixClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetConsumers_ReturnsOk_WithConsumerList()
    {
        // Arrange
        var apisixResponse = JsonSerializer.Serialize(new
        {
            list = new[]
            {
                new
                {
                    value = new
                    {
                        username = "test-user",
                        plugins = new
                        {
                            limit_count = new { count = 500, time_window = 3600, rejected_code = 429, rejected_msg = "Rate limited" }
                        }
                    }
                }
            }
        });

        // The APISIX response uses "limit-count" with a dash. We need to handle the JSON properly.
        apisixResponse = apisixResponse.Replace("limit_count", "limit-count");

        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ReturnsAsync(apisixResponse);

        // Act
        var result = await _controller.GetConsumers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetConsumers_EmptyList_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ReturnsAsync("{\"list\":[]}");

        var result = await _controller.GetConsumers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var consumers = Assert.IsAssignableFrom<List<object>>(okResult.Value);
        Assert.Empty(consumers);
    }

    [Fact]
    public async Task GetConsumers_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ThrowsAsync(new Exception("APISIX error"));

        var result = await _controller.GetConsumers();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateConsumer_ValidData_ReturnsOk()
    {
        // Arrange
        var consumerJson = JsonSerializer.Serialize(new
        {
            username = "update-user",
            quota = new
            {
                count = 1000,
                time_window = 3600,
                rejected_code = 429,
                rejected_msg = "Quota exceeded"
            }
        });
        var consumerData = JsonSerializer.Deserialize<JsonElement>(consumerJson);

        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("update-user", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateConsumer(consumerData);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockApisixClient.Verify(c => c.UpdateConsumerAsync("update-user", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConsumer_ValidUsername_ReturnsNoContent()
    {
        _mockApisixClient.Setup(c => c.DeleteConsumerAsync("del-user"))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteConsumer("del-user");

        Assert.IsType<NoContentResult>(result);
        _mockApisixClient.Verify(c => c.DeleteConsumerAsync("del-user"), Times.Once);
    }

    [Fact]
    public async Task DeleteConsumer_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteConsumerAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteConsumer("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
