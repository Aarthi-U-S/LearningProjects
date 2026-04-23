using Auth.RateLimiting.Algorithms;

namespace Auth.RateLimiting;

public interface IRateLimitAlgorithm
{
    Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window);
}
