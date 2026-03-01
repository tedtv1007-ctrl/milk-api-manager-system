using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using MilkAdminBlazor.Data;
using Microsoft.Extensions.Http.Resilience;

namespace MilkAdminBlazor;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddMudServices(); // UI Component Library
        builder.Services.AddHealthChecks(); // Add HealthChecks service

        // Register HttpClient for ApisixService to talk to MilkApiManager (E-5: with resilience handler)
        builder.Services.AddHttpClient<ApisixService>(client =>
        {
            var backendUrl = builder.Configuration["BackendApiUrl"] ?? "http://localhost:5001/";
            client.BaseAddress = new Uri(backendUrl);

            var apiKey = builder.Configuration["BackendApiKey"] ?? "milk-admin-secret-key-change-me";
            client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
            
            client.Timeout = TimeSpan.FromMinutes(2); // Increase base timeout
        })
        .AddStandardResilienceHandler(options => {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(1);
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.UseRouting();

        app.MapHealthChecks("/health");
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
