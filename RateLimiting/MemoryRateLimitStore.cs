using Microsoft.Extensions.Caching.Memory;

namespace Auth.RateLimiting;

/// <summary>
/// In-memory implementation of rate limit store
/// </summary>
public class MemoryRateLimitStore : IRateLimitStore
{
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public MemoryRateLimitStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<long> IncrementAsync(string key, TimeSpan expiry)
    {
        await _semaphore.WaitAsync();
        try
        {
            var count = _cache.Get<long?>(key) ?? 0;
            count++;
            _cache.Set(key, count, expiry);
            return count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<long> GetCountAsync(string key)
    {
        var count = _cache.Get<long?>(key) ?? 0;
        return Task.FromResult(count);
    }

    public Task SetAsync(string key, long value, TimeSpan expiry)
    {
        _cache.Set(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key)
    {
        _cache.Remove(key);
        return Task.FromResult(true);
    }

    public Task<List<(long timestamp, int count)>> GetSlidingWindowDataAsync(string key)
    {
        var data = _cache.Get<List<(long timestamp, int count)>>(key) ?? new List<(long timestamp, int count)>();
        return Task.FromResult(data);
    }

    public Task AddSlidingWindowEntryAsync(string key, long timestamp, TimeSpan expiry)
    {
        var data = _cache.Get<List<(long timestamp, int count)>>(key) ?? new List<(long timestamp, int count)>();
        data.Add((timestamp, 1));
        _cache.Set(key, data, expiry);
        return Task.CompletedTask;
    }

    public Task<(long tokens, long lastRefill)> GetTokenBucketStateAsync(string key)
    {
        var state = _cache.Get<(long tokens, long lastRefill)?>(key) ?? (0, 0);
        return Task.FromResult(state);
    }

    public Task UpdateTokenBucketStateAsync(string key, long tokens, long lastRefill, TimeSpan expiry)
    {
        _cache.Set(key, (tokens, lastRefill), expiry);
        return Task.CompletedTask;
    }

    public Task ResetAsync(string key)
    {
        //var prefixes = new[] { "fw", "sw", "tb" }; // FixedWindow, SlidingWindow, TokenBucket

        //foreach (var prefix in prefixes)
        //{
        //    var fullKey = $"{key}:{prefix}";
        //    _cache.Remove(fullKey);
        //}

        // Also remove the base key
        _cache.Remove(key);

        return Task.CompletedTask;
    }
}
