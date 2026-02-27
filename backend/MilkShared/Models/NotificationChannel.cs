using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public class NotificationChannel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; // e.g. "DevOps Slack"

    [Required]
    public string Type { get; set; } = "Webhook"; // Webhook, Email

    [Required]
    public string Target { get; set; } = string.Empty; // URL or Email Address

    public bool IsEnabled { get; set; } = true;

    // Optional: Only send Critical alerts
    public bool CriticalOnly { get; set; } = false;
}
