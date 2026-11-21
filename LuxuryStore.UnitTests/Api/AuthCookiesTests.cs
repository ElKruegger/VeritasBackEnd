using LuxuryStore.Application.DTOs;
using LuxuryStore_Backend.Controllers;
using Microsoft.AspNetCore.Http;

namespace LuxuryStore.UnitTests.Api;

// The cookie attributes are the contract with the storefront: change one and
// login breaks in the browser without a single failing request in Scalar.
public class AuthCookiesTests
{
    private static List<string> SetCookieHeaders(Action<HttpResponse> write)
    {
        var context = new DefaultHttpContext();
        write(context.Response);
        return context.Response.Headers.SetCookie.Select(value => value!.ToLowerInvariant()).ToList();
    }

    private static AuthResultDto Result() => new()
    {
        AccessToken = "access",
        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
        RefreshToken = "refresh",
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
        User = new UserDto()
    };

    [Fact]
    public void Write_sets_both_tokens_as_first_party_httponly_cookies()
    {
        var cookies = SetCookieHeaders(response => AuthCookies.Write(response, Result()));

        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, cookie =>
        {
            Assert.Contains("httponly", cookie);
            Assert.Contains("secure", cookie);
            Assert.Contains("samesite=lax", cookie);
        });
        Assert.Contains(cookies, c => c.StartsWith("access_token=access;") && c.Contains("path=/;"));
        Assert.Contains(cookies, c => c.StartsWith("refresh_token=refresh;") && c.Contains("path=/api/v1/auth"));
    }

    [Fact]
    public void Clear_expires_both_cookies_with_the_same_attributes()
    {
        var cookies = SetCookieHeaders(AuthCookies.Clear);

        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, cookie =>
        {
            Assert.Contains("expires=thu, 01 jan 1970", cookie);
            Assert.Contains("httponly", cookie);
            Assert.Contains("secure", cookie);
            Assert.Contains("samesite=lax", cookie);
        });
        Assert.Contains(cookies, c => c.StartsWith("access_token=;") && c.Contains("path=/;"));
        Assert.Contains(cookies, c => c.StartsWith("refresh_token=;") && c.Contains("path=/api/v1/auth"));
    }
}
