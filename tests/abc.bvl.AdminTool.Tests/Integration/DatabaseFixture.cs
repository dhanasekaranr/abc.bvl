using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace abc.bvl.AdminTool.Tests.Integration;

/// <summary>
/// Base fixture for integration tests with Oracle database
/// Handles database context setup and cleanup for xUnit tests
/// </summary>
public class DatabaseFixture : IDisposable
{
    public AdminDbPrimaryContext Context { get; }
    public ICurrentDbContextProvider ContextProvider { get; }
    private readonly string _connectionString;

    public DatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Integration.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = configuration.GetConnectionString("AdminDb_Integration") 
            ?? Environment.GetEnvironmentVariable("ADMIN_DB_INTEGRATION_CONNECTION")
            ?? throw new InvalidOperationException(
                "Integration test database connection string not found. " +
                "Set ADMIN_DB_INTEGRATION_CONNECTION environment variable or configure appsettings.Integration.json");

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbPrimaryContext>();
        optionsBuilder.UseOracle(_connectionString);
        Context = new AdminDbPrimaryContext(optionsBuilder.Options);

        // Provide a test context provider that always returns this context
        ContextProvider = new TestDbContextProvider(Context);

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

    private class TestDbContextProvider : ICurrentDbContextProvider
    {
        private readonly AdminDbPrimaryContext _context;
        public TestDbContextProvider(AdminDbPrimaryContext context) => _context = context;
        public AdminDbContext GetContext() => _context;
        public void SetContextType(DatabaseContextType contextType) { /* no-op for tests */ }
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
