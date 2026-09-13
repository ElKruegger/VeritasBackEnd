using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
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
/// the brief) and the client never touches their raw values. Register/Login/
/// Refresh are rate-limited (brief section 9) since they're exactly the
/// endpoints an unauthenticated attacker would hammer.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(dto, cancellationToken);
        SetAuthCookies(result);
        return CreatedAtAction(nameof(Me), null, result.User);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        SetAuthCookies(result);
        return Ok(result.User);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        SetAuthCookies(result);
        return Ok(result.User);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, cancellationToken);

        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/v1/auth" });
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
        // just to answer "who is currently logged in".
        var id = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

        if (id is null || email is null)
            return Unauthorized();

        return Ok(new UserDto { Id = Guid.Parse(id), Email = email });
    }

    private void SetAuthCookies(AuthResultDto result)
    {
        // SameSite=None + Secure: the frontend (Vercel) and backend (Render)
        // are different origins even in production, so the cookie must be
        // marked cross-site-sendable. That requires Secure=true, which in
        // turn requires HTTPS locally too — `dotnet dev-certs https --trust`
        // if this 401s unexpectedly during local development.
        Response.Cookies.Append(AccessTokenCookie, result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = result.AccessTokenExpiresAt,
            Path = "/"
        });

        Response.Cookies.Append(RefreshTokenCookie, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = result.RefreshTokenExpiresAt,
            // Narrower path: the refresh token only ever needs to travel to
            // /auth/refresh and /auth/logout, so it's never sent (and never
            // at risk) on every other request the way the access token is.
            Path = "/api/v1/auth"
        });
    }
}
