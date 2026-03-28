using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace MilkApiManager.Tests.Integration;

public class AuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("USE_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("USE_DEMO_AUTH", "true");
        Environment.SetEnvironmentVariable("API_AUTH_KEY", "milk-admin-secret-key-change-me");
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-apisix-admin-key");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-for-unit-testing-32chars!");

        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithApiKey_ReturnsSuccessStatus()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "milk-admin-secret-key-change-me");

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_WithoutAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogin_WithoutAuthHeader_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var payload = new StringContent("{\"username\":\"admin\",\"password\":\"admin\"}", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/auth/login", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CanReadRoutes_ButCannotCreateRoute()
    {
        var client = _factory.CreateClient();
        var viewerToken = await GetTokenAsync(client, "viewer", "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        var getResponse = await client.GetAsync("/api/Route");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var createPayload = new StringContent(
            "{\"id\":\"rbac-viewer-test\",\"name\":\"rbac-viewer-test\",\"uri\":\"/rbac-viewer-test\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var postResponse = await client.PostAsync("/api/Route", createPayload);
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [Fact]
    public async Task Operator_CanAccessOperatorEndpoint_ButNotAdminEndpoint()
    {
        var client = _factory.CreateClient();
        var operatorToken = await GetTokenAsync(client, "operator", "operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        var operatorEndpointResponse = await client.GetAsync("/api/AuditLogs?limit=1");
        Assert.Equal(HttpStatusCode.OK, operatorEndpointResponse.StatusCode);

        // POST to Blacklist requires AdminOnly policy — Operator should be denied
        var adminPayload = new StringContent(
            "{\"ip\":\"10.0.0.1\",\"action\":\"add\",\"reason\":\"test\"}",
            System.Text.Encoding.UTF8,
            "application/json");
        var adminEndpointResponse = await client.PostAsync("/api/Blacklist", adminPayload);
        Assert.Equal(HttpStatusCode.Forbidden, adminEndpointResponse.StatusCode);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var payload = new StringContent(
            JsonSerializer.Serialize(new { username, password }),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/login", payload);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("token", out var tokenElement))
        {
            throw new InvalidOperationException("Login response did not contain token.");
        }

        return tokenElement.GetString() ?? throw new InvalidOperationException("Token is null.");
    }
}
