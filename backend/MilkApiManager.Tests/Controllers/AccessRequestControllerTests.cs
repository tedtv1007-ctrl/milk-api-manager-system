using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MilkApiManager.Controllers;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Models.Apisix;
using MilkApiManager.Services;
using Moq;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

public class AccessRequestControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IApisixClient> _mockApisixClient;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<ILogger<AccessRequestController>> _mockLogger;
    private readonly AccessRequestController _controller;

    public AccessRequestControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockApisixClient = new Mock<IApisixClient>();
        _mockNotification = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<AccessRequestController>>();
        _controller = new AccessRequestController(_context, _mockApisixClient.Object, _mockNotification.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetRequests_ReturnsAllOrderedByCreatedAt()
    {
        _context.AccessRequests.AddRange(
            new AccessRequest { Id = 1, ProjectName = "Proj1", ApplicantEmail = "a@b.com", Purpose = "Testing", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new AccessRequest { Id = 2, ProjectName = "Proj2", ApplicantEmail = "c@d.com", Purpose = "Prod", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var result = await _controller.GetRequests();

        var requests = Assert.IsAssignableFrom<IEnumerable<AccessRequest>>(result.Value);
        var list = requests.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("Proj2", list.First().ProjectName); // Most recent first
    }

    [Fact]
    public async Task GetRequests_EmptyDb_ReturnsEmptyList()
    {
        var result = await _controller.GetRequests();

        var requests = Assert.IsAssignableFrom<IEnumerable<AccessRequest>>(result.Value);
        Assert.Empty(requests);
    }

    [Fact]
    public async Task SubmitRequest_SetsStatusToPendingAndSaves()
    {
        _mockNotification.Setup(n => n.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var request = new AccessRequest
        {
            ProjectName = "TestProject",
            ApplicantEmail = "test@example.com",
            RequestedTier = "Silver",
            Purpose = "Integration test"
        };

        var result = await _controller.SubmitRequest(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var saved = Assert.IsType<AccessRequest>(okResult.Value);
        Assert.Equal(RequestStatus.Pending, saved.Status);
        Assert.Single(_context.AccessRequests);
    }

    [Fact]
    public async Task SubmitRequest_SendsNotification()
    {
        _mockNotification.Setup(n => n.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var request = new AccessRequest
        {
            ProjectName = "NotifyProject",
            ApplicantEmail = "test@example.com",
            RequestedTier = "Gold",
            Purpose = "Testing"
        };

        await _controller.SubmitRequest(request);

        _mockNotification.Verify(n => n.AlertAsync("New Access Request", It.Is<string>(s => s.Contains("NotifyProject")), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ApproveRequest_ValidPendingRequest_ApprovesAndProvisions()
    {
        var request = new AccessRequest
        {
            Id = 1,
            ProjectName = "ApproveTest",
            ApplicantEmail = "a@b.com",
            RequestedTier = "Silver",
            Purpose = "Test",
            Status = RequestStatus.Pending
        };
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.CreateConsumerAsync(It.IsAny<string>(), It.IsAny<Consumer>()))
            .Returns(Task.CompletedTask);
        _mockNotification.Setup(n => n.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.ApproveRequest(1, "Looks good");

        Assert.IsType<OkResult>(result);
        var updated = await _context.AccessRequests.FindAsync(1);
        Assert.Equal(RequestStatus.Approved, updated!.Status);
        Assert.Equal("Looks good", updated.AdminComment);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task ApproveRequest_AlreadyApproved_ReturnsBadRequest()
    {
        var request = new AccessRequest
        {
            Id = 1,
            ProjectName = "AlreadyDone",
            ApplicantEmail = "a@b.com",
            RequestedTier = "Free",
            Purpose = "Test",
            Status = RequestStatus.Approved
        };
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _controller.ApproveRequest(1);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task ApproveRequest_NonExistent_ReturnsBadRequest()
    {
        var result = await _controller.ApproveRequest(999);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task ApproveRequest_ApisixFailure_Returns500()
    {
        var request = new AccessRequest
        {
            Id = 1,
            ProjectName = "FailTest",
            ApplicantEmail = "a@b.com",
            RequestedTier = "Gold",
            Purpose = "Test",
            Status = RequestStatus.Pending
        };
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        _mockApisixClient.Setup(c => c.CreateConsumerAsync(It.IsAny<string>(), It.IsAny<Consumer>()))
            .ThrowsAsync(new HttpRequestException("APISIX down"));

        var result = await _controller.ApproveRequest(1);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task RejectRequest_ValidPendingRequest_Rejects()
    {
        var request = new AccessRequest
        {
            Id = 1,
            ProjectName = "RejectTest",
            ApplicantEmail = "a@b.com",
            RequestedTier = "Free",
            Purpose = "Test",
            Status = RequestStatus.Pending
        };
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _controller.RejectRequest(1, "Not approved");

        Assert.IsType<OkResult>(result);
        var updated = await _context.AccessRequests.FindAsync(1);
        Assert.Equal(RequestStatus.Rejected, updated!.Status);
        Assert.Equal("Not approved", updated.AdminComment);
    }

    [Fact]
    public async Task RejectRequest_NonExistent_ReturnsBadRequest()
    {
        var result = await _controller.RejectRequest(999);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task RejectRequest_AlreadyRejected_ReturnsBadRequest()
    {
        var request = new AccessRequest
        {
            Id = 1,
            ProjectName = "AlreadyRejected",
            ApplicantEmail = "a@b.com",
            RequestedTier = "Free",
            Purpose = "Test",
            Status = RequestStatus.Rejected
        };
        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        var result = await _controller.RejectRequest(1);

        Assert.IsType<BadRequestResult>(result);
    }
}
