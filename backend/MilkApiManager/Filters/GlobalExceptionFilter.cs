using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MilkApiManager.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions in controllers
/// and returns a standardized ProblemDetails response.
/// Replaces per-action try-catch blocks with a consistent error format.
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception,
            "Unhandled exception on {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        var statusCode = context.Exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.NotFound
                => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                400 => "Bad Request",
                403 => "Forbidden",
                404 => "Not Found",
                _ => "Internal Server Error"
            },
            Detail = _environment.IsDevelopment() ? context.Exception.Message : "An unexpected error occurred.",
            Instance = context.HttpContext.Request.Path
        };

        // Include trace ID for correlation
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
