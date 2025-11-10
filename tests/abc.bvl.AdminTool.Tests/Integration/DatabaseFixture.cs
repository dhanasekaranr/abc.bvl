using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace abc.bvl.AdminTool.Tests.Integration;

/// <summary>
/// Base fixture for integration tests with Oracle database
/// Handles database context setup and cleanup for xUnit tests
/// </summary>
public class DatabaseFixture : IDisposable
{
    public AdminDbContext Context { get; }
    private readonly string _connectionString;

    public DatabaseFixture()
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Integration.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string - fallback to environment variable
        _connectionString = configuration.GetConnectionString("AdminDb_Integration") 
            ?? Environment.GetEnvironmentVariable("ADMIN_DB_INTEGRATION_CONNECTION")
            ?? throw new InvalidOperationException(
                "Integration test database connection string not found. " +
                "Set ADMIN_DB_INTEGRATION_CONNECTION environment variable or configure appsettings.Integration.json");

        // Configure DbContext with Oracle
        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseOracle(_connectionString);

        Context = new AdminDbContext(optionsBuilder.Options);

        // Ensure database is accessible
        try
        {
            Context.Database.CanConnect();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot connect to integration test database. Ensure Oracle DB is accessible. Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears all test data from tables (records with IDs >= 900000)
    /// </summary>
    public async Task ClearTestDataAsync()
    {
        try
        {
            // Clear change tracker to avoid entity tracking conflicts
            Context.ChangeTracker.Clear();

            // Delete in order to respect foreign keys
            await Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM APP_USER.ADMIN_SCREENPILOT WHERE SCREENPILOT_GK >= 900000");
            
            await Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM APP_USER.ADMIN_SCREENDEFN WHERE SCREEN_GK >= 900000");
            
            await Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM CVLWebTools.AdminToolOutBox WHERE ID >= 900000");
        }
        catch (Exception ex)
        {
            // Log but don't fail - might be first run
            Console.WriteLine($"Warning: Could not clear test data: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes the database context
    /// </summary>
    public void Dispose()
    {
        Context?.Dispose();
    }
}

/// <summary>
/// xUnit collection fixture for sharing database context across tests
/// </summary>
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
