using abc.bvl.AdminTool.Infrastructure.Replication.Configuration;
using abc.bvl.AdminTool.Infrastructure.Replication.Context;
using abc.bvl.AdminTool.Infrastructure.Replication.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Replication.Repositories;
using abc.bvl.AdminTool.Infrastructure.Replication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace abc.bvl.AdminTool.Infrastructure.Replication.Extensions;

/// <summary>
/// Extension methods for registering Outbox pattern services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Outbox pattern services including the background processor
    /// </summary>
    public static IServiceCollection AddOutboxPattern(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var outboxSettings = new OutboxSettings();
        configuration.GetSection("Outbox").Bind(outboxSettings);
        services.Configure<OutboxSettings>(configuration.GetSection("Outbox"));

        // Register SecondaryDbContext
        if (!string.IsNullOrEmpty(outboxSettings.SecondaryConnectionString))
        {
            services.AddDbContext<SecondaryDbContext>(options =>
                options.UseOracle(outboxSettings.SecondaryConnectionString));
        }

        // Register services
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IEntityReplicator, EntityReplicator>();

        // Register background processor as hosted service only if enabled
        if (outboxSettings.Enabled)
        {
            services.AddHostedService<OutboxProcessor>();
        }

        return services;
    }
}
