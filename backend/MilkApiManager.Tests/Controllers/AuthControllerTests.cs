using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using System.Security.Claims;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class AuthControllerTests
{
    private readonly AuthController _controller;
    private readonly AuthService _authService;

    public AuthControllerTests()
    {
        Environment.SetEnvironmentVariable("USE_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-for-unit-testing-32chars!");

        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "MilkApiManager" },
            { "Jwt:Audience", "MilkApiClients" },
            { "Jwt:ExpirationMinutes", "60" },
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthService(config, Mock.Of<ILogger<AuthService>>(),
            Microsoft.Extensions.Options.Options.Create(new MilkApiManager.Options.JwtOptions
            {
                Secret = "milk-api-default-jwt-secret-change-in-production-32chars!",
                Issuer = "MilkApiManager",
                Audience = "MilkApiClients",
                ExpirationMinutes = 60
            }),
            Microsoft.Extensions.Options.Options.Create(new MilkApiManager.Options.AuthOptions
            {
                UseTestMode = true,
                UseDemoAuth = true
            }));
        _controller = new AuthController(_authService, Mock.Of<ILogger<AuthController>>());
    }

    [Fact]
    public async Task Login_ValidAdmin_ReturnsOkWithToken()
    {
        var request = new LoginRequest { Username = "admin", Password = "admin" };

        var result = await _controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal("admin", response.DisplayName);
        Assert.Contains("Admin", response.Roles);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var request = new LoginRequest { Username = "admin", Password = "wrong" };

        var result = await _controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyUsername_ReturnsBadRequest()
    {
        var request = new LoginRequest { Username = "", Password = "test" };

        var result = await _controller.Login(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        var request = new LoginRequest { Username = "admin", Password = "" };

        var result = await _controller.Login(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetCurrentUser_Authenticated_ReturnsUserInfo()
    {
        // Arrange: simulate an authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "Operator"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Login_OperatorUser_ReturnsCorrectRoles()
    {
        var request = new LoginRequest { Username = "operator", Password = "operator" };

        var result = await _controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Contains("Operator", response.Roles);
        Assert.Contains("Viewer", response.Roles);
        Assert.DoesNotContain("Admin", response.Roles);
    }
}
