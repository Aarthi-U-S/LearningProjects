using System.ComponentModel.DataAnnotations;

namespace EFCore.Models;

/// <summary>
/// Rate limit log entry for tracking and analytics
/// </summary>
public class RateLimitLog
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Rate limit key (IP, UserID, etc.)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint path
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    [MaxLength(50)]
    public string? ClientIp { get; set; }

    /// <summary>
    /// User ID if authenticated
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Algorithm used
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Strategy used
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// Was the request allowed?
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Request count at the time
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Configured limit
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Timestamp of the request
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Retry after duration in seconds if blocked
    /// </summary>
    public int? RetryAfterSeconds { get; set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User? User { get; set; }
}
