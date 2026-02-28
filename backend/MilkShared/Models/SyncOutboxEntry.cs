namespace MilkApiManager.Models;

public static class SyncOutboxEventType
{
    public const string BlacklistSync = "BlacklistSync";
    public const string AuditLogShip = "AuditLogShip";
}

public static class SyncOutboxStatus
{
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Completed = "Completed";
    public const string DeadLetter = "DeadLetter";
}

public class SyncOutboxEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = SyncOutboxStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}

public class BlacklistSyncPayload
{
    public List<string> Blacklist { get; set; } = new();
}

public class AuditLogShipPayload
{
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? DetailsJson { get; set; }
    public string? CorrelationId { get; set; }
    public string? OperatorIp { get; set; }
    public string? RequestId { get; set; }
}
