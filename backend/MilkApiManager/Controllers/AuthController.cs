using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

/// <summary>
/// Authentication controller for LDAP/AD SSO login.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with LDAP/AD credentials and receive a JWT token.
    /// In demo mode (USE_TEST_MODE=true), use admin/admin, operator/operator, or viewer/viewer.
    /// </summary>
    /// <param name="request">Username and password</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required." });
        }

        var result = await _authService.AuthenticateAsync(request.Username, request.Password);

        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized(new { error = "Invalid username or password." });
        }

        _logger.LogInformation("User {Username} logged in successfully with roles: {Roles}", 
            request.Username, string.Join(", ", result.Roles));

        return Ok(result);
    }

    /// <summary>
    /// Get current authenticated user information from JWT claims.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name ?? "Unknown";
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            Username = username,
            Roles = roles,
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}
