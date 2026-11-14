using LuxuryStore.Application.DTOs;

namespace LuxuryStore_Backend.Controllers;

/// <summary>
/// The one place that knows how auth cookies are written AND deleted.
///
/// A cookie is only removed by the browser when the expiring Set-Cookie
/// matches the original's name, Path and Domain — and for a cross-site
/// response, a SameSite=None cookie without Secure is rejected outright.
/// Deleting with fresh `new CookieOptions { Path = ... }` (the previous
/// logout code) dropped Secure/SameSite, so on the deployed Vercel -> Render
/// setup the browser could ignore the deletion and keep the cookies.
/// Building both operations from the same Options() method makes that
/// mismatch impossible.
/// </summary>
internal static class AuthCookies
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";

    // Narrower path: the refresh token only ever needs to travel to
    // /auth/refresh and /auth/logout, so it's never sent (and never at risk)
    // on every other request the way the access token is.
    private const string AccessTokenPath = "/";
    private const string RefreshTokenPath = "/api/v1/auth";

    public static void Write(HttpResponse response, AuthResultDto result)
    {
        response.Cookies.Append(AccessToken, result.AccessToken, Options(AccessTokenPath, result.AccessTokenExpiresAt));
        response.Cookies.Append(RefreshToken, result.RefreshToken, Options(RefreshTokenPath, result.RefreshTokenExpiresAt));
    }

    public static void Clear(HttpResponse response)
    {
        // Delete() sets an expiry in the past itself; the remaining options
        // must match the ones the cookie was created with.
        response.Cookies.Delete(AccessToken, Options(AccessTokenPath, expires: null));
        response.Cookies.Delete(RefreshToken, Options(RefreshTokenPath, expires: null));
    }

    // SameSite=None + Secure: the frontend (Vercel) and backend (Render) are
    // different origins even in production, so the cookie must be marked
    // cross-site-sendable. That requires Secure=true, which in turn requires
    // HTTPS locally too — `dotnet dev-certs https --trust` if this 401s
    // unexpectedly during local development.
    private static CookieOptions Options(string path, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = path,
        Expires = expires
    };
}
