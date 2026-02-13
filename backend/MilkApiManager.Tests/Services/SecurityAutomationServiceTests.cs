using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class SecurityAutomationServiceTests
{
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly Mock<IVaultService> _mockVaultService;
    private readonly Mock<ILogger<SecurityAutomationService>> _mockLogger;
    private readonly SecurityAutomationService _service;

    public SecurityAutomationServiceTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );
        _mockVaultService = new Mock<IVaultService>();
        _mockLogger = new Mock<ILogger<SecurityAutomationService>>();

        _service = new SecurityAutomationService(
            _mockApisixClient.Object,
            _mockVaultService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CheckAndRotateKeys_ExpiredKey_TriggersRotation()
    {
        // Arrange
        _mockVaultService.Setup(v => v.RotateApiKeyAsync(It.IsAny<string>()))
            .ReturnsAsync("new-key-rotated");

        // Act - the internal list contains an expired key for "payment-gateway"
        await _service.CheckAndRotateKeys();

        // Assert - should rotate the expired key
        _mockVaultService.Verify(v => v.RotateApiKeyAsync("payment-gateway"), Times.Once);
    }

    [Fact]
    public async Task CheckAndRotateKeys_NearExpiryKey_DoesNotRotate()
    {
        // Arrange & Act
        await _service.CheckAndRotateKeys();

        // Assert - "tsp-partner-01" has 5 days to expiry, should only notify, not rotate
        _mockVaultService.Verify(v => v.RotateApiKeyAsync("tsp-partner-01"), Times.Never);
    }

    [Fact]
    public async Task BlockMaliciousIP_UpdatesGlobalPlugin()
    {
        // Arrange
        _mockApisixClient.Setup(c => c.UpdateGlobalPlugin("ip-restriction", It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.BlockMaliciousIP("10.0.0.99", "DDoS attack");

        // Assert
        _mockApisixClient.Verify(c => c.UpdateGlobalPlugin(
            "ip-restriction",
            It.Is<object>(o => o != null)),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndRotateKeys_CompletesWithoutError()
    {
        // Arrange
        _mockVaultService.Setup(v => v.RotateApiKeyAsync(It.IsAny<string>()))
            .ReturnsAsync("new-key");

        // Act & Assert - should not throw
        var ex = await Record.ExceptionAsync(() => _service.CheckAndRotateKeys());
        Assert.Null(ex);
    }
}
