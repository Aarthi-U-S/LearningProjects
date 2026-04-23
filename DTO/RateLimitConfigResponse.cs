namespace Auth.DTO;

public class RateLimitConfigResponse
{
    public string EndpointPattern { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public int RequestLimit { get; set; }
    public int TimeWindowMinutes { get; set; }
    public int? TokenBucketCapacity { get; set; }
    public int? TokenBucketRefillRate { get; set; }
    public bool IsActive { get; set; }
}
