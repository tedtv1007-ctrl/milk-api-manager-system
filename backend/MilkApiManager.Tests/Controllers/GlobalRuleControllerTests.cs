using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class GlobalRuleControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<GlobalRuleController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly GlobalRuleController _controller;

    public GlobalRuleControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<GlobalRuleController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new GlobalRuleController(
            _mockApisixClient.Object,
            _mockLogger.Object,
            _mockAuditLogService.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ============================================================
    // GET /api/GlobalRule (List)
    // ============================================================

    [Fact]
    public async Task GetGlobalRules_ReturnsOk_WithRuleList()
    {
        var rules = new List<GlobalRule>
        {
            new GlobalRule
            {
                Id = "rule-1",
                Plugins = new Dictionary<string, object>
                {
                    { "prometheus", new { } },
                    { "cors", new { allow_origins = "*" } }
                }
            }
        };

        _mockApisixClient.Setup(c => c.GetGlobalRulesTypedAsync())
            .ReturnsAsync(rules);

        var result = await _controller.GetGlobalRules();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<GlobalRule>>(okResult.Value);
        Assert.Single(returned);
        Assert.Equal("rule-1", returned[0].Id);
        Assert.Equal(2, returned[0].Plugins!.Count);
    }

    [Fact]
    public async Task GetGlobalRules_EmptyList_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetGlobalRulesTypedAsync())
            .ReturnsAsync(new List<GlobalRule>());

        var result = await _controller.GetGlobalRules();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<List<GlobalRule>>(okResult.Value);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task GetGlobalRules_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetGlobalRulesTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetGlobalRules();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // PUT /api/GlobalRule/{id} (CreateOrUpdate)
    // ============================================================

    [Fact]
    public async Task CreateOrUpdateGlobalRule_ValidConfig_ReturnsOk()
    {
        var ruleConfig = new GlobalRule
        {
            Plugins = new Dictionary<string, object>
            {
                { "prometheus", new { } }
            }
        };

        _mockApisixClient.Setup(c => c.CreateGlobalRuleAsync("rule-new", It.IsAny<GlobalRule>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateOrUpdateGlobalRule("rule-new", ruleConfig);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "CreateOrUpdate" && e.Resource == "GlobalRule")),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateGlobalRule_SetsIdFromRoute()
    {
        var ruleConfig = new GlobalRule
        {
            Plugins = new Dictionary<string, object> { { "cors", new { } } }
        };

        GlobalRule? captured = null;
        _mockApisixClient.Setup(c => c.CreateGlobalRuleAsync("rule-id-test", It.IsAny<GlobalRule>()))
            .Callback<string, GlobalRule>((id, config) => captured = config)
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        await _controller.CreateOrUpdateGlobalRule("rule-id-test", ruleConfig);

        Assert.NotNull(captured);
        Assert.Equal("rule-id-test", captured!.Id);
    }

    [Fact]
    public async Task CreateOrUpdateGlobalRule_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.CreateOrUpdateGlobalRule("rule-1", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateGlobalRule_OnException_Returns500()
    {
        var ruleConfig = new GlobalRule
        {
            Plugins = new Dictionary<string, object> { { "fail", new { } } }
        };

        _mockApisixClient.Setup(c => c.CreateGlobalRuleAsync("err", It.IsAny<GlobalRule>()))
            .ThrowsAsync(new Exception("APISIX error"));

        var result = await _controller.CreateOrUpdateGlobalRule("err", ruleConfig);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // DELETE /api/GlobalRule/{id}
    // ============================================================

    [Fact]
    public async Task DeleteGlobalRule_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteGlobalRuleAsync("rule-del"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteGlobalRule("rule-del");

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Delete" && e.Resource == "GlobalRule")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteGlobalRule_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteGlobalRuleAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteGlobalRule("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
