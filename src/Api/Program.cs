using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
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

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
