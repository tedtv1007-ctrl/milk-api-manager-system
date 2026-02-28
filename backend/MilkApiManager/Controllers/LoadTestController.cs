using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Auth;
using MilkApiManager.Services;
using Asp.Versioning;

namespace MilkApiManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.OperatorOrAbove)]
public class LoadTestController : ControllerBase
{
    private readonly ILoadTestService _loadTestService;

    public LoadTestController(ILoadTestService loadTestService)
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
