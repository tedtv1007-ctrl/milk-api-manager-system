using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MilkApiManager.Controllers;
using MilkApiManager.Services;
using MilkApiManager.Data;
using MilkApiManager.Models;

namespace MilkApiManager.Tests.Controllers
{
    public class WhitelistControllerTests
    {
        private AppDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private IConfiguration CreateConfig(bool persist)
        {
            var dict = new Dictionary<string, string>
            {
                {"Whitelist:PersistToDatabase", persist ? "true" : "false"}
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public async Task GetWhitelistAsync_ReturnsFromDatabase_WhenPersistEnabled()
        {
            var db = CreateInMemoryDb("GetWhitelistDb");
            db.WhitelistEntries.Add(new WhitelistEntry { RouteId = "route1", IpCidr = "1.2.3.4/32", AddedAt = DateTime.UtcNow });
            db.WhitelistEntries.Add(new WhitelistEntry { RouteId = "route1", IpCidr = "2.2.2.0/24", AddedAt = DateTime.UtcNow.AddMinutes(-5), ExpiresAt = DateTime.UtcNow.AddMinutes(10) });
            await db.SaveChangesAsync();

            var apisixMock = new Mock<ApisixClient>(MockBehavior.Strict, new object[] { null, Mock.Of<ILogger<ApisixClient>>() });
            var logger = Mock.Of<ILogger<WhitelistController>>();
            var config = CreateConfig(true);
            var auditMock = new Mock<AuditLogService>(MockBehavior.Strict, new object[] { Mock.Of<System.Net.Http.HttpClient>(), Mock.Of<IConfiguration>(), Mock.Of<IServiceScopeFactory>() });
            auditMock.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask).Verifiable();

            var controller = new WhitelistController(apisixMock.Object, logger, db, config, auditMock.Object);

            var result = await controller.GetWhitelistForRoute("route1");
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<WhitelistEntry>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task GetWhitelistAsync_FallsBackToApisix_WhenPersistDisabled()
        {
            var db = CreateInMemoryDb("GetWhitelistFallbackDb");

            var apisixMock = new Mock<ApisixClient>(MockBehavior.Strict, new object[] { null, Mock.Of<ILogger<ApisixClient>>() });
            apisixMock.Setup(a => a.GetWhitelistForRouteAsync("routeX")).ReturnsAsync(new List<string>{"9.9.9.9/32", "10.0.0.0/8"});

            var logger = Mock.Of<ILogger<WhitelistController>>();
            var config = CreateConfig(false);
            var auditMock = new Mock<AuditLogService>(MockBehavior.Strict, new object[] { Mock.Of<System.Net.Http.HttpClient>(), Mock.Of<IConfiguration>(), Mock.Of<IServiceScopeFactory>() });
            auditMock.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

            var controller = new WhitelistController(apisixMock.Object, logger, db, config, auditMock.Object);

            var result = await controller.GetWhitelistForRoute("routeX");
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<WhitelistEntry>>(ok.Value);
            Assert.Equal(2, list.Count());
            apisixMock.Verify(a => a.GetWhitelistForRouteAsync("routeX"), Times.Once);
        }

        [Fact]
        public async Task AddEntryAsync_PersistsToDb_And_CallsSync()
        {
            var db = CreateInMemoryDb("AddEntryDb");
            var apisixMock = new Mock<ApisixClient>(MockBehavior.Strict, new object[] { null, Mock.Of<ILogger<ApisixClient>>() });
            // Expect UpdateWhitelistForRouteAsync when Sync called
            apisixMock.Setup(a => a.UpdateWhitelistForRouteAsync("r1", It.IsAny<List<string>>())).Returns(Task.CompletedTask).Verifiable();

            var logger = Mock.Of<ILogger<WhitelistController>>();
            var config = CreateConfig(true);
            var auditMock = new Mock<AuditLogService>(MockBehavior.Strict, new object[] { Mock.Of<System.Net.Http.HttpClient>(), Mock.Of<IConfiguration>(), Mock.Of<IServiceScopeFactory>() });
            auditMock.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask).Verifiable();

            var controller = new WhitelistController(apisixMock.Object, logger, db, config, auditMock.Object);

            var req = new WhitelistUpdateRequest { Action = "add", IpCidr = "7.7.7.7/32", AddedBy = "tester" };

            var result = await controller.AddWhitelistEntry("r1", req);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

            // Verify persisted
            var entries = db.WhitelistEntries.Where(w => w.RouteId == "r1").ToList();
            Assert.Single(entries);
            Assert.Equal("7.7.7.7/32", entries[0].IpCidr);

            // Verify sync called
            apisixMock.Verify();
            auditMock.Verify(a => a.LogAsync(It.Is<AuditLogEntry>(e => e.Action == "Create" && e.Resource == "Whitelist")), Times.Once);
        }

        [Fact]
        public async Task RemoveEntryAsync_DeletesFromDb_And_CallsSync()
        {
            var db = CreateInMemoryDb("RemoveEntryDb");
            db.WhitelistEntries.Add(new WhitelistEntry { RouteId = "r2", IpCidr = "3.3.3.0/24", AddedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var apisixMock = new Mock<ApisixClient>(MockBehavior.Strict, new object[] { null, Mock.Of<ILogger<ApisixClient>>() });
            apisixMock.Setup(a => a.UpdateWhitelistForRouteAsync("r2", It.IsAny<List<string>>())).Returns(Task.CompletedTask).Verifiable();

            var logger = Mock.Of<ILogger<WhitelistController>>();
            var config = CreateConfig(true);
            var auditMock = new Mock<AuditLogService>(MockBehavior.Strict, new object[] { Mock.Of<System.Net.Http.HttpClient>(), Mock.Of<IConfiguration>(), Mock.Of<IServiceScopeFactory>() });
            auditMock.Setup(a => a.LogAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask).Verifiable();

            var controller = new WhitelistController(apisixMock.Object, logger, db, config, auditMock.Object);

            var req = new WhitelistUpdateRequest { Action = "remove", IpCidr = "3.3.3.0/24" };
            var result = await controller.AddWhitelistEntry("r2", req);
            var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

            var entries = db.WhitelistEntries.Where(w => w.RouteId == "r2").ToList();
            Assert.Empty(entries);

            apisixMock.Verify();
            auditMock.Verify(a => a.LogAsync(It.Is<AuditLogEntry>(e => e.Action == "Delete" && e.Resource == "Whitelist")), Times.Once);
        }
    }
}
