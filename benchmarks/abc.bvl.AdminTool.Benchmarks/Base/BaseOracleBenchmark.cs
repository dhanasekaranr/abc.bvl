using abc.bvl.AdminTool.Infrastructure.Data.Context;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Benchmarks.Base;

/// <summary>
/// Base class for benchmarks that use actual Oracle database
/// WARNING: This will create and delete real database tables!
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public abstract class BaseOracleBenchmark
{
    protected AdminDbContext? Context { get; private set; }
    
    /// <summary>
    /// Oracle connection string - override in appsettings or environment variable
    /// </summary>
    protected virtual string ConnectionString => 
        Environment.GetEnvironmentVariable("BENCHMARK_ORACLE_CONNECTION") 
        ?? "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=App_User_Pass@ss1;";

    /// <summary>
    /// Unique schema suffix to isolate benchmark runs
    /// </summary>
    protected string SchemaPrefix { get; private set; } = string.Empty;

    /// <summary>
    /// Override this to specify how many records to seed
    /// </summary>
    protected abstract int RecordCountToSeed { get; }

    /// <summary>
    /// Override this to seed your specific test data
    /// </summary>
    protected abstract Task SeedDataAsync(AdminDbContext context);

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        SchemaPrefix = $"BM_{DateTime.Now:yyyyMMddHHmmss}";
        
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseOracle(ConnectionString)
            .EnableSensitiveDataLogging(false)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        Context = new AdminDbContext(options);
        
        try
        {
            // Test connection
            await Context.Database.CanConnectAsync();
            Console.WriteLine($"✅ Connected to Oracle database");
            
            // Clean up any existing benchmark data (using high ID ranges >= 1000000)
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM APP_USER.ADMIN_SCREENPILOT WHERE SCREENPILOTID >= 2000000");
            await Context.Database.ExecuteSqlRawAsync("DELETE FROM APP_USER.ADMIN_SCREENDEFN WHERE SCREENDEFNID >= 1000000");
            Console.WriteLine($"✅ Cleaned up old benchmark data");
            
            // Seed test data
            await SeedDataAsync(Context);
            await Context.SaveChangesAsync();
            
            Console.WriteLine($"✅ Oracle benchmark setup complete: {RecordCountToSeed} records seeded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Oracle benchmark setup failed: {ex.Message}");
            Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message}");
            throw;
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (Context != null)
        {
            try
            {
                // Clean up test data (high ID ranges)
                await Context.Database.ExecuteSqlRawAsync("DELETE FROM APP_USER.ADMIN_SCREENPILOT WHERE SCREENPILOTID >= 2000000");
                await Context.Database.ExecuteSqlRawAsync("DELETE FROM APP_USER.ADMIN_SCREENDEFN WHERE SCREENDEFNID >= 1000000");
                Console.WriteLine($"✅ Oracle benchmark cleanup complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Oracle benchmark cleanup warning: {ex.Message}");
            }
            finally
            {
                await Context.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Helper method to generate random string
    /// </summary>
    protected static string GenerateRandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
