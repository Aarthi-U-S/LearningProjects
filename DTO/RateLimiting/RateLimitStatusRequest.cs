using Auth.RateLimiting;

namespace Auth.DTO.RateLimiting;

/// <summary>
/// Request body to check rate limit status (strategy comes from query param)
/// </summary>
public class RateLimitStatusRequest
{
    /// <summary>
    /// Client IP address or user ID
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint to check
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
}
