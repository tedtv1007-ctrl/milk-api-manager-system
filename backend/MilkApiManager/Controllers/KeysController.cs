using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Models;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeysController : ControllerBase
    {
        private readonly IVaultService _vaultService;

        public KeysController(IVaultService vaultService)
        {
            _vaultService = vaultService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            // 1. 生成高強度 API Key
            var rawKey = $"milk_{Guid.NewGuid().ToString("N")}";
            
            // 2. 存入 Vault
            await _vaultService.StoreSecretAsync($"secret/apikeys/{request.Owner}", rawKey);

            // 3. 建立 DB 紀錄 (只存 Hash 或是 Metadata)
            var record = new ApiKey
            {
                Id = Guid.NewGuid(),
                Owner = request.Owner,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ValidityDays),
                IsActive = true
            };

            // 4. 回傳原始 Key 給用戶 (這是唯一一次看到它的機會)
            return Ok(new { 
                ApiKey = rawKey, 
                Message = "Please save this key immediately. It will not be shown again." 
            });
        }
    }
}
