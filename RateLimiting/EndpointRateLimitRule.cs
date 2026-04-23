using Auth.Enums;

namespace Auth.RateLimiting;

public class EndpointRateLimitRule
{
    public RateLimitAlgorithm Algorithm { get; set; }
    public RateLimitStrategy Strategy { get; set; }
    public int RequestLimit { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public int? TokenBucketCapacity { get; set; }
    public int? TokenBucketRefillRate { get; set; }
}
