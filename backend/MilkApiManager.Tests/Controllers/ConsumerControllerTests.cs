using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models.Apisix;
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

    // ============================================================
    // GET /api/Consumer (List)
    // ============================================================

    [Fact]
    public async Task GetConsumers_ReturnsOk_WithConsumerList()
    {
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
        apisixResponse = apisixResponse.Replace("limit_count", "limit-count");

        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ReturnsAsync(apisixResponse);

        var result = await _controller.GetConsumers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetConsumers_WithLimitReq_ParsesRateLimit()
    {
        // 模擬同時含有 limit-count 和 limit-req 的 consumer
        var rawJson = """
        {
            "list": [{
                "value": {
                    "username": "rate-limited-user",
                    "plugins": {
                        "limit-count": { "count": 1000, "time_window": 3600, "rejected_code": 429, "rejected_msg": "Quota exceeded" },
                        "limit-req": { "rate": 10, "burst": 20, "rejected_code": 503, "key": "remote_addr" }
                    }
                }
            }]
        }
        """;

        _mockApisixClient.Setup(c => c.GetConsumersAsync())
            .ReturnsAsync(rawJson);

        var result = await _controller.GetConsumers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // 驗證回傳的 consumers 含有 rate_limit
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("rate_limit", json);
        Assert.Contains("rate", json);
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

    // ============================================================
    // GET /api/Consumer/{username} (Single)
    // ============================================================

    [Fact]
    public async Task GetConsumer_ValidUsername_ReturnsOk()
    {
        var consumer = new Consumer
        {
            Username = "detail-user",
            Plugins = new Dictionary<string, object>
            {
                ["limit-count"] = new { count = 2000, time_window = 7200, rejected_code = 429, rejected_msg = "Quota exceeded" },
                ["limit-req"] = new { rate = 5, burst = 10, rejected_code = 503, key = "remote_addr" }
            }
        };

        _mockApisixClient.Setup(c => c.GetConsumerAsync("detail-user"))
            .ReturnsAsync(consumer);

        var result = await _controller.GetConsumer("detail-user");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("quota", json);
        Assert.Contains("rate_limit", json);
    }

    [Fact]
    public async Task GetConsumer_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetConsumerAsync("nonexistent"))
            .ThrowsAsync(new HttpRequestException("404 Not Found", null, System.Net.HttpStatusCode.NotFound));

        var result = await _controller.GetConsumer("nonexistent");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // POST /api/Consumer (Update/Create)
    // ============================================================

    [Fact]
    public async Task UpdateConsumer_WithQuotaAndRateLimit_ReturnsOk()
    {
        var consumerJson = JsonSerializer.Serialize(new
        {
            username = "full-config-user",
            quota = new
            {
                count = 5000,
                time_window = 3600,
                rejected_code = 429,
                rejected_msg = "Quota exceeded"
            },
            rate_limit = new
            {
                rate = 10,
                burst = 20,
                rejected_code = 503,
                key = "remote_addr"
            }
        });
        var consumerData = JsonSerializer.Deserialize<JsonElement>(consumerJson);

        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("full-config-user", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateConsumer(consumerData);

        Assert.IsType<OkResult>(result);
        _mockApisixClient.Verify(c => c.UpdateConsumerAsync("full-config-user", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConsumer_QuotaOnly_ReturnsOk()
    {
        var consumerJson = JsonSerializer.Serialize(new
        {
            username = "quota-only-user",
            quota = new
            {
                count = 1000,
                time_window = 3600,
                rejected_code = 429,
                rejected_msg = "Quota exceeded"
            }
        });
        var consumerData = JsonSerializer.Deserialize<JsonElement>(consumerJson);

        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("quota-only-user", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateConsumer(consumerData);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task UpdateConsumer_MissingUsername_ReturnsBadRequest()
    {
        var consumerJson = JsonSerializer.Serialize(new { quota = new { count = 100 } });
        var consumerData = JsonSerializer.Deserialize<JsonElement>(consumerJson);

        var result = await _controller.UpdateConsumer(consumerData);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // DELETE /api/Consumer/{username}
    // ============================================================

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
