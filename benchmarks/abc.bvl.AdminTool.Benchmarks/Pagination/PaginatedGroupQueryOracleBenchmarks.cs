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
/// Real Oracle database benchmarks for pagination strategies
/// Compares three approaches with actual database I/O, network latency, and query optimization
/// 
/// SETUP REQUIRED:
/// 1. Set environment variable: BENCHMARK_ORACLE_CONNECTION
///    Example: "Data Source=localhost:1521/XE;User Id=ADMINTOOL;Password=your_password;"
/// 2. Ensure database is accessible and user has CREATE TABLE permissions
/// 3. Run: dotnet run -c Release --filter *OracleBenchmarks*
/// </summary>
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class PaginatedGroupQueryOracleBenchmarks : BaseOracleBenchmark
{
    private ScreenPilotRepository? _pilotRepository;
    private ScreenDefinitionRepository? _screenRepository;

    [Params(10000)]
    public int RecordCount { get; set; }

    [Params(10)]
    public int PageSize { get; set; }

    [Params(1)]
    public int PageNumber { get; set; }

    protected override int RecordCountToSeed => RecordCount;

    protected override async Task SeedDataAsync(AdminDbContext context)
    {
        // Use raw SQL to avoid EF Core INSERT complexity with Oracle
        var now = DateTime.UtcNow.AddDays(-30);
        
        // Use high ID numbers and unique codes to avoid conflicts
        var startId = 1000000;
        var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);  // Use first 8 chars of GUID
        
        // Insert 100 screen definitions using raw SQL
        for (int i = 1; i <= 100; i++)
        {
            await context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO APP_USER.ADMIN_SCREENDEFN 
                  (SCREENDEFNID, SCREENCODE, SCREENNAME, STATUS, DISPLAYORDER,
                   CREATEDAT, CREATEDBY, UPDATEDAT, UPDATEDBY, ROWVERSION) 
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
                startId + i, $"BM{uniqueSuffix}{i:D3}", $"Screen_{uniqueSuffix}_{i:D3}", 1, i,
                now, "benchmark-setup", now, "benchmark-setup", $"v{uniqueSuffix}_{i}");
        }

        // Insert RecordCount screen pilots using raw SQL (batch inserts)
        var userCount = RecordCount / 10;
        var pilotNow = DateTime.UtcNow.AddDays(-10);
        var pilotStartId = 2000000;
        
        Console.WriteLine($"🔄 Seeding {RecordCount} pilot assignments...");
        for (int i = 0; i < RecordCount; i++)
        {
            await context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO APP_USER.ADMIN_SCREENPILOT 
                  (SCREENPILOTID, SCREENDEFNID, USERID, STATUS,
                   CREATEDAT, CREATEDBY, UPDATEDAT, UPDATEDBY, ROWVERSION) 
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})",
                pilotStartId + i + 1, startId + ((i % 100) + 1), $"user_{(i % userCount):D5}", 1,
                pilotNow, "benchmark-setup", pilotNow, "benchmark-setup", $"v{uniqueSuffix}_{i}");
            
            if ((i + 1) % 1000 == 0)
                Console.WriteLine($"   Progress: {i + 1}/{RecordCount} records inserted");
        }

        // Initialize repositories after seeding
        _pilotRepository = new ScreenPilotRepository(Context);
        _screenRepository = new ScreenDefinitionRepository(Context);

        Console.WriteLine($"✅ Seeded 100 screens and {RecordCount} pilot assignments for {userCount} users");
    }

    [Benchmark(Baseline = true, Description = "Traditional: Load all → Group → Paginate")]
    public async Task<List<PilotEnablementDto>> TraditionalPagination()
    {
        // APPROACH 1: Traditional - Load ALL records into memory, then paginate
        // This is what most developers do by default - very inefficient!
        
        var allPilots = await Context!.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => sp.StatusId == 1)
            .ToListAsync(); // ⚠️ Loads ENTIRE table (RecordCount records + network transfer)

        var groupedByUser = allPilots
            .GroupBy(p => p.NbUserGk)
            .OrderBy(g => g.Key)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize);

        var results = groupedByUser.Select(userGroup => new PilotEnablementDto
        {
            NbUserGk = userGroup.Key,
            ScreenAssignments = userGroup.Select(p => new ScreenDefnPilotUserDto
            {
                ScreenPilotGk = p.ScreenPilotGk,
                ScreenGk = p.ScreenGk,
                ScreenName = p.ScreenDefinition?.ScreenName ?? "Unknown",
                StatusId = p.StatusId,
                DualMode = p.DualMode,
                AssignedBy = p.CreatedBy
            }).ToList()
        }).ToList();

        return results;
    }

    [Benchmark(Description = "Optimized: Database-level pagination (PaginatedGroupQuery)")]
    public async Task<List<PilotEnablementDto>> OptimizedPagination()
    {
        // APPROACH 2: PaginatedGroupQuery - Two-phase database-level pagination
        // Phase 1: Get paginated user IDs only (~10-100 IDs)
        // Phase 2: Load data ONLY for those users (~10-1000 records)
        
        // First get all screens for lookup
        var screens = await Context!.ScreenDefinitions.AsNoTracking().ToDictionaryAsync(s => s.ScreenGk, s => s);
        
        // Then use paginated query on pilots
        return await Context!.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.StatusId == 1)
            .GroupByPaginated(
                groupKeySelector: p => p.NbUserGk,
                resultSelector: userGroup => new PilotEnablementDto
                {
                    NbUserGk = userGroup.Key,
                    ScreenAssignments = userGroup.Select(p => new ScreenDefnPilotUserDto
                    {
                        ScreenPilotGk = p.ScreenPilotGk,
                        ScreenGk = p.ScreenGk,
                        ScreenName = screens.TryGetValue(p.ScreenGk, out var scr) ? scr.ScreenName : "Unknown",
                        StatusId = p.StatusId,
                        DualMode = p.DualMode,
                        AssignedBy = p.CreatedBy
                    }).ToList()
                })
            .OrderGroupKeysBy(keys => keys.OrderBy(userId => userId))
            .Paginate(PageNumber, PageSize)
            .ExecuteAsync(CancellationToken.None);
    }

    [Benchmark(Description = "Hybrid: Pre-fetch keys → Load data")]
    public async Task<List<PilotEnablementDto>> HybridPagination()
    {
        // APPROACH 3: Hybrid - Pre-fetch paginated keys, then load related data
        // This is a middle ground between traditional and optimized
        
        var skip = (PageNumber - 1) * PageSize;
        var take = PageSize;

        // Query 1: Get paginated user IDs only
        var userIds = await Context!.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.StatusId == 1)
            .Select(sp => sp.NbUserGk)
            .Distinct()
            .OrderBy(userId => userId)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Query 2: Load pilots for those users only
        var pilots = await Context!.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => userIds.Contains(sp.NbUserGk) && sp.StatusId == 1)
            .ToListAsync();

        var results = pilots
            .GroupBy(p => p.NbUserGk)
            .Select(userGroup => new PilotEnablementDto
            {
                NbUserGk = userGroup.Key,
                ScreenAssignments = userGroup.Select(p => new ScreenDefnPilotUserDto
                {
                    ScreenPilotGk = p.ScreenPilotGk,
                    ScreenGk = p.ScreenGk,
                    ScreenName = p.ScreenDefinition?.ScreenName ?? "Unknown",
                    StatusId = p.StatusId,
                    DualMode = p.DualMode,
                    AssignedBy = p.CreatedBy
                }).ToList()
            })
            .ToList();

        return results;
    }
}
