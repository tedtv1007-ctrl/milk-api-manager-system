using MilkApiManager.Services;

namespace MilkApiManager.Workers;

public class BlacklistReconcileWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlacklistReconcileWorker> _logger;
    private readonly TimeSpan _interval;

    public BlacklistReconcileWorker(IServiceProvider serviceProvider, ILogger<BlacklistReconcileWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var intervalSeconds = configuration.GetValue<int?>("Sync:Blacklist:ReconcileIntervalSeconds") ?? 120;
        _interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlacklistReconcileWorker started. Interval: {IntervalSeconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<BlacklistConsistencyService>();
                var report = await service.ReconcileDatabaseToGatewayAsync(stoppingToken);

                if (!report.IsInSync)
                {
                    _logger.LogWarning("Blacklist reconcile still reports drift. DB-only: {DbOnly}, Gateway-only: {GatewayOnly}",
                        report.DatabaseOnly.Count, report.GatewayOnly.Count);
                }
                else
                {
                    _logger.LogInformation("Blacklist reconcile completed and is in sync.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blacklist reconcile loop failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
