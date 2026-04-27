using Auth.DTO.Auth;
using Auth.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learning.Controllers;

/// <summary>
/// JWT Authentication API - Register, Login, Token Management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register new user
    /// </summary>
    /// <remarks>
    /// **Creates** user with BCrypt-hashed password  
    /// **Roles:** Admin, User, Manager  
    /// **Validation:** Email unique, password min 6 chars
    /// </remarks>
    /// <response code="200">User registered</response>
    /// <response code="400">Email exists or invalid input</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerRequest);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration." });
        }
    }

    /// <summary>
    /// Login - Get JWT tokens
    /// </summary>
    /// <remarks>
    /// **Returns 2 tokens:**
    /// - **accessToken** → Use in `Authorization: Bearer {token}` for APIs (15min)
    /// - **refreshToken** → Get new access token when expired (7 days)
    /// 
    /// **Flow:** Login → Save both → Use access token → Expires? → Use refresh token
    /// </remarks>
    /// <response code="200">Login successful with tokens</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var result = await _authService.LoginAsync(loginRequest);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login." });
        }
    }

    /// <summary>
    /// Refresh expired access token
    /// </summary>
    /// <remarks>
    /// **When:** Access token expired (after 15 min)
    /// 
    /// **Returns:** New access + refresh tokens  
    /// **Security:** Old refresh token auto-revoked (rotation)
    /// </remarks>
    /// <response code="200">Tokens refreshed</response>
    /// <response code="401">Invalid/expired refresh token</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh." });
        }
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    /// <remarks>
    /// **Effect:** Refresh token invalidated  
    /// **Note:** Access token works until expiry (max 15 min)
    /// </remarks>
    /// <response code="200">Logged out successfully</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout." });
        }
    }

    /// <summary>
    /// Revoke any token (Admin only)
    /// </summary>
    /// <remarks>
    /// **Use:** Force logout, security breach, suspension  
    /// **Tracking:** Audit trail (who revoked, when)
    /// </remarks>
    /// <response code="200">Token revoked</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    [HttpPost("revoke-token")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        try
        {
            var revokedBy = User.Identity?.Name ?? "Admin";
            await _authService.RevokeTokenAsync(request, revokedBy);
            return Ok(new { message = "Token revoked successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, new { message = "An error occurred during token revocation." });
        }
    }

    [HttpPost("echo")]
    [AllowAnonymous]
    public IActionResult Echo([FromBody] object data)
    {
        return Ok(new
        {
            message = "Echo endpoint",
            receivedData = data,
            timestamp = DateTime.UtcNow,
            endpoint = "/api/test/echo"
        });
    }
}

