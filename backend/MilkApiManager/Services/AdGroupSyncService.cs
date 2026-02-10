using System.Text.Json;
using System.Text.Json.Serialization;
using MilkApiManager.Models.Apisix;

namespace MilkApiManager.Services
{
    public class AdGroupSyncService : IHostedService, IDisposable
    {
        private readonly ILogger<AdGroupSyncService> _logger;
        private readonly ApisixClient _apisixClient;
        private Timer? _timer;
        private string _syncStatus = "Idle";
        private DateTime? _lastSyncTime;

        public AdGroupSyncService(ILogger<AdGroupSyncService> logger, ApisixClient apisixClient)
        {
            _logger = logger;
            _apisixClient = apisixClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AD Group Sync Service is starting.");
            // Sync every 30 minutes
            _timer = new Timer(DoSync, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private async void DoSync(object? state)
        {
            _syncStatus = "Syncing";
            try
            {
                _logger.LogInformation("Starting AD Group Sync...");
                
                // Mock AD Group fetching
                var adGroups = await FetchAdGroupsMock();
                
                foreach (var group in adGroups)
                {
                    await SyncGroupToApisix(group);
                }

                _lastSyncTime = DateTime.UtcNow;
                _syncStatus = "Success";
                _logger.LogInformation("AD Group Sync completed successfully.");
            }
            catch (Exception ex)
            {
                _syncStatus = "Failed";
                _logger.LogError(ex, "Error during AD Group Sync.");
            }
        }

        private async Task<List<AdGroup>> FetchAdGroupsMock()
        {
            // In a real implementation, this would use System.DirectoryServices.AccountManagement
            await Task.Delay(500); // Simulate network delay
            return new List<AdGroup>
            {
                new AdGroup { Name = "Developers", Members = new List<string> { "alice", "bob" } },
                new AdGroup { Name = "Managers", Members = new List<string> { "charlie" } }
            };
        }

        private async Task SyncGroupToApisix(AdGroup group)
        {
            var groupId = group.Name.ToLower();
            // In APISIX, Consumer Groups are handled via the 'consumer-group' resource
            // Note: ApisixClient needs to be extended to support consumer groups explicitly if needed, 
            // but for now we can use the generic method or add it.
            
            _logger.LogInformation($"Syncing group {group.Name} to APISIX...");
            
            // Example body for APISIX Consumer Group
            var groupConfig = new
            {
                id = groupId,
                plugins = new { } 
            };

            await _apisixClient.CreateConsumerGroupAsync(groupId, groupConfig);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AD Group Sync Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public string GetStatus() => _syncStatus;
        public DateTime? GetLastSyncTime() => _lastSyncTime;
    }

    public class AdGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new();
    }
}
