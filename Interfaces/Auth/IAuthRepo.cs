using EFCore.Models;

namespace Auth.Interfaces.Auth;

public interface IAuthRepo
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid userId);
    Task AddUserAsync(User user);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllUserRefreshTokensAsync(Guid userId);
    Task<int> RemoveExpiredRefreshTokensAsync();
}

