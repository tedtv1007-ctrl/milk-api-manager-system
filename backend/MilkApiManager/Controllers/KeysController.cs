using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MilkApiManager.Models;
using MilkApiManager.Services;
using MilkApiManager.Models.Apisix;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KeysController : ControllerBase
    {
        private readonly IVaultService _vaultService;
        private readonly ApisixClient _apisixClient;
        private readonly ApiDbContext _context;
        private readonly QuotaService _quotaService;

        public KeysController(IVaultService vaultService, ApisixClient apisixClient, ApiDbContext context, QuotaService quotaService)
        {
            _vaultService = vaultService;
            _apisixClient = apisixClient;
            _context = context;
            _quotaService = quotaService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            // 1. 生成高強度 API Key
            var rawKey = $"milk_{Guid.NewGuid().ToString("N")}";
            var keyHash = ComputeSha256Hash(rawKey);

            // 2. 存入 Vault
            await _vaultService.StoreSecretAsync($"secret/apikeys/{request.Owner}", rawKey);

            // 3. 同步至 APISIX Consumer with rate limiting
            var consumer = new Consumer
            {
                Username = request.Owner,
                Plugins = new Dictionary<string, object>
                {
                    { "key-auth", new { key = rawKey } },
                    { "limit-count", new { count = request.RequestsPerMinute, time_window = 60, key = "consumer_name" } } // Example for per minute
                }
            };
            await _apisixClient.CreateConsumerAsync(request.Owner, consumer);

            // 4. 建立 DB 紀錄
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = keyHash,
                Owner = request.Owner,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ValidityDays),
                IsActive = true
            };

            var quota = new Quota
            {
                Id = Guid.NewGuid(),
                ApiKeyId = apiKey.Id,
                RequestsPerMinute = request.RequestsPerMinute,
                RequestsPerHour = request.RequestsPerHour,
                RequestsPerDay = request.RequestsPerDay
            };

            _context.ApiKeys.Add(apiKey);
            _context.Quotas.Add(quota);
            await _context.SaveChangesAsync();

            // 5. 回傳原始 Key 給用戶
            return Ok(new {
                ApiKey = rawKey,
                Message = "Please save this key immediately. It will not be shown again."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetKeys()
        {
            var keys = await _context.ApiKeys
                .Include(k => k.Quota)
                .Select(k => new
                {
                    k.Id,
                    k.Owner,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.IsActive,
                    k.Quota
                })
                .ToListAsync();
            return Ok(keys);
        }

        [HttpPut("{id}/quota")]
        public async Task<IActionResult> UpdateQuota(Guid id, [FromBody] UpdateQuotaRequest request)
        {
            var quota = await _context.Quotas.FirstOrDefaultAsync(q => q.ApiKeyId == id);
            if (quota == null) return NotFound();

            quota.RequestsPerMinute = request.RequestsPerMinute;
            quota.RequestsPerHour = request.RequestsPerHour;
            quota.RequestsPerDay = request.RequestsPerDay;

            await _quotaService.UpdateQuotaAsync(quota);

            // Update APISIX consumer plugin if needed
            // For simplicity, assume manual or separate sync

            return Ok();
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }

    public class UpdateQuotaRequest
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
        public int RequestsPerDay { get; set; }
    }
}