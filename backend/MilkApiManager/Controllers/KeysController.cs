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
            // ... (existing code)
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
