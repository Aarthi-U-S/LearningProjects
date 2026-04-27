using EFCore.Models;

namespace Auth.Interfaces.RateLimiting;

/// <summary>
/// Repository for rate limit data persistence
/// </summary>
public interface IRateLimitRepo
{
    /// <summary>
    /// Log a rate limit event
    /// </summary>
    Task LogRateLimitEventAsync(RateLimitLog log);

    /// <summary>
    /// Get rate limit statistics for a key
    /// </summary>
    Task<RateLimitStats?> GetStatsAsync(string key, DateTime? since = null);

    /// <summary>
    /// Get top blocked IPs/Users
    /// </summary>
    Task<List<BlockedClientInfo>> GetTopBlockedClientsAsync(int topCount = 10, DateTime? since = null);

    /// <summary>
    /// Get rate limit logs with filtering
    /// </summary>
    Task<List<RateLimitLog>> GetLogsAsync(
        string? endpoint = null,
        string? clientIp = null,
        Guid? userId = null,
        bool? isAllowed = null,
        DateTime? since = null,
        int limit = 100);

    /// <summary>
    /// Clean up old rate limit logs
    /// </summary>
    Task<int> CleanupOldLogsAsync(DateTime olderThan);
}

/// <summary>
/// Rate limit statistics aggregate
/// </summary>
public class RateLimitStats
{
    public string Key { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long BlockedRequests { get; set; }
    public DateTime FirstRequestAt { get; set; }
    public DateTime LastRequestAt { get; set; }
    public DateTime? LastBlockedAt { get; set; }
}

/// <summary>
/// Information about blocked clients
/// </summary>
public class BlockedClientInfo
{
    public string Key { get; set; } = string.Empty;
    public string? ClientIp { get; set; }
    public Guid? UserId { get; set; }
    public long BlockedCount { get; set; }
    public DateTime LastBlockedAt { get; set; }
}
