using Microsoft.EntityFrameworkCore;
using Oz.Domain.Entities;
using Oz.Infrastructure.Data;

namespace Oz.Api.Services;

public class AdminInitializer : IHostedService
{
    private readonly IServiceProvider _services;

    public AdminInitializer(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Admins.AnyAsync(cancellationToken))
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("admin123", 12);

            db.Admins.Add(new Admin
            {
                Id = Guid.NewGuid(),
                Email = "admin@oz.com",
                PasswordHash = hash
            });
            await db.SaveChangesAsync(cancellationToken);
            Console.WriteLine("[AdminInitializer] Default admin created: admin@oz.com / admin123");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
