using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

/// <summary>
/// TDD tests for TestExecutionController — validates scenario CRUD and test execution.
/// </summary>
public class TestExecutionControllerTests
{
    private readonly AppDbContext _db;
    private readonly TestExecutionController _controller;

    public TestExecutionControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestExecTest_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var httpClient = new HttpClient();
        _controller = new TestExecutionController(_db, httpClient, Mock.Of<ILogger<TestExecutionController>>());
    }

    [Fact]
    public async Task GetScenarios_Empty_ReturnsEmptyList()
    {
        var result = await _controller.GetScenarios(999);

        var okResult = Assert.IsType<ActionResult<IEnumerable<ApiTestScenario>>>(result);
        var list = Assert.IsAssignableFrom<List<ApiTestScenario>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetScenarios_WithData_ReturnsFilteredByServiceId()
    {
        // Seed test data
        var service = new ApiServiceMetadata { Name = "Test Service", BasePath = "/api" };
        _db.ApiServices.Add(service);
        await _db.SaveChangesAsync();

        _db.ApiTestScenarios.Add(new ApiTestScenario
        {
            ServiceId = service.Id,
            Name = "Health Check",
            Endpoint = "/health",
            HttpMethod = "GET",
            ExpectedStatusCode = 200
        });
        _db.ApiTestScenarios.Add(new ApiTestScenario
        {
            ServiceId = service.Id,
            Name = "Get Routes",
            Endpoint = "/routes",
            HttpMethod = "GET",
            ExpectedStatusCode = 200
        });
        _db.ApiTestScenarios.Add(new ApiTestScenario
        {
            ServiceId = 9999, // Different service
            Name = "Other",
            Endpoint = "/other",
            HttpMethod = "GET",
            ExpectedStatusCode = 200
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetScenarios(service.Id);

        var okResult = Assert.IsType<ActionResult<IEnumerable<ApiTestScenario>>>(result);
        var list = Assert.IsAssignableFrom<List<ApiTestScenario>>(okResult.Value);
        Assert.Equal(2, list.Count);
        Assert.All(list, s => Assert.Equal(service.Id, s.ServiceId));
    }

    [Fact]
    public async Task CreateScenario_ValidInput_ReturnsOkAndPersists()
    {
        var scenario = new ApiTestScenario
        {
            ServiceId = 1,
            Name = "New Scenario",
            Endpoint = "/new",
            HttpMethod = "POST",
            ExpectedStatusCode = 201
        };

        var result = await _controller.CreateScenario(scenario);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var saved = Assert.IsType<ApiTestScenario>(okResult.Value);
        Assert.Equal("New Scenario", saved.Name);
        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task RunTest_NonExistentScenario_ReturnsNotFound()
    {
        var result = await _controller.RunTest(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RunTest_ScenarioWithMissingService_ReturnsBadRequest()
    {
        var scenario = new ApiTestScenario
        {
            ServiceId = 88888, // Non-existent service
            Name = "Orphan Test",
            Endpoint = "/test",
            HttpMethod = "GET",
            ExpectedStatusCode = 200
        };
        _db.ApiTestScenarios.Add(scenario);
        await _db.SaveChangesAsync();

        var result = await _controller.RunTest(scenario.Id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Service metadata not found", badRequest.Value?.ToString());
    }
}
