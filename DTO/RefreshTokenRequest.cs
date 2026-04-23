using System.ComponentModel.DataAnnotations;

namespace Auth.DTO;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
