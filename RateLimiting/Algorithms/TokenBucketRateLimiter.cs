namespace Auth.RateLimiting.Algorithms;

public class TokenBucketRateLimiter : IRateLimitAlgorithm
{
    private readonly IRateLimitStore _store;
    private readonly int _capacity;
    private readonly int _refillRate;

    public TokenBucketRateLimiter(IRateLimitStore store, int capacity, int refillRate)
    {
        _store = store;
        _capacity = capacity;
        _refillRate = refillRate;
    }

    public async Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var (tokens, lastRefill) = await _store.GetTokenBucketStateAsync(key);

        if (tokens == 0 && lastRefill == 0)
        {
            tokens = _capacity;
            lastRefill = now;
        }
        else
        {
            var timePassed = now - lastRefill;
            var tokensToAdd = (long)(timePassed / window.TotalMilliseconds * _refillRate);
            tokens = Math.Min(_capacity, tokens + tokensToAdd);

            if (tokensToAdd > 0)
            {
                lastRefill = now;
            }
        }

        var isAllowed = tokens > 0;
        if (isAllowed)
        {
            tokens--;
        }

        await _store.UpdateTokenBucketStateAsync(key, tokens, lastRefill, TimeSpan.FromDays(1));

        var retryAfter = !isAllowed
            ? TimeSpan.FromMilliseconds(window.TotalMilliseconds / _refillRate)
            : (TimeSpan?)null;

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            RequestsRemaining = tokens,
            AlgorithmUsed = "TokenBucket",
            RetryAfter = retryAfter,
            ResetTime = DateTime.UtcNow.Add(window)
        };
    }
}
