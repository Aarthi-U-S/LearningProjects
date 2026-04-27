using Auth.Enums;

namespace Auth.DTO.RateLimiting;

public class RateLimitConfigRequest
{
    /// <summary>
    /// Endpoint pattern (e.g., "/api/auth/login")
    /// </summary>
    public string EndpointPattern { get; set; } = string.Empty;

    public int RequestLimit { get; set; } = 100;

    public int TimeWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Token bucket capacity (for TokenBucket algorithm)
    /// </summary>
    public int? TokenBucketCapacity { get; set; }

    /// <summary>
    /// Token bucket refill rate (for TokenBucket algorithm)
    /// </summary>
    public int? TokenBucketRefillRate { get; set; }
}

public class RateLimitConfig
{
    public string EndpointPattern { get; set; } = string.Empty;
    public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.SlidingWindow;
    public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.PerUser;
    public int RequestLimit { get; set; } = 100;
    public int TimeWindowMinutes { get; set; } = 1;
    public int? TokenBucketCapacity { get; set; }
    public int? TokenBucketRefillRate { get; set; }
}
