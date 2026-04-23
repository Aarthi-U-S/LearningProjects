using Auth.Enums;
using Auth.RateLimiting.Algorithms;

namespace Auth.RateLimiting;

/// <summary>
/// Factory for creating rate limit algorithm instances
/// </summary>
public class RateLimitAlgorithmFactory : IRateLimitAlgorithmFactory
{
    private readonly IRateLimitStore _store;
    private readonly RateLimitOptions _options;

    public RateLimitAlgorithmFactory(IRateLimitStore store, RateLimitOptions options)
    {
        _store = store;
        _options = options;
    }

    public IRateLimitAlgorithm Create(RateLimitAlgorithm algorithm, EndpointRateLimitRule? rule = null)
    {
        return algorithm switch
        {
            RateLimitAlgorithm.FixedWindow => new FixedWindowRateLimiter(_store),
            RateLimitAlgorithm.SlidingWindow => new SlidingWindowRateLimiter(_store),
            RateLimitAlgorithm.TokenBucket => new TokenBucketRateLimiter(
                _store,
                rule?.TokenBucketCapacity ?? _options.TokenBucketDefaultCapacity,
                rule?.TokenBucketRefillRate ?? _options.TokenBucketDefaultRefillRate),
            _ => throw new ArgumentException($"Unknown algorithm: {algorithm}", nameof(algorithm))
        };
    }
}
