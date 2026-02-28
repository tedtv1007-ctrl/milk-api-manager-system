using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkWorker.Tests;

public class KeyRotationBackgroundServiceTests
{
    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var dbName = $"KeyRotationTests_{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddLogging();
        // NotificationService requires IServiceScopeFactory internally, but we just
        // register it with mocked dependencies — the test won't actually hit notification channels
        // since the DB will have no NotificationChannels entries
        services.AddSingleton<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceScopeFactory>());
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task CheckAndProcessKeys_DeactivatesExpiredKeys()
    {
        var serviceProvider = BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        
        // Seed test data
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = "expired-hash",
                Owner = "expired-owner",
                CreatedAt = DateTime.UtcNow.AddDays(-100),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // 已過期
                IsActive = true,
                Scopes = "[\"read\"]",
                ContactEmail = "expired@test.com"
            });
            await db.SaveChangesAsync();
        }

        var service = new KeyRotationBackgroundService(
            scopeFactory,
            Mock.Of<ILogger<KeyRotationBackgroundService>>()
        );

        await service.CheckAndProcessKeysAsync();

        // Verify
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var key = await db.ApiKeys.FirstAsync(k => k.Owner == "expired-owner");
            Assert.False(key.IsActive); // 應被停用
        }
    }

    [Fact]
    public async Task CheckAndProcessKeys_KeepsSoonToExpireKeysActive()
    {
        var serviceProvider = BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = "soon-expire-hash",
                Owner = "soon-expire-owner",
                CreatedAt = DateTime.UtcNow.AddDays(-87),
                ExpiresAt = DateTime.UtcNow.AddDays(3), // 3 天內到期但尚未過期
                IsActive = true,
                Scopes = "[\"read\"]",
                ContactEmail = "soon@test.com"
            });
            await db.SaveChangesAsync();
        }

        var service = new KeyRotationBackgroundService(
            scopeFactory,
            Mock.Of<ILogger<KeyRotationBackgroundService>>()
        );

        await service.CheckAndProcessKeysAsync();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var key = await db.ApiKeys.FirstAsync(k => k.Owner == "soon-expire-owner");
            Assert.True(key.IsActive); // 應保持啟用（尚未過期）
        }
    }

    [Fact]
    public async Task CheckAndProcessKeys_SkipsActiveNonExpiringKeys()
    {
        var serviceProvider = BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = "healthy-hash",
                Owner = "healthy-owner",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(60), // 60 天後到期
                IsActive = true,
                Scopes = "[\"read\"]",
                ContactEmail = ""
            });
            await db.SaveChangesAsync();
        }

        var service = new KeyRotationBackgroundService(
            scopeFactory,
            Mock.Of<ILogger<KeyRotationBackgroundService>>()
        );

        await service.CheckAndProcessKeysAsync();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var key = await db.ApiKeys.FirstAsync(k => k.Owner == "healthy-owner");
            Assert.True(key.IsActive); // 應保持啟用
        }
    }

    [Fact]
    public async Task CheckAndProcessKeys_MultipleExpiredKeys_DeactivatesAll()
    {
        var serviceProvider = BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (int i = 0; i < 3; i++)
            {
                db.ApiKeys.Add(new ApiKey
                {
                    Id = Guid.NewGuid(),
                    KeyHash = $"multi-expired-hash-{i}",
                    Owner = $"multi-expired-{i}",
                    CreatedAt = DateTime.UtcNow.AddDays(-100),
                    ExpiresAt = DateTime.UtcNow.AddDays(-i - 1),
                    IsActive = true,
                    Scopes = "[\"read\"]",
                    ContactEmail = ""
                });
            }
            await db.SaveChangesAsync();
        }

        var service = new KeyRotationBackgroundService(
            scopeFactory,
            Mock.Of<ILogger<KeyRotationBackgroundService>>()
        );

        await service.CheckAndProcessKeysAsync();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeKeys = await db.ApiKeys.Where(k => k.IsActive).CountAsync();
            Assert.Equal(0, activeKeys);
        }
    }

    [Fact]
    public async Task CheckAndProcessKeys_MixedKeys_CorrectlyProcesses()
    {
        var serviceProvider = BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // 已過期
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(), KeyHash = "h1", Owner = "expired",
                CreatedAt = DateTime.UtcNow.AddDays(-100), ExpiresAt = DateTime.UtcNow.AddDays(-5),
                IsActive = true, Scopes = "[\"read\"]", ContactEmail = ""
            });
            // 即將到期
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(), KeyHash = "h2", Owner = "soon-expire",
                CreatedAt = DateTime.UtcNow.AddDays(-80), ExpiresAt = DateTime.UtcNow.AddDays(2),
                IsActive = true, Scopes = "[\"read\"]", ContactEmail = ""
            });
            // 健康
            db.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(), KeyHash = "h3", Owner = "healthy",
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(90),
                IsActive = true, Scopes = "[\"read\"]", ContactEmail = ""
            });
            await db.SaveChangesAsync();
        }

        var service = new KeyRotationBackgroundService(
            scopeFactory,
            Mock.Of<ILogger<KeyRotationBackgroundService>>()
        );

        await service.CheckAndProcessKeysAsync();

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var expired = await db.ApiKeys.FirstAsync(k => k.Owner == "expired");
            var soonExpire = await db.ApiKeys.FirstAsync(k => k.Owner == "soon-expire");
            var healthy = await db.ApiKeys.FirstAsync(k => k.Owner == "healthy");

            Assert.False(expired.IsActive);      // 已過期 → 停用
            Assert.True(soonExpire.IsActive);     // 即將到期 → 仍啟用
            Assert.True(healthy.IsActive);        // 健康 → 仍啟用
        }
    }
}
