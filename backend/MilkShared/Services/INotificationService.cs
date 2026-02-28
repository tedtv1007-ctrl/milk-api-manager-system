namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for sending alerts via webhook/email notification channels.
/// </summary>
public interface INotificationService
{
    Task AlertAsync(string title, string message, bool isCritical = false);
}
