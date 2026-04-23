namespace Auth.RateLimiting;

/// <summary>
/// Interface for rate limit data storage
/// </summary>
public interface IRateLimitStore
{
    Task<long> IncrementAsync(string key, TimeSpan expiry);
    Task<long> GetCountAsync(string key);
    Task SetAsync(string key, long value, TimeSpan expiry);
    Task<bool> DeleteAsync(string key);
    Task<List<(long timestamp, int count)>> GetSlidingWindowDataAsync(string key);
    Task AddSlidingWindowEntryAsync(string key, long timestamp, TimeSpan expiry);
    Task<(long tokens, long lastRefill)> GetTokenBucketStateAsync(string key);
    Task UpdateTokenBucketStateAsync(string key, long tokens, long lastRefill, TimeSpan expiry);

    /// <summary>
    /// Reset all rate limit data for a specific key (support tool)
    /// </summary>
    Task ResetAsync(string key);
}
