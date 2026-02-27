using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models
{
    public class AlertRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public required string Name { get; set; }
        
        [Required]
        public required string MetricName { get; set; } // e.g., "http_5xx_spikes", "high_frequency_ip"
        
        public double Threshold { get; set; }
        
        public required string Duration { get; set; } // e.g., "5m"
        
        public bool IsEnabled { get; set; } = true;
        
        public List<string> NotificationChannels { get; set; } = new List<string>(); // "Email", "Mattermost"
        
        public string Severity { get; set; } = "Warning"; // "Info", "Warning", "Critical"
    }

    public class AnomalyAlert
    {
        public required string RuleId { get; set; }
        public required string RuleName { get; set; }
        public required string MetricValue { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Message { get; set; }
    }
}