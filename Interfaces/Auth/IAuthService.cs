using Auth.DTO.Auth;

namespace Auth.Interfaces.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest);
    Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeTokenAsync(RevokeTokenRequest request, string? revokedBy = null);
    Task LogoutAsync(string refreshToken);
}

