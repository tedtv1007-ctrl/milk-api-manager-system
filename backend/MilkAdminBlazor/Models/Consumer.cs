namespace MilkAdminBlazor.Models;

public class Consumer
{
    public string Username { get; set; } = string.Empty;
    public string CustomId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
}
