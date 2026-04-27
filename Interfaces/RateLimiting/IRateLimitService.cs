using Auth.DTO.RateLimiting;
using Auth.Enums;

namespace Auth.Interfaces.RateLimiting;

public interface IRateLimitService
{
    Task<RateLimitConfigResponse> ConfigureEndpointAsync(RateLimitConfig request);

    Task<List<RateLimitConfigResponse>> GetAllConfigurationsAsync();

    Task<RateLimitConfigResponse?> UpdateEndpointConfigAsync(RateLimitConfig request);

    Task<bool> RemoveEndpointConfigAsync(string endpointPattern);

    Task<RateLimitStatusResponse> CheckStatusAsync(string identifier, string endpoint, RateLimitStrategy strategy);

    Task ResetRateLimitAsync(string identifier, string endpoint, RateLimitStrategy strategy);

    Task SetEnabledAsync(bool enabled);
}
