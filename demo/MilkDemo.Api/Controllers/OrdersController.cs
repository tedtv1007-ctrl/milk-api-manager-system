using MilkDemo.Api.Services;
using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilkDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] OrderStatus? status = null)
    {
        var result = await _orderService.GetOrdersAsync(page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(new ApiErrorDto { Code = "NotFound", Message = $"Order {id} not found." });
        return Ok(order);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorDto { Code = "BusinessError", Message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto dto)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
        if (order == null)
            return NotFound(new ApiErrorDto { Code = "NotFound", Message = $"Order {id} not found." });
        return Ok(order);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var result = await _orderService.CancelOrderAsync(id);
        if (!result)
            return BadRequest(new ApiErrorDto { Code = "BusinessError", Message = "Order cannot be cancelled." });
        return Ok(new { Message = "Order cancelled successfully." });
    }
}

public class OrderStatusUpdateDto
{
    public OrderStatus Status { get; set; }
}
