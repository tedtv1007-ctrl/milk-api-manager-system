using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MilkApiManager.Models;

public class AuditLogEntry
{
    [Key]
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "System";
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Read
    public string Resource { get; set; } = string.Empty; // e.g. "Route", "Consumer"

    [NotMapped]
    public object? Details { get; set; } // Flexible object for runtime usage

    public string? DetailsJson { get; set; } // Serialized JSON for DB storage

    // New audit metadata for better traceability
    public string? CorrelationId { get; set; }
    public string? OperatorIp { get; set; }
    public string? RequestId { get; set; }
}
