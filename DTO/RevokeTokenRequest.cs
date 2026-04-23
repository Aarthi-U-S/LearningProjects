using System.ComponentModel.DataAnnotations;

namespace Auth.DTO;

public class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
