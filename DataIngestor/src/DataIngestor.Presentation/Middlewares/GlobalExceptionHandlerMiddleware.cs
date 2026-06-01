using Microsoft.AspNetCore.Mvc;

namespace DataIngestor.Presentation.Middlewares;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception while processing {Method} {Path} (traceId={TraceId})",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response has already started; cannot write error payload for traceId={TraceId}",
                context.TraceIdentifier);
            return;
        }

        var (statusCode, title) = MapStatus(ex);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path,
            Detail = _environment.IsDevelopment() ? ex.ToString() : ex.Message,
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            context.RequestAborted);
    }

    private static (int StatusCode, string Title) MapStatus(Exception ex) => ex switch
    {
        InvalidOperationException => (StatusCodes.Status409Conflict, "Invalid operation"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
    };
}
