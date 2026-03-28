using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MilkApiManager.Controllers;
using MilkApiManager.Models;
using MilkApiManager.Services;
using Xunit;

namespace MilkApiManager.Tests.Controllers;

/// <summary>
/// TDD tests for ConsumerGroupController — validates CRUD operations against APISIX.
/// </summary>
public class ConsumerGroupControllerTests
{
    private readonly Mock<IApisixClient> _mockApisix;
    private readonly ConsumerGroupController _controller;

    public ConsumerGroupControllerTests()
    {
        _mockApisix = new Mock<IApisixClient>();
        _controller = new ConsumerGroupController(_mockApisix.Object, Mock.Of<ILogger<ConsumerGroupController>>());
    }

    [Fact]
    public async Task GetGroups_EmptyList_ReturnsOkWithEmptyArray()
    {
        _mockApisix.Setup(c => c.GetConsumerGroupsAsync())
            .ReturnsAsync("{\"list\":[]}");

        var result = await _controller.GetGroups();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var groups = Assert.IsAssignableFrom<List<MilkApiManager.Models.Apisix.ConsumerGroup>>(okResult.Value);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task GetGroups_WithData_ReturnsOkWithParsedGroups()
    {
        var json = "{\"list\":[{\"value\":{\"id\":\"grp-1\",\"plugins\":{}}},{\"value\":{\"id\":\"grp-2\",\"plugins\":{}}}]}";
        _mockApisix.Setup(c => c.GetConsumerGroupsAsync()).ReturnsAsync(json);

        var result = await _controller.GetGroups();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var groups = Assert.IsAssignableFrom<List<MilkApiManager.Models.Apisix.ConsumerGroup>>(okResult.Value);
        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public async Task GetGroups_ApisixError_Returns500()
    {
        _mockApisix.Setup(c => c.GetConsumerGroupsAsync()).ThrowsAsync(new Exception("APISIX unreachable"));

        var result = await _controller.GetGroups();

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateGroup_ValidData_SetsIdAndReturnsOk()
    {
        var groupData = new MilkApiManager.Models.Apisix.ConsumerGroup { Id = "temp" };

        var result = await _controller.UpdateGroup("grp-test", groupData);

        Assert.IsType<OkResult>(result);
        Assert.Equal("grp-test", groupData.Id);
        _mockApisix.Verify(c => c.CreateConsumerGroupAsync("grp-test", groupData), Times.Once);
    }

    [Fact]
    public async Task UpdateGroup_NullData_ReturnsBadRequest()
    {
        var result = await _controller.UpdateGroup("grp-null", null!);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_Existing_ReturnsNoContent()
    {
        var result = await _controller.DeleteGroup("grp-del");

        Assert.IsType<NoContentResult>(result);
        _mockApisix.Verify(c => c.DeleteConsumerGroupAsync("grp-del"), Times.Once);
    }

    [Fact]
    public async Task DeleteGroup_ApisixError_Returns500()
    {
        _mockApisix.Setup(c => c.DeleteConsumerGroupAsync("bad-id"))
            .ThrowsAsync(new Exception("Delete failed"));

        var result = await _controller.DeleteGroup("bad-id");

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
