using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;

namespace MilkDemo.Api.Services;

public interface IOrderService
{
    Task<PagedResult<Order>> GetOrdersAsync(int page = 1, int pageSize = 10, OrderStatus? status = null);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(OrderCreateDto dto);
    Task<Order?> UpdateOrderStatusAsync(int id, OrderStatus status);
    Task<bool> CancelOrderAsync(int id);
}
