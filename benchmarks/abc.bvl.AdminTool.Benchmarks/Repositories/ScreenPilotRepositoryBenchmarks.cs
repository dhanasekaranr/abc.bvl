using abc.bvl.AdminTool.Benchmarks.Base;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using BenchmarkDotNet.Attributes;

namespace abc.bvl.AdminTool.Benchmarks.Repositories;

/// <summary>
/// Benchmarks for ScreenPilotRepository operations
/// Tests repository performance with various data sizes
/// </summary>
public class ScreenPilotRepositoryBenchmarks : BaseRepositoryBenchmark
{
    private ScreenPilotRepository? _repository;
    private List<long> _userIds = new();

    [Params(1000, 10000, 100000)]
    public int RecordCount { get; set; }

    protected override int RecordCountToSeed => RecordCount;

    protected override async Task SeedDataAsync(AdminDbContext context)
    {
        // Seed screen definitions
        var screens = new List<ScreenDefinition>();
        for (int i = 1; i <= 100; i++)
        {
            screens.Add(new ScreenDefinition
            {
                ScreenGk = i,
                ScreenName = $"Screen {i}",
                StatusId = 1,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 1000,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 1000
            });
        }
        await context.ScreenDefinitions.AddRangeAsync(screens);
        await context.SaveChangesAsync();

        // Seed screen pilots (user assignments)
        var pilots = new List<ScreenPilot>();
        var random = new Random(42); // Fixed seed for consistent results
        long pilotGk = 1;
        
        for (int i = 0; i < RecordCount; i++)
        {
            var userId = i % 10000; // Reuse userIds to simulate multiple assignments
            if (!_userIds.Contains(userId))
            {
                _userIds.Add(userId);
            }

            pilots.Add(new ScreenPilot
            {
                ScreenPilotGk = pilotGk++,
                ScreenGk = random.Next(1, 101),
                NbUserGk = userId,
                StatusId = 1,
                DualMode = 0,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 1000,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 1000
            });
        }

        await context.ScreenPilots.AddRangeAsync(pilots);
        await context.SaveChangesAsync();
        
        // Initialize repository after seeding
        _repository = new ScreenPilotRepository(context);
    }

    [Benchmark(Description = "GetByUserIdAsync - Single user query")]
    public async Task<int> GetByUserIdAsync()
    {
        var userId = _userIds[0];
        var result = await _repository!.GetByUserIdAsync(userId);
        return result.Count();
    }

    [Benchmark(Description = "GetByScreenGkAsync - Single screen query")]
    public async Task<int> GetByScreenGkAsync()
    {
        var result = await _repository!.GetByScreenGkAsync(1);
        return result.Count();
    }

    [Benchmark(Description = "GetAllAsync - Load all active pilots")]
    public async Task<int> GetAllAsync()
    {
        var result = await _repository!.GetAllAsync(statusId: 1);
        return result.Count();
    }

    [Benchmark(Baseline = true, Description = "GetAllQueryable - IQueryable (no execution)")]
    public int GetAllQueryable()
    {
        var query = _repository!.GetAllQueryable(statusId: 1);
        return query.Count(); // Forces execution for benchmark
    }
}
