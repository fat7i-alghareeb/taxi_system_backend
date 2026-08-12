using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Taxi.Api.Infrastructure;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // The full exception belongs in the logs, never in the response. Npgsql, Stripe and
        // token-validation messages leak schema names, constraint names, file paths and
        // provider internals to anyone who can trigger a 500.
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var isDevelopment = environment.IsDevelopment();

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = isDevelopment ? exception.GetType().Name : "InternalServerError",
                Title = "Application error",
                Detail = isDevelopment
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
            },
        });
    }
}
