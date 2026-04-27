using System.ComponentModel.DataAnnotations;

namespace Auth.DTO.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
