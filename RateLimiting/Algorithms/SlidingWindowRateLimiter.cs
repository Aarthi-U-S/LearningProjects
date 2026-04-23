namespace Auth.RateLimiting.Algorithms;

public class SlidingWindowRateLimiter : IRateLimitAlgorithm
{
    private readonly IRateLimitStore _store;

    public SlidingWindowRateLimiter(IRateLimitStore store)
    {
        _store = store;
    }

    public async Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds;

        var entries = await _store.GetSlidingWindowDataAsync(key);
        var validEntries = entries.Where(e => e.timestamp >= windowStart).ToList();

        var currentCount = validEntries.Sum(e => e.count);
        var isAllowed = currentCount < limit;

        if (isAllowed)
        {
            await _store.AddSlidingWindowEntryAsync(key, now, window);
            currentCount++;
        }

        var remaining = Math.Max(0, limit - currentCount);
        var oldestEntry = validEntries.OrderBy(e => e.timestamp).FirstOrDefault();
        var retryAfter = oldestEntry.timestamp > 0
            ? TimeSpan.FromMilliseconds(oldestEntry.timestamp + (long)window.TotalMilliseconds - now)
            : window;

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            RequestsRemaining = remaining,
            AlgorithmUsed = "SlidingWindow",
            RetryAfter = isAllowed ? null : retryAfter,
            ResetTime = oldestEntry.timestamp > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(oldestEntry.timestamp).Add(window).DateTime
                : DateTime.UtcNow.Add(window)
        };
    }
}
