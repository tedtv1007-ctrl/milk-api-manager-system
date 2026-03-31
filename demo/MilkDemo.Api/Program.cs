using MilkDemo.Api.Data;
using MilkDemo.Api.Services;
using MilkDemo.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<DemoDbContext>(options =>
    options.UseInMemoryDatabase("MilkDemoDB"));

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// HttpClient for API Manager communication
builder.Services.AddHttpClient("MilkApiManager", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "milk-demo-jwt-secret-key-change-in-production-32chars!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MilkDemo",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MilkDemoClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow Blazor WASM frontend (local dev + Docker)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5002",
                "https://localhost:7002",
                "http://localhost:5010",
                "http://milk-demo-webapp"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
    SeedData(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Auth endpoint for demo (simplified JWT token issuer)
app.MapPost("/api/auth/login", (MilkDemo.Shared.DTOs.LoginRequestDto request) =>
{
    // Demo mode: accept predefined users
    var users = new Dictionary<string, (string password, string displayName, string[] roles)>
    {
        ["admin"] = ("admin", "Admin User", new[] { "Admin", "Operator", "Viewer" }),
        ["operator"] = ("operator", "Operator User", new[] { "Operator", "Viewer" }),
        ["viewer"] = ("viewer", "Viewer User", new[] { "Viewer" }),
        ["demo"] = ("demo", "Demo User", new[] { "Viewer" })
    };

    if (!users.TryGetValue(request.Username, out var user) || user.password != request.Password)
    {
        return Results.Unauthorized();
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, request.Username),
        new("display_name", user.displayName)
    };
    foreach (var role in user.roles)
        claims.Add(new(System.Security.Claims.ClaimTypes.Role, role));

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"] ?? "MilkDemo",
        audience: builder.Configuration["Jwt:Audience"] ?? "MilkDemoClients",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new MilkDemo.Shared.DTOs.LoginResponseDto
    {
        Token = tokenString,
        ExpiresAt = token.ValidTo,
        DisplayName = user.displayName,
        Roles = user.roles.ToList()
    });
});

app.Run();

void SeedData(DemoDbContext db)
{
    if (db.Products.Any()) return;

    db.Products.AddRange(
        new Product { Name = "Premium Milk", Description = "Fresh organic whole milk 1L", Price = 89.00m, StockQuantity = 500, Category = "Dairy" },
        new Product { Name = "Low-Fat Yogurt", Description = "Greek style low-fat yogurt 500g", Price = 65.00m, StockQuantity = 300, Category = "Dairy" },
        new Product { Name = "Cheddar Cheese", Description = "Aged cheddar cheese block 200g", Price = 120.00m, StockQuantity = 150, Category = "Dairy" },
        new Product { Name = "Butter Cookies", Description = "Imported butter cookies tin 300g", Price = 199.00m, StockQuantity = 200, Category = "Snacks" },
        new Product { Name = "Green Tea", Description = "Japanese Sencha green tea 100 bags", Price = 250.00m, StockQuantity = 100, Category = "Beverages" },
        new Product { Name = "Coffee Beans", Description = "Colombian medium roast 500g", Price = 380.00m, StockQuantity = 80, Category = "Beverages" },
        new Product { Name = "Organic Honey", Description = "Raw wildflower honey 500ml", Price = 320.00m, StockQuantity = 60, Category = "Pantry" },
        new Product { Name = "Dark Chocolate", Description = "72% cacao dark chocolate bar 100g", Price = 95.00m, StockQuantity = 250, Category = "Snacks" },
        new Product { Name = "Oat Milk", Description = "Barista edition oat milk 1L", Price = 79.00m, StockQuantity = 400, Category = "Dairy" },
        new Product { Name = "Sparkling Water", Description = "Natural mineral sparkling water 500ml", Price = 35.00m, StockQuantity = 1000, Category = "Beverages" }
    );
    db.SaveChanges();

    // Seed some sample orders
    var products = db.Products.ToList();
    db.Orders.AddRange(
        new Order
        {
            CustomerName = "Alice Wang",
            CustomerEmail = "alice@example.com",
            CustomerPhone = "0912345678",
            Status = OrderStatus.Confirmed,
            TotalAmount = 254.00m,
            Items = new List<OrderItem>
            {
                new() { ProductId = products[0].Id, ProductName = products[0].Name, Quantity = 2, UnitPrice = products[0].Price },
                new() { ProductId = products[2].Id, ProductName = products[2].Name, Quantity = 1, UnitPrice = products[2].Price }
            }
        },
        new Order
        {
            CustomerName = "Bob Chen",
            CustomerEmail = "bob@example.com",
            Status = OrderStatus.Pending,
            TotalAmount = 630.00m,
            Items = new List<OrderItem>
            {
                new() { ProductId = products[4].Id, ProductName = products[4].Name, Quantity = 1, UnitPrice = products[4].Price },
                new() { ProductId = products[5].Id, ProductName = products[5].Name, Quantity = 1, UnitPrice = products[5].Price }
            }
        }
    );
    db.SaveChanges();
}

// Make Program class accessible for integration tests
public partial class Program { }
