using abc.bvl.AdminTool.Api.Configuration;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Providers;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Api.Services;

public static class DatabaseConfigurationExtensions
{
    /// <summary>
    /// Registers both Primary and Secondary DbContexts for dual-database routing
    /// Uses middleware-based context provider pattern for request-scoped routing
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

        // Register PRIMARY DbContext - APP_USER schema
        services.AddDbContext<AdminDbPrimaryContext>(options =>
        {
            options.UseOracle(primaryConnectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        }, ServiceLifetime.Scoped);

        // Register SECONDARY DbContext - CVLWEBTOOLS schema
        // If no secondary connection, use primary connection with different schema
        var effectiveSecondaryConnection = string.IsNullOrWhiteSpace(secondaryConnectionString) 
            ? primaryConnectionString 
            : secondaryConnectionString;

        services.AddDbContext<AdminDbSecondaryContext>(options =>
        {
            options.UseOracle(effectiveSecondaryConnection);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        }, ServiceLifetime.Scoped);

        // Register the current context provider for request-scoped routing
        services.AddScoped<ICurrentDbContextProvider, CurrentDbContextProvider>();

        return services;
    }
}