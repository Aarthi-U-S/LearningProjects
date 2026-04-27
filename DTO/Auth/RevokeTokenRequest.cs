using System.ComponentModel.DataAnnotations;

namespace Auth.DTO.Auth;

public class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
