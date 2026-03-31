using MilkDemo.Api.Data;
using MilkDemo.Api.Services;
using MilkDemo.Shared.DTOs;
using MilkDemo.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace MilkDemo.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly DemoDbContext _context;
    private readonly IOrderService _service;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DemoDbContext(options);
        _service = new OrderService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsOrderWithTotals()
    {
        _context.Products.Add(new Product { Id = 1, Name = "Widget", Price = 25.00m, StockQuantity = 100 });
        _context.Products.Add(new Product { Id = 2, Name = "Gadget", Price = 50.00m, StockQuantity = 50 });
        await _context.SaveChangesAsync();

        var dto = new OrderCreateDto
        {
            CustomerName = "John Doe",
            CustomerEmail = "john@example.com",
            CustomerPhone = "0912345678",
            Items = new List<OrderItemCreateDto>
            {
                new() { ProductId = 1, Quantity = 2 },
                new() { ProductId = 2, Quantity = 1 }
            }
        };

        var result = await _service.CreateOrderAsync(dto);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CustomerName.Should().Be("John Doe");
        result.CustomerEmail.Should().Be("john@example.com");
        result.TotalAmount.Should().Be(100.00m); // 2*25 + 1*50
        result.Status.Should().Be(OrderStatus.Pending);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockQuantity()
    {
        _context.Products.Add(new Product { Id = 1, Name = "Widget", Price = 25.00m, StockQuantity = 100 });
        await _context.SaveChangesAsync();

        var dto = new OrderCreateDto
        {
            CustomerName = "Jane",
            CustomerEmail = "jane@example.com",
            Items = new List<OrderItemCreateDto>
            {
                new() { ProductId = 1, Quantity = 3 }
            }
        };

        await _service.CreateOrderAsync(dto);

        var product = await _context.Products.FindAsync(1);
        product!.StockQuantity.Should().Be(97);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ThrowsException()
    {
        _context.Products.Add(new Product { Id = 1, Name = "Widget", Price = 25.00m, StockQuantity = 1 });
        await _context.SaveChangesAsync();

        var dto = new OrderCreateDto
        {
            CustomerName = "Jane",
            CustomerEmail = "jane@example.com",
            Items = new List<OrderItemCreateDto>
            {
                new() { ProductId = 1, Quantity = 5 }
            }
        };

        var act = () => _service.CreateOrderAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*insufficient stock*");
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ReturnsFilteredResults()
    {
        _context.Orders.Add(new Order { CustomerName = "A", CustomerEmail = "a@x.com", Status = OrderStatus.Pending, TotalAmount = 10 });
        _context.Orders.Add(new Order { CustomerName = "B", CustomerEmail = "b@x.com", Status = OrderStatus.Confirmed, TotalAmount = 20 });
        _context.Orders.Add(new Order { CustomerName = "C", CustomerEmail = "c@x.com", Status = OrderStatus.Pending, TotalAmount = 30 });
        await _context.SaveChangesAsync();

        var result = await _service.GetOrdersAsync(status: OrderStatus.Pending);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(o => o.Status.Should().Be(OrderStatus.Pending));
    }

    [Fact]
    public async Task UpdateOrderStatus_ValidTransition_ReturnsUpdatedOrder()
    {
        _context.Orders.Add(new Order { Id = 1, CustomerName = "A", CustomerEmail = "a@x.com", Status = OrderStatus.Pending, TotalAmount = 10 });
        await _context.SaveChangesAsync();

        var result = await _service.UpdateOrderStatusAsync(1, OrderStatus.Confirmed);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Confirmed);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrder_PendingOrder_ReturnsTrueAndRestoresStock()
    {
        _context.Products.Add(new Product { Id = 1, Name = "Widget", Price = 25.00m, StockQuantity = 97 });
        var order = new Order
        {
            CustomerName = "A",
            CustomerEmail = "a@x.com",
            Status = OrderStatus.Pending,
            TotalAmount = 75,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Widget", Quantity = 3, UnitPrice = 25.00m }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _service.CancelOrderAsync(order.Id);

        result.Should().BeTrue();
        var cancelled = await _context.Orders.FindAsync(order.Id);
        cancelled!.Status.Should().Be(OrderStatus.Cancelled);

        var product = await _context.Products.FindAsync(1);
        product!.StockQuantity.Should().Be(100); // restored
    }

    [Fact]
    public async Task CancelOrder_ShippedOrder_ReturnsFalse()
    {
        _context.Orders.Add(new Order { Id = 1, CustomerName = "A", CustomerEmail = "a@x.com", Status = OrderStatus.Shipped, TotalAmount = 10 });
        await _context.SaveChangesAsync();

        var result = await _service.CancelOrderAsync(1);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderById_ExistingOrder_ReturnsOrderWithItems()
    {
        var order = new Order
        {
            CustomerName = "A",
            CustomerEmail = "a@x.com",
            Status = OrderStatus.Pending,
            TotalAmount = 50,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "P1", Quantity = 2, UnitPrice = 25m }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _service.GetOrderByIdAsync(order.Id);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }
}
