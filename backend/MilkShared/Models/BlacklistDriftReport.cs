namespace MilkApiManager.Models;

public class BlacklistDriftReport
{
    public List<string> DatabaseOnly { get; set; } = new();
    public List<string> GatewayOnly { get; set; } = new();
    public bool IsInSync => !DatabaseOnly.Any() && !GatewayOnly.Any();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
