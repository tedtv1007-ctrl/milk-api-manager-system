namespace MilkApiManager.Models
{
    public class AnalyticsQuery
    {
        public string? Consumer { get; set; }
        public string? Route { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Step { get; set; } = "1m";
    }

    public class MetricPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class AnalyticsResult
    {
        public required string Label { get; set; }
        public List<MetricPoint> Data { get; set; } = new();
    }
}
