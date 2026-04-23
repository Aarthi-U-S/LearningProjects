namespace Auth.DTO;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = null!;
}

