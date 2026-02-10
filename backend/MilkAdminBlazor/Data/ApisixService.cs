using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MilkAdminBlazor.Data
{
    public class ApiRoute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string RiskLevel { get; set; } // L1, L2, L3
        public string Owner { get; set; }
    }

    public class Consumer
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string Scopes { get; set; }
    }

    public class ApisixService
    {
        public Task<List<ApiRoute>> GetRoutesAsync()
        {
            // Mock Data for now
            return Task.FromResult(new List<ApiRoute>
            {
                new ApiRoute { Id = "1", Name = "User Profile", Uri = "/api/user/*", RiskLevel = "L3", Owner = "Customer Team" },
                new ApiRoute { Id = "2", Name = "Product List", Uri = "/api/products", RiskLevel = "L1", Owner = "Sales Team" },
                new ApiRoute { Id = "3", Name = "Payment Gateway", Uri = "/api/payment", RiskLevel = "L3", Owner = "Finance Team" },
                new ApiRoute { Id = "4", Name = "Branch Locations", Uri = "/api/locations", RiskLevel = "L1", Owner = "Ops Team" }
            });
        }

        public Task<List<Consumer>> GetConsumersAsync()
        {
            // Mock Data for now
            return Task.FromResult(new List<Consumer>
            {
                new Consumer { Username = "dev-app-01", Role = "Developer", Scopes = "read, write" },
                new Consumer { Username = "partner-svc", Role = "Partner", Scopes = "read" }
            });
        }
    }
}
