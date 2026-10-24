using Microsoft.Extensions.Configuration;

namespace MilkApiManager.Data;

public static class DatabaseProviderConfiguration
{
    public static bool ShouldUseInMemoryDatabase(IConfiguration configuration, bool isTestMode)
    {
        var configuredValue = configuration["Database:UseInMemory"];
        if (configuredValue is null)
        {
            return isTestMode;
        }

        if (bool.TryParse(configuredValue, out var useInMemoryDatabase))
        {
            return useInMemoryDatabase;
        }

        throw new InvalidOperationException(
            "Configuration value 'Database:UseInMemory' must be either 'true' or 'false'.");
    }
}
