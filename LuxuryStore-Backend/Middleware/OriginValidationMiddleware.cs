namespace LuxuryStore_Backend.Middleware;

/// <summary>
/// CSRF defense for cookie-authenticated, state-changing requests.
///
/// Why it's needed: the auth cookies are SameSite=None (the Vercel frontend
/// and the Render API are different sites), so the browser attaches them to
/// cross-site requests too. CORS doesn't stop that — it only hides the
/// response from the attacker's script; a cross-site form POST or
/// no-preflight fetch still reaches the endpoint and still runs.
///
/// What it does (OWASP "verify origin with standard headers"): for POST,
/// PUT, PATCH and DELETE, the browser-supplied Origin header (or, if absent,
/// the origin of the Referer) must be either this API's own origin (Scalar
/// in development) or one of the CORS allowed origins (the frontend).
/// Anything else is rejected with 403 before authentication or the
/// controller run. Browsers always send Origin on cross-origin POSTs and a
/// page cannot forge it, so a malicious site can never pass.
///
/// Requests with neither header pass: they don't come from a browser page
/// (curl, Postman, server-to-server, the future Stripe webhook), and
/// non-browser clients can't ride a victim's cookies — CSRF is a
/// browser-only attack. An endpoint that must accept cross-site browser
/// posts anyway can opt out with [SkipOriginValidation].
///
/// Chosen over a mandatory custom header (X-Requested-With): same
/// protection, but no client — frontend, Scalar, .http files — has to
/// remember to add anything.
/// </summary>
public class OriginValidationMiddleware(RequestDelegate next, string[] allowedOrigins, ILogger<OriginValidationMiddleware> logger)
{
    private readonly HashSet<string> _allowedOrigins = new(allowedOrigins, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsStateChanging(context.Request.Method) ||
            context.GetEndpoint()?.Metadata.GetMetadata<SkipOriginValidationAttribute>() is not null)
        {
            await next(context);
            return;
        }

        var origin = GetRequestOrigin(context.Request);
        if (origin is null || IsTrusted(origin, context.Request))
        {
            await next(context);
            return;
        }

        logger.LogWarning("Rejected cross-site {Method} {Path} from origin {Origin}",
            context.Request.Method, context.Request.Path, origin);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = "Cross-site request rejected.",
            Instance = context.Request.Path
        });
    }

    private static bool IsStateChanging(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    /// <summary>
    /// Origin header if present, otherwise scheme://host[:port] of the
    /// Referer, otherwise null (not a browser page). "null" (sandboxed
    /// iframes, file://) is returned as-is so it is never trusted.
    /// </summary>
    private static string? GetRequestOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
            return origin.TrimEnd('/');

        var referer = request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            return uri.GetLeftPart(UriPartial.Authority);

        return null;
    }

    private bool IsTrusted(string origin, HttpRequest request)
    {
        // Same origin as the API itself (e.g. Scalar at /scalar). Scheme and
        // Host are the client-facing ones thanks to UseForwardedHeaders.
        var self = $"{request.Scheme}://{request.Host}";
        return string.Equals(origin, self, StringComparison.OrdinalIgnoreCase) ||
               _allowedOrigins.Contains(origin);
    }
}

/// <summary>
/// Opts an endpoint out of OriginValidationMiddleware — only for endpoints
/// that are not cookie-authenticated and must accept cross-site posts
/// (e.g. a payment provider webhook verified by its own signature).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipOriginValidationAttribute : Attribute;
