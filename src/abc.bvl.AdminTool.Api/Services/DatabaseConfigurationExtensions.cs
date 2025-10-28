using abc.bvl.AdminTool.Api.Configuration;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace abc.bvl.AdminTool.Api.Services;

public static class DatabaseConfigurationExtensions
{
    /// <summary>
    /// Registers both Primary and Secondary DbContexts for dual-database routing
    /// Uses keyed services (.NET 8+) to register multiple DbContext instances
    /// </summary>
    public static IServiceCollection AddConfigurableDatabase(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var databaseSettings = configuration
            .GetSection(DatabaseSettings.SectionName)
            .Get<DatabaseSettings>() ?? new DatabaseSettings();

        services.Configure<DatabaseSettings>(
            configuration.GetSection(DatabaseSettings.SectionName));

        var provider = databaseSettings.Provider.ToUpperInvariant();
        
        if (provider != "ORACLE")
        {
            throw new InvalidOperationException(
                $"Only Oracle provider is supported in production. Current provider: {provider}. " +
                "For testing, use InMemory databases directly in test projects.");
        }

        // Get connection strings
        var primaryConnectionString = configuration.GetConnectionString("AdminDb_Primary");
        var secondaryConnectionString = configuration.GetConnectionString("AdminDb_Secondary");

        if (string.IsNullOrWhiteSpace(primaryConnectionString))
        {
            throw new InvalidOperationException("AdminDb_Primary connection string is required");
        }

        // Register PRIMARY DbContext (keyed service)
        services.AddKeyedScoped<AdminDbContext>("Primary", (serviceProvider, key) =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
            optionsBuilder.UseOracle(primaryConnectionString);
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableDetailedErrors(false);
            
            return new AdminDbContext(optionsBuilder.Options);
        });

        // Register SECONDARY DbContext (keyed service) - if connection string provided
        if (!string.IsNullOrWhiteSpace(secondaryConnectionString))
        {
            services.AddKeyedScoped<AdminDbContext>("Secondary", (serviceProvider, key) =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
                optionsBuilder.UseOracle(secondaryConnectionString);
                optionsBuilder.EnableSensitiveDataLogging(false);
                optionsBuilder.EnableDetailedErrors(false);
                
                return new AdminDbContext(optionsBuilder.Options);
            });
        }
        else
        {
            // If no secondary connection, register Primary as Secondary fallback
            services.AddKeyedScoped<AdminDbContext>("Secondary", (serviceProvider, key) =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
                optionsBuilder.UseOracle(primaryConnectionString);
                optionsBuilder.EnableSensitiveDataLogging(false);
                optionsBuilder.EnableDetailedErrors(false);
                
                return new AdminDbContext(optionsBuilder.Options);
            });
        }

        // Register DbContextResolver for routing logic
        services.AddScoped<IDbContextResolver, DbContextResolver>();

        return services;
    }
}