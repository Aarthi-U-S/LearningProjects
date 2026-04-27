using Auth.DTO.RateLimiting;
using Auth.Enums;
using Auth.Interfaces.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers;


[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RateLimitController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitController> _logger;

    public RateLimitController(IRateLimitService rateLimitService, ILogger<RateLimitController> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Configure rate limit
    /// </summary>
    /// <remarks>
    /// - Sets limit per endpoint  
    /// **Example Request:**  
    /// ```
    /// POST /api/ratelimit/configure?algorithm=FixedWindow&amp;strategy=PerUser
    /// Body: {
    ///   "endpointPattern": "/api/auth/login",
    ///   "requestLimit": 5,
    ///   "timeWindowMinutes": 1
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Configuration created successfully</response>
    /// <response code="400">Invalid configuration (e.g., negative limit, invalid algorithm)</response>
    /// <response code="401">Unauthorized - Admin role required</response>
    [HttpPost("configure")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RateLimitConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfigureEndpoint(
        [FromBody] RateLimitConfigRequest request,
        [FromQuery] RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        [FromQuery] RateLimitStrategy strategy = RateLimitStrategy.PerUser)
    {
        try
        {
            var fullRequest = new RateLimitConfig
            {
                EndpointPattern = request.EndpointPattern,
                Algorithm = algorithm,
                Strategy = strategy,
                RequestLimit = request.RequestLimit,
                TimeWindowMinutes = request.TimeWindowMinutes,
                TokenBucketCapacity = request.TokenBucketCapacity,
                TokenBucketRefillRate = request.TokenBucketRefillRate
            };

            var result = await _rateLimitService.ConfigureEndpointAsync(fullRequest);
            _logger.LogInformation(
                "Rate limit configured: {Endpoint} - {Limit} req/{Window}min - {Algorithm} - {Strategy}",
                request.EndpointPattern, request.RequestLimit, request.TimeWindowMinutes,
                algorithm, strategy);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid rate limit configuration: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring rate limit for {Endpoint}", request.EndpointPattern);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all rate limit configurations
    /// </summary>
    /// <remarks>
    /// For debugging and understanding current system state
    /// </remarks>
    /// <response code="200">List of all configurations</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("configure")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<RateLimitConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllConfigurations()
    {
        var configs = await _rateLimitService.GetAllConfigurationsAsync();
        return Ok(configs);
    }

    /// <summary>
    /// Update rate limit configuration for an endpoint
    /// </summary>
    /// <response code="200">Configuration updated</response>
    /// <response code="400">Invalid configuration</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Configuration not found</response>
    [HttpPut("configure")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RateLimitConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEndpointConfig(
        [FromBody] RateLimitConfigRequest request,
        [FromQuery] RateLimitAlgorithm algorithm = RateLimitAlgorithm.SlidingWindow,
        [FromQuery] RateLimitStrategy strategy = RateLimitStrategy.PerUser)
    {
        try
        {
            var fullRequest = new RateLimitConfig
            {
                EndpointPattern = request.EndpointPattern,
                Algorithm = algorithm,
                Strategy = strategy,
                RequestLimit = request.RequestLimit,
                TimeWindowMinutes = request.TimeWindowMinutes,
                TokenBucketCapacity = request.TokenBucketCapacity,
                TokenBucketRefillRate = request.TokenBucketRefillRate
            };

            var result = await _rateLimitService.UpdateEndpointConfigAsync(fullRequest);

            if (result == null)
                return NotFound(new { message = "Configuration not found" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate limit for {Endpoint}", request.EndpointPattern);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove rate limit configuration for an endpoint
    /// </summary>
    /// <remarks>
    /// Instantly disable rate limiting for a specific endpoint
    /// </remarks>
    /// <param name="endpointPattern">Endpoint pattern to remove</param>
    /// <response code="200">Configuration removed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Configuration not found</response>
    [HttpDelete("configure")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEndpointConfig(string endpointPattern)
    {
        var removed = await _rateLimitService.RemoveEndpointConfigAsync(endpointPattern);

        if (!removed)
            return NotFound(new { message = "Configuration not found" });

        return Ok(new { message = "Configuration removed successfully", endpoint = endpointPattern });
    }

    /// <summary>
    /// Check current rate limit status
    /// </summary>
    /// <remarks>
    /// Answers critical production questions:  
    /// - Is this user/IP blocked?  
    /// - How many requests remain?  
    /// - When can they retry?  
    /// Essential for customer support and debugging
    /// </remarks>
    /// <response code="200">Rate limit status with remaining quota and retry time</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("status")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RateLimitStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckStatus(
        [FromBody] RateLimitStatusRequest request,
        [FromQuery] RateLimitStrategy strategy = RateLimitStrategy.PerUser)
    {
        try
        {
            var status = await _rateLimitService.CheckStatusAsync(request.Identifier, request.Endpoint, strategy);
            return Ok(status);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reset rate limit for a specific identifier and endpoint
    /// </summary>
    /// <remarks>
    /// Instant unblock for legitimate users.  
    /// Use the identifier (IP address or username) and endpoint from the 429 response.
    /// </remarks>
    /// <param name="identifier">The client identifier (IP address, username, etc.) shown in the 429 response</param>
    /// <param name="endpoint">The endpoint path, e.g. /api/auth/login</param>
    /// <param name="strategy">The rate limit strategy used for this endpoint</param>
    /// <response code="200">Rate limit reset successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("reset")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetRateLimit(
        [FromQuery] string identifier,
        [FromQuery] string endpoint,
        [FromQuery] RateLimitStrategy strategy = RateLimitStrategy.PerIp)
    {
        await _rateLimitService.ResetRateLimitAsync(identifier, endpoint, strategy);
        var key = $"ratelimit:{strategy}:{identifier}:{endpoint}";
        _logger.LogInformation("Rate limit reset for key: {Key}", key);
        return Ok(new { message = "Rate limit reset successfully", key });
    }

    /// <summary>
    /// Enable or disable rate limiting globally
    /// </summary>
    /// <remarks> 
    /// - Bug in rate limiter → disable instantly to keep service running  
    /// - Traffic spike/DDoS → enable stricter rules immediately  
    /// - Maintenance mode → temporarily disable  
    /// </remarks>
    /// <response code="200">Rate limiting status updated</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("enable")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetEnabled([FromQuery] bool enabled)
    {
        await _rateLimitService.SetEnabledAsync(enabled);
        _logger.LogWarning("Rate limiting globally {Status}", enabled ? "ENABLED" : "DISABLED");
        return Ok(new
        {
            message = $"Rate limiting {(enabled ? "enabled" : "disabled")}",
            enabled,
            timestamp = DateTime.UtcNow
        });
    }
}
