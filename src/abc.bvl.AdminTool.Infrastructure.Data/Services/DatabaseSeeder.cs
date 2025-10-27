using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

        // Ensure database is created (for in-memory this is automatic)
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Check if data already exists
        if (await context.ScreenDefinitions.AnyAsync(cancellationToken))
        {
            return; // Data already seeded
        }

        // Seed sample screen definitions
        var screenDefinitions = new[]
        {
            new ScreenDefinition
            {
                Id = 1,
                Name = "Orders Management",
                Status = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenDefinition
            {
                Id = 2,
                Name = "Customer Portal",
                Status = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenDefinition
            {
                Id = 3,
                Name = "Inventory Control",
                Status = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenDefinition
            {
                Id = 4,
                Name = "Financial Dashboard",
                Status = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "system",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            }
        };

        context.ScreenDefinitions.AddRange(screenDefinitions);

        // Seed sample screen pilots
        var screenPilots = new[]
        {
            new ScreenPilot
            {
                Id = 1,
                ScreenDefnId = 1,
                UserId = "john.doe",
                Status = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenPilot
            {
                Id = 2,
                ScreenDefnId = 1,
                UserId = "jane.smith",
                Status = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenPilot
            {
                Id = 3,
                ScreenDefnId = 2,
                UserId = "john.doe",
                Status = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            },
            new ScreenPilot
            {
                Id = 4,
                ScreenDefnId = 4,
                UserId = "mary.johnson",
                Status = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "admin"
            }
        };

        context.ScreenPilots.AddRange(screenPilots);

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
