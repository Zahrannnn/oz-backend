using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Oz.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private const int SlowMs = 5000;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
            correlationId = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object> { ["correlation_id"] = correlationId }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                LogRequest(context, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private void LogRequest(HttpContext context, long elapsedMs)
    {
        var status = context.Response.StatusCode;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var path = context.Request.Path + context.Request.QueryString.Value;

        if (status >= 500)
        {
            _logger.LogError("HTTP {Method} {Path} -> {Status} in {DurationMs}ms, client {ClientIp}, admin {AdminId}",
                context.Request.Method, path, status, elapsedMs,
                context.Connection.RemoteIpAddress, userId);
        }
        else if (elapsedMs >= SlowMs)
        {
            _logger.LogWarning("HTTP {Method} {Path} -> {Status} in {DurationMs}ms (slow), client {ClientIp}, admin {AdminId}",
                context.Request.Method, path, status, elapsedMs,
                context.Connection.RemoteIpAddress, userId);
        }
        else
        {
            _logger.LogInformation("HTTP {Method} {Path} -> {Status} in {DurationMs}ms, client {ClientIp}, admin {AdminId}",
                context.Request.Method, path, status, elapsedMs,
                context.Connection.RemoteIpAddress, userId);
        }
    }
}
