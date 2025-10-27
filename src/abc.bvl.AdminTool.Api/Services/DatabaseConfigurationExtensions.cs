using abc.bvl.AdminTool.Api.Configuration;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace abc.bvl.AdminTool.Api.Services;

public static class DatabaseConfigurationExtensions
{
    public static IServiceCollection AddConfigurableDatabase(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var databaseSettings = configuration
            .GetSection(DatabaseSettings.SectionName)
            .Get<DatabaseSettings>() ?? new DatabaseSettings();

        services.Configure<DatabaseSettings>(
            configuration.GetSection(DatabaseSettings.SectionName));

        switch (databaseSettings.Provider.ToUpperInvariant())
        {
            case "ORACLE":
                // Register DbContext with Oracle - uses primary by default
                // For dual-database support, routing logic can be added via middleware
                services.AddDbContext<AdminDbContext>(options =>
                {
                    var primaryConnectionString = configuration.GetConnectionString("AdminDb_Primary");
                    options.UseOracle(primaryConnectionString);
                    options.EnableSensitiveDataLogging(false);
                    options.EnableDetailedErrors(false);
                });
                break;
            
            case "INMEMORY":
            default:
                services.AddDbContext<AdminDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AdminToolDb_{Guid.NewGuid()}");
                    options.EnableSensitiveDataLogging(true);
                });
                break;
        }

        return services;
    }
}