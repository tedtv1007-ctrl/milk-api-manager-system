using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class VaultServiceTests
{
    private readonly Mock<ILogger<VaultService>> _mockLogger;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<AuditLogService> _mockAuditLogService;
    private readonly VaultService _vaultService;

    public VaultServiceTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockLogger = new Mock<ILogger<VaultService>>();
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );
        _mockAuditLogService = new Mock<AuditLogService>(
            Mock.Of<HttpClient>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
        );

        _vaultService = new VaultService(
            _mockLogger.Object,
            _mockApisixClient.Object,
            _mockAuditLogService.Object
        );
    }

    [Fact]
    public async Task StoreSecretAsync_ReturnsVaultVersion()
    {
        var result = await _vaultService.StoreSecretAsync("secret/path", "my-secret");

        Assert.Equal("vault-version-1", result);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsMockSecret()
    {
        var result = await _vaultService.GetSecretAsync("secret/path");

        Assert.Equal("mock-secret-value", result);
    }

    [Fact]
    public async Task RotateApiKeyAsync_ValidConsumer_ReturnsNewKey()
    {
        // Arrange
        var consumer = new Consumer
        {
            Username = "test-consumer",
            Plugins = new Dictionary<string, object>
            {
                ["key-auth"] = new { key = "old-key" }
            }
        };

        _mockApisixClient.Setup(c => c.GetConsumerAsync("test-consumer"))
            .ReturnsAsync(consumer);
        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("test-consumer", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.ShipLogsToSIEM(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        var newKey = await _vaultService.RotateApiKeyAsync("test-consumer");

        // Assert
        Assert.NotNull(newKey);
        Assert.NotEmpty(newKey);
        Assert.Equal(32, newKey.Length); // Guid without dashes = 32 chars
        _mockApisixClient.Verify(c => c.UpdateConsumerAsync("test-consumer", It.IsAny<object>()), Times.Once);
        _mockAuditLogService.Verify(a => a.ShipLogsToSIEM(
            It.Is<AuditLogEntry>(e => e.Action == "API_KEY_ROTATION")), Times.Once);
    }

    [Fact]
    public async Task RotateApiKeyAsync_ConsumerNotFound_ThrowsException()
    {
        // Arrange
        _mockApisixClient.Setup(c => c.GetConsumerAsync("nonexistent"))
            .ReturnsAsync((Consumer?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => _vaultService.RotateApiKeyAsync("nonexistent"));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task RotateApiKeyAsync_ConsumerWithNullPlugins_InitializesPlugins()
    {
        // Arrange
        var consumer = new Consumer
        {
            Username = "no-plugins",
            Plugins = null
        };

        _mockApisixClient.Setup(c => c.GetConsumerAsync("no-plugins"))
            .ReturnsAsync(consumer);
        _mockApisixClient.Setup(c => c.UpdateConsumerAsync("no-plugins", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.ShipLogsToSIEM(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        var newKey = await _vaultService.RotateApiKeyAsync("no-plugins");

        // Assert
        Assert.NotNull(newKey);
        Assert.NotNull(consumer.Plugins);
        Assert.True(consumer.Plugins.ContainsKey("key-auth"));
    }
}
