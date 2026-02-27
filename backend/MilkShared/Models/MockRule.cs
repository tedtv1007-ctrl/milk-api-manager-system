using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public class MockRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; } = 200;

    public string ResponseBody { get; set; } = "{}";

    public string ContentType { get; set; } = "application/json";

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
