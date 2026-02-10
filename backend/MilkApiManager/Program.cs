using MilkApiManager.Services;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ?? "Host=postgres;Database=milkapi;Username=milkapi;Password=milkapi"));

// Add OIDC authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = Environment.GetEnvironmentVariable("KEYCLOAK_AUTHORITY") ?? "http://keycloak:8080/realms/milk-api-manager";
    options.ClientId = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "milk-api-manager-client";
    options.ClientSecret = Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET") ?? "client-secret";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

// Register services
builder.Services.AddHttpClient<ApisixClient>();
builder.Services.AddHttpClient<KeycloakService>();
builder.Services.AddSingleton<IVaultService, VaultService>();
builder.Services.AddScoped<QuotaService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();