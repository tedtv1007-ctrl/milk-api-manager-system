using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class KeysControllerTests
{
    private readonly Mock<IVaultService> _mockVaultService;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly AppDbContext _dbContext;
    private readonly KeysController _controller;

    public KeysControllerTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");
        _mockVaultService = new Mock<IVaultService>();
        _mockApisixClient = new Mock<ApisixClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<ILogger<ApisixClient>>()
        );

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"KeysControllerTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _controller = new KeysController(_mockVaultService.Object, _mockApisixClient.Object, _dbContext);
    }

    // ============================================================
    // CREATE
    // ============================================================

    [Fact]
    public async Task CreateKey_ValidRequest_ReturnsCreated()
    {
        var request = new CreateKeyRequest { Owner = "test-consumer" };
        _mockVaultService.Setup(v => v.StoreSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockApisixClient.Setup(c => c.CreateConsumerAsync("test-consumer", It.IsAny<Consumer>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateKey(request);

        var createdResult = Assert.IsType<CreatedResult>(result);
        _mockVaultService.Verify(v => v.StoreSecretAsync(
            It.Is<string>(s => s.Contains("test-consumer")),
            It.IsAny<string>()), Times.Once);
        _mockApisixClient.Verify(c => c.CreateConsumerAsync("test-consumer", It.IsAny<Consumer>()), Times.Once);

        // 驗證 DB 持久化
        var keys = await _dbContext.ApiKeys.ToListAsync();
        Assert.Single(keys);
        Assert.Equal("test-consumer", keys[0].Owner);
        Assert.True(keys[0].IsActive);
        Assert.True(keys[0].ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateKey_WithValidityDays_SetsCorrectExpiresAt()
    {
        var request = new CreateKeyRequest { Owner = "expiry-test", ValidityDays = 30, ContactEmail = "test@example.com" };
        _mockVaultService.Setup(v => v.StoreSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockApisixClient.Setup(c => c.CreateConsumerAsync("expiry-test", It.IsAny<Consumer>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.CreateKey(request);

        Assert.IsType<CreatedResult>(result);
        var key = await _dbContext.ApiKeys.FirstAsync();
        Assert.Equal("test@example.com", key.ContactEmail);
        // ExpiresAt should be approximately 30 days from now
        var expectedExpiry = DateTime.UtcNow.AddDays(30);
        Assert.InRange(key.ExpiresAt, expectedExpiry.AddMinutes(-1), expectedExpiry.AddMinutes(1));
    }

    [Fact]
    public async Task CreateKey_NullRequest_ReturnsBadRequest()
    {
        var result = await _controller.CreateKey(null!);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateKey_EmptyOwner_ReturnsBadRequest()
    {
        var request = new CreateKeyRequest { Owner = "" };
        var result = await _controller.CreateKey(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // READ (List)
    // ============================================================

    [Fact]
    public async Task GetKeys_ReturnsOk_WithKeysList()
    {
        // Seed test data
        _dbContext.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "hash1",
            Owner = "owner1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            Scopes = "[\"read\"]",
            ContactEmail = "a@b.com"
        });
        _dbContext.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "hash2",
            Owner = "owner2",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20),
            IsActive = true,
            Scopes = "[\"read\",\"write\"]",
            ContactEmail = "b@c.com"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetKeys();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetKeys_EmptyDb_ReturnsEmptyList()
    {
        var result = await _controller.GetKeys();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    // ============================================================
    // READ (Single)
    // ============================================================

    [Fact]
    public async Task GetKey_ExistingKey_ReturnsOk()
    {
        var keyId = Guid.NewGuid();
        _dbContext.ApiKeys.Add(new ApiKey
        {
            Id = keyId,
            KeyHash = "hash-test",
            Owner = "get-test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            Scopes = "[\"read\"]",
            ContactEmail = "test@test.com"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetKey(keyId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetKey_NonExistingKey_ReturnsNotFound()
    {
        var result = await _controller.GetKey(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // UPDATE (Rotate)
    // ============================================================

    [Fact]
    public async Task RotateKey_ValidConsumer_ReturnsOkAndUpdatesDb()
    {
        // Seed existing key
        _dbContext.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "old-hash",
            Owner = "rotate-consumer",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = DateTime.UtcNow.AddDays(60),
            IsActive = true,
            Scopes = "[\"read\"]",
            ContactEmail = ""
        });
        await _dbContext.SaveChangesAsync();

        _mockVaultService.Setup(v => v.RotateApiKeyAsync("rotate-consumer"))
            .ReturnsAsync("new-rotated-key-1234");

        var result = await _controller.RotateKey("rotate-consumer");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify DB was updated
        var key = await _dbContext.ApiKeys.FirstAsync(k => k.Owner == "rotate-consumer");
        Assert.NotNull(key.LastRotatedAt);
        Assert.NotEqual("old-hash", key.KeyHash);
    }

    [Fact]
    public async Task RotateKey_ConsumerNotFound_ReturnsBadRequest()
    {
        _mockVaultService.Setup(v => v.RotateApiKeyAsync("nonexistent"))
            .ThrowsAsync(new Exception("Consumer nonexistent not found in APISIX"));

        var result = await _controller.RotateKey("nonexistent");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteKey_ExistingKey_ReturnsNoContent()
    {
        var keyId = Guid.NewGuid();
        _dbContext.ApiKeys.Add(new ApiKey
        {
            Id = keyId,
            KeyHash = "hash-delete",
            Owner = "delete-test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            Scopes = "[\"read\"]",
            ContactEmail = ""
        });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteKey(keyId);

        Assert.IsType<NoContentResult>(result);

        // 驗證金鑰已被停用
        var key = await _dbContext.ApiKeys.FindAsync(keyId);
        Assert.NotNull(key);
        Assert.False(key!.IsActive);
    }

    [Fact]
    public async Task DeleteKey_NonExistingKey_ReturnsNotFound()
    {
        var result = await _controller.DeleteKey(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
