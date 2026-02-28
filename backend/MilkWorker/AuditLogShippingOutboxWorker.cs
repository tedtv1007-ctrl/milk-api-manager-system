using MilkApiManager.Services;

namespace MilkApiManager.Workers;

public class AuditLogShippingOutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogShippingOutboxWorker> _logger;
    private readonly TimeSpan _interval;

    public AuditLogShippingOutboxWorker(IServiceProvider serviceProvider, ILogger<AuditLogShippingOutboxWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var intervalSeconds = configuration.GetValue<int?>("AuditLog:Outbox:PollIntervalSeconds") ?? 10;
        _interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditLogShippingOutboxWorker started. Poll interval: {IntervalSeconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<AuditLogShippingOutboxProcessor>();
                var processed = await processor.ProcessBatchAsync(cancellationToken: stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Audit outbox processor handled {Count} event(s)", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audit outbox batch");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
