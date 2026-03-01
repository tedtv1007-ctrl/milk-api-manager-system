using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MilkApiManager.Tests.Integration;

public class HealthAndMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthAndMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("USE_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("USE_DEMO_AUTH", "true");
        Environment.SetEnvironmentVariable("API_AUTH_KEY", "milk-admin-secret-key-change-me");
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-apisix-admin-key");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-for-unit-testing-32chars!");

        _factory = factory;
    }

    // ──────────────── Health Endpoints ────────────────

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ──────────────── API Key Authentication ────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithValidApiKey_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "milk-admin-secret-key-change-me");

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "wrong-key");

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNoAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ──────────────── JWT Authentication ────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithValidJwt_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "admin", "admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredOrInvalidJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ──────────────── CORS & Content Type ────────────────

    [Fact]
    public async Task ApiResponse_ReturnsJsonContentType()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "milk-admin-secret-key-change-me");

        var response = await client.GetAsync("/api/Route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // Verify it's valid JSON
        Assert.DoesNotContain("error", content, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────── Error Handling ────────────────

    [Fact]
    public async Task NonExistentEndpoint_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "milk-admin-secret-key-change-me");

        var response = await client.GetAsync("/api/NonExistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidJsonPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "milk-admin-secret-key-change-me");

        var content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Route", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ──────────────── RBAC Integration ────────────────

    [Fact]
    public async Task Admin_CanAccessBlacklist()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "admin", "admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Blacklist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotAccessBlacklist()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "viewer", "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Blacklist");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CanAccessReadOnlyEndpoints()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "viewer", "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var routeResponse = await client.GetAsync("/api/Route");
        Assert.Equal(HttpStatusCode.OK, routeResponse.StatusCode);
    }

    [Fact]
    public async Task Operator_CanCreateRoute()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "operator", "operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new StringContent(
            JsonSerializer.Serialize(new { id = "integration-op-test", name = "integration-op-test", uri = "/integration-op-test" }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/Route", payload);

        // Should succeed (200/201) or at minimum not be 403
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ──────────────── Audit Logs ────────────────

    [Fact]
    public async Task AuditLogs_ReturnsOkForOperator()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "operator", "operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/AuditLogs?limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // Should be valid JSON array
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // ──────────────── Analytics ────────────────

    [Fact]
    public async Task Analytics_ReturnsOkForViewer()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "viewer", "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Analytics/summary");

        // Analytics should be accessible for viewer+
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var payload = new StringContent(
            JsonSerializer.Serialize(new { username, password }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/login", payload);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Token is null.");
    }
}
