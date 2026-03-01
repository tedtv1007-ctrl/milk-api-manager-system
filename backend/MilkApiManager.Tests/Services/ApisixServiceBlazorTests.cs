using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MilkAdminBlazor.Data;
using Xunit;

namespace MilkApiManager.Tests.Services;

/// <summary>
/// T-2: Unit tests for ApisixService (Blazor frontend service).
/// Uses a mocked HttpMessageHandler to test HTTP interactions.
/// </summary>
public class ApisixServiceBlazorTests
{
    private readonly Mock<ILogger<ApisixService>> _mockLogger;

    public ApisixServiceBlazorTests()
    {
        _mockLogger = new Mock<ILogger<ApisixService>>();
    }

    private (ApisixService service, Mock<HttpMessageHandler> handler) CreateService(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseContent = "[]")
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5001/")
        };

        var service = new ApisixService(httpClient, _mockLogger.Object);
        return (service, mockHandler);
    }

    // ---- Sync Status ----

    [Fact]
    public async Task GetSyncStatusAsync_Success_ReturnsStatus()
    {
        var json = JsonSerializer.Serialize(new { status = "Synced", lastSyncTime = DateTime.UtcNow });
        var (service, _) = CreateService(responseContent: json);

        var result = await service.GetSyncStatusAsync();

        Assert.NotNull(result);
        Assert.Equal("Synced", result!.Status);
    }

    [Fact]
    public async Task GetSyncStatusAsync_Failure_ReturnsOffline()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "error");

        var result = await service.GetSyncStatusAsync();

        Assert.NotNull(result);
        Assert.Equal("Offline", result!.Status);
    }

    // ---- Blacklist ----

    [Fact]
    public async Task GetBlacklistedIpsAsync_Success_ReturnsList()
    {
        var items = new[] { new { ipOrCidr = "10.0.0.1", reason = "test" } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(items));

        var result = await service.GetBlacklistedIpsAsync();

        Assert.Single(result);
        Assert.Equal("10.0.0.1", result[0].IpOrCidr);
    }

    [Fact]
    public async Task GetBlacklistedIpsAsync_Failure_ReturnsEmpty()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "error");

        var result = await service.GetBlacklistedIpsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddIpToBlacklistAsync_Success_NoException()
    {
        var (service, handler) = CreateService(HttpStatusCode.OK, "{\"message\":\"ok\"}");

        await service.AddIpToBlacklistAsync("10.0.0.1", "test", "admin");

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AddIpToBlacklistAsync_Failure_Throws()
    {
        var (service, _) = CreateService(HttpStatusCode.BadRequest, "bad request");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.AddIpToBlacklistAsync("invalid", "test"));
    }

    // ---- Consumers ----

    [Fact]
    public async Task GetConsumersAsync_Success_ReturnsList()
    {
        var consumers = new[] { new { username = "user1", desc = "Test User" } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(consumers));

        var result = await service.GetConsumersAsync();

        Assert.Single(result);
        Assert.Equal("user1", result[0].Username);
    }

    [Fact]
    public async Task DeleteConsumerAsync_Success_NoException()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, "{}");

        await service.DeleteConsumerAsync("user1");
    }

    [Fact]
    public async Task DeleteConsumerAsync_Failure_Throws()
    {
        var (service, _) = CreateService(HttpStatusCode.NotFound, "not found");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.DeleteConsumerAsync("nonexistent"));
    }

    // ---- Consumer Stats (A-4: now calls real API) ----

    [Fact]
    public async Task GetConsumerStatsAsync_Success_ReturnsList()
    {
        var stats = new[] { new { username = "user1", requestCount = 5000, errorRate = 1.5, timestamp = DateTime.UtcNow } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(stats));

        var result = await service.GetConsumerStatsAsync("user1");

        Assert.Single(result);
        Assert.Equal("user1", result[0].Username);
    }

    [Fact]
    public async Task GetConsumerStatsAsync_Failure_ReturnsEmpty()
    {
        var (service, _) = CreateService(HttpStatusCode.ServiceUnavailable, "error");

        var result = await service.GetConsumerStatsAsync();

        Assert.Empty(result);
    }

    // ---- PII Rules ----

    [Fact]
    public async Task GetPiiRulesAsync_Success_ReturnsList()
    {
        var rules = new[] { new { id = 1, routeId = "route1", fieldPath = "$.ssn", isActive = true } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(rules));

        var result = await service.GetPiiRulesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task SavePiiRuleAsync_NewRule_PostsAndChecksResponse()
    {
        var (service, handler) = CreateService(HttpStatusCode.OK, "{}");

        await service.SavePiiRuleAsync(new PiiMaskingRuleDto { Id = 0, RouteId = "r1", FieldPath = "$.name" });

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SavePiiRuleAsync_ExistingRule_PutsAndChecksResponse()
    {
        var (service, handler) = CreateService(HttpStatusCode.OK, "{}");

        await service.SavePiiRuleAsync(new PiiMaskingRuleDto { Id = 5, RouteId = "r1", FieldPath = "$.name" });

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    // ---- Mock Rules ----

    [Fact]
    public async Task GetMockRulesAsync_Success_ReturnsList()
    {
        var rules = new[] { new { id = 1, routeId = "r1", responseStatusCode = 200, isEnabled = true } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(rules));

        var result = await service.GetMockRulesAsync();

        Assert.Single(result);
    }

    // ---- Access Requests ----

    [Fact]
    public async Task GetAccessRequestsAsync_Success_ReturnsList()
    {
        var requests = new[] { new { id = 1, projectName = "TestProject", status = "Pending" } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(requests));

        var result = await service.GetAccessRequestsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task SubmitAccessRequestAsync_Failure_Throws()
    {
        var (service, _) = CreateService(HttpStatusCode.BadRequest, "validation error");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SubmitAccessRequestAsync(new AccessRequestDto { ProjectName = "Test" }));
    }

    // ---- APISIX Route CRUD ----

    [Fact]
    public async Task GetApisixRoutesAsync_Success_ReturnsList()
    {
        var routes = new[] { new { id = "r1", name = "TestRoute", uri = "/test" } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(routes));

        var result = await service.GetApisixRoutesAsync();

        Assert.Single(result);
        Assert.Equal("r1", result[0].Id);
    }

    [Fact]
    public async Task SaveApisixRouteAsync_NewRoute_GeneratesId()
    {
        var (service, handler) = CreateService(HttpStatusCode.OK, "{}");

        var route = new ApisixRouteDto { Id = null, Name = "NewRoute", Uri = "/new" };
        await service.SaveApisixRouteAsync(route);

        Assert.NotNull(route.Id);
        Assert.Equal(12, route.Id.Length);
    }

    [Fact]
    public async Task DeleteApisixRouteAsync_Failure_Throws()
    {
        var (service, _) = CreateService(HttpStatusCode.NotFound, "not found");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.DeleteApisixRouteAsync("nonexistent"));
    }

    // ---- Audit Logs ----

    [Fact]
    public async Task GetAuditLogsAsync_Success_ReturnsList()
    {
        var logs = new[] { new { id = 1, user = "admin", action = "Create", resource = "Route", timestamp = DateTime.UtcNow } };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(logs));

        var result = await service.GetAuditLogsAsync(50);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAuditLogsAsync_Failure_ReturnsEmpty()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "error");

        var result = await service.GetAuditLogsAsync();

        Assert.Empty(result);
    }

    // ---- Load Testing ----

    [Fact]
    public async Task RunLoadTestAsync_Success_ReturnsReport()
    {
        var json = JsonSerializer.Serialize(new { report = "Test completed. 100 requests." });
        var (service, _) = CreateService(HttpStatusCode.OK, json);

        var result = await service.RunLoadTestAsync("http://example.com", 10, 30);

        Assert.Contains("100 requests", result);
    }

    [Fact]
    public async Task RunLoadTestAsync_Failure_ReturnsErrorString()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "k6 crashed");

        var result = await service.RunLoadTestAsync("http://example.com", 10, 30);

        Assert.Contains("Error", result);
    }

    // ---- Dashboard Stats ----

    [Fact]
    public async Task GetDashboardStatsAsync_Success_ReturnsStats()
    {
        var stats = new { routeCount = 5, serviceCount = 3, upstreamCount = 2, consumerCount = 10, sslCount = 1, globalRuleCount = 2 };
        var (service, _) = CreateService(responseContent: JsonSerializer.Serialize(stats));

        var result = await service.GetDashboardStatsAsync();

        Assert.Equal(5, result.RouteCount);
        Assert.Equal(10, result.ConsumerCount);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_Failure_ReturnsDefault()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "error");

        var result = await service.GetDashboardStatsAsync();

        Assert.Equal(0, result.RouteCount);
    }
}
