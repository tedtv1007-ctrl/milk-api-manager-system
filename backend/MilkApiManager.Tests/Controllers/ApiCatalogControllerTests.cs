using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

/// <summary>
/// TDD tests for ApiCatalogController — validates API catalog CRUD with EF InMemory.
/// </summary>
public class ApiCatalogControllerTests
{
    private readonly AppDbContext _db;
    private readonly ApiCatalogController _controller;

    public ApiCatalogControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ApiCatalogTest_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _controller = new ApiCatalogController(_db);
    }

    [Fact]
    public async Task GetCatalog_Empty_ReturnsEmptyList()
    {
        var result = await _controller.GetCatalog();

        var okResult = Assert.IsType<ActionResult<IEnumerable<ApiServiceMetadata>>>(result);
        var list = Assert.IsAssignableFrom<List<ApiServiceMetadata>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCatalog_WithData_ReturnsSortedByName()
    {
        _db.ApiServices.Add(new ApiServiceMetadata { Name = "Zebra Service", Description = "Z" });
        _db.ApiServices.Add(new ApiServiceMetadata { Name = "Alpha Service", Description = "A" });
        await _db.SaveChangesAsync();

        var result = await _controller.GetCatalog();

        var okResult = Assert.IsType<ActionResult<IEnumerable<ApiServiceMetadata>>>(result);
        var list = Assert.IsAssignableFrom<List<ApiServiceMetadata>>(okResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("Alpha Service", list[0].Name);
        Assert.Equal("Zebra Service", list[1].Name);
    }

    [Fact]
    public async Task RegisterService_NewService_AddsToDb()
    {
        var metadata = new ApiServiceMetadata
        {
            Name = "New API",
            Description = "Brand new service",
            BasePath = "/api/new",
            OpenApiUrl = "http://localhost/swagger.json",
            OwnerTeam = "Team A"
        };

        var result = await _controller.RegisterService(metadata);

        Assert.IsType<OkResult>(result);
        var saved = await _db.ApiServices.FirstOrDefaultAsync(s => s.Name == "New API");
        Assert.NotNull(saved);
        Assert.Equal("/api/new", saved.BasePath);
    }

    [Fact]
    public async Task RegisterService_ExistingService_UpdatesFields()
    {
        _db.ApiServices.Add(new ApiServiceMetadata
        {
            Name = "Existing API",
            Description = "Old desc",
            BasePath = "/old",
            OwnerTeam = "Team B"
        });
        await _db.SaveChangesAsync();

        var updated = new ApiServiceMetadata
        {
            Name = "Existing API",
            Description = "Updated desc",
            BasePath = "/new",
            OpenApiUrl = "http://updated/swagger.json"
        };

        var result = await _controller.RegisterService(updated);

        Assert.IsType<OkResult>(result);
        var saved = await _db.ApiServices.FirstOrDefaultAsync(s => s.Name == "Existing API");
        Assert.NotNull(saved);
        Assert.Equal("Updated desc", saved.Description);
        Assert.Equal("/new", saved.BasePath);
        Assert.Equal("http://updated/swagger.json", saved.OpenApiUrl);
        Assert.True(saved.LastSyncedAt > DateTime.UtcNow.AddMinutes(-1), "LastSyncedAt should be recently updated");
    }
}
