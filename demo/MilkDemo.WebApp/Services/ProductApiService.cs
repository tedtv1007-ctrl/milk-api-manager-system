using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;
using System.Net.Http.Json;

namespace MilkDemo.WebApp.Services;

public interface IProductApiService
{
    Task<PagedResult<Product>?> GetProductsAsync(int page = 1, int pageSize = 10, string? category = null);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product?> CreateProductAsync(ProductCreateDto dto);
    Task<Product?> UpdateProductAsync(int id, ProductUpdateDto dto);
    Task<bool> DeleteProductAsync(int id);
    Task<List<string>?> GetCategoriesAsync();
}

public class ProductApiService : IProductApiService
{
    private readonly HttpClient _httpClient;

    public ProductApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<Product>?> GetProductsAsync(int page = 1, int pageSize = 10, string? category = null)
    {
        var url = $"api/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(category))
            url += $"&category={Uri.EscapeDataString(category)}";
        return await _httpClient.GetFromJsonAsync<PagedResult<Product>>(url);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Product>($"api/products/{id}");
    }

    public async Task<Product?> CreateProductAsync(ProductCreateDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Product>();
        return null;
    }

    public async Task<Product?> UpdateProductAsync(int id, ProductUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Product>();
        return null;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/products/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<string>?> GetCategoriesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<string>>("api/products/categories");
    }
}
