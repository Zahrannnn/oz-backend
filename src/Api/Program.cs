using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.Extensions.Logging;
using Oz.Infrastructure.Data;
using Oz.Api.Filters;
using Oz.Api.Middleware;
using Oz.Api.Jobs;
using Oz.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
});

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
    "https://oz-uniform.vercel.app"
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

// Rate limiting (per IP + path, same limits as old middleware)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue) ? (int)retryAfterValue.TotalSeconds : 1;
        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Too Many Requests",
            status = 429,
            detail = $"Rate limit exceeded. Retry after {retryAfter} seconds."
        }), ct);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = http.Request.Path.Value?.ToLowerInvariant() ?? "";
        var key = $"{ip}:{path}";

        FixedWindowRateLimiterOptions limits;
        if (path.StartsWith("/api/v1/admin/"))
            limits = new() { AutoReplenishment = true, PermitLimit = 30, Window = TimeSpan.FromSeconds(60) };
        else if (path == "/api/v1/orders" && http.Request.Method == "POST")
            limits = new() { AutoReplenishment = true, PermitLimit = 5, Window = TimeSpan.FromSeconds(1) };
        else if (path.StartsWith("/api/v1/"))
            limits = new() { AutoReplenishment = true, PermitLimit = 60, Window = TimeSpan.FromSeconds(60) };
        else
            return RateLimitPartition.GetNoLimiter(key);

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => limits);
    });
});

// Application services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuditLogService>();

// Email service
builder.Services.AddScoped<SmtpEmailService>();

// Bosta client
builder.Services.AddHttpClient<BostaClient>();

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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSecurityHeaders();
app.UseRateLimiter();
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
RecurringJob.AddOrUpdate<AutoCancelOrdersJob>(
    "auto-cancel-orders",
    job => job.ExecuteAsync(null!),
    Cron.Daily(3));

app.Run();
