using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Tests.Services;

public class AuditLogShippingOutboxProcessorTests : IDisposable
{
    private readonly AppDbContext _dbContext;

    public AuditLogShippingOutboxProcessorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenDeliverySucceeds_CompletesEntry()
    {
        var payload = new AuditLogShipPayload
        {
            Timestamp = DateTime.UtcNow,
            User = "admin",
            Action = "Create",
            Resource = "Consumer",
            StatusCode = 201
        };

        _dbContext.SyncOutboxEntries.Add(new SyncOutboxEntry
        {
            EventType = SyncOutboxEventType.AuditLogShip,
            Status = SyncOutboxStatus.Pending,
            PayloadJson = JsonSerializer.Serialize(payload),
            NextAttemptAt = DateTime.UtcNow.AddSeconds(-1)
        });
        await _dbContext.SaveChangesAsync();

        var httpClient = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuditLog:Outbox:MaxAttempts"] = "2",
                ["AuditLog:LogstashUrl"] = "http://logstash:8080"
            })
            .Build();

        var processor = new AuditLogShippingOutboxProcessor(
            _dbContext,
            httpClient,
            configuration,
            Mock.Of<ILogger<AuditLogShippingOutboxProcessor>>());

        var processed = await processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        var saved = await _dbContext.SyncOutboxEntries.FirstAsync();
        Assert.Equal(SyncOutboxStatus.Completed, saved.Status);
        Assert.NotNull(saved.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenDeliveryKeepsFailing_MovesToDeadLetterAtMaxAttempts()
    {
        var payload = new AuditLogShipPayload
        {
            Timestamp = DateTime.UtcNow,
            User = "admin",
            Action = "Delete",
            Resource = "Route",
            StatusCode = 500
        };

        _dbContext.SyncOutboxEntries.Add(new SyncOutboxEntry
        {
            EventType = SyncOutboxEventType.AuditLogShip,
            Status = SyncOutboxStatus.Pending,
            PayloadJson = JsonSerializer.Serialize(payload),
            NextAttemptAt = DateTime.UtcNow.AddSeconds(-1)
        });
        await _dbContext.SaveChangesAsync();

        var httpClient = new HttpClient(new StubHandler(_ => throw new HttpRequestException("logstash down")));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuditLog:Outbox:MaxAttempts"] = "2",
                ["AuditLog:LogstashUrl"] = "http://logstash:8080"
            })
            .Build();

        var processor = new AuditLogShippingOutboxProcessor(
            _dbContext,
            httpClient,
            configuration,
            Mock.Of<ILogger<AuditLogShippingOutboxProcessor>>());

        await processor.ProcessBatchAsync();

        var firstTry = await _dbContext.SyncOutboxEntries.FirstAsync();
        Assert.Equal(SyncOutboxStatus.Failed, firstTry.Status);
        Assert.Equal(1, firstTry.AttemptCount);
        Assert.NotNull(firstTry.NextAttemptAt);

        firstTry.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
        await _dbContext.SaveChangesAsync();

        await processor.ProcessBatchAsync();

        var secondTry = await _dbContext.SyncOutboxEntries.FirstAsync();
        Assert.Equal(SyncOutboxStatus.DeadLetter, secondTry.Status);
        Assert.Equal(2, secondTry.AttemptCount);
        Assert.NotNull(secondTry.ProcessedAt);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
