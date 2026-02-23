using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;

    public AuthServiceTests()
    {
        Environment.SetEnvironmentVariable("USE_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-for-unit-testing-32chars!");

        _mockLogger = new Mock<ILogger<AuthService>>();
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "MilkApiManager" },
            { "Jwt:Audience", "MilkApiClients" },
            { "Jwt:ExpirationMinutes", "60" },
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(config, _mockLogger.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_AdminUser_ReturnsTokenWithAllRoles()
    {
        // Act
        var result = await _authService.AuthenticateAsync("admin", "admin");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("admin", result.DisplayName);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("Operator", result.Roles);
        Assert.Contains("Viewer", result.Roles);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task AuthenticateAsync_OperatorUser_ReturnsOperatorAndViewerRoles()
    {
        var result = await _authService.AuthenticateAsync("operator", "operator");

        Assert.NotNull(result);
        Assert.Contains("Operator", result.Roles);
        Assert.Contains("Viewer", result.Roles);
        Assert.DoesNotContain("Admin", result.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_ViewerUser_ReturnsViewerRoleOnly()
    {
        var result = await _authService.AuthenticateAsync("viewer", "viewer");

        Assert.NotNull(result);
        Assert.Single(result.Roles);
        Assert.Contains("Viewer", result.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_WrongPassword_ReturnsNull()
    {
        var result = await _authService.AuthenticateAsync("admin", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _authService.AuthenticateAsync("nobody", "nobody");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyCredentials_ReturnsNull()
    {
        var result = await _authService.AuthenticateAsync("", "");

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_JwtTokenIsValidFormat()
    {
        var result = await _authService.AuthenticateAsync("admin", "admin");

        Assert.NotNull(result);
        // JWT has 3 parts separated by dots
        var parts = result.Token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public async Task AuthenticateAsync_ExpirationIsInFuture()
    {
        var result = await _authService.AuthenticateAsync("admin", "admin");

        Assert.NotNull(result);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        // Should expire within configured minutes (60 in test)
        Assert.True(result.ExpiresAt < DateTime.UtcNow.AddMinutes(61));
    }
}
