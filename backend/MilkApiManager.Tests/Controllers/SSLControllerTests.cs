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

public class SSLControllerTests
{
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<ILogger<SSLController>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly SSLController _controller;

    public SSLControllerTests()
    {
        _mockApisixClient = new Mock<IApisixClient>();
        _mockLogger = new Mock<ILogger<SSLController>>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new SSLController(
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
    // GET /api/SSL (List) — strips cert/key for security
    // ============================================================

    [Fact]
    public async Task GetSsls_ReturnsOk_WithSafeProjection()
    {
        var ssls = new List<SslCertificate>
        {
            new SslCertificate
            {
                Id = "ssl-1",
                Cert = "-----BEGIN CERTIFICATE-----\nMIIB...",
                Key = "-----BEGIN RSA PRIVATE KEY-----\nMIIE...",
                Snis = new List<string> { "example.com" },
                Status = 1,
                ValidityStart = 1700000000,
                ValidityEnd = 1800000000
            }
        };

        _mockApisixClient.Setup(c => c.GetSslsTypedAsync())
            .ReturnsAsync(ssls);

        var result = await _controller.GetSsls();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // The result should be an anonymous type list; verify it serializes correctly
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"HasCert\":true", json);
        Assert.Contains("\"HasKey\":true", json);
        // Should NOT contain actual cert/key content
        Assert.DoesNotContain("BEGIN CERTIFICATE", json);
        Assert.DoesNotContain("BEGIN RSA PRIVATE KEY", json);
    }

    [Fact]
    public async Task GetSsls_EmptyList_ReturnsOk()
    {
        _mockApisixClient.Setup(c => c.GetSslsTypedAsync())
            .ReturnsAsync(new List<SslCertificate>());

        var result = await _controller.GetSsls();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSsls_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetSslsTypedAsync())
            .ThrowsAsync(new Exception("APISIX down"));

        var result = await _controller.GetSsls();

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // GET /api/SSL/{id}
    // ============================================================

    [Fact]
    public async Task GetSsl_ExistingId_ReturnsOk()
    {
        var ssl = new SslCertificate
        {
            Id = "ssl-1",
            Cert = "cert-data",
            Key = "key-data",
            Snis = new List<string> { "api.example.com" },
            Status = 1
        };

        _mockApisixClient.Setup(c => c.GetSslAsync("ssl-1"))
            .ReturnsAsync(ssl);

        var result = await _controller.GetSsl("ssl-1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<SslCertificate>(okResult.Value);
        Assert.Equal("ssl-1", returned.Id);
    }

    [Fact]
    public async Task GetSsl_NotFound_ReturnsNotFound()
    {
        _mockApisixClient.Setup(c => c.GetSslAsync("nonexistent"))
            .ReturnsAsync((SslCertificate?)null);

        var result = await _controller.GetSsl("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetSsl_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.GetSslAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.GetSsl("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // PUT /api/SSL/{id} (CreateOrUpdate)
    // ============================================================

    [Fact]
    public async Task CreateOrUpdateSsl_ValidConfig_ReturnsOk()
    {
        var sslConfig = new SslCertificate
        {
            Cert = "-----BEGIN CERTIFICATE-----\nMIIB...",
            Key = "-----BEGIN RSA PRIVATE KEY-----\nMIIE...",
            Snis = new List<string> { "secure.example.com" },
            Status = 1
        };

        _mockApisixClient.Setup(c => c.CreateSslAsync("ssl-new", It.IsAny<SslCertificate>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateOrUpdateSsl("ssl-new", sslConfig);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "CreateOrUpdate" && e.Resource == "SSL")),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateSsl_NullConfig_ReturnsBadRequest()
    {
        var result = await _controller.CreateOrUpdateSsl("ssl-1", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateSsl_EmptyCert_ReturnsBadRequest()
    {
        var sslConfig = new SslCertificate
        {
            Cert = "",
            Key = "some-key",
            Snis = new List<string> { "example.com" }
        };

        var result = await _controller.CreateOrUpdateSsl("ssl-1", sslConfig);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateSsl_EmptyKey_ReturnsBadRequest()
    {
        var sslConfig = new SslCertificate
        {
            Cert = "some-cert",
            Key = "",
            Snis = new List<string> { "example.com" }
        };

        var result = await _controller.CreateOrUpdateSsl("ssl-1", sslConfig);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOrUpdateSsl_AuditLogDoesNotContainCertContent()
    {
        var sslConfig = new SslCertificate
        {
            Cert = "SECRET-CERT-DATA",
            Key = "SECRET-KEY-DATA",
            Snis = new List<string> { "example.com" },
            Status = 1
        };

        AuditLogEntry? capturedLog = null;
        _mockApisixClient.Setup(c => c.CreateSslAsync("ssl-audit", It.IsAny<SslCertificate>()))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Callback<AuditLogEntry>(e => capturedLog = e)
            .Returns(Task.CompletedTask);

        await _controller.CreateOrUpdateSsl("ssl-audit", sslConfig);

        Assert.NotNull(capturedLog);
        var detailsJson = System.Text.Json.JsonSerializer.Serialize(capturedLog!.Details);
        // Audit log should record SNIs but NOT actual cert/key content
        Assert.Contains("example.com", detailsJson);
        Assert.DoesNotContain("SECRET-CERT-DATA", detailsJson);
        Assert.DoesNotContain("SECRET-KEY-DATA", detailsJson);
    }

    [Fact]
    public async Task CreateOrUpdateSsl_OnException_Returns500()
    {
        var sslConfig = new SslCertificate
        {
            Cert = "cert",
            Key = "key",
            Snis = new List<string> { "example.com" }
        };

        _mockApisixClient.Setup(c => c.CreateSslAsync("err", It.IsAny<SslCertificate>()))
            .ThrowsAsync(new Exception("APISIX error"));

        var result = await _controller.CreateOrUpdateSsl("err", sslConfig);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ============================================================
    // DELETE /api/SSL/{id}
    // ============================================================

    [Fact]
    public async Task DeleteSsl_ValidId_Returns204()
    {
        _mockApisixClient.Setup(c => c.DeleteSslAsync("ssl-del"))
            .Returns(Task.CompletedTask);
        _mockAuditLogService.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteSsl("ssl-del");

        Assert.IsType<NoContentResult>(result);
        _mockAuditLogService.Verify(a => a.LogAsync(
            It.Is<AuditLogEntry>(e => e.Action == "Delete" && e.Resource == "SSL")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSsl_OnException_Returns500()
    {
        _mockApisixClient.Setup(c => c.DeleteSslAsync("err"))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.DeleteSsl("err");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
