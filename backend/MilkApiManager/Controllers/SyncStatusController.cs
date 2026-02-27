using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncStatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = "Service migrated to MilkWorker background process.",
                LastSyncTime = DateTime.UtcNow
            });
        }
    }
}
