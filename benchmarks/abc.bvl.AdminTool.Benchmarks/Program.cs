using BenchmarkDotNet.Running;

namespace abc.bvl.AdminTool.Benchmarks;

/// <summary>
/// Entry point for running benchmarks
/// Usage:
///   dotnet run -c Release -- --filter *RepositoryBenchmarks*
///   dotnet run -c Release -- --filter *PaginationBenchmarks*
///   dotnet run -c Release -- --list flat
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks or specific ones based on command-line filters
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
