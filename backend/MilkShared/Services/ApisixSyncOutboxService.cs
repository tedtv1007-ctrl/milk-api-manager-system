using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;

namespace MilkApiManager.Services;

public class ApisixSyncOutboxService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ApisixSyncOutboxService> _logger;

    public ApisixSyncOutboxService(AppDbContext dbContext, ILogger<ApisixSyncOutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public virtual async Task EnqueueBlacklistSyncAsync(List<string> blacklist, CancellationToken cancellationToken = default)
    {
        var entry = new SyncOutboxEntry
        {
            EventType = SyncOutboxEventType.BlacklistSync,
            Status = SyncOutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            NextAttemptAt = DateTime.UtcNow,
            PayloadJson = JsonSerializer.Serialize(new BlacklistSyncPayload
            {
                Blacklist = blacklist
            })
        };

        _dbContext.SyncOutboxEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Enqueued outbox event {EventType} with id {OutboxId}", entry.EventType, entry.Id);
    }
}

public class ApisixSyncOutboxProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IApisixClient _apisixClient;
    private readonly ILogger<ApisixSyncOutboxProcessor> _logger;
    private readonly int _maxAttempts;

    public ApisixSyncOutboxProcessor(AppDbContext dbContext, IApisixClient apisixClient, ILogger<ApisixSyncOutboxProcessor> logger, int maxAttempts = 8)
    {
        _dbContext = dbContext;
        _apisixClient = apisixClient;
        _logger = logger;
        _maxAttempts = maxAttempts;
    }

    public virtual async Task<int> ProcessBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entries = await _dbContext.SyncOutboxEntries
            .Where(e => (e.Status == SyncOutboxStatus.Pending || e.Status == SyncOutboxStatus.Failed)
                        && e.EventType == SyncOutboxEventType.BlacklistSync
                        && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            await ProcessOneAsync(entry, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }

    private async Task ProcessOneAsync(SyncOutboxEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            if (entry.EventType == SyncOutboxEventType.BlacklistSync)
            {
                var payload = JsonSerializer.Deserialize<BlacklistSyncPayload>(entry.PayloadJson);
                if (payload == null)
                {
                    throw new InvalidOperationException("Invalid blacklist outbox payload.");
                }

                await _apisixClient.UpdateBlacklistAsync(payload.Blacklist);
                entry.Status = SyncOutboxStatus.Completed;
                entry.ProcessedAt = DateTime.UtcNow;
                entry.LastError = null;

                _logger.LogInformation("Processed outbox event {OutboxId}", entry.Id);
                return;
            }

            entry.Status = SyncOutboxStatus.DeadLetter;
            entry.LastError = $"Unsupported event type: {entry.EventType}";
            entry.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            entry.AttemptCount += 1;
            entry.LastError = ex.Message;

            if (entry.AttemptCount >= _maxAttempts)
            {
                entry.Status = SyncOutboxStatus.DeadLetter;
                entry.ProcessedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Outbox event {OutboxId} reached max retry attempts and moved to dead letter.", entry.Id);
                return;
            }

            var delaySeconds = Math.Min((int)Math.Pow(2, Math.Max(entry.AttemptCount, 1)), 300);
            entry.Status = SyncOutboxStatus.Failed;
            entry.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);

            _logger.LogWarning(ex, "Failed to process outbox event {OutboxId}, attempt {Attempt}. Next retry at {NextAttemptAt}",
                entry.Id, entry.AttemptCount, entry.NextAttemptAt);
        }
    }
}
