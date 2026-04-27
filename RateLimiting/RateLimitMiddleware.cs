using Auth.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using static System.Net.WebRequestMethods;

namespace Auth.RateLimiting;

/// <summary>
///Intercepts EVERY HTTP request
///Checks rate limit before controllerS
/// Blocks(429) or Allows(continue)
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly IRateLimitAlgorithmFactory _algorithmFactory;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, RateLimitOptions options, IRateLimitAlgorithmFactory algorithmFactory, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _options = options;
        _algorithmFactory = algorithmFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var endpoint = context.Request.Path.Value ?? "/";

        // Skip rate limiting for the rate limit management endpoints
        if (endpoint.StartsWith("/api/ratelimit", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        var rule = GetRuleForEndpoint(endpoint);
        var algorithm = rule?.Algorithm ?? _options.DefaultAlgorithm;
        var strategy = rule?.Strategy ?? _options.DefaultStrategy;
        var limit = rule?.RequestLimit ?? _options.DefaultRequestLimit;
        var window = rule?.TimeWindow ?? _options.DefaultTimeWindow;

        var identifier = GetIdentifier(context, strategy);
        var key = GenerateKey(identifier, endpoint, strategy);

        var limiter = _algorithmFactory.Create(algorithm, rule);
        var result = await limiter.CheckLimitAsync(key, limit, window);

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.RequestsRemaining.ToString();

        if (result.ResetTime.HasValue)
        {
            context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(result.ResetTime.Value).ToUnixTimeSeconds().ToString();
        }

        if (!result.IsAllowed)
        {
            context.Response.StatusCode = 429;
            if (result.RetryAfter.HasValue)
            {
                context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.Value.TotalSeconds).ToString();
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = result.RetryAfter?.TotalSeconds,
                rateLimitKey = key,
                identifier,
                endpoint,
                strategy = strategy.ToString()
            });

            _logger.LogWarning("Rate limit exceeded for {Identifier} on {Endpoint}", identifier, endpoint);
            return;
        }

        await _next(context);
    }

    private EndpointRateLimitRule? GetRuleForEndpoint(string endpoint)
    {
        foreach (var (pattern, rule) in _options.EndpointRules)
        {
            if (endpoint.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return null;
    }

    private static string GetIdentifier(HttpContext context, RateLimitStrategy strategy)
    {
        return strategy switch
        {
            RateLimitStrategy.PerIp => context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            RateLimitStrategy.PerUser => context.User?.Identity?.Name ?? "anonymous",
            RateLimitStrategy.PerEndpoint => context.Request.Path.Value ?? "/",
            RateLimitStrategy.Global => "global",
            _ => "unknown"
        };
    }

    private static string GenerateKey(string identifier, string endpoint, RateLimitStrategy strategy)
    {
        return $"ratelimit:{strategy}:{identifier}:{endpoint}";
    }
}
