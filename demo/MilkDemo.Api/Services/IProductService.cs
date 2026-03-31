using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;

namespace MilkDemo.Api.Services;

public interface IProductService
{
    Task<PagedResult<Product>> GetProductsAsync(int page = 1, int pageSize = 10, string? category = null);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(ProductCreateDto dto);
    Task<Product?> UpdateProductAsync(int id, ProductUpdateDto dto);
    Task<bool> DeleteProductAsync(int id);
    Task<List<string>> GetCategoriesAsync();
}
