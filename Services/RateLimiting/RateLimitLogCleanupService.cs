using Auth.Interfaces.RateLimiting;

namespace Auth.Services.RateLimiting;

/// <summary>
/// Background service to periodically clean up old rate limit logs
/// </summary>
public class RateLimitLogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RateLimitLogCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30);

    public RateLimitLogCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RateLimitLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rate Limit Log Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IRateLimitRepo>();

                var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);
                var deletedCount = await repo.CleanupOldLogsAsync(cutoffDate);

                if (deletedCount > 0)
                {
                    _logger.LogInformation(
                        "Cleaned up {Count} rate limit logs older than {CutoffDate}",
                        deletedCount, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rate limit log cleanup");
            }
        }

        _logger.LogInformation("Rate Limit Log Cleanup Service stopped");
    }
}
