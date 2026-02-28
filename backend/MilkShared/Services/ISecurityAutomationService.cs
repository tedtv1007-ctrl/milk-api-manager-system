namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for automated security operations (key rotation, IP blocking).
/// </summary>
public interface ISecurityAutomationService
{
    Task CheckAndRotateKeys();
    Task BlockMaliciousIP(string ip, string reason);
}
