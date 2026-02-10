using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Models;

namespace MilkApiManager.Services
{
    public class QuotaService
    {
        private readonly ApiDbContext _context;

        public QuotaService(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckAndIncrementUsageAsync(string keyHash, string endpoint)
        {
            var apiKey = await _context.ApiKeys
                .Include(k => k.Quota)
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

            if (apiKey == null || apiKey.Quota == null)
                return false;

            var quota = apiKey.Quota;
            var now = DateTime.UtcNow;

            // Reset counters if needed
            if (quota.LastReset.Date != now.Date)
            {
                quota.LastReset = now.Date;
                // In a real system, you might reset daily/hourly counters
            }

            // Check limits (simplified, using total requests per day)
            var todayUsage = await _context.UsageRecords
                .Where(u => u.ApiKeyId == apiKey.Id && u.Timestamp.Date == now.Date)
                .SumAsync(u => u.RequestCount);

            if (todayUsage >= quota.RequestsPerDay)
                return false;

            // Record usage
            var record = new UsageRecord
            {
                ApiKeyId = apiKey.Id,
                Timestamp = now,
                Endpoint = endpoint,
                RequestCount = 1
            };

            _context.UsageRecords.Add(record);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Quota> GetQuotaAsync(Guid apiKeyId)
        {
            return await _context.Quotas.FirstOrDefaultAsync(q => q.ApiKeyId == apiKeyId);
        }

        public async Task UpdateQuotaAsync(Quota quota)
        {
            _context.Quotas.Update(quota);
            await _context.SaveChangesAsync();
        }
    }
}