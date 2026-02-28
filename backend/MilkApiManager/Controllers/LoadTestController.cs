using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;

namespace MilkApiManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
public class LoadTestController : ControllerBase
{
    private readonly LoadTestService _loadTestService;

    public LoadTestController(LoadTestService loadTestService)
    {
        _loadTestService = loadTestService;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunTest([FromQuery] string url, [FromQuery] int vus = 10, [FromQuery] int duration = 30)
    {
        var result = await _loadTestService.RunTestAsync(url, vus, duration);
        return Ok(new { report = result });
    }
}
