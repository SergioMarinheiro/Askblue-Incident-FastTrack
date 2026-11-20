using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Exceptions;

// The one place that turns exceptions into HTTP responses (ProblemDetails, RFC 9457).
// Services throw; controllers don't catch. Registered in Program.cs with
// AddExceptionHandler<GlobalExceptionHandler>() and run by app.UseExceptionHandler().
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Order matters: ConflictException is a BusinessRuleException, so it must come first.
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict."),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated."),
            // The service stubs that are not written yet.
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Not implemented yet."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        var isOurException = exception is NotFoundException or ForbiddenException or BusinessRuleException;

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            // Returning true below stops the middleware from logging it, so log it here.
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // Only our own exceptions carry a message meant for the client.
                // Anything else may contain SQL, file paths or other internals.
                Detail = isOurException ? exception.Message : null
            }
        });
    }
}
