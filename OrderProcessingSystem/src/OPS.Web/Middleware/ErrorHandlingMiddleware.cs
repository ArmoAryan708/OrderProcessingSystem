using System.Net;
using System.Text.Json;
using OPS.Domain.Exceptions;

namespace OPS.Web.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, details) = exception switch
        {
            NotFoundException ex         => (HttpStatusCode.NotFound,           ex.ErrorCode,                     ex.Message, (object?)null),
            OrderNotCancellableException ex  => (HttpStatusCode.UnprocessableEntity, ex.ErrorCode,               ex.Message, (object?)null),
            InvalidStatusTransitionException ex => (HttpStatusCode.UnprocessableEntity, ex.ErrorCode,            ex.Message, (object?)null),
            Domain.Exceptions.ValidationException ex => (HttpStatusCode.BadRequest, ex.ErrorCode,                ex.Message, (object?)ex.Errors),
            _                            => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR",         "An unexpected error occurred.", (object?)null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { error = errorCode, message, details };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
