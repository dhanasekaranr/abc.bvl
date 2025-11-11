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
        
        var options = new DbContextOptionsBuilder<abc.bvl.AdminTool.Infrastructure.Data.Context.AdminDbPrimaryContext>()
            .UseOracle(ConnectionString)
            .EnableSensitiveDataLogging(false)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        Context = new abc.bvl.AdminTool.Infrastructure.Data.Context.AdminDbPrimaryContext(options);
        
        try
        {
            // Test connection
            await Context.Database.CanConnectAsync();
            Console.WriteLine($"✅ Connected to Oracle database");
            
            // ⚠️ NOTE: Oracle benchmarks use EXISTING data - no cleanup/seeding
            // The benchmark runs on real production-like data already in the database
            // This provides realistic performance metrics with actual data distribution
            
            // Verify data exists
            var screenCount = await Context.ScreenDefinitions.CountAsync();
            var pilotCount = await Context.ScreenPilots.CountAsync();
            
            Console.WriteLine($"📊 Using existing data: {screenCount} screens, {pilotCount} pilot assignments");
            
            if (pilotCount == 0)
            {
                Console.WriteLine($"⚠️ WARNING: No pilot data found in database!");
                Console.WriteLine($"   Benchmarks will run but may not be representative.");
                Console.WriteLine($"   Consider seeding data or running in-memory benchmarks instead.");
            }
            
            Console.WriteLine($"✅ Oracle benchmark setup complete - using {pilotCount} existing records");
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
                // ⚠️ NOTE: Oracle benchmarks do NOT clean up data
                // Data remains in database for subsequent benchmark runs
                // This allows benchmarks to run on consistent, realistic datasets
                
                Console.WriteLine($"✅ Oracle benchmark cleanup complete (no data deleted)");
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
