using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using MilkApiManager.Models.Apisix;

namespace MilkApiManager.Services;

public class ApisixRouteSyncService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApisixRouteSyncService> _logger;

    public ApisixRouteSyncService(IServiceProvider serviceProvider, ILogger<ApisixRouteSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting APISIX Route Synchronization...");
        
        // Use a background task to avoid blocking startup
        _ = Task.Run(async () =>
        {
            try
            {
                // Wait a bit for APISIX to be ready
                await Task.Delay(TimeSpan.FromSeconds(10));
                await SyncRoutesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync routes to APISIX");
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SyncRoutesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var apisixClient = scope.ServiceProvider.GetRequiredService<ApisixClient>();
        
        // Scan Controllers using Reflection
        var controllers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type));

        foreach (var controller in controllers)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            var baseRoute = routeAttr?.Template ?? "";

            var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                // Determine HTTP Method and Template
                var httpMethodAttr = method.GetCustomAttributes().OfType<HttpMethodAttribute>().FirstOrDefault();
                if (httpMethodAttr == null) continue; // Skip non-endpoint methods

                var methodTemplate = httpMethodAttr.Template ?? "";
                var fullUri = CombinePaths(baseRoute, methodTemplate);
                
                // Replace {param} with * or keep as is? APISIX uses * for wildcards usually, or standard URI matching
                // APISIX: /api/users/{id} -> /api/users/* or exact match
                // For simplicity, we sync exact paths or simple wildcards.
                
                // Clean up URI
                fullUri = "/" + fullUri.Trim('/');

                var routeId = $"auto-{controller.Name}-{method.Name}".ToLower();
                var methodsList = httpMethodAttr.HttpMethods.ToList();

                var routeConfig = new Models.Apisix.Route
                {
                    Name = routeId,
                    Uri = fullUri,
                    Uris = new List<string> { fullUri },
                    Methods = methodsList,
                    ServiceId = "insure-tech-backend" // Map to default service
                };

                _logger.LogInformation($"Syncing Route: {routeId} -> {fullUri}");
                // await apisixClient.UpdateRouteAsync(routeId, routeConfig); 
                // Commented out until we refine the logic to avoid overwriting manual configs
            }
        }
    }

    private string CombinePaths(string p1, string p2)
    {
        return Path.Combine(p1, p2).Replace("", "/");
    }
}
