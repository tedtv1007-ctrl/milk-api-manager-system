using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public class ApiTestScenario
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ServiceId { get; set; } // Link to ApiServiceMetadata

    [Required]
    public string Name { get; set; } = "Default Test";

    [Required]
    public string Endpoint { get; set; } = "/"; // Relative to BasePath

    public string HttpMethod { get; set; } = "GET";

    public int ExpectedStatusCode { get; set; } = 200;

    public string? LastResult { get; set; } // Success, Fail, or Status Code

    public DateTime? LastRunAt { get; set; }
}
