namespace Auth.RateLimiting.Algorithms;

public class FixedWindowRateLimiter : IRateLimitAlgorithm
{
    private readonly IRateLimitStore _store;

    public FixedWindowRateLimiter(IRateLimitStore store)
    {
        _store = store;
    }

    public async Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window)
    {
        var count = await _store.IncrementAsync(key, window);
        var remaining = Math.Max(0, limit - count);
        var isAllowed = count <= limit;

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            RequestsRemaining = remaining,
            AlgorithmUsed = "FixedWindow",
            RetryAfter = isAllowed ? null : window,
            ResetTime = DateTime.UtcNow.Add(window)
        };
    }
}
