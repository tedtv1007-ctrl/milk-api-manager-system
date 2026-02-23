using System.Net;

namespace MilkApiManager.Middleware;

/// <summary>
/// Middleware that validates API Key authentication for all /api/* endpoints.
/// The key is read from the API_AUTH_KEY environment variable.
/// Swagger endpoints and the /health endpoint are excluded.
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

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedKey) || extractedKey != _apiKey)
        {
            _logger.LogWarning("Unauthorized API access attempt from {RemoteIp} to {Path}", 
                context.Connection.RemoteIpAddress, path);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API Key" });
            return;
        }

        await _next(context);
    }
}
