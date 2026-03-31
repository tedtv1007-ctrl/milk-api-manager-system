using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;
using System.Net.Http.Json;

namespace MilkDemo.WebApp.Services;

public interface IOrderApiService
{
    Task<PagedResult<Order>?> GetOrdersAsync(int page = 1, int pageSize = 10, OrderStatus? status = null);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order?> CreateOrderAsync(OrderCreateDto dto);
    Task<bool> CancelOrderAsync(int id);
}

public class OrderApiService : IOrderApiService
{
    private readonly HttpClient _httpClient;

    public OrderApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<Order>?> GetOrdersAsync(int page = 1, int pageSize = 10, OrderStatus? status = null)
    {
        var url = $"api/orders?page={page}&pageSize={pageSize}";
        if (status.HasValue)
            url += $"&status={status.Value}";
        return await _httpClient.GetFromJsonAsync<PagedResult<Order>>(url);
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Order>($"api/orders/{id}");
    }

    public async Task<Order?> CreateOrderAsync(OrderCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/orders", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Order>();
        return null;
    }

    public async Task<bool> CancelOrderAsync(int id)
    {
        var response = await _httpClient.PostAsync($"api/orders/{id}/cancel", null);
        return response.IsSuccessStatusCode;
    }
}
