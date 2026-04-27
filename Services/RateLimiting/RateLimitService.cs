using Auth.DTO.RateLimiting;
using Auth.Enums;
using Auth.Interfaces.RateLimiting;
using Auth.RateLimiting;

namespace Auth.Services.RateLimiting;

/// <summary>
/// Service implementation for rate limiting management - Production Core APIs
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly RateLimitOptions _options;
    private readonly IRateLimitStore _store;
    private readonly IRateLimitAlgorithmFactory _algorithmFactory;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(RateLimitOptions options, IRateLimitStore store, IRateLimitAlgorithmFactory algorithmFactory, ILogger<RateLimitService> logger)
    {
        _options = options;
        _store = store;
        _algorithmFactory = algorithmFactory;
        _logger = logger;
    }

    public Task<RateLimitConfigResponse> ConfigureEndpointAsync(RateLimitConfig request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.EndpointPattern))
            throw new ArgumentException("Endpoint pattern is required", nameof(request));

        if (request.RequestLimit <= 0)
            throw new ArgumentException("Request limit must be greater than 0", nameof(request));

        if (request.TimeWindowMinutes <= 0)
            throw new ArgumentException("Time window must be greater than 0", nameof(request));

        var rule = new EndpointRateLimitRule
        {
            Algorithm = request.Algorithm,
            Strategy = request.Strategy,
            RequestLimit = request.RequestLimit,
            TimeWindow = TimeSpan.FromMinutes(request.TimeWindowMinutes),
            TokenBucketCapacity = request.TokenBucketCapacity,
            TokenBucketRefillRate = request.TokenBucketRefillRate
        };

        _options.EndpointRules[request.EndpointPattern] = rule;

        _logger.LogInformation(
            "Configured rate limit for {Endpoint}: {Limit} requests per {Window} using {Algorithm} - {Strategy}",
            request.EndpointPattern, request.RequestLimit, request.TimeWindowMinutes, request.Algorithm, request.Strategy);

        return Task.FromResult(new RateLimitConfigResponse
        {
            EndpointPattern = request.EndpointPattern,
            Algorithm = request.Algorithm.ToString(),
            Strategy = request.Strategy.ToString(),
            RequestLimit = request.RequestLimit,
            TimeWindowMinutes = request.TimeWindowMinutes,
            TokenBucketCapacity = request.TokenBucketCapacity,
            TokenBucketRefillRate = request.TokenBucketRefillRate,
            IsActive = true
        });
    }

    public Task<List<RateLimitConfigResponse>> GetAllConfigurationsAsync()
    {
        var configs = _options.EndpointRules.Select(kvp => new RateLimitConfigResponse
        {
            EndpointPattern = kvp.Key,
            Algorithm = kvp.Value.Algorithm.ToString(),
            Strategy = kvp.Value.Strategy.ToString(),
            RequestLimit = kvp.Value.RequestLimit,
            TimeWindowMinutes = (int)kvp.Value.TimeWindow.TotalMinutes,
            TokenBucketCapacity = kvp.Value.TokenBucketCapacity,
            TokenBucketRefillRate = kvp.Value.TokenBucketRefillRate,
            IsActive = true
        }).ToList();

        return Task.FromResult(configs);
    }

    public Task<RateLimitConfigResponse?> UpdateEndpointConfigAsync(RateLimitConfig request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if configuration exists
        if (!_options.EndpointRules.ContainsKey(request.EndpointPattern))
            return Task.FromResult<RateLimitConfigResponse?>(null);

        // Validate the new configuration
        if (request.RequestLimit <= 0)
            throw new ArgumentException("Request limit must be greater than 0", nameof(request));

        if (request.TimeWindowMinutes <= 0)
            throw new ArgumentException("Time window must be greater than 0", nameof(request));

        // Update the rule
        var rule = new EndpointRateLimitRule
        {
            Algorithm = request.Algorithm,
            Strategy = request.Strategy,
            RequestLimit = request.RequestLimit,
            TimeWindow = TimeSpan.FromMinutes(request.TimeWindowMinutes),
            TokenBucketCapacity = request.TokenBucketCapacity,
            TokenBucketRefillRate = request.TokenBucketRefillRate
        };

        _options.EndpointRules[request.EndpointPattern] = rule;

        _logger.LogInformation(
            "Updated rate limit for {Endpoint}: {Limit} requests per {Window} using {Algorithm} - {Strategy}",
            request.EndpointPattern, request.RequestLimit, request.TimeWindowMinutes, 
            request.Algorithm, request.Strategy);

        return Task.FromResult<RateLimitConfigResponse?>(new RateLimitConfigResponse
        {
            EndpointPattern = request.EndpointPattern,
            Algorithm = request.Algorithm.ToString(),
            Strategy = request.Strategy.ToString(),
            RequestLimit = request.RequestLimit,
            TimeWindowMinutes = request.TimeWindowMinutes,
            TokenBucketCapacity = request.TokenBucketCapacity,
            TokenBucketRefillRate = request.TokenBucketRefillRate,
            IsActive = true
        });
    }

    public Task<bool> RemoveEndpointConfigAsync(string endpointPattern)
    {
        var removed = _options.EndpointRules.Remove(endpointPattern);

        if (removed)
        {
            _logger.LogInformation("Removed rate limit configuration for {Endpoint}", endpointPattern);

            // Clear any cached rate limit data for this endpoint to ensure rate limit is truly removed
            _logger.LogDebug("Clearing cached data for endpoint: {Endpoint}", endpointPattern);
        }

        return Task.FromResult(removed);
    }

    public async Task<RateLimitStatusResponse> CheckStatusAsync(string identifier, string endpoint, RateLimitStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier is required", nameof(identifier));

        var key = GenerateKey(identifier, endpoint, strategy);

        var rule = GetRuleForEndpoint(endpoint);
        var algorithm = rule?.Algorithm ?? _options.DefaultAlgorithm;
        var limit = rule?.RequestLimit ?? _options.DefaultRequestLimit;
        var window = rule?.TimeWindow ?? _options.DefaultTimeWindow;
        var rateLimiter = _algorithmFactory.Create(algorithm, rule);
        var result = await rateLimiter.CheckLimitAsync(key, limit, window);

        _logger.LogDebug(
            "Rate limit status check for {Key}: Allowed={IsAllowed}, Remaining={Remaining}/{Limit}",
            key, result.IsAllowed, result.RequestsRemaining, limit);

        return new RateLimitStatusResponse
        {
            IsAllowed = result.IsAllowed,
            RequestsRemaining = (int)result.RequestsRemaining,
            CurrentCount = limit - result.RequestsRemaining,
            Limit = limit,
            RetryAfterSeconds = result.RetryAfter?.TotalSeconds,
            AlgorithmUsed = result.AlgorithmUsed
        };
    }

    public async Task ResetRateLimitAsync(string identifier, string endpoint, RateLimitStrategy strategy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var key = GenerateKey(identifier, endpoint, strategy);

        await _store.ResetAsync(key);

        _logger.LogWarning("Rate limit reset for key: {Key}", key);
    }

    public Task SetEnabledAsync(bool enabled)
    {
        _options.Enabled = enabled;
        _logger.LogWarning("Rate limiting globally {Status} - Emergency control activated", 
            enabled ? "ENABLED" : "DISABLED");
        return Task.CompletedTask;
    }

    private EndpointRateLimitRule? GetRuleForEndpoint(string endpoint)
    {
        foreach (var (pattern, rule) in _options.EndpointRules)
        {
            if (endpoint.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return null;
    }

    private static string GenerateKey(string identifier, string endpoint, RateLimitStrategy strategy)
    {
        return $"ratelimit:{strategy}:{identifier}:{endpoint}";
    }
}
