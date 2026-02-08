using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MilkApiManager.Services
{
    public interface IVaultService
    {
        Task<string> StoreSecretAsync(string path, string secret);
        Task<string> GetSecretAsync(string path);
    }

    public class VaultService : IVaultService
    {
        private readonly ILogger<VaultService> _logger;

        public VaultService(ILogger<VaultService> logger)
        {
            _logger = logger;
        }

        public async Task<string> StoreSecretAsync(string path, string secret)
        {
            _logger.LogInformation($"[Mock] Storing secret at {path} in Vault...");
            // 實際專案會使用 VaultSharp 庫
            return "vault-version-1";
        }

        public async Task<string> GetSecretAsync(string path)
        {
            _logger.LogInformation($"[Mock] Retrieving secret from {path}...");
            return "mock-secret-value";
        }
    }
}
