using System.Collections.Concurrent;
using System.Text.Json;

namespace Oz.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();

    public RateLimitingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{path}";

        int limit, windowSeconds;
        if (path.StartsWith("/api/v1/admin/"))
        {
            limit = 30; windowSeconds = 60;
        }
        else if (path == "/api/v1/orders" && context.Request.Method == "POST")
        {
            limit = 5; windowSeconds = 1;
        }
        else if (path.StartsWith("/api/v1/"))
        {
            limit = 60; windowSeconds = 60;
        }
        else
        {
            await _next(context); return;
        }

        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry(windowSeconds));

        if (!entry.TryConsume(limit, out var retryAfter))
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Retry after {retryAfter} seconds."
            }));
            return;
        }

        await _next(context);
    }
}
