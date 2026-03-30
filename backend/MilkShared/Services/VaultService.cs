using MilkApiManager.Models;
using System.Text.Json;

namespace MilkApiManager.Services
{
    public interface IVaultService
    {
        Task<string> GetSecretAsync(string key);
        Task StoreSecretAsync(string key, string value);
        Task<string> RotateApiKeyAsync(string consumerName);
    }

    public class VaultService : IVaultService
    {
        private readonly ILogger<VaultService> _logger;
        private readonly IApisixClient _apisixClient;
        private readonly string _storagePath = "vault_storage";

        public VaultService(ILogger<VaultService> logger, IApisixClient apisixClient)
        {
            _logger = logger;
            _apisixClient = apisixClient;
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        public async Task<string> GetSecretAsync(string key)
        {
            // In a real Intranet scenario, this would call HashiCorp Vault.
            // For now, we use a Secure Environment Variable lookup with a file fallback.
            var envSecret = Environment.GetEnvironmentVariable($"VAULT_{SanitizeKey(key).ToUpper()}");
            if (!string.IsNullOrEmpty(envSecret)) return envSecret;

            var filePath = Path.Combine(_storagePath, $"{SanitizeKey(key)}.secret");
            if (File.Exists(filePath)) return await File.ReadAllTextAsync(filePath);

            return "mock-secret-value";
        }

        public async Task StoreSecretAsync(string key, string value)
        {
            _logger.LogWarning("Storing secret for {Key} to local file system — use a proper vault in production", key);
            var filePath = Path.Combine(_storagePath, $"{SanitizeKey(key)}.secret");
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(filePath, value);
        }

        public async Task<string> RotateApiKeyAsync(string consumerName)
        {
            _logger.LogWarning("Rotating API Key for Consumer: {ConsumerName}", consumerName);
            
            // 1. Generate a new high-entropy key
            var newKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // 2. Fetch the existing consumer config from APISIX
            var consumer = await _apisixClient.GetConsumerAsync(consumerName);
            if (consumer == null) throw new Exception("Consumer not found");

            // 3. Update the key-auth plugin
            if (consumer.Plugins == null) consumer.Plugins = new Dictionary<string, object>();
            
            consumer.Plugins["key-auth"] = new { key = newKey };

            // 4. Push back to APISIX
            await _apisixClient.UpdateConsumerAsync(consumerName, consumer);

            // 5. Store in our local 'Vault'
            await StoreSecretAsync($"apikey_{consumerName}", newKey);

            return newKey;
        }

        /// <summary>
        /// Sanitize key to prevent path traversal attacks.
        /// </summary>
        private static string SanitizeKey(string key)
        {
            return string.Concat(key.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        }
    }
}
