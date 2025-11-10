using abc.bvl.AdminTool.Infrastructure.Data.Context;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace abc.bvl.AdminTool.Benchmarks.Base;

/// <summary>
/// Base class for repository benchmarks
/// Provides in-memory database setup and cleanup
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5)]
public abstract class BaseRepositoryBenchmark
{
    protected AdminDbContext? Context { get; private set; }
    protected ILogger Logger { get; private set; } = NullLogger.Instance;

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
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"BenchmarkDb_{Guid.NewGuid()}")
            .Options;

        Context = new AdminDbContext(options);
        
        // Seed data
        await SeedDataAsync(Context);
        await Context.SaveChangesAsync();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        Context?.Database.EnsureDeleted();
        Context?.Dispose();
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

    /// <summary>
    /// Helper method to generate random user ID
    /// </summary>
    protected static string GenerateUserId(int index)
    {
        return $"user{index:D6}";
    }
}
