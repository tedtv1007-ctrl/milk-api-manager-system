using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MilkApiManager.Models.Apisix;
using Novell.Directory.Ldap;

namespace MilkApiManager.Services
{
    public class AdGroupSyncService : IHostedService, IDisposable
    {
        private readonly ILogger<AdGroupSyncService> _logger;
        private readonly ApisixClient _apisixClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private Timer? _timer;
        private string _syncStatus = "Idle";
        private DateTime? _lastSyncTime;

        public AdGroupSyncService(ILogger<AdGroupSyncService> logger, ApisixClient apisixClient, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _apisixClient = apisixClient;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AD Group Sync and Security Automation Service is starting.");
            // Sync and check security every 30 minutes
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                await DoSync();
                await DoSecurityCheck();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in AdGroupSyncService DoWork");
            }
        }

        private async Task DoSecurityCheck()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var securityService = scope.ServiceProvider.GetRequiredService<SecurityAutomationService>();
                    await securityService.CheckAndRotateKeys();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Security Lifecycle check.");
            }
        }

        private async Task DoSync()
        {
            _syncStatus = "Syncing";
            try
            {
                _logger.LogInformation("Starting AD Group Sync...");
                
                // Fetch groups from real LDAP
                var adGroups = FetchAdGroupsFromLdap();
                
                foreach (var group in adGroups)
                {
                    await SyncGroupToApisix(group);
                    
                    foreach (var member in group.Members)
                    {
                        await SyncUserToApisix(member, group.Name.ToLower());
                    }
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

        private List<AdGroup> FetchAdGroupsFromLdap()
        {
            var groups = new List<AdGroup>();
            var ldapHost = _configuration["Ldap:Host"];
            var ldapPort = _configuration.GetValue<int>("Ldap:Port");
            var bindDn = _configuration["Ldap:BindDn"];
            var bindPassword = _configuration["Ldap:BindPassword"];
            var searchBase = _configuration["Ldap:SearchBase"];
            var groupFilter = _configuration["Ldap:GroupFilter"];

            // Basic validation
            if (string.IsNullOrEmpty(ldapHost))
            {
                _logger.LogWarning("LDAP Host is not configured. Skipping sync.");
                return groups;
            }

            using (var connection = new LdapConnection())
            {
                try
                {
                    _logger.LogInformation($"Connecting to LDAP at {ldapHost}:{ldapPort}...");
                    connection.Connect(ldapHost, ldapPort);
                    connection.Bind(bindDn, bindPassword);

                    var searchResults = connection.Search(
                        searchBase,
                        LdapConnection.ScopeSub,
                        groupFilter,
                        new string[] { "cn", "member", "uniqueMember" },
                        false
                    );

                    while (searchResults.HasMore())
                    {
                        try 
                        {
                            var entry = searchResults.Next();
                            var groupNameAttribute = entry.GetAttribute("cn");
                            if (groupNameAttribute == null) continue;
                            
                            var groupName = groupNameAttribute.StringValue;
                            if (string.IsNullOrEmpty(groupName)) continue;

                            var members = new List<string>();
                            var memberAttribute = entry.GetAttribute("member") ?? entry.GetAttribute("uniqueMember");
                            
                            if (memberAttribute != null)
                            {
                                var stringValues = memberAttribute.StringValues;
                                while (stringValues.MoveNext())
                                {
                                    var value = stringValues.Current;
                                    // Extract CN from DN (e.g., "cn=alice,ou=users,dc=example,dc=com" -> "alice")
                                    var memberCn = GetCnFromDn(value); 
                                    if (!string.IsNullOrEmpty(memberCn))
                                    {
                                        members.Add(memberCn);
                                    }
                                }
                            }

                            groups.Add(new AdGroup { Name = groupName, Members = members });
                        }
                        catch (LdapReferralException) 
                        {
                            // Ignore referrals
                            continue;
                        }
                    }
                }
                catch (LdapException ex)
                {
                    _logger.LogError(ex, "LDAP error: {LdapError}", ex.LdapErrorMessage);
                    // For now, rethrow or handle gracefully. 
                    // In production, we might want to continue or retry.
                }
                finally
                {
                    if (connection.Connected)
                    {
                        connection.Disconnect();
                    }
                }
            }
            return groups;
        }

        private string GetCnFromDn(string dn)
        {
            var parts = dn.Split(',');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("cn=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Trim().Substring(3);
                }
            }
            return dn; 
        }

        private async Task SyncGroupToApisix(AdGroup group)
        {
            var groupId = group.Name.ToLower();
            _logger.LogInformation($"Syncing group {group.Name} to APISIX...");
            
            var groupConfig = new ConsumerGroup
            {
                Id = groupId,
                Plugins = new Dictionary<string, object>()
            };

            await _apisixClient.CreateConsumerGroupAsync(groupId, groupConfig);
        }

        private async Task SyncUserToApisix(string username, string groupId)
        {
            _logger.LogInformation($"Syncing user {username} to group {groupId} in APISIX...");
            
            var consumer = new Consumer
            {
                Username = username,
                GroupId = groupId,
                Plugins = new Dictionary<string, object>()
            };

            await _apisixClient.UpdateConsumerAsync(username, consumer);
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
