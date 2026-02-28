using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ViewerOrAbove)]
    public class SyncStatusController : ControllerBase
    {
        private readonly BlacklistConsistencyService _blacklistConsistencyService;

        public SyncStatusController(BlacklistConsistencyService blacklistConsistencyService)
        {
            _blacklistConsistencyService = blacklistConsistencyService;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = "Service migrated to MilkWorker background process.",
                LastSyncTime = DateTime.UtcNow
            });
        }

        [HttpGet("blacklist-drift")]
        [Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
        public async Task<IActionResult> GetBlacklistDrift(CancellationToken cancellationToken)
        {
            var report = await _blacklistConsistencyService.GetBlacklistDriftReportAsync(cancellationToken);
            return Ok(report);
        }

        [HttpPost("reconcile-blacklist")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> ReconcileBlacklist(CancellationToken cancellationToken)
        {
            var report = await _blacklistConsistencyService.ReconcileDatabaseToGatewayAsync(cancellationToken);
            return Ok(new
            {
                Message = "Blacklist reconciliation executed.",
                Report = report
            });
        }
    }
}
