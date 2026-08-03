using System.Diagnostics;
using AssignmentHub.Api.Contracts;

namespace AssignmentHub.Api.Middleware;

/// <summary>
/// Catches anything that escapes the pipeline, logs it once with a correlation
/// id, and returns the same <see cref="ApiErrorResponse"/> shape the rest of the
/// API uses. Registered first so it wraps every downstream component.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path} (traceId: {TraceId})",
                context.Request.Method,
                context.Request.Path.Value,
                traceId);

            if (context.Response.HasStarted)
            {
                // Headers are already on the wire, so a JSON body would corrupt the
                // response. Rethrow and let Kestrel abort the connection instead.
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ApiErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred while processing your request.",
                // Stack traces stay out of non-development responses; the traceId is
                // the supported way to correlate a report with the server log.
                Detail = _environment.IsDevelopment()
                    ? exception.ToString()
                    : "Quote the traceId when reporting this error.",
                TraceId = traceId
            };

            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
