using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;

namespace MilkApiManager.Services;

public class AuditLogShippingOutboxProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditLogShippingOutboxProcessor> _logger;
    private readonly string _logstashUrl;
    private readonly int _maxAttempts;

    public AuditLogShippingOutboxProcessor(
        AppDbContext dbContext,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AuditLogShippingOutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
        _logstashUrl = Environment.GetEnvironmentVariable("LOGSTASH_URL")
            ?? configuration["AuditLog:LogstashUrl"]
            ?? "http://logstash:8080";
        _maxAttempts = Math.Max(configuration.GetValue<int?>("AuditLog:Outbox:MaxAttempts") ?? 8, 1);
    }

    public virtual async Task<int> ProcessBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entries = await _dbContext.SyncOutboxEntries
            .Where(e => (e.Status == SyncOutboxStatus.Pending || e.Status == SyncOutboxStatus.Failed)
                        && e.EventType == SyncOutboxEventType.AuditLogShip
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
            var payload = JsonSerializer.Deserialize<AuditLogShipPayload>(entry.PayloadJson);
            if (payload == null)
            {
                throw new InvalidOperationException("Invalid audit shipping outbox payload.");
            }

            var response = await _httpClient.PostAsJsonAsync(_logstashUrl, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            entry.Status = SyncOutboxStatus.Completed;
            entry.ProcessedAt = DateTime.UtcNow;
            entry.LastError = null;

            _logger.LogInformation("Processed audit shipping outbox event {OutboxId}", entry.Id);
        }
        catch (Exception ex)
        {
            entry.AttemptCount += 1;
            entry.LastError = ex.Message;

            if (entry.AttemptCount >= _maxAttempts)
            {
                entry.Status = SyncOutboxStatus.DeadLetter;
                entry.ProcessedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Audit outbox event {OutboxId} reached max retry attempts and moved to dead letter.", entry.Id);
                return;
            }

            var delaySeconds = Math.Min((int)Math.Pow(2, Math.Max(entry.AttemptCount, 1)), 300);
            entry.Status = SyncOutboxStatus.Failed;
            entry.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);

            _logger.LogWarning(ex, "Failed to process audit outbox event {OutboxId}, attempt {Attempt}. Next retry at {NextAttemptAt}",
                entry.Id, entry.AttemptCount, entry.NextAttemptAt);
        }
    }
}
