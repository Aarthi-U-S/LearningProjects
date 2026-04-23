using Auth.Enums;

namespace Auth.RateLimiting;

/// <summary>
/// Factory for creating rate limit algorithm instances
/// </summary>
public interface IRateLimitAlgorithmFactory
{
    IRateLimitAlgorithm Create(RateLimitAlgorithm algorithm, EndpointRateLimitRule? rule = null);
}
