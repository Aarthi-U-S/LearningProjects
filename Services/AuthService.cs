using Auth.DTO;
using Auth.Interfaces;
using EFCore.Models;

namespace Auth.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepo _authRepo;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthRepo authRepo, IJwtTokenService jwtTokenService, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _authRepo = authRepo;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest)
    {
        ArgumentNullException.ThrowIfNull(registerRequest);

        var existingUser = await _authRepo.GetByEmailAsync(registerRequest.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerRequest.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password),
            Role = registerRequest.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _authRepo.AddUserAsync(user);

        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return new RegisterResponse
        {
            UserId = user.Id,
            Message = "User registered successfully."
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
    {
        ArgumentNullException.ThrowIfNull(loginRequest);

        var user = await _authRepo.GetByEmailAsync(loginRequest.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
        var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _authRepo.AddRefreshTokenAsync(refreshToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var refreshToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is no longer active.");
        }

        var user = refreshToken.User;

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
        var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedBy = "Token Rotation";
        refreshToken.ReplacedByToken = newRefreshTokenString;

        await _authRepo.UpdateRefreshTokenAsync(refreshToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _authRepo.AddRefreshTokenAsync(newRefreshToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes),
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        };
    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, string? revokedBy = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var refreshToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

        if (refreshToken == null)
        {
            throw new InvalidOperationException("Refresh token not found.");
        }

        if (refreshToken.IsRevoked)
        {
            throw new InvalidOperationException("Refresh token is already revoked.");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedBy = revokedBy ?? "User";

        await _authRepo.UpdateRefreshTokenAsync(refreshToken);

        _logger.LogInformation("Refresh token revoked for user {UserId} by {RevokedBy}", 
            refreshToken.UserId, refreshToken.RevokedBy);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var token = await _authRepo.GetRefreshTokenAsync(refreshToken);

        if (token != null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedBy = "Logout";

            await _authRepo.UpdateRefreshTokenAsync(token);

            _logger.LogInformation("User {UserId} logged out successfully", token.UserId);
        }
    }
}

