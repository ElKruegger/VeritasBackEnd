using LuxuryStore.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LuxuryStore_Backend.Middleware;

/// <summary>
/// Centralized error-handling middleware (brief section 3): every unhandled
/// exception from a controller or an Application service passes through
/// here exactly once and comes out as a consistent ProblemDetails response,
/// instead of each controller action wrapping its own body in try/catch.
///
/// This is the IExceptionHandler pattern introduced in .NET 8 — registered
/// with builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()
/// and app.UseExceptionHandler() in Program.cs, no lambda needed.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // Only log the exception itself for the cases we didn't anticipate —
        // a 404/409 from a NotFoundException/ConflictException is expected
        // application flow, not something that needs an error-level log entry.
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Never leak internal exception details for a 500 — do surface
            // the message for the exceptions we threw on purpose (404/409),
            // since that message is written for the API consumer to read.
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred. Please try again later."
                : exception.Message,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
