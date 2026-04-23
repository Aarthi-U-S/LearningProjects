namespace Auth.Enums;

public enum RateLimitStrategy
{
    PerIp,
    PerUser,
    PerEndpoint,
    PerIpAndEndpoint,
    PerUserAndEndpoint,
    Global
}
