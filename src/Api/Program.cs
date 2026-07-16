using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Oz.Infrastructure.Data;
using Oz.Infrastructure.Repositories;
using Oz.Domain.Repositories;
using Oz.Api.Filters;
using Oz.Api.Middleware;
using Oz.Api.Jobs;
using Oz.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var problem = new ValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1",
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = "One or more validation errors occurred",
                Instance = context.HttpContext.Request.Path,
                Errors = errors
            };

            return new UnprocessableEntityObjectResult(problem);
        };
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// EF Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not configured.")));

// CORS
var allowedOrigins = new[]
{
    "http://localhost:3000",
    "http://localhost:3001",
    "https://oz-frontend.vercel.app",
    "https://oz-storefront.vercel.app"
};
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// FluentValidation - register validators + auto-validate on controller actions
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddFluentValidationAutoValidation();

// Hangfire - MSSQL-backed job storage
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? throw new InvalidOperationException("Connection string 'Default' not configured.")));

builder.Services.AddHangfireServer();

// JWT auth config
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT secret not configured");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "oz-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "oz-admin",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ") == true)
                    context.Token = authHeader["Bearer ".Length..];
                else
                    context.Token = context.Request.Cookies["admin_session"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<IdempotencyService>();

// Generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Email service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Bosta client
builder.Services.AddHttpClient<IBostaClient, BostaClient>();

// Hangfire jobs
builder.Services.AddScoped<SendEmailJob>();
builder.Services.AddScoped<AutoCancelOrdersJob>();
builder.Services.AddScoped<SendNotifyMeEmailsJob>();

// Seed initial admin on first run
builder.Services.AddHostedService<AdminInitializer>();

var app = builder.Build();

EnvironmentValidator.Validate(builder.Configuration, app.Environment,
    app.Services.GetRequiredService<ILogger<Program>>());

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSecurityHeaders();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseCors();

// Static files - serve product images from /uploads (content-root relative)
var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

// OpenAPI spec endpoint (built-in minimal API; controllers not auto-included on .NET 10)
app.MapOpenApi();

// API route inventory - lists every controller route for discoverability
app.MapGet("/swagger", (HttpContext ctx) =>
{
    var endpoints = ctx.RequestServices
        .GetRequiredService<EndpointDataSource>()
        .Endpoints
        .OfType<RouteEndpoint>()
        .Select(e =>
        {
            var methods = string.Join(",", e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? new[] { "GET" });
            var pattern = e.RoutePattern.RawText ?? "";
            var displayName = e.DisplayName ?? "";
            return new { methods, pattern, displayName };
        })
        .Where(x => x.pattern.StartsWith("api/") || x.pattern.StartsWith("hangfire") || x.pattern == "swagger")
        .OrderBy(x => x.pattern)
        .ToList();

    var rows = string.Join("", endpoints.Select(e =>
        $"<tr><td>{e.methods}</td><td><code>{e.pattern}</code></td><td><small>{e.displayName}</small></td></tr>"));

    return Results.Content($$"""
    <!DOCTYPE html>
    <html lang="en">
    <head><meta charset="utf-8"><title>Oz Backend - API Routes</title>
    <style>body{font-family:system-ui;max-width:1100px;margin:30px auto;padding:0 20px;color:#222}
    h1{margin:0 0 6px}small{color:#666}table{border-collapse:collapse;width:100%;margin-top:20px;font-size:14px}
    th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background:#f5f5f5}
    code{background:#f0f0f0;padding:2px 6px;border-radius:3px}
    .pill{display:inline-block;padding:2px 8px;border-radius:10px;font-size:11px;font-weight:600}
    .GET{background:#dbeafe;color:#1e40af}.POST{background:#dcfce7;color:#166534}
    .PUT{background:#fef3c7;color:#854d0e}.DELETE{background:#fee2e2;color:#991b1b}</style>
    </head><body>
    <h1>Oz Backend — API Routes</h1>
    <small>{{endpoints.Count}} endpoints. See <code>docs/api/*.md</code> for full reference. Swagger UI requires .NET 10-compatible Swashbuckle (not yet available).</small>
    <table><tr><th>Method</th><th>Path</th><th>Handler</th></tr>{{rows}}</table>
    </body></html>
    """, "text/html; charset=utf-8");
}).ExcludeFromDescription();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (localhost only in dev)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
    IgnoreAntiforgeryToken = true
});

app.MapControllers();

// Recurring jobs
RecurringJob.AddOrUpdate(
    "heartbeat",
    () => Console.WriteLine($"[Hangfire heartbeat] {DateTime.UtcNow:O}"),
    Cron.Minutely);

RecurringJob.AddOrUpdate<AutoCancelOrdersJob>(
    "auto-cancel-orders",
    job => job.ExecuteAsync(),
    Cron.Daily(3));

app.Run();
