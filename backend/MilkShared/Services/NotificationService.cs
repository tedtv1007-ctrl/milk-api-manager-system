using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationService(ILogger<NotificationService> logger, HttpClient httpClient, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
        }

        public async Task AlertAsync(string title, string message, bool isCritical = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var channels = await dbContext.NotificationChannels
                .Where(c => c.IsEnabled)
                .ToListAsync();

            if (!channels.Any())
            {
                _logger.LogWarning("No notification channels configured. Alert suppressed: {Title}", title);
                return;
            }

            foreach (var channel in channels)
            {
                if (channel.CriticalOnly && !isCritical) continue;

                if (channel.Type == "Webhook")
                {
                    await SendWebhookAsync(channel.Target, title, message, isCritical);
                }
                else if (channel.Type == "Email")
                {
                    _logger.LogInformation("[EMAIL MOCK] To: {Target}, Subject: {Title}, Body: {Message}", channel.Target, title, message);
                }
            }
        }

        private async Task SendWebhookAsync(string url, string title, string message, bool isCritical)
        {
            try
            {
                var icon = isCritical ? "🚨" : "📢";
                var color = isCritical ? "#FF0000" : "#36a64f";
                
                // Generic Payload (Slack/Mattermost compatible)
                var payload = new 
                { 
                    text = $"{icon} **{title}**\n{message}",
                    username = "Milk API Guardian",
                    icon_emoji = ":shield:",
                    attachments = new[] {
                        new {
                            color = color,
                            title = title,
                            text = message,
                            ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Webhook failed: {Url} {Status}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook to {Url}", url);
            }
        }
    }
}
