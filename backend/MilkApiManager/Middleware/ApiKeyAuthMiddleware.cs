using System.Net;

namespace MilkApiManager.Middleware;

/// <summary>
/// Middleware that validates API access for all /api/* endpoints.
/// Supports two authentication methods:
/// 1. API Key via X-API-KEY header (for SDK/programmatic access)
/// 2. JWT Bearer token via Authorization header (for SSO/user access)
/// 
/// Endpoints excluded from auth: /api/auth/login, /health, /swagger
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string API_KEY_HEADER = "X-API-KEY";

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("API_AUTH_KEY") 
            ?? "milk-admin-secret-key-change-me";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for non-API paths (swagger, health, etc.)
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip auth for login endpoint (must be accessible anonymously)
        if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Check 1: API Key header
        if (context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey) && extractedKey == _apiKey)
        {
            await _next(context);
            return;
        }

        // Check 2: JWT Bearer token (already validated by ASP.NET JWT middleware)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Neither auth method provided
        _logger.LogWarning("Unauthorized API access attempt from {RemoteIp} to {Path}", 
            context.Connection.RemoteIpAddress, path);
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API Key or JWT token" });
    }
}
