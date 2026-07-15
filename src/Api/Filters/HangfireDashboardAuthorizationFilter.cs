using System.Security.Claims;
using Hangfire.Dashboard;

namespace Oz.Api.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var remoteIp = httpContext.Connection.RemoteIpAddress;

        if (remoteIp is not null && System.Net.IPAddress.IsLoopback(remoteIp))
            return true;

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader["Bearer ".Length..];
            return token.Length > 20;
        }

        var cookie = httpContext.Request.Cookies["admin_session"];
        return !string.IsNullOrEmpty(cookie) && cookie.Length > 20;
    }
}