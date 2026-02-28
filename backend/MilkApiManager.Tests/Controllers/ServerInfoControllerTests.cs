using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class ServerInfoControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<ServerInfoController>> _mockLogger;
    private readonly ServerInfoController _controller;

    public ServerInfoControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<ServerInfoController>>();

        _controller = new ServerInfoController(
            _mockApisixClient.Object,
            _mockLogger.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ============================================================
    // GET /api/ServerInfo
    // ============================================================

    [Fact]
    public async Task GetServerInfo_ReturnsJsonContent()
    {
        var serverInfoJson = """{"hostname":"apisix","version":"3.11.0","boot_time":1700000000}""";

        _mockApisixClient.Setup(c => c.GetServerInfoAsync())
            .ReturnsAsync(serverInfoJson);

        var result = await _controller.GetServerInfo();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("application/json", contentResult.ContentType);
        Assert.Contains("3.11.0", contentResult.Content);
        Assert.Contains("apisix", contentResult.Content);
    }

    [Fact]
    public async Task GetServerInfo_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetServerInfoAsync())
            .ThrowsAsync(new Exception("APISIX unreachable"));

        var result = await _controller.GetServerInfo();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // GET /api/ServerInfo/dashboard
    // ============================================================

    [Fact]
    public async Task GetDashboardStats_ReturnsAggregatedCounts()
    {
        _mockApisixClient.Setup(c => c.GetRoutesTypedAsync())
            .ReturnsAsync(new List<Route>
            {
                new Route { Id = "r1", Name = "Route1", Uri = "/r1" },
                new Route { Id = "r2", Name = "Route2", Uri = "/r2" }
            });
        _mockApisixClient.Setup(c => c.GetServicesTypedAsync())
            .ReturnsAsync(new List<Service>
            {
                new Service { Id = "s1", Name = "Svc1", Upstream = new Upstream { Nodes = new Dictionary<string, int> { { "127.0.0.1:80", 1 } } } }
            });
        _mockApisixClient.Setup(c => c.GetUpstreamsTypedAsync())
            .ReturnsAsync(new List<StandaloneUpstream>
            {
                new StandaloneUpstream { Id = "u1", Name = "Up1" },
                new StandaloneUpstream { Id = "u2", Name = "Up2" },
                new StandaloneUpstream { Id = "u3", Name = "Up3" }
            });
        _mockApisixClient.Setup(c => c.GetConsumersTypedAsync())
            .ReturnsAsync(new List<Consumer>
            {
                new Consumer { Username = "user1" }
            });
        _mockApisixClient.Setup(c => c.GetSslsTypedAsync())
            .ReturnsAsync(new List<SslCertificate>());
        _mockApisixClient.Setup(c => c.GetGlobalRulesTypedAsync())
            .ReturnsAsync(new List<GlobalRule>
            {
                new GlobalRule { Id = "gr1" }
            });

        var result = await _controller.GetDashboardStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"routeCount\":2", json);
        Assert.Contains("\"serviceCount\":1", json);
        Assert.Contains("\"upstreamCount\":3", json);
        Assert.Contains("\"consumerCount\":1", json);
        Assert.Contains("\"sslCount\":0", json);
        Assert.Contains("\"globalRuleCount\":1", json);
    }

    [Fact]
    public async Task GetDashboardStats_OnException_ReturnsDefaultCounts()
    {
        _mockApisixClient.Setup(c => c.GetRoutesTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetDashboardStats();

        // Even on error, dashboard returns Ok with zero counts + error message
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"routeCount\":0", json);
        Assert.Contains("\"error\"", json);
    }

    [Fact]
    public async Task GetDashboardStats_AllEmpty_ReturnsZeroCounts()
    {
        _mockApisixClient.Setup(c => c.GetRoutesTypedAsync()).ReturnsAsync(new List<Route>());
        _mockApisixClient.Setup(c => c.GetServicesTypedAsync()).ReturnsAsync(new List<Service>());
        _mockApisixClient.Setup(c => c.GetUpstreamsTypedAsync()).ReturnsAsync(new List<StandaloneUpstream>());
        _mockApisixClient.Setup(c => c.GetConsumersTypedAsync()).ReturnsAsync(new List<Consumer>());
        _mockApisixClient.Setup(c => c.GetSslsTypedAsync()).ReturnsAsync(new List<SslCertificate>());
        _mockApisixClient.Setup(c => c.GetGlobalRulesTypedAsync()).ReturnsAsync(new List<GlobalRule>());

        var result = await _controller.GetDashboardStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"routeCount\":0", json);
        Assert.Contains("\"serviceCount\":0", json);
        Assert.Contains("\"upstreamCount\":0", json);
        Assert.Contains("\"consumerCount\":0", json);
        Assert.Contains("\"sslCount\":0", json);
        Assert.Contains("\"globalRuleCount\":0", json);
        // Should NOT contain error field when successful
        Assert.DoesNotContain("\"error\"", json);
    }
}
