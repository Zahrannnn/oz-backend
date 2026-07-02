using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using oz_backend.Data;
using oz_backend.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// EF Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Hangfire dashboard (dev: localhost only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireLocalhostAuthorizationFilter()],
});

// Recurring job placeholder
RecurringJob.AddOrUpdate(
    "heartbeat",
    () => Console.WriteLine($"Hangfire heartbeat: {DateTime.UtcNow:O}"),
    Cron.Minutely);

app.MapControllers();

app.Run();

public class HangfireLocalhostAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.Connection.RemoteIpAddress?.IsLoopback == true;
    }
}
