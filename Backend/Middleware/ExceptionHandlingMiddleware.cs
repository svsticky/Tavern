using Backend.Controllers.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Backend.Middleware;

/// <summary>
/// Middleware to handle exceptions globally and convert them into proper HTTP responses (400, 401, 403, 404, 500).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string message = exception.Message;

        switch (exception)
        {
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                break;
            case UnauthorizedAccessException:
                statusCode = context.User.Identity?.IsAuthenticated == true
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized;
                break;
            case ArgumentException:
            case InvalidOperationException:
            case ValidationException:
            case FormatException:
                statusCode = StatusCodes.Status400BadRequest;
                break;
            default:
                _logger.LogError(exception, "An unhandled exception occurred during request processing.");
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An internal server error occurred.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new ErrorResponseDto { Message = message };
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
