using MilkApiManager.Models;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public class AlertMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertMonitoringService> _logger;
        private readonly List<AlertRule> _rules;

        public AlertMonitoringService(IServiceProvider serviceProvider, ILogger<AlertMonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // In a real app, these would come from a database
            _rules = new List<AlertRule>
            {
                new AlertRule { 
                    Name = "5xx Error Spike", 
                    MetricName = "apisix_http_status", 
                    Threshold = 10, 
                    Duration = "1m",
                    NotificationChannels = new List<string> { "Mattermost" }
                },
                new AlertRule { 
                    Name = "High Frequency IP Access", 
                    MetricName = "apisix_http_requests_total", 
                    Threshold = 100, 
                    Duration = "1m",
                    NotificationChannels = new List<string> { "Email", "Mattermost" }
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking alert rules at: {time}", DateTimeOffset.Now);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var prometheusService = scope.ServiceProvider.GetRequiredService<PrometheusService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    foreach (var rule in _rules.Where(r => r.IsEnabled))
                    {
                        try 
                        {
                            await CheckRuleAsync(rule, prometheusService, notificationService);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking alert rule {RuleName}", rule.Name);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckRuleAsync(AlertRule rule, PrometheusService prometheus, NotificationService notifier)
        {
            string query = "";
            if (rule.MetricName == "apisix_http_status")
            {
                // Query for 5xx rate in the last duration
                query = $"sum(rate(apisix_http_status{{code=~\"5..\"}}[{rule.Duration}]))";
            }
            else if (rule.MetricName == "apisix_http_requests_total")
            {
                // Query for high frequency IP
                query = $"max(rate(apisix_http_requests_total[{rule.Duration}])) by (remote_addr)";
            }

            if (string.IsNullOrEmpty(query)) return;

            // Using Prometheus Instant Query API would be better, but we reuse existing GetMetricAsync for simplicity
            var results = await prometheus.GetMetricAsync(query, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow, "1m");

            foreach (var result in results)
            {
                var latestPoint = result.Data.OrderByDescending(p => p.Timestamp).FirstOrDefault();
                if (latestPoint != null && latestPoint.Value > rule.Threshold)
                {
                    _logger.LogWarning("ALERT TRIGGERED: {RuleName} - Value: {Value}", rule.Name, latestPoint.Value);
                    
                    var message = $"Alert '{rule.Name}' triggered! Metric: {rule.MetricName}, Value: {latestPoint.Value}, Threshold: {rule.Threshold}";
                    await notifier.AlertAsync($"🔥 Alert: {rule.Name}", message, isCritical: true);
                }
            }
        }
    }
}
