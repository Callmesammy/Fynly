namespace AiCFO.API.Middleware;

using System.Text.Json;
using AiCFO.Application.Common;

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions and returns consistent API error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse
        {
            Success = false,
            Error = new ApiError
            {
                Message = exception.Message,
                Code = exception.GetType().Name
            }
        };

        context.Response.StatusCode = exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Middleware to add request ID to logs and response headers.
/// </summary>
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string RequestIdHeader = "X-Request-ID";

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[RequestIdHeader].ToString();
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = Guid.NewGuid().ToString();
        }

        context.Items["RequestId"] = requestId;
        context.Response.Headers.Append(RequestIdHeader, requestId);

        return _next(context);
    }
}
