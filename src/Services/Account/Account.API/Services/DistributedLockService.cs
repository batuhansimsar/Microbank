using StackExchange.Redis;

namespace Account.API.Services;

/// <summary>
/// Simple distributed lock implementation using Redis SETNX
/// </summary>
public interface IDistributedLockService
{
    Task<IAsyncDisposable?> AcquireLockAsync(string resource, TimeSpan expiry);
}

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> AcquireLockAsync(string resource, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{resource}";
        var lockValue = Guid.NewGuid().ToString();
        
        // Try to acquire lock using SET NX (set if not exists)
        var acquired = await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
        
        if (acquired)
        {
            _logger.LogDebug("Lock acquired: {LockKey}", lockKey);
            return new RedisLock(db, lockKey, lockValue, _logger);
        }
        
        _logger.LogWarning("Failed to acquire lock: {LockKey}", lockKey);
        return null;
    }

    private class RedisLock : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _value;
        private readonly ILogger _logger;
        private bool _disposed;

        public RedisLock(IDatabase db, string key, string value, ILogger logger)
        {
            _db = db;
            _key = key;
            _value = value;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            
            try
            {
                // Only delete if we still own the lock (value matches)
                var script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";
                
                await _db.ScriptEvaluateAsync(script, new RedisKey[] { _key }, new RedisValue[] { _value });
                _logger.LogDebug("Lock released: {LockKey}", _key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing lock: {LockKey}", _key);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
