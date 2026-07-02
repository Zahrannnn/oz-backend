using System.Net;
using System.Text.Json;

namespace Oz.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            title = "An error occurred while processing your request.",
            status = context.Response.StatusCode,
            instance = $"{context.Request.Method} {context.Request.Path}",
        };

        var json = JsonSerializer.Serialize(problem);
        return context.Response.WriteAsync(json);
    }
}
