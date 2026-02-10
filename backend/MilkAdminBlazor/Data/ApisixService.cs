using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MilkAdminBlazor.Models;

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

    public class ApisixService
    {
        private List<Consumer> _consumers = new List<Consumer>
        {
            new Consumer { Username = "milk-mobile-app", CustomId = "C001", Key = "key-12345", Scopes = new List<string> { "read:profile", "write:orders" } },
            new Consumer { Username = "partner-analytics", CustomId = "C002", Key = "key-67890", Scopes = new List<string> { "read:stats" } },
            new Consumer { Username = "internal-ops", CustomId = "C003", Key = "key-admin", Scopes = new List<string> { "admin", "read:all" } }
        };

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
            return Task.FromResult(_consumers);
        }

        public Task UpdateConsumerAsync(Consumer updatedConsumer)
        {
            var index = _consumers.FindIndex(c => c.Username == updatedConsumer.Username);
            if (index != -1)
            {
                _consumers[index] = updatedConsumer;
            }
            return Task.CompletedTask;
        }
    }
}
