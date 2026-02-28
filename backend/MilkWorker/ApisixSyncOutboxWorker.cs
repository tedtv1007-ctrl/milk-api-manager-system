using MilkApiManager.Services;

namespace MilkApiManager.Workers;

public class ApisixSyncOutboxWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApisixSyncOutboxWorker> _logger;
    private readonly TimeSpan _interval;

    public ApisixSyncOutboxWorker(IServiceProvider serviceProvider, ILogger<ApisixSyncOutboxWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var intervalSeconds = configuration.GetValue<int?>("Sync:Outbox:PollIntervalSeconds") ?? 10;
        _interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ApisixSyncOutboxWorker started. Poll interval: {IntervalSeconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ApisixSyncOutboxProcessor>();
                var processed = await processor.ProcessBatchAsync(cancellationToken: stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Outbox processor handled {Count} event(s)", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing APISIX sync outbox batch");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
