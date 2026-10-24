using Microsoft.Extensions.Configuration;
using MilkApiManager.Data;

namespace MilkApiManager.Tests.Services;

public class DatabaseProviderConfigurationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldUseInMemoryDatabase_WhenNotConfigured_FollowsTestMode(bool isTestMode)
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = DatabaseProviderConfiguration.ShouldUseInMemoryDatabase(configuration, isTestMode);

        Assert.Equal(isTestMode, result);
    }

    [Fact]
    public void ShouldUseInMemoryDatabase_WhenExplicitlyDisabled_UsesConfiguredPostgresInTestMode()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:UseInMemory"] = "false"
            })
            .Build();

        var result = DatabaseProviderConfiguration.ShouldUseInMemoryDatabase(configuration, isTestMode: true);

        Assert.False(result);
    }

    [Fact]
    public void ShouldUseInMemoryDatabase_WhenValueIsInvalid_ThrowsConfigurationError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:UseInMemory"] = "sometimes"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseProviderConfiguration.ShouldUseInMemoryDatabase(configuration, isTestMode: true));

        Assert.Contains("Database:UseInMemory", exception.Message);
    }
}
