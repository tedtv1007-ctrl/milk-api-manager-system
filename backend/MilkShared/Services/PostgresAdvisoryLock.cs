using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MilkApiManager.Services;

/// <summary>
/// PostgreSQL advisory-lock based distributed lock implementation.
/// Uses pg_try_advisory_lock / pg_advisory_unlock to coordinate
/// singleton background workers across multiple replicas.
/// </summary>
public class PostgresAdvisoryLock : IDistributedLock
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresAdvisoryLock> _logger;

    public PostgresAdvisoryLock(IConfiguration configuration, ILogger<PostgresAdvisoryLock> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required for distributed locking.");
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string lockName, CancellationToken cancellationToken = default)
    {
        var lockId = ComputeLockId(lockName);
        var connection = new NpgsqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT pg_try_advisory_lock(@lockId)";
            cmd.Parameters.AddWithValue("lockId", lockId);

            var acquired = (bool)(await cmd.ExecuteScalarAsync(cancellationToken))!;

            if (!acquired)
            {
                await connection.DisposeAsync();
                _logger.LogDebug("Advisory lock '{LockName}' (id={LockId}) is held by another instance.", lockName, lockId);
                return null;
            }

            _logger.LogDebug("Acquired advisory lock '{LockName}' (id={LockId}).", lockName, lockId);
            return new LockHandle(connection, lockId, lockName, _logger);
        }
        catch (Exception ex)
        {
            await connection.DisposeAsync();
            _logger.LogWarning(ex, "Failed to acquire advisory lock '{LockName}'.", lockName);
            return null;
        }
    }

    /// <summary>
    /// Converts a human-readable lock name into a deterministic int64 hash
    /// suitable for pg_advisory_lock.
    /// </summary>
    private static long ComputeLockId(string lockName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockName));
        return BitConverter.ToInt64(hash, 0);
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _lockId;
        private readonly string _lockName;
        private readonly ILogger _logger;
        private bool _disposed;

        public LockHandle(NpgsqlConnection connection, long lockId, string lockName, ILogger logger)
        {
            _connection = connection;
            _lockId = lockId;
            _lockName = lockName;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                await using var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT pg_advisory_unlock(@lockId)";
                cmd.Parameters.AddWithValue("lockId", _lockId);
                await cmd.ExecuteScalarAsync();
                _logger.LogDebug("Released advisory lock '{LockName}' (id={LockId}).", _lockName, _lockId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release advisory lock '{LockName}'.", _lockName);
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
