using Auth.Interfaces.RateLimiting;
using Auth.Models;
using EFCore.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repository;

/// <summary>
/// Repository implementation for rate limit data persistence
/// </summary>
public class RateLimitRepo : IRateLimitRepo
{
    private readonly AppDbContext _context;

    public RateLimitRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogRateLimitEventAsync(RateLimitLog log)
    {
        _context.RateLimitLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<RateLimitStats?> GetStatsAsync(string key, DateTime? since = null)
    {
        var query = _context.RateLimitLogs.Where(l => l.Key == key);

        if (since.HasValue)
        {
            query = query.Where(l => l.Timestamp >= since.Value);
        }

        var logs = await query.ToListAsync();

        if (!logs.Any())
            return null;

        return new RateLimitStats
        {
            Key = key,
            TotalRequests = logs.Count,
            BlockedRequests = logs.Count(l => !l.IsAllowed),
            FirstRequestAt = logs.Min(l => l.Timestamp),
            LastRequestAt = logs.Max(l => l.Timestamp),
            LastBlockedAt = logs.Where(l => !l.IsAllowed)
                                .OrderByDescending(l => l.Timestamp)
                                .Select(l => (DateTime?)l.Timestamp)
                                .FirstOrDefault()
        };
    }

    public async Task<List<BlockedClientInfo>> GetTopBlockedClientsAsync(int topCount = 10, DateTime? since = null)
    {
        var query = _context.RateLimitLogs.Where(l => !l.IsAllowed);

        if (since.HasValue)
        {
            query = query.Where(l => l.Timestamp >= since.Value);
        }

        return await query
            .GroupBy(l => new { l.Key, l.ClientIp, l.UserId })
            .Select(g => new BlockedClientInfo
            {
                Key = g.Key.Key,
                ClientIp = g.Key.ClientIp,
                UserId = g.Key.UserId,
                BlockedCount = g.Count(),
                LastBlockedAt = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(b => b.BlockedCount)
            .Take(topCount)
            .ToListAsync();
    }

    public async Task<List<RateLimitLog>> GetLogsAsync(
        string? endpoint = null,
        string? clientIp = null,
        Guid? userId = null,
        bool? isAllowed = null,
        DateTime? since = null,
        int limit = 100)
    {
        var query = _context.RateLimitLogs.AsQueryable();

        if (!string.IsNullOrEmpty(endpoint))
            query = query.Where(l => l.Endpoint.Contains(endpoint));

        if (!string.IsNullOrEmpty(clientIp))
            query = query.Where(l => l.ClientIp == clientIp);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (isAllowed.HasValue)
            query = query.Where(l => l.IsAllowed == isAllowed.Value);

        if (since.HasValue)
            query = query.Where(l => l.Timestamp >= since.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CleanupOldLogsAsync(DateTime olderThan)
    {
        var oldLogs = await _context.RateLimitLogs
            .Where(l => l.Timestamp < olderThan)
            .ToListAsync();

        _context.RateLimitLogs.RemoveRange(oldLogs);
        return await _context.SaveChangesAsync();
    }
}
