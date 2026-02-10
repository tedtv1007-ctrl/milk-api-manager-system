using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Models;
using MilkApiManager.Services;
using MilkApiManager.Models.Apisix;
using System.Collections.Generic;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeysController : ControllerBase
    {
        private readonly IVaultService _vaultService;
        private readonly ApisixClient _apisixClient;

        public KeysController(IVaultService vaultService, ApisixClient apisixClient)
        {
            _vaultService = vaultService;
            _apisixClient = apisixClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Owner))
            {
                return BadRequest("Invalid request payload");
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

                // 3. 回傳建立結果（不回傳明文於真實環境中）
                return Created(string.Empty, new { Owner = request.Owner, Message = "API key created and synced to APISIX." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{consumerName}/rotate")]
        public async Task<IActionResult> RotateKey(string consumerName)
        {
            try
            {
                var newKey = await _vaultService.RotateApiKeyAsync(consumerName);
                return Ok(new
                {
                    Consumer = consumerName,
                    NewKey = newKey,
                    Message = "API Key has been rotated and synced to APISIX."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
