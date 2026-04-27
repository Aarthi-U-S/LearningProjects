namespace Auth.DTO.RateLimiting;

/// <summary>
/// Rate limit statistics response
/// </summary>
public class RateLimitStatsResponse
{
    public string Key { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long BlockedRequests { get; set; }
    public DateTime FirstRequestAt { get; set; }
    public DateTime LastRequestAt { get; set; }
    public DateTime? LastBlockedAt { get; set; }
}
