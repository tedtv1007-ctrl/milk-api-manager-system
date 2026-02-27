using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class AccessRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    public string ApplicantEmail { get; set; } = string.Empty;

    [Required]
    public string RequestedTier { get; set; } = "Free"; // e.g. Free, Silver, Gold

    [Required]
    public string Purpose { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? AdminComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
}
