using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public class ApiServiceMetadata
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; // e.g. "Payment API"

    public string Description { get; set; } = string.Empty;

    [Required]
    public string BasePath { get; set; } = string.Empty; // e.g. "/api/v1/payment"

    [Required]
    public string OpenApiUrl { get; set; } = string.Empty; // URL to swagger.json or openapi.yaml

    public string OwnerTeam { get; set; } = "General";

    public bool IsPublic { get; set; } = true;

    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
