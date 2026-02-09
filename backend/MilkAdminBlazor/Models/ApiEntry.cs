namespace Milk.Apim.Admin.Models;

public enum RiskLevel { L1, L2, L3 }

public class ApiEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string RoutePath { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; } = RiskLevel.L3;
    public string Department { get; set; } = "IT";
    public DateTime LastReviewedAt { get; set; } = DateTime.Now;
    public bool IsExposed { get; set; } = false;
}
