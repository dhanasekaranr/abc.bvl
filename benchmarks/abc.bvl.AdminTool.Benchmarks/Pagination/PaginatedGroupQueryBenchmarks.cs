using abc.bvl.AdminTool.Application.Common.Pagination;
using abc.bvl.AdminTool.Benchmarks.Base;
using abc.bvl.AdminTool.Contracts.PilotEnablement;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Benchmarks.Pagination;

/// <summary>
/// Benchmarks comparing traditional pagination vs PaginatedGroupQuery
/// Demonstrates the performance improvement of database-level pagination
/// </summary>
[SimpleJob(warmupCount: 1, iterationCount: 3)]  // Fast execution
public class PaginatedGroupQueryBenchmarks : BasePaginationBenchmark
{
    private ScreenPilotRepository? _pilotRepository;
    private ScreenDefinitionRepository? _screenRepository;
    private Dictionary<long, string> _screenDict = new();

    [Params(10000)]  // Just test with 10K records
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
                ScreenName = $"Screen_{i}",
                StatusId = 1,
                CreatedDt = DateTime.UtcNow,
                CreatedBy = 1000,
                UpdatedDt = DateTime.UtcNow,
                UpdatedBy = 1000
            });
            _screenDict[i] = $"Screen_{i}";
        }
        await context.ScreenDefinitions.AddRangeAsync(screens);
        await context.SaveChangesAsync();

        // Seed screen pilots with realistic distribution
        // 1000 unique users, each with 10-20 screen assignments
        var pilots = new List<ScreenPilot>();
        var random = new Random(42);
        int uniqueUsers = Math.Min(1000, RecordCount / 10);
        long pilotGk = 1;

        for (int userId = 0; userId < uniqueUsers; userId++)
        {
            int assignmentsPerUser = random.Next(10, 21);
            for (int assignment = 0; assignment < assignmentsPerUser; assignment++)
            {
                if (pilots.Count >= RecordCount) break;

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
            if (pilots.Count >= RecordCount) break;
        }

        await context.ScreenPilots.AddRangeAsync(pilots);
        await context.SaveChangesAsync();
        
    // Initialize repositories after seeding
    var contextProvider = new Benchmarks.Base.BenchmarkDbContextProvider(context);
    _pilotRepository = new ScreenPilotRepository(contextProvider);
    _screenRepository = new ScreenDefinitionRepository(contextProvider);
    }

    [Benchmark(Baseline = true, Description = "Traditional: Load all → Group → Paginate")]
    public async Task<List<PilotEnablementDto>> TraditionalPagination()
    {
        // ❌ OLD WAY: Load all records into memory, then paginate
        var allPilots = await Context!.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => sp.StatusId == 1)
            .ToListAsync();

        // Group in memory
        var grouped = allPilots
            .GroupBy(p => p.NbUserGk)
            .OrderBy(g => g.Key)
            .Skip(Skip)
            .Take(Take)
            .Select(g => new PilotEnablementDto
            {
                NbUserGk = g.Key,
                UserName = g.Key.ToString(),
                ScreenAssignments = g.Select(pilot => new ScreenDefnPilotUserDto
                {
                    ScreenPilotGk = pilot.ScreenPilotGk,
                    ScreenGk = pilot.ScreenGk,
                    ScreenName = _screenDict.GetValueOrDefault((int)pilot.ScreenGk, "Unknown"),
                    StatusId = pilot.StatusId
                }).ToList()
            })
            .ToList();

        return grouped;
    }

    [Benchmark(Description = "Optimized: Database-level pagination (PaginatedGroupQuery)")]
    public async Task<List<PilotEnablementDto>> OptimizedPagination()
    {
        // ✅ NEW WAY: Two-phase database-level pagination
        var query = _pilotRepository!
            .GetAllQueryable(statusId: 1)
            .GroupByPaginated(
                groupKeySelector: p => p.NbUserGk,
                resultSelector: g => new PilotEnablementDto
                {
                    NbUserGk = g.Key,
                    UserName = g.Key.ToString(),
                    ScreenAssignments = g.Select(pilot => new ScreenDefnPilotUserDto
                    {
                        ScreenPilotGk = pilot.ScreenPilotGk,
                        ScreenGk = pilot.ScreenGk,
                        ScreenName = _screenDict.GetValueOrDefault((int)pilot.ScreenGk, "Unknown"),
                        StatusId = pilot.StatusId,
                        DualMode = pilot.DualMode,
                        AssignedBy = pilot.CreatedBy
                    }).ToList()
                })
            .OrderGroupKeysBy(keys => keys.OrderBy(userId => userId))
            .Paginate(PageNumber, PageSize);

        return await query.ExecuteAsync(CancellationToken.None);
    }

    [Benchmark(Description = "Hybrid: Pre-fetch keys → Load data")]
    public async Task<List<PilotEnablementDto>> HybridPagination()
    {
        // Alternative approach: Get paginated userIds first, then load their data
        var userIds = await Context!.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.StatusId == 1)
            .Select(sp => sp.NbUserGk)
            .Distinct()
            .OrderBy(userId => userId)
            .Skip(Skip)
            .Take(Take)
            .ToListAsync();

        // Load data only for those users
        var pilots = await Context!.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => userIds.Contains(sp.NbUserGk) && sp.StatusId == 1)
            .ToListAsync();

        var result = pilots
            .GroupBy(p => p.NbUserGk)
            .Select(g => new PilotEnablementDto
            {
                NbUserGk = g.Key,
                UserName = g.Key.ToString(),
                ScreenAssignments = g.Select(pilot => new ScreenDefnPilotUserDto
                {
                    ScreenPilotGk = pilot.ScreenPilotGk,
                    ScreenGk = pilot.ScreenGk,
                    ScreenName = _screenDict.GetValueOrDefault((int)pilot.ScreenGk, "Unknown"),
                    StatusId = pilot.StatusId,
                    DualMode = pilot.DualMode,
                    AssignedBy = pilot.CreatedBy
                }).ToList()
            })
            .ToList();

        return result;
    }
}
