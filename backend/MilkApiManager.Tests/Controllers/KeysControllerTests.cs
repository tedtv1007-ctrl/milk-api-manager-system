using Microsoft.AspNetCore.Mvc;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class KeysControllerTests
{
    private readonly Mock<IVaultService> _mockVaultService;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly KeysController _controller;

    public KeysControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockVaultService = new Mock<IVaultService>();
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );

        _controller = new KeysController(_mockVaultService.Object, _mockApisixClient.Object);
    }

    [Fact]
    public async Task CreateKey_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateKeyRequest { Owner = "test-consumer" };
        _mockVaultService.Setup(v => v.StoreSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("vault-version-1");
        _mockApisixClient.Setup(c => c.CreateConsumerAsync("test-consumer", It.IsAny<Consumer>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateKey(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        _mockVaultService.Verify(v => v.StoreSecretAsync(
            It.Is<string>(s => s.Contains("test-consumer")),
            It.IsAny<string>()), Times.Once);
        _mockApisixClient.Verify(c => c.CreateConsumerAsync("test-consumer", It.IsAny<Consumer>()), Times.Once);
    }

    [Fact]
    public async Task CreateKey_NullRequest_ReturnsBadRequest()
    {
        var result = await _controller.CreateKey(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateKey_EmptyOwner_ReturnsBadRequest()
    {
        var request = new CreateKeyRequest { Owner = "" };

        var result = await _controller.CreateKey(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RotateKey_ValidConsumer_ReturnsOk()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync("rotate-consumer"))
            .ReturnsAsync("new-rotated-key-1234");

        var result = await _controller.RotateKey("rotate-consumer");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RotateKey_ConsumerNotFound_ReturnsBadRequest()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync("nonexistent"))
            .ThrowsAsync(new Exception("Consumer nonexistent not found in APISIX"));

        var result = await _controller.RotateKey("nonexistent");

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
    }
}
