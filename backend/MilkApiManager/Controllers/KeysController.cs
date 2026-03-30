using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using Microsoft.EntityFrameworkCore;
using MilkApiManager.Data;
using MilkApiManager.Models;
using MilkApiManager.Services;
using MilkApiManager.Models.Apisix;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
    public class KeysController : ControllerBase
    {
        private readonly IVaultService _vaultService;
        private readonly IApisixClient _apisixClient;
        private readonly AppDbContext _dbContext;

        public KeysController(IVaultService vaultService, IApisixClient apisixClient, AppDbContext dbContext)
        {
            _vaultService = vaultService;
            _apisixClient = apisixClient;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 取得所有 API 金鑰清單 (不含明文金鑰)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetKeys()
        {
            var keys = await _dbContext.ApiKeys
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new
                {
                    k.Id,
                    k.Owner,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.LastRotatedAt,
                    k.IsActive,
                    k.Scopes,
                    k.ContactEmail,
                    IsExpired = k.ExpiresAt < DateTime.UtcNow,
                    DaysUntilExpiry = (int)(k.ExpiresAt - DateTime.UtcNow).TotalDays
                })
                .ToListAsync();

            return Ok(keys);
        }

        /// <summary>
        /// 取得單一 API 金鑰明細
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetKey(Guid id)
        {
            var key = await _dbContext.ApiKeys.FindAsync(id);
            if (key == null)
            {
                return NotFound(new { Error = $"API Key with ID {id} not found." });
            }

            return Ok(new
            {
                key.Id,
                key.Owner,
                key.CreatedAt,
                key.ExpiresAt,
                key.LastRotatedAt,
                key.IsActive,
                key.Scopes,
                key.ContactEmail,
                IsExpired = key.ExpiresAt < DateTime.UtcNow,
                DaysUntilExpiry = (int)(key.ExpiresAt - DateTime.UtcNow).TotalDays
            });
        }

        /// <summary>
        /// 建立新 API 金鑰，並同步至 APISIX Consumer
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Owner))
            {
                return BadRequest(new ApiError("ValidationError", "Invalid request payload"));
            }

            try
            {
                // 1. 產生新金鑰並儲存至 Vault
                var newKey = Guid.NewGuid().ToString("N");
                var vaultPath = $"secret/data/api-keys/{request.Owner}";
                await _vaultService.StoreSecretAsync(vaultPath, newKey);

                // 2. 建立或更新 APISIX Consumer
                var consumer = new MilkApiManager.Models.Apisix.Consumer
                {
                    Username = request.Owner,
                    Plugins = new Dictionary<string, object>
                    {
                        ["key-auth"] = new { key = newKey }
                    }
                };

                await _apisixClient.CreateConsumerAsync(request.Owner, consumer);

                // 3. 持久化金鑰資訊至資料庫
                var validityDays = request.ValidityDays > 0 ? request.ValidityDays : 90;
                var apiKey = new ApiKey
                {
                    Id = Guid.NewGuid(),
                    KeyHash = ComputeHash(newKey),
                    Owner = request.Owner,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
                    IsActive = true,
                    Scopes = request.Scopes ?? "[\"read\"]",
                    ContactEmail = request.ContactEmail ?? ""
                };

                _dbContext.ApiKeys.Add(apiKey);
                await _dbContext.SaveChangesAsync();

                // 4. 回傳建立結果
                return Created(string.Empty, new
                {
                    apiKey.Id,
                    Owner = request.Owner,
                    apiKey.ExpiresAt,
                    apiKey.Scopes,
                    Message = "API key created and synced to APISIX."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 輪替指定 Consumer 的 API 金鑰
        /// </summary>
        [HttpPost("{consumerName}/rotate")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> RotateKey(string consumerName)
        {
            try
            {
                var newKey = await _vaultService.RotateApiKeyAsync(consumerName);

                // 更新資料庫中的金鑰記錄
                var existingKey = await _dbContext.ApiKeys
                    .FirstOrDefaultAsync(k => k.Owner == consumerName && k.IsActive);

                if (existingKey != null)
                {
                    existingKey.KeyHash = ComputeHash(newKey);
                    existingKey.LastRotatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new
                {
                    Consumer = consumerName,
                    RotatedAt = DateTime.UtcNow,
                    Message = "API Key has been rotated and synced to APISIX."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 停用/刪除 API 金鑰
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteKey(Guid id)
        {
            var key = await _dbContext.ApiKeys.FindAsync(id);
            if (key == null)
            {
                return NotFound(new { Error = $"API Key with ID {id} not found." });
            }

            key.IsActive = false;
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// 計算金鑰 SHA256 Hash（僅儲存 Hash，不儲存明文）
        /// </summary>
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
