namespace MilkApiManager.Models;

/// <summary>
/// Consistent error response format for the Milk API.
/// </summary>
/// <param name="Error">Machine-readable error code (e.g. "NotFound")</param>
/// <param name="Message">Human-readable error description</param>
/// <param name="Details">Optional additional context or validation errors</param>
public record ApiError(string Error, string Message, object? Details = null);
