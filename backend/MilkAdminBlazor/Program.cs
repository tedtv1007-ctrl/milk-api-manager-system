using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using MilkAdminBlazor.Data;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices(); // UI Component Library

// Register DbContext (shared with MilkApiManager)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=../MilkApiManager/audit.db"));

// Register AuditLogService (needs HttpClient and ScopeFactory)
builder.Services.AddHttpClient<AuditLogService>();

// Register HttpClient for ApisixService to talk to MilkApiManager
builder.Services.AddHttpClient<ApisixService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5001/"); // MilkApiManager runs on 5001
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
