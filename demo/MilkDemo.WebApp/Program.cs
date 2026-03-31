using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MilkDemo.WebApp;
using MilkDemo.WebApp.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5003";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductApiService, ProductApiService>();
builder.Services.AddScoped<IOrderApiService, OrderApiService>();
builder.Services.AddScoped<IGatewayApiService, GatewayApiService>();
builder.Services.AddScoped<AuthenticationStateProvider, DemoAuthStateProvider>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
