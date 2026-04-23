namespace Auth.RateLimiting.Algorithms;

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public long RequestsRemaining { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string AlgorithmUsed { get; set; } = string.Empty;
    public DateTime? ResetTime { get; set; }
}
