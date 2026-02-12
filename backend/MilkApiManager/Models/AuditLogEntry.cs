using System.Text.Json.Nodes;

namespace MilkApiManager.Models;

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "System";
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Read
    public string Resource { get; set; } = string.Empty; // e.g. "Route", "Consumer"
    public object? Details { get; set; } // Flexible JSON object
}
