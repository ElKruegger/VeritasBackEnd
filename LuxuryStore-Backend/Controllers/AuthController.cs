using System.IdentityModel.Tokens.Jwt;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuxuryStore_Backend.Controllers;

/// <summary>
/// Minimal endpoint set for brief 7.2: register, login, refresh, logout,
/// and "who am I" for the frontend to check session state on load.
///
/// AccessToken/RefreshToken never appear in a JSON response body — they're
/// written straight into httpOnly, Secure cookies (never localStorage, per
/// the brief; see AuthCookies) and the client never touches their raw
/// values. Register/Login/Refresh are rate-limited (brief section 9) since
/// they're exactly the endpoints an unauthenticated attacker would hammer.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(dto, cancellationToken);
        AuthCookies.Write(Response, result);
        return CreatedAtAction(nameof(Me), null, result.User);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        AuthCookies.Write(Response, result);
        return Ok(result.User);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        AuthCookies.Write(Response, result);
        return Ok(result.User);
    }

    /// <summary>
    /// Deliberately NOT [Authorize]: the access token lives only 15 minutes,
    /// so requiring it meant a user returning after a break could not log
    /// out — the call 401'd and both cookies (plus a still-valid refresh
    /// token on the server) survived. Logout only needs the refresh-token
    /// cookie, is idempotent, and revoking your own session can't harm anyone.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, cancellationToken);

        AuthCookies.Clear(Response);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserDto> Me()
    {
        // Populated straight from the validated JWT's claims (see
        // TokenService.GenerateAccessToken) — no database round trip needed
        // just to answer "who is currently logged in". Short claim names
        // work here because Program.cs sets MapInboundClaims = false.
        var id = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

        if (!Guid.TryParse(id, out var userId) || email is null)
            return Unauthorized();

        return Ok(new UserDto
        {
            Id = userId,
            Email = email,
            FirstName = User.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value ?? string.Empty,
            LastName = User.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value ?? string.Empty,
            Role = User.FindFirst(JwtClaimTypes.Role)?.Value ?? string.Empty
        });
    }
}
