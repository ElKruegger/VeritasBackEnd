namespace LuxuryStore.Application.DTOs;

// Validation rules for these DTOs live in Validation/AuthValidators.cs
// (FluentValidation, brief section 9) — not as attributes here.

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // "Customer" or "Admin" — lets the frontend decide whether to show
    // operator-only UI. Purely cosmetic on the client: every admin endpoint
    // is still enforced server-side by the AdminOnly policy.
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Internal shape returned by IAuthService up to AuthController. The raw
/// AccessToken/RefreshToken values never reach the HTTP response body —
/// the controller writes them into httpOnly cookies (brief 7.2: "never
/// store tokens in localStorage") and returns only the User to the caller.
/// </summary>
public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
