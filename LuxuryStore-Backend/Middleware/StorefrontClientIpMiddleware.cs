using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LuxuryStore_Backend.Middleware;

/// <summary>
/// Restores the visitor's IP for requests relayed by the storefront.
///
/// The problem: the browser never calls this API directly. Its requests go
/// to the storefront (Vercel), which relays /api/* here, and Render's edge
/// proxy adds one more hop. With UseForwardedHeaders trusting only the
/// nearest hop, RemoteIpAddress is the storefront server's IP, the same for
/// every visitor, so every customer would share one auth rate-limit bucket.
///
/// The fix: the storefront sends the visitor's IP in X-Veritas-Client-IP,
/// together with a secret only it and this API know (STOREFRONT_PROXY_SECRET).
/// The IP header is believed only when the secret matches; a client calling
/// the API directly can set the IP header but not the secret, so it can't
/// pick its own rate-limit bucket. Both headers are removed before anything
/// else (logging, controllers) sees the request.
///
/// Without a configured secret (local development) this does nothing but
/// strip the headers.
/// </summary>
public class StorefrontClientIpMiddleware
{
    public const string SecretHeader = "X-Veritas-Proxy-Secret";
    public const string ClientIpHeader = "X-Veritas-Client-IP";
    public const int MinimumSecretLength = 32;

    private readonly RequestDelegate _next;
    private readonly byte[]? _secret;

    public StorefrontClientIpMiddleware(RequestDelegate next, string? secret)
    {
        _next = next;
        if (string.IsNullOrEmpty(secret)) return;

        // A short secret could be guessed; fail at startup rather than run
        // with a check that only looks secure.
        if (secret.Length < MinimumSecretLength)
            throw new InvalidOperationException(
                $"STOREFRONT_PROXY_SECRET must be at least {MinimumSecretLength} characters.");
        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers;

        if (_secret is not null &&
            IsSecret(headers[SecretHeader].ToString()) &&
            IPAddress.TryParse(headers[ClientIpHeader].ToString(), out var clientIp))
        {
            context.Connection.RemoteIpAddress = clientIp;
        }

        headers.Remove(SecretHeader);
        headers.Remove(ClientIpHeader);
        return _next(context);
    }

    // Constant-time comparison: how long a wrong guess takes to be rejected
    // must not reveal how many leading characters were right.
    private bool IsSecret(string candidate) =>
        candidate.Length > 0 &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), _secret);
}
