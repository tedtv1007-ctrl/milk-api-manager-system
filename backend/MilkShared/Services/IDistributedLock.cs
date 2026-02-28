namespace MilkApiManager.Services;

/// <summary>
/// Abstraction for distributed locking, preventing duplicate execution
/// when multiple service instances are running.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire a named lock. Returns an IAsyncDisposable handle
    /// on success, or null if the lock is already held by another instance.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string lockName, CancellationToken cancellationToken = default);
}
