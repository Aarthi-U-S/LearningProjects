namespace Auth.DTO.RateLimiting;

/// <summary>
/// Rate limit status response
/// </summary>
public class RateLimitStatusResponse
{
    public bool IsAllowed { get; set; }
    public int RequestsRemaining { get; set; }
    public long CurrentCount { get; set; }
    public int Limit { get; set; }
    public double? RetryAfterSeconds { get; set; }
    public string AlgorithmUsed { get; set; } = string.Empty;
}
