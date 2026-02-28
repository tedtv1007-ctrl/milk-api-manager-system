using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_DurableShippingEnabled_PersistsAuditAndOutbox()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuditLog:EnableDatabaseWrite"] = "true",
                ["AuditLog:EnableLogstashShipping"] = "true",
                ["AuditLog:UseDurableShipping"] = "true"
            })
            .Build();

        Assert.True(config.GetValue<bool>("AuditLog:EnableDatabaseWrite"));
        Assert.True(config.GetValue<bool>("AuditLog:EnableLogstashShipping"));
        Assert.True(config.GetValue<bool>("AuditLog:UseDurableShipping"));

        var scopedProvider = new Mock<IServiceProvider>();
        scopedProvider
            .Setup(p => p.GetService(typeof(AppDbContext)))
            .Returns(dbContext);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(scopedProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var auditLogService = new AuditLogService(
            new HttpClient(),
            config,
            scopeFactory.Object,
            Mock.Of<ILogger<AuditLogService>>());

        await auditLogService.LogAsync(new AuditLogEntry
        {
            User = "operator",
            Action = "Update",
            Resource = "Route",
            StatusCode = 200,
            Details = new { id = "r1" }
        });

        var logs = await dbContext.AuditLogs.ToListAsync();
        var outbox = await dbContext.SyncOutboxEntries
            .Where(e => e.EventType == SyncOutboxEventType.AuditLogShip)
            .ToListAsync();

        Assert.Single(logs);
        Assert.Single(outbox);
        Assert.Equal(SyncOutboxStatus.Pending, outbox[0].Status);

        var payload = JsonSerializer.Deserialize<AuditLogShipPayload>(outbox[0].PayloadJson);
        Assert.NotNull(payload);
        Assert.Equal("operator", payload.User);
        Assert.Equal("Update", payload.Action);
        Assert.Equal("Route", payload.Resource);
    }
}
