using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Repositories;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Benchmarks.Base;

/// <summary>
/// Base class for pagination benchmarks
/// Compares different pagination strategies
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public abstract class BasePaginationBenchmark : BaseRepositoryBenchmark
{
    [Params(10)]  // Just test with page size 10
    public int PageSize { get; set; }

    [Params(1)]  // Just test page 1
    public int PageNumber { get; set; }

    protected int Skip => (PageNumber - 1) * PageSize;
    protected int Take => PageSize;
}
