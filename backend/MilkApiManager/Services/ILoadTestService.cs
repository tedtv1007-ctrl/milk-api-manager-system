namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for load test execution via k6.
/// </summary>
public interface ILoadTestService
{
    Task<string> RunTestAsync(string targetUrl, int vus, int durationSeconds);
}
