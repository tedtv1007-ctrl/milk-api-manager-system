using MilkDemo.Api.Data;
using MilkDemo.Api.Services;
using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace MilkDemo.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly DemoDbContext _context;
    private readonly IProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DemoDbContext(options);
        _service = new ProductService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateProduct_WithValidDto_ReturnsCreatedProduct()
    {
        var dto = new ProductCreateDto
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 29.99m,
            StockQuantity = 100,
            Category = "Electronics"
        };

        var result = await _service.CreateProductAsync(dto);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(29.99m);
        result.StockQuantity.Should().Be(100);
        result.Category.Should().Be("Electronics");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProduct()
    {
        var product = new Product { Name = "Existing", Price = 10m, StockQuantity = 5 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.GetProductByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Existing");
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ReturnsNull()
    {
        var result = await _service.GetProductByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProducts_WithPagination_ReturnsPagedResult()
    {
        for (int i = 1; i <= 25; i++)
        {
            _context.Products.Add(new Product { Name = $"Product {i}", Price = i * 10m, StockQuantity = i });
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetProductsAsync(page: 2, pageSize: 10);

        result.TotalCount.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredResults()
    {
        _context.Products.Add(new Product { Name = "Phone", Price = 500m, Category = "Electronics", StockQuantity = 10 });
        _context.Products.Add(new Product { Name = "Book", Price = 20m, Category = "Books", StockQuantity = 50 });
        _context.Products.Add(new Product { Name = "Laptop", Price = 1000m, Category = "Electronics", StockQuantity = 5 });
        await _context.SaveChangesAsync();

        var result = await _service.GetProductsAsync(category: "Electronics");

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(p => p.Category.Should().Be("Electronics"));
    }

    [Fact]
    public async Task UpdateProduct_ExistingProduct_ReturnsUpdatedProduct()
    {
        var product = new Product { Name = "Old Name", Price = 10m, StockQuantity = 5 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var dto = new ProductUpdateDto
        {
            Name = "New Name",
            Price = 20m,
            StockQuantity = 10,
            Category = "Updated",
            IsActive = true
        };

        var result = await _service.UpdateProductAsync(product.Id, dto);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Price.Should().Be(20m);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProduct_NonExistingProduct_ReturnsNull()
    {
        var dto = new ProductUpdateDto { Name = "X", Price = 1m };
        var result = await _service.UpdateProductAsync(999, dto);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_ReturnsTrue()
    {
        var product = new Product { Name = "ToDelete", Price = 10m, StockQuantity = 1 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteProductAsync(product.Id);
        result.Should().BeTrue();

        var deleted = await _context.Products.FindAsync(product.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProduct_NonExistingProduct_ReturnsFalse()
    {
        var result = await _service.DeleteProductAsync(999);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategories_ReturnsDistinctCategories()
    {
        _context.Products.Add(new Product { Name = "A", Price = 1m, Category = "Cat1", StockQuantity = 1 });
        _context.Products.Add(new Product { Name = "B", Price = 1m, Category = "Cat2", StockQuantity = 1 });
        _context.Products.Add(new Product { Name = "C", Price = 1m, Category = "Cat1", StockQuantity = 1 });
        await _context.SaveChangesAsync();

        var result = await _service.GetCategoriesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain("Cat1");
        result.Should().Contain("Cat2");
    }
}
