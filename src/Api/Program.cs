using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Oz.Infrastructure.Data;
using Oz.Infrastructure.Repositories;
using Oz.Domain.Repositories;
using Oz.Api.Filters;
using Oz.Api.Middleware;
using System.Reflection;

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

// CORS - locked to Vercel origin
var vercelOrigin = Environment.GetEnvironmentVariable("VERCEL_ORIGIN") ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(vercelOrigin)
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

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSecurityHeaders();
app.UseCors();

// OpenAPI spec endpoint
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger UI - serves HTML from CDN, reads spec from built-in OpenAPI
app.MapGet("/swagger", () => Results.Content("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Oz Backend API - Swagger UI</title>
    <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
    <script>
        window.onload = function() {
            window.ui = SwaggerUIBundle({
                url: "/openapi/v1.json",
                dom_id: "#swagger-ui",
                presets: [SwaggerUIBundle.presets.apis],
                layout: "BaseLayout"
            });
        };
    </script>
</body>
</html>
""", "text/html; charset=utf-8")).ExcludeFromDescription();

app.UseHttpsRedirection();
app.UseAuthorization();

// Hangfire dashboard (localhost only in dev)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
    IgnoreAntiforgeryToken = true
});

app.MapControllers();

// Recurring job placeholder
RecurringJob.AddOrUpdate(
    "heartbeat",
    () => Console.WriteLine($"[Hangfire heartbeat] {DateTime.UtcNow:O}"),
    Cron.Minutely);

app.Run();
