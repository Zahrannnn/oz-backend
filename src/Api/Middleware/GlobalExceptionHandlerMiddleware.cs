using System.Net;
using FluentValidation;

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
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            title = "Validation Failed",
            status = context.Response.StatusCode,
            detail = "One or more validation errors occurred",
            instance = context.Request.Path,
            errors
        };

        await context.Response.WriteAsJsonAsync(problem);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception");

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807#section-3.1",
            title = "Internal Server Error",
            status = context.Response.StatusCode,
            detail = "An unexpected error occurred",
            instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
