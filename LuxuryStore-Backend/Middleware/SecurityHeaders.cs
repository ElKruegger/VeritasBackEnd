namespace LuxuryStore_Backend.Middleware;

/// <summary>
/// Response headers that tell the browser to switch on protections it
/// would otherwise leave off. This API only ever returns JSON, so the
/// policy can be as strict as possible.
///
///   X-Content-Type-Options: nosniff
///       The browser must trust Content-Type and never "guess" that a
///       JSON response is actually HTML/JS and execute it.
///   Content-Security-Policy: default-src 'none'; frame-ancestors 'none'
///       Nothing (script, style, image...) may be loaded by a response of
///       this API, and no site may embed it in a frame (clickjacking).
///   X-Frame-Options: DENY
///       Same anti-framing rule for older browsers that ignore CSP.
///   Referrer-Policy: no-referrer
///       Never leak this API's URLs (which can contain ids) to other sites.
///   Permissions-Policy
///       Camera, microphone, geolocation... are never needed here.
///
/// HSTS (Strict-Transport-Security) is NOT set here: app.UseHsts() in
/// Program.cs handles it, only outside Development and only on HTTPS.
/// </summary>
public static class SecurityHeaders
{
    private const string StrictCsp = "default-src 'none'; frame-ancestors 'none'";

    // Scalar (development only) is an HTML page that needs its own scripts
    // and styles; it only keeps the anti-framing part of the policy.
    private const string DocsCsp = "frame-ancestors 'none'";

    public static void Apply(IHeaderDictionary headers, PathString path)
    {
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers.ContentSecurityPolicy = IsDocsPath(path) ? DocsCsp : StrictCsp;
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    }

    private static bool IsDocsPath(PathString path) =>
        path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi");

    /// <summary>
    /// Adds the headers right before the response starts. Registering them
    /// with OnStarting (instead of setting them before calling next) matters:
    /// UseExceptionHandler clears every header when it turns an exception
    /// into a Problem Details response, and the error responses must be
    /// protected too.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                Apply(context.Response.Headers, context.Request.Path);
                return Task.CompletedTask;
            });
            await next(context);
        });
}
