using System.ComponentModel.DataAnnotations;

namespace MilkApiManager.Models;

public class PiiMaskingRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RouteId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FieldPath { get; set; } = string.Empty; // e.g., "user.email"

    [Required]
    public string RegexPattern { get; set; } = ".*"; // e.g., "(.{3}).*(@.*)"

    [Required]
    public string ReplacePattern { get; set; } = "***"; // e.g., "$1***$2"

    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Description { get; set; } = string.Empty;
}
