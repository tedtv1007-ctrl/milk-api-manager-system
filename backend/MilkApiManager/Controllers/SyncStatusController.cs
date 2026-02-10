using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncStatusController : ControllerBase
    {
        private readonly AdGroupSyncService _syncService;

        public SyncStatusController(AdGroupSyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = _syncService.GetStatus(),
                LastSyncTime = _syncService.GetLastSyncTime()
            });
        }
    }
}
