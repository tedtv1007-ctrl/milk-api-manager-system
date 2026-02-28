using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Services;

public class ApisixSyncOutboxProcessorTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ApisixClient> _mockApisixClient;
    private readonly ApisixSyncOutboxProcessor _processor;

    public ApisixSyncOutboxProcessorTests()
    {
        Environment.SetEnvironmentVariable("APISIX_ADMIN_KEY", "test-key");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _mockApisixClient = new Mock<ApisixClient>(Mock.Of<HttpClient>(), Mock.Of<ILogger<ApisixClient>>());
        _processor = new ApisixSyncOutboxProcessor(_dbContext, _mockApisixClient.Object, Mock.Of<ILogger<ApisixSyncOutboxProcessor>>(), maxAttempts: 3);
    }

    [Fact]
    public async Task ProcessBatchAsync_BlacklistSync_CompletesEntry()
    {
        var entry = new SyncOutboxEntry
        {
            EventType = SyncOutboxEventType.BlacklistSync,
            Status = SyncOutboxStatus.Pending,
            PayloadJson = "{\"blacklist\":[\"1.2.3.4\"]}",
            NextAttemptAt = DateTime.UtcNow.AddSeconds(-1)
        };
        _dbContext.SyncOutboxEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.Is<List<string>>(l => l.Count == 1 && l[0] == "1.2.3.4")))
            .Returns(Task.CompletedTask);

        var processed = await _processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        var saved = await _dbContext.SyncOutboxEntries.FirstAsync();
        Assert.Equal(SyncOutboxStatus.Completed, saved.Status);
        Assert.NotNull(saved.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenApisixFails_MarksFailedAndSchedulesRetry()
    {
        var entry = new SyncOutboxEntry
        {
            EventType = SyncOutboxEventType.BlacklistSync,
            Status = SyncOutboxStatus.Pending,
            PayloadJson = "{\"blacklist\":[\"5.6.7.8\"]}",
            NextAttemptAt = DateTime.UtcNow.AddSeconds(-1)
        };
        _dbContext.SyncOutboxEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.UpdateBlacklistAsync(It.IsAny<List<string>>()))
            .ThrowsAsync(new Exception("apisix unavailable"));

        await _processor.ProcessBatchAsync();

        var saved = await _dbContext.SyncOutboxEntries.FirstAsync();
        Assert.Equal(SyncOutboxStatus.Failed, saved.Status);
        Assert.Equal(1, saved.AttemptCount);
        Assert.NotNull(saved.NextAttemptAt);
        Assert.Contains("apisix unavailable", saved.LastError);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
